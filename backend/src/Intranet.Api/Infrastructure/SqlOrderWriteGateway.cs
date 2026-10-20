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
        if(!options.Value.EnableOrderRegistration || !options.Value.WriteWindowsUsers.Contains(windowsUser,StringComparer.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("El usuario no tiene habilitado el registro de órdenes.");
        var connection=await factory.OpenAsync(ct);
        try
        {
            const string permission="""
                SELECT CASE WHEN (ORIGINAL_LOGIN()=@WindowsUser AND IS_SRVROLEMEMBER('sysadmin')=1)
                    OR EXISTS(SELECT 1 FROM dbo.LG_SEGORDCOMP WHERE Cod_Usuario=@GestionUser AND Cod_ClaOrdComp='01' AND Tip_Act='A')
                    THEN 1 ELSE 0 END
                """;
            if(await connection.ExecuteScalarAsync<int>(new CommandDefinition(permission,new{GestionUser=gestionUser,WindowsUser=windowsUser},cancellationToken:ct))!=1)
                throw new UnauthorizedAccessException("No hay permiso administrativo existente de clase 01.");
            var transaction=(SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted,ct);
            return new Session(connection,transaction,options.Value.CommandTimeoutSeconds);
        }
        catch { await connection.DisposeAsync(); throw; }
    }
    private sealed class Session(SqlConnection connection, SqlTransaction transaction, int timeout) : IOrderWriteSession
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
        public async Task LinkGroup(OrderReference order, string group, CancellationToken ct)=>await Execute("dbo.LG_MAN_ORDCOMP_GRUPOS",new DynamicParameters(new{ACCION="I",SER_ORDCOMP=order.Series,COD_ORDCOMP=order.Number,COD_GRUPO=group}),ct);
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
