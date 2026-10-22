using System.Data;
using Dapper;
using Intranet.Api.Contracts;
using Intranet.Api.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Intranet.Api.Infrastructure;

public sealed class SqlOrderWriteGateway(GestionConnectionFactory factory, IOptions<GestionOptions> options) : IOrderWriteGateway
{
    public async Task<IOrderWriteSession> Begin(string gestionUser, string windowsUser, CancellationToken ct)
    {
        if(!options.Value.EnableOrderRegistration || (!options.Value.AllowAllAuthenticatedWrites && !options.Value.WriteWindowsUsers.Contains(windowsUser,StringComparer.OrdinalIgnoreCase)) || string.IsNullOrWhiteSpace(gestionUser) || string.IsNullOrWhiteSpace(windowsUser))
            throw new UnauthorizedAccessException("El usuario no tiene habilitado el registro de órdenes.");
        var connection=await factory.OpenAsync(ct);
        try
        {
            const string permission="""
                SELECT CASE WHEN (ORIGINAL_LOGIN()=@WindowsUser AND IS_SRVROLEMEMBER('sysadmin')=1)
                    OR EXISTS(SELECT 1 FROM dbo.LG_SEGORDCOMP WHERE Cod_Usuario=@GestionUser AND Cod_ClaOrdComp='01' AND Tip_Act='A')
                    THEN 1 ELSE 0 END
                """;
            if(!options.Value.AllowAllAuthenticatedWrites && await connection.ExecuteScalarAsync<int>(new CommandDefinition(permission,new{GestionUser=gestionUser,WindowsUser=windowsUser},cancellationToken:ct))!=1)
                throw new UnauthorizedAccessException("No hay permiso administrativo existente de clase 01.");
            var transaction=(SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted,ct);
            return new Session(connection,transaction,options.Value.CommandTimeoutSeconds,gestionUser);
        }
        catch { await connection.DisposeAsync(); throw; }
    }
    private sealed class Session(SqlConnection connection, SqlTransaction transaction, int timeout,string gestionUser) : IOrderWriteSession
    {
        private bool committed;
        private CommandDefinition Procedure(string name, object parameters, CancellationToken ct)=>new(name,parameters,transaction,commandType:CommandType.StoredProcedure,commandTimeout:timeout,cancellationToken:ct);
        private async Task Execute(string name, DynamicParameters parameters, CancellationToken ct)
        {
            parameters.Add("ReturnValue",dbType:DbType.Int32,direction:ParameterDirection.ReturnValue);
            await connection.ExecuteAsync(Procedure(name,parameters,ct));
            if(parameters.Get<int>("ReturnValue")<0) throw new RegistrationConflictException("Gestión rechazó la operación. No se guardó el detalle.");
        }
        public async Task<OrderReference> CreateHeader(CreateOrderRequest request, CancellationToken ct)
        {
            // The existing procedure allocates the series/number. Never calculate MAX+1 in the API.
            // Lock its sequence row before invoking it; its own select does not use UPDLOCK.
            await connection.ExecuteAsync(new CommandDefinition("SELECT n.ULT_ORDCOMP FROM dbo.LG_NUMORDCOMP n WITH (UPDLOCK,HOLDLOCK) INNER JOIN dbo.LG_CLAORDCOMP c ON c.Tip_Item=n.Tip_Item AND c.Tip_Presentacion=n.Tip_Presentacion WHERE c.Cod_ClaOrdComp='01'",transaction:transaction,cancellationToken:ct));
            var p=RegistrationParameters.Header(request);
            var rows=await connection.QueryAsync(Procedure("dbo.UP_MAN_ORDCOMP",p,ct));
            if(p.Get<int>("ReturnValue")<0) throw new RegistrationConflictException("Gestión rechazó la creación de la OC.");
            foreach(var raw in rows)
            {
                var row=(IDictionary<string,object>)raw;
                var series=RegistrationQueries.Text(row,"SER_ORDCOMP");
                var number=RegistrationQueries.Text(row,"COD_ORDCOMP");
                if(series.Length==3 && number.Length==6)return new(series,number);
            }
            throw new RegistrationConflictException("El procedimiento no devolvió una referencia válida de OC.");
        }
        public async Task LinkGroup(OrderReference order,string group,CancellationToken ct)
        {
            if(await connection.ExecuteScalarAsync<int>(new CommandDefinition("SELECT COUNT(*) FROM dbo.ES_GRUPOLOG WHERE Cod_GrupoLog=@Group",new{Group=group},transaction,cancellationToken:ct))!=1)throw new ArgumentException("El grupo de manufactura no existe.");
            if(await connection.ExecuteScalarAsync<int>(new CommandDefinition("SELECT COUNT(*) FROM dbo.LG_OrdComp_Grupos WITH (UPDLOCK,HOLDLOCK) WHERE Ser_OrdComp=@Series AND Cod_OrdComp=@Number AND Cod_Grupo=@Group",new{order.Series,order.Number,Group=group},transaction,cancellationToken:ct))>0)return;
            await Execute("dbo.LG_MAN_ORDCOMP_GRUPOS",new DynamicParameters(new{ACCION="I",SER_ORDCOMP=order.Series,COD_ORDCOMP=order.Number,COD_GRUPO=group}),ct);
        }
        public async Task RemoveRequirements(OrderReference order,string sequence,IReadOnlyList<string> keys,CancellationToken ct)
        {
            var received=await connection.QuerySingleOrDefaultAsync<decimal?>(new CommandDefinition("SELECT Can_Recibida FROM dbo.LG_ORDCOMPITEM WITH(UPDLOCK,HOLDLOCK) WHERE Ser_OrdComp=@Series AND Cod_OrdComp=@Number AND Sec_OrdComp=@Sequence",new{order.Series,order.Number,Sequence=sequence},transaction,cancellationToken:ct));
            if(received is null)throw new RegistrationConflictException("La secuencia ya no existe.");
            if(received>0)throw new RegistrationConflictException("La secuencia tiene cantidades recibidas; no se pueden retirar requerimientos.");
            var rows=await OrderRequirementLinks.Read(connection,transaction,order,sequence,ct);
            var selected=new List<IDictionary<string,object>>();
            foreach(var key in keys){var matches=rows.Where(r=>OrderRequirementLinks.Key(r)==key).ToList();if(matches.Count!=1)throw new RegistrationConflictException("Los requerimientos cambiaron. Actualiza la lista antes de retirar.");selected.Add(matches[0]);}
            foreach(var row in selected){
                var p=new DynamicParameters(new{Ser_OrdComp=order.Series,Cod_OrdComp=order.Number,Sec_OrdComp=sequence,FLG_TRANSACCION="N"});
                foreach(var field in OrderRequirementLinks.Fields)p.Add(field,field=="Can_Requerida"?RegistrationQueries.Number(row,field):(object)RegistrationQueries.Text(row,field));
                await Execute("dbo.UP_MAN_ORDCOMPITEMREQ",p,ct);
            }
        }
        public async Task ChangeItem(OrderReference order,string sequence,UpdateOrderItemRequest? request,CancellationToken ct)
        {
            var raw=await connection.QuerySingleOrDefaultAsync(new CommandDefinition("SELECT * FROM dbo.LG_ORDCOMPITEM WITH(UPDLOCK,HOLDLOCK) WHERE Ser_OrdComp=@Series AND Cod_OrdComp=@Number AND Sec_OrdComp=@Sequence",new{order.Series,order.Number,Sequence=sequence},transaction,cancellationToken:ct));
            if(raw is null)throw new RegistrationConflictException("La secuencia ya no existe. Actualiza el detalle.");
            var row=(IDictionary<string,object>)raw;string T(string key)=>RegistrationQueries.Text(row,key);decimal N(string key)=>RegistrationQueries.Number(row,key);
            if(N("Can_Recibida")>0)throw new RegistrationConflictException("La secuencia tiene cantidades recibidas; no puede modificarse ni eliminarse.");
            if(request is null && await connection.ExecuteScalarAsync<int>(new CommandDefinition("SELECT COUNT(*) FROM dbo.LG_ORDCOMPITEMREQ WITH(UPDLOCK,HOLDLOCK) WHERE Ser_OrdComp=@Series AND Cod_OrdComp=@Number AND Sec_OrdComp=@Sequence",new{order.Series,order.Number,Sequence=sequence},transaction,cancellationToken:ct))>0)
                throw new RegistrationConflictException("No se puede eliminar la secuencia porque tiene requerimientos asociados. Gestión también bloquea esta eliminación. Revisa sus requerimientos antes de intentar eliminarla.");
            var p=new DynamicParameters(new{Opc_Mant=request is null?"D":"U",Ser_OrdComp=order.Series,Cod_OrdComp=order.Number,Sec_OrdComp=sequence,
                Cod_Item=T("Cod_Item"),Cod_Comb=T("Cod_Comb"),Cod_Color=T("Cod_Color"),Cod_Talla=T("Cod_Talla"),Cod_Destino=T("Cod_Destino"),Cod_EstCli=T("Cod_EstCli"),
                Cod_Descuento=request?.DiscountCode??T("Cod_Descuento"),Porc_IGV=request?.TaxPercent??N("Porc_IGV"),
                Fec_Entrega_Inicio=request?.DeliveryStart??(DateTime)row["Fec_Entrega_Inicio"],Fec_Entrega_Fin=request?.DeliveryEnd??(DateTime)row["Fec_Entrega_Fin"],
                Pre_Unitario=request?.UnitPrice??N("Pre_Unitario"),Can_Comprada=request?.Purchased??N("Can_Comprada"),Cod_ItemProv=T("Cod_ItemProv"),Observaciones=request?.Observations??T("Observaciones"),
                Repetir_Precio="N",Cod_Prov=T("Cod_Prov"),INVOCADO_DIRECTO="N",Cod_Hilado=T("Cod_Hilado"),Cod_Fabrica=T("Cod_Fabrica"),Cod_Ordpro=T("Cod_OrdPro"),
                flg_incluido_precio_cotizacion=T("flg_incluido_precio_cotizacion"),cod_usuario=gestionUser,flg_incluye_igv=T("flg_incluye_igv"),Repetir_Descuento="N",
                Cod_Item_Aplicado=T("cod_item_aplicado"),Cod_Comb_Aplicado=T("cod_comb_aplicado"),Cod_Area_Produccion=T("Cod_Area_Produccion"),Cod_Equipo=T("Cod_Equipo"),Cod_CentroCosto=T("Cod_CentroCosto"),cod_unimed_proveedor=T("cod_unimed_proveedor")});
            await Execute("dbo.UP_MAN_ORDCOMPITEMN",p,ct);
        }
        public async Task UpdateHeader(OrderReference order,UpdateOrderRequest request,CancellationToken ct)
        {
            var raw=await connection.QuerySingleAsync(new CommandDefinition("SELECT * FROM dbo.LG_ORDCOMP WHERE Ser_OrdComp=@Series AND Cod_OrdComp=@Number",order,transaction,cancellationToken:ct));
            var row=(IDictionary<string,object>)raw;
            string T(string key)=>RegistrationQueries.Text(row,key);
            var p=RegistrationParameters.Header(new CreateOrderRequest{SupplierCode=request.SupplierCode,ConditionCode=request.ConditionCode,DiscountCode=request.DiscountCode,CurrencyCode=request.CurrencyCode,DeliveryLocationCode=request.DeliveryLocationCode,CostCenterCode=request.CostCenterCode,TaxPercent=request.TaxPercent,DeliveryStart=request.DeliveryStart,DeliveryEnd=request.DeliveryEnd,Observations=request.Observations,ManufacturingGroup=T("COD_GRUPO")});
            p.Add("OPC_MANT","U");p.Add("SER_ORDCOMP",order.Series);p.Add("COD_ORDCOMP",order.Number);
            p.Add("COD_STAORDCOMP",T("COD_STAORDCOMP"));p.Add("COD_CLAORDCOMP",T("COD_CLAORDCOMP"));
            foreach(var key in new[]{"COD_PROTEX","FLG_TOTAL_PARCIAL","COD_CLIENTE","COD_PURORD","COD_RESPONSABLE","COD_AREA_PRODUCCION","COD_EQUIPO","COD_TEMCLI","NUM_ENSAYO","COD_GRUPO_DIVERSO"})p.Add(key,T(key));
            p.Add("MOTSOLICITUD",T("MOTIVO_SOLICITUD"));
            foreach(var key in new[]{"NUM_SOLICITUD","NUM_REQUERIMIENTO"})p.Add(key,RegistrationQueries.Number(row,key));
            p.Add("ID",RegistrationQueries.Number(row,"ID_OBRA"));p.Add("flg_maquina_en_detalle",row.TryGetValue("flg_maquina_en_detalle",out var flag)&&flag is not null and not DBNull&&Convert.ToBoolean(flag));p.Add("NUM_ENSAYO",T("NumEnsayo"));
            p.Add("Cod_Cliente_Tex",T("Cod_Cliente_Tex"));p.Add("Orden_Cliente_Aso",T("Orden_Cliente_Aso"));
            await connection.ExecuteAsync(Procedure("dbo.UP_MAN_ORDCOMP",p,ct));
            if(p.Get<int>("ReturnValue")<0)throw new RegistrationConflictException("Gestión rechazó la actualización de la OC.");
        }
        public Task RemoveGroup(OrderReference order,string group,CancellationToken ct)=>Execute("dbo.LG_MAN_ORDCOMP_GRUPOS",new DynamicParameters(new{ACCION="D",SER_ORDCOMP=order.Series,COD_ORDCOMP=order.Number,COD_GRUPO=group}),ct);
        public Task<OrderRegistrationHeader> LockOrder(OrderReference order,CancellationToken ct)=>RegistrationQueries.Header(connection,order,transaction,true,ct);
        public async Task<IReadOnlyList<PendingRequirement>> Pending(OrderRegistrationHeader order,string op,string item,CancellationToken ct)
        {
            // Lock the demand as well as the OC: another order must not consume this
            // same OP/item between refreshing the balance and inserting the detail.
            await connection.ExecuteAsync(new CommandDefinition("SELECT Cod_OrdPro FROM dbo.ES_ORDPROREQ_ITEMS WITH (UPDLOCK,HOLDLOCK) WHERE Cod_OrdPro=@Op AND Cod_Item=@Item",new {Op=op,Item=item},transaction,commandTimeout:timeout,cancellationToken:ct));
            return await RegistrationQueries.Pending(connection,order,op,item,transaction,timeout,ct);
        }
        public async Task<IReadOnlyList<SavedItem>> Items(OrderReference order,CancellationToken ct)
        {
            var rows=await connection.QueryAsync(new CommandDefinition("SELECT Sec_OrdComp,Can_Requerida,Pre_Unitario FROM dbo.LG_ORDCOMPITEM WITH (UPDLOCK,HOLDLOCK) WHERE Ser_OrdComp=@Series AND Cod_OrdComp=@Number",order,transaction,cancellationToken:ct));
            return rows.Select(raw=>{var row=(IDictionary<string,object>)raw;return new SavedItem(RegistrationQueries.Text(row,"Sec_OrdComp"),RegistrationQueries.Number(row,"Can_Requerida"),RegistrationQueries.Number(row,"Pre_Unitario"));}).ToList();
        }
        public Task InsertRequirement(OrderReference order,string previousSequence,PendingRequirement requirement,CancellationToken ct)=>Execute("dbo.UP_ACTUALIZA_REQ_OC",RegistrationParameters.Detail(order,previousSequence,requirement),ct);
        public Task SetPurchase(OrderReference order,string sequence,decimal quantity,decimal unitPrice,CancellationToken ct)=>Execute("dbo.UP_MAN_ORDCOMPITEM_CANTIDAD_COMPRADA",RegistrationParameters.Purchase(order,sequence,quantity,unitPrice),ct);
        public async Task Commit(CancellationToken ct){await transaction.CommitAsync(ct);committed=true;}
        public async ValueTask DisposeAsync()
        {
            try{if(!committed && transaction.Connection is not null)await transaction.RollbackAsync(CancellationToken.None);}
            finally{await transaction.DisposeAsync();await connection.DisposeAsync();}
        }
    }
}

public static class RegistrationParameters
{
    private static DynamicParameters Named(object values)
    {
        var parameters=new DynamicParameters();
        foreach(var property in values.GetType().GetProperties()) parameters.Add(property.Name,property.GetValue(values));
        return parameters;
    }
    public static DynamicParameters Header(CreateOrderRequest r)
    {
        var p=Named(new {
            OPC_MANT="I",SER_ORDCOMP="",COD_ORDCOMP="",COD_PROVEEDOR=r.SupplierCode.Trim(),COD_CONDVENT=r.ConditionCode.Trim(),COD_DESCUENTO=r.DiscountCode.Trim(),
            PORC_IGV=r.TaxPercent,COD_MONEDA=r.CurrencyCode.Trim(),COD_LUGENTR=r.DeliveryLocationCode.Trim(),OBSERVACIONES=r.Observations.Trim(),
            COD_STAORDCOMP="P",COD_CLAORDCOMP="01",FEC_ENTREGA_INICIO=r.DeliveryStart.Date,FEC_ENTREGA_FIN=r.DeliveryEnd.Date,COD_CENCOST=r.CostCenterCode.Trim(),
            COD_GRUPO=r.ManufacturingGroup.Trim(),COD_PROTEX="",FLG_TOTAL_PARCIAL="T",FLG_TRANSACCION="N"
        });
        p.Add("ReturnValue",dbType:DbType.Int32,direction:ParameterDirection.ReturnValue);
        return p;
    }
    public static DynamicParameters Detail(OrderReference order,string previous,PendingRequirement r)=>Named(new {
        SER_ORDCOMP=order.Series,COD_ORDCOMP=order.Number,ULT_SECORDCOMP=previous,ACCION="C",COD_FABRICA=r.Factory,COD_ORDPRO=r.ProductionOrder,COD_PRESENT=r.PresentationCode,
        COD_COMPEST=r.Component,COD_ITEM=r.ItemCode,COD_COMB=r.Combination,COD_COLOR=r.Color,COD_TALLA=r.Size,COD_DESTINO=r.Destination,COD_ESTCLI=r.Style,
        CANTIDAD=r.Required,COD_PROV=r.ProviderReference,FLG_TRANSACCION="N"
    });
    public static DynamicParameters Purchase(OrderReference order,string sequence,decimal quantity,decimal unitPrice)=>Named(new {
        Ser_OrdComp=order.Series,Cod_OrdComp=order.Number,Sec_OrdComp=sequence,Can_Comprada=quantity,pre_unitario=unitPrice
    });
}

