using Dapper;
using Intranet.Api.Contracts;
using Intranet.Api.Services;
using Microsoft.Extensions.Options;

namespace Intranet.Api.Infrastructure;

public sealed class RegistrationReadRepository(GestionConnectionFactory factory,IOptions<GestionOptions> options)
{
    private static IReadOnlyList<LookupOption> Lookup(IEnumerable<dynamic> rows)=>rows.Select(raw=>{var r=(IDictionary<string,object>)raw;return new LookupOption(RegistrationQueries.Text(r,"Code"),RegistrationQueries.Text(r,"Description"));}).ToList();
    public async Task<object> List(string user,string? windows,int page,string search,CancellationToken ct)
    {
        await using var connection=await factory.OpenAsync(ct);
        var term=search.Trim();
        var numeric=term.Length>0 && term.All(char.IsAsciiDigit);
        var reference=System.Text.RegularExpressions.Regex.IsMatch(term,@"^\d{3}-\d{6}$");
        var predicate=term.Length==0 ? "1=1" : reference ? "o.Ser_OrdComp=@Series AND o.Cod_OrdComp=@Number" : numeric ? (term.Length==6 ? "o.Cod_OrdComp=@Search" : "o.Cod_OrdComp LIKE @Prefix") : "p.Des_Proveedor LIKE @Pattern";
        // Read-all access is granted by the server-configured application identity.
        var readAll=windows is not null && options.Value.ReadAllWindowsUsers.Contains(windows,StringComparer.OrdinalIgnoreCase);
        var access=readAll ? "1=1" : "EXISTS(SELECT 1 FROM dbo.LG_SEGORDCOMP s WHERE s.Cod_Usuario=@User AND s.Cod_ClaOrdComp=o.Cod_ClaOrdComp)";
        var sql="""
          ;WITH orders AS (
            SELECT RTRIM(o.Ser_OrdComp) Series,RTRIM(o.Cod_OrdComp) Number,RTRIM(p.Des_Proveedor) Supplier,
              RTRIM(o.Cod_StaOrdComp) Status,RTRIM(o.Cod_ClaOrdComp) Class,o.Fec_Creacion CreatedAt,
              ROW_NUMBER() OVER(ORDER BY o.Fec_Creacion DESC,o.Ser_OrdComp DESC,o.Cod_OrdComp DESC) rn
            FROM dbo.LG_ORDCOMP o LEFT JOIN dbo.LG_PROVEEDOR p ON p.Cod_Proveedor=o.Cod_Proveedor
            WHERE {ACCESS}
            AND {FILTER}
          ) SELECT Series,Number,Supplier,Status,Class,CreatedAt FROM orders WHERE rn>@Offset AND rn<=@End ORDER BY rn OPTION (RECOMPILE);
          SELECT COUNT(*) FROM dbo.LG_ORDCOMP o LEFT JOIN dbo.LG_PROVEEDOR p ON p.Cod_Proveedor=o.Cod_Proveedor WHERE {ACCESS} AND {FILTER} OPTION (RECOMPILE);
          """;
        using var result=await connection.QueryMultipleAsync(new CommandDefinition(sql.Replace("{FILTER}",predicate).Replace("{ACCESS}",access),new{User=user,Windows=windows,Search=term,Prefix=term+"%",Pattern="%"+term+"%",Series=reference?term[..3]:"",Number=reference?term[4..]:"",Offset=(page-1)*50,End=page*50+1},commandTimeout:options.Value.CommandTimeoutSeconds,cancellationToken:ct));
        var rows=(await result.ReadAsync()).ToList();var totalRows=await result.ReadSingleAsync<int>();var totalPages=Math.Max(1,(int)Math.Ceiling(totalRows/50d));
        return new{rows=rows.Take(50).Select(x=>new{series=(string)x.Series,number=(string)x.Number,supplier=(string?)x.Supplier,status=(string)x.Status,@class=(string)x.Class,createdAt=(DateTime?)x.CreatedAt}),hasMore=page<totalPages,page,totalRows,totalPages};
    }
    public async Task<RegistrationCatalogs> Catalogs(CancellationToken ct)
    {
        await using var connection=await factory.OpenAsync(ct);
        const string sql="""
            SELECT Cod_CondVent Code,Des_CondVent Description FROM dbo.LG_CONDVENT WHERE ISNULL(FLG_ACTIVO,'S')<>'N' ORDER BY Des_CondVent;
            SELECT Cod_Descuento Code,CONVERT(varchar(30),CAST(ISNULL(Porcentaje1,0) AS decimal(18,2)))+' - '+CONVERT(varchar(30),CAST(ISNULL(Porcentaje2,0) AS decimal(18,2))) Description FROM dbo.LG_DSCTOS ORDER BY Cod_Descuento;
            SELECT Cod_Moneda Code,Nom_Moneda Description FROM dbo.TG_MONEDA WHERE ISNULL(Flg_Activa,'S')<>'N' ORDER BY Cod_Moneda;
            SELECT Cod_LugEntr Code,Des_LugEntr Description FROM dbo.LG_LUGENTR ORDER BY Des_LugEntr;
            SELECT Cod_CenCost Code,Des_CenCost Description FROM dbo.TG_CENCOSTO WHERE ISNULL(Flg_Activo,'S')<>'N' ORDER BY Des_CenCost;
            SELECT Cod_ClaOrdComp Code,Des_ClaOrdComp Description FROM dbo.LG_CLAORDCOMP ORDER BY Des_ClaOrdComp;
            """;
        using var results=await connection.QueryMultipleAsync(new CommandDefinition(sql,cancellationToken:ct));
        return new(Lookup(await results.ReadAsync()),Lookup(await results.ReadAsync()),Lookup(await results.ReadAsync()),Lookup(await results.ReadAsync()),Lookup(await results.ReadAsync()),Lookup(await results.ReadAsync()));
    }
    public async Task<IReadOnlyList<LookupOption>> DetailGroups(OrderReference order,CancellationToken ct)
    {
        await using var c=await factory.OpenAsync(ct);
        const string sql="SELECT DISTINCT RTRIM(g.Cod_GrupoLog) Code,RTRIM(g.Des_Grupo) Description FROM dbo.ES_GRUPOLOG g WHERE EXISTS(SELECT 1 FROM dbo.LG_OrdComp_Grupos x WHERE x.Ser_OrdComp=@Series AND x.Cod_OrdComp=@Number AND x.Cod_Grupo=g.Cod_GrupoLog) ORDER BY Code";
        return Lookup(await c.QueryAsync(new CommandDefinition(sql,order,cancellationToken:ct)));
    }
    public async Task<IReadOnlyList<LookupOption>> DetailOps(OrderReference order,string group,CancellationToken ct)
    {
        await using var c=await factory.OpenAsync(ct);
        const string sql="SELECT DISTINCT RTRIM(p.Cod_OrdPro) Code,RTRIM(ISNULL(e.Des_EstPro,'')) Description FROM dbo.ES_ORDPRO p LEFT JOIN dbo.ES_ESTPRO e ON e.Cod_EstPro=p.Cod_EstPro WHERE p.Cod_GrupoLog=@Group AND EXISTS(SELECT 1 FROM dbo.LG_OrdComp_Grupos g WHERE g.Ser_OrdComp=@Series AND g.Cod_OrdComp=@Number AND g.Cod_Grupo=@Group) ORDER BY Code";
        return Lookup(await c.QueryAsync(new CommandDefinition(sql,new{order.Series,order.Number,Group=group},cancellationToken:ct)));
    }
    public async Task<IReadOnlyList<LookupOption>> DetailItems(OrderReference order,string group,string op,CancellationToken ct)
    {
        if(!(await DetailOps(order,group,ct)).Any(x=>x.Code==op))throw new RegistrationConflictException("La OP no pertenece al grupo seleccionado de esta OC.");
        await using var c=await factory.OpenAsync(ct);var header=await RegistrationQueries.Header(c,order,null,false,ct);
        var rows=await c.QueryAsync(new CommandDefinition("dbo.UP_SEL_REQUEXCOMPRARLOG",new{COD_GRUPOLOG=group,COD_ORDPRO=op,COD_FAMITEM="",COD_PROVEEDOR=header.SupplierCode,COD_ITEM="",SER_ORDCOMP=order.Series,COD_ORDCOMP=order.Number,DES_PRESENTACION=""},commandType:System.Data.CommandType.StoredProcedure,commandTimeout:options.Value.CommandTimeoutSeconds,cancellationToken:ct));
        return rows.Select(raw=>(IDictionary<string,object>)raw).Where(r=>RegistrationQueries.Number(r,"CANTXCOMPRAR")>0&&RegistrationQueries.Text(r,"COD_ORDPRO")==op).Select(r=>new LookupOption(RegistrationQueries.Text(r,"COD_ITEM"),RegistrationQueries.Text(r,"ITEM"))).Where(x=>x.Code.Length>0).DistinctBy(x=>x.Code).OrderBy(x=>x.Code).ToList();
    }
    public async Task<object> LinkedRequirements(OrderReference order,string sequence,CancellationToken ct)
    {
        await using var c=await factory.OpenAsync(ct);var rows=await OrderRequirementLinks.Read(c,null,order,sequence,ct);
        return rows.Select(r=>new{key=OrderRequirementLinks.Key(r),factory=RegistrationQueries.Text(r,"Cod_Fabrica"),productionOrder=RegistrationQueries.Text(r,"Cod_OrdPro"),presentation=RegistrationQueries.Text(r,"Presentation"),itemCode=RegistrationQueries.Text(r,"Cod_Item"),size=RegistrationQueries.Text(r,"Cod_Talla"),quantity=RegistrationQueries.Number(r,"Can_Requerida")}).ToList();
    }
    public async Task<UpdateOrderItemRequest?> EditableItem(OrderReference order,string sequence,CancellationToken ct)
    {
        await using var c=await factory.OpenAsync(ct);
        return await c.QuerySingleOrDefaultAsync<UpdateOrderItemRequest>(new CommandDefinition("SELECT Can_Comprada Purchased,Pre_Unitario UnitPrice,RTRIM(Cod_Descuento) DiscountCode,Porc_IGV TaxPercent,Fec_Entrega_Inicio DeliveryStart,Fec_Entrega_Fin DeliveryEnd,ISNULL(CONVERT(varchar(2000),Observaciones),'') Observations FROM dbo.LG_ORDCOMPITEM WHERE Ser_OrdComp=@Series AND Cod_OrdComp=@Number AND Sec_OrdComp=@Sequence",new{order.Series,order.Number,Sequence=sequence},commandTimeout:options.Value.CommandTimeoutSeconds,cancellationToken:ct));
    }
    public async Task<object> EditableHeader(OrderReference order,CancellationToken ct)
    {
        await using var c=await factory.OpenAsync(ct);
        const string sql="""
            SELECT RTRIM(o.Cod_Proveedor) SupplierCode,RTRIM(o.Cod_CondVent) ConditionCode,RTRIM(o.Cod_Descuento) DiscountCode,
              RTRIM(o.Cod_Moneda) CurrencyCode,RTRIM(o.Cod_LugEntr) DeliveryLocationCode,RTRIM(o.Cod_CenCost) CostCenterCode,
              o.Porc_IGV TaxPercent,o.Fec_Entrega_Inicio DeliveryStart,o.Fec_Entrega_Fin DeliveryEnd,ISNULL(CONVERT(varchar(2000),o.Observaciones),'') Observations
            FROM dbo.LG_ORDCOMP o WHERE o.Ser_OrdComp=@Series AND o.Cod_OrdComp=@Number;
            SELECT RTRIM(p.Des_Proveedor) FROM dbo.LG_ORDCOMP o INNER JOIN dbo.LG_PROVEEDOR p ON p.Cod_Proveedor=o.Cod_Proveedor WHERE o.Ser_OrdComp=@Series AND o.Cod_OrdComp=@Number;
            """;
        using var result=await c.QueryMultipleAsync(new CommandDefinition(sql,order,commandTimeout:options.Value.CommandTimeoutSeconds,cancellationToken:ct));
        var header=await result.ReadSingleAsync<UpdateOrderRequest>();var supplierName=await result.ReadFirstOrDefaultAsync<string>();
        return new{header,supplierName};
    }
    public async Task<IReadOnlyList<LookupOption>> OrderGroups(OrderReference order,CancellationToken ct)
    {
        await using var c=await factory.OpenAsync(ct);
        var rows=await c.QueryAsync(new CommandDefinition("dbo.LG_MUESTRA_ORDCOMP_GRUPOS",new{SER_ORDCOMP=order.Series,COD_ORDCOMP=order.Number},commandType:System.Data.CommandType.StoredProcedure,commandTimeout:options.Value.CommandTimeoutSeconds,cancellationToken:ct));
        return rows.Select(raw=>new LookupOption(RegistrationQueries.Text((IDictionary<string,object>)raw,"Cod_Grupo"),"")).ToList();
    }
    public async Task<IReadOnlyList<LookupOption>> Factories(CancellationToken ct)
    {
        await using var c=await factory.OpenAsync(ct);
        return Lookup(await c.QueryAsync(new CommandDefinition("SELECT RTRIM(Abr_Fabrica) Code,RTRIM(Nom_Fabrica) Description FROM dbo.TG_FABRICA WHERE ISNULL(Abr_Fabrica,'')<>'' ORDER BY Abr_Fabrica",commandTimeout:options.Value.CommandTimeoutSeconds,cancellationToken:ct)));
    }
    public async Task<IReadOnlyList<LookupOption>> GroupsByOp(string factoryAbbreviation,string productionOrder,CancellationToken ct)
    {
        await using var c=await factory.OpenAsync(ct);
        const string sql="""
            SELECT DISTINCT RTRIM(g.Cod_GrupoLog) Code,RTRIM(g.Des_Grupo) Description
            FROM dbo.ES_GRUPOLOG g
            INNER JOIN dbo.ES_ORDPRO p ON p.Cod_GrupoLog=g.Cod_GrupoLog
            INNER JOIN dbo.TG_FABRICA f ON f.Cod_Fabrica=p.Cod_Fabrica
            WHERE f.Abr_Fabrica=@Factory AND p.Cod_OrdPro=@Op
            ORDER BY Code
            """;
        return Lookup(await c.QueryAsync(new CommandDefinition(sql,new{Factory=factoryAbbreviation,Op=productionOrder},commandTimeout:options.Value.CommandTimeoutSeconds,cancellationToken:ct)));
    }
    public async Task<SupplierDefaults?> SupplierDefaults(string code,CancellationToken ct)
    {
        await using var c=await factory.OpenAsync(ct);
        const string sql="""
            SELECT RTRIM(ISNULL(Cod_CondVent,'')) ConditionCode,RTRIM(ISNULL(Cod_Descuento,'')) DiscountCode,RTRIM(ISNULL(Origen,'')) Origin,
              CAST(CASE WHEN Origen='E' THEN 0 WHEN Origen='N' THEN 18 ELSE NULL END AS decimal(18,2)) TaxPercent
            FROM dbo.LG_PROVEEDOR WHERE Cod_Proveedor=@Code AND ISNULL(Flg_Activo,'S')<>'N'
            """;
        return await c.QuerySingleOrDefaultAsync<SupplierDefaults>(new CommandDefinition(sql,new{Code=code},commandTimeout:options.Value.CommandTimeoutSeconds,cancellationToken:ct));
    }
    public async Task<IReadOnlyList<LookupOption>> Search(string kind,string search,CancellationToken ct)
    {
        var sql=kind switch {
            "proveedores"=>"SELECT TOP 30 Cod_Proveedor Code,Des_Proveedor Description FROM dbo.LG_PROVEEDOR WHERE ISNULL(Flg_Activo,'S')<>'N' AND (Cod_Proveedor LIKE @Search OR Des_Proveedor LIKE @Search) ORDER BY Des_Proveedor",
            "grupos"=>"SELECT TOP 30 Cod_GrupoLog Code,Des_Grupo Description FROM dbo.ES_GRUPOLOG WHERE (Cod_GrupoLog LIKE @Search OR Des_Grupo LIKE @Search) AND EXISTS(SELECT 1 FROM dbo.ES_ORDPRO p WHERE p.Cod_GrupoLog=ES_GRUPOLOG.Cod_GrupoLog) ORDER BY Cod_GrupoLog DESC",
            _=>throw new ArgumentException("Catálogo no válido.")
        };
        await using var connection=await factory.OpenAsync(ct);
        return Lookup(await connection.QueryAsync(new CommandDefinition(sql,new{Search="%"+search.Trim()+"%"},cancellationToken:ct)));
    }
    public async Task<IReadOnlyList<PendingRequirement>> Pending(OrderReference order,string op,string item,CancellationToken ct)
    {
        await using var connection=await factory.OpenAsync(ct);
        var header=await RegistrationQueries.Header(connection,order,null,false,ct);
        if(header.Status!="P" || header.ClassCode!="01")throw new RegistrationConflictException("Agrega detalle únicamente a una OC de avíos Planeada.");
        return await RegistrationQueries.Pending(connection,header,op,item,null,options.Value.CommandTimeoutSeconds,ct);
    }
    public async Task<PrintOrder> Print(PurchaseOrder order,IReadOnlyList<OrderItem> items,CancellationToken ct)
    {
        await using var connection=await factory.OpenAsync(ct);
        const string sql="""
            SELECT o.Cod_Moneda,o.Cod_Grupo,o.Observaciones,o.Porc_IGV,o.Fec_Entrega_Inicio,o.Fec_Entrega_Fin,c.Des_CondVent,l.Des_LugEntr
            FROM dbo.LG_ORDCOMP o LEFT JOIN dbo.LG_CONDVENT c ON c.Cod_CondVent=o.Cod_CondVent LEFT JOIN dbo.LG_LUGENTR l ON l.Cod_LugEntr=o.Cod_LugEntr
            WHERE o.Ser_OrdComp=@Series AND o.Cod_OrdComp=@Number;
            SELECT i.Sec_OrdComp,i.Cod_Item,a.Des_Item,i.Cod_Talla,i.Can_Comprada,i.Pre_Unitario FROM dbo.LG_ORDCOMPITEM i LEFT JOIN dbo.LG_ITEM a ON a.Cod_Item=i.Cod_Item
            WHERE i.Ser_OrdComp=@Series AND i.Cod_OrdComp=@Number ORDER BY i.Sec_OrdComp;
            """;
        using var results=await connection.QueryMultipleAsync(new CommandDefinition(sql,new{order.Series,order.Number},cancellationToken:ct));
        var header=(IDictionary<string,object>)(await results.ReadSingleAsync());
        var lines=(await results.ReadAsync()).Select(raw=>{var r=(IDictionary<string,object>)raw;var qty=RegistrationQueries.Number(r,"Can_Comprada");var price=RegistrationQueries.Number(r,"Pre_Unitario");return new PrintLine(RegistrationQueries.Text(r,"Sec_OrdComp"),RegistrationQueries.Text(r,"Cod_Item"),RegistrationQueries.Text(r,"Des_Item"),RegistrationQueries.Text(r,"Cod_Talla"),qty,price,qty*price);}).ToList();
        DateTime? Date(string key)=>header.TryGetValue(key,out var value)&&value is DateTime d?d:null;
        var breakdown=await PrintBreakdown(connection,order,ct);
        var reportRaw=await connection.QueryAsync(new CommandDefinition("dbo.UP_SEL_ORDCOMPITEM",new{Tip_Item="I",Ser_OrdComp=order.Series,Cod_OrdComp=order.Number},commandType:System.Data.CommandType.StoredProcedure,commandTimeout:options.Value.CommandTimeoutSeconds,cancellationToken:ct));
        var reportRows=reportRaw.Select(raw=>{var d=(IDictionary<string,object>)raw;
            string T(params string[] keys)=>RegistrationQueries.Text(d,keys);
            decimal N(string key)=>RegistrationQueries.Number(d,key);
            string Pair(string code,string desc)=>string.Join(" - ",new[]{T(code),T(desc)}.Where(x=>x.Length>0));
            return new GestionPrintLine(T("ITEM"),Pair("cod_comb","des_comb"),Pair("cod_color","des_color"),T("medida"),T("cod_destino"),T("estilo_cli"),T("cod_unimed"),N("pre_unitario"),N("can_requerida"),N("can_comprada"),N("can_recibida"),N("Fac_Equiprov"),T("cod_itemprov"),T("cod_prov"),string.Join(", ",breakdown.Where(x=>x.Sequence==T("Sec_OrdComp")).Select(x=>x.ProductionOrder).Where(x=>x.Length>0).Distinct()));
        }).ToList();
        return new(order,items,RegistrationQueries.Text(header,"Cod_Moneda"),RegistrationQueries.Text(header,"Des_CondVent"),RegistrationQueries.Text(header,"Des_LugEntr"),RegistrationQueries.Text(header,"Cod_Grupo"),RegistrationQueries.Text(header,"Observaciones"),RegistrationQueries.Number(header,"Porc_IGV"),Date("Fec_Entrega_Inicio"),Date("Fec_Entrega_Fin"),lines,breakdown,reportRows);
    }
    private static async Task<IReadOnlyList<PrintBreakdown>> PrintBreakdown(Microsoft.Data.SqlClient.SqlConnection c,PurchaseOrder order,CancellationToken ct)
    {
        const string sql="""
          SELECT i.Sec_OrdComp Sequence,RTRIM(i.Cod_Item)+'-'+RTRIM(ISNULL(a.Des_Item,'')) Item,
            RTRIM(ISNULL(NULLIF(ic.Des_Comb,''),ep.Des_Present)) Combination,
            RTRIM(ISNULL(NULLIF(r.Cod_Medida,''),NULLIF(r.Cod_Talla,''))) MeasureCode,
            RTRIM(ISNULL(im.Descripcion,'')) MeasureDescription,
            RTRIM(ISNULL(NULLIF(r.Cod_EstCli,''),i.Cod_EstCli)) CustomerStyle,
            RTRIM(ISNULL(r.Cod_OrdPro,i.Cod_OrdPro)) ProductionOrder,
            ISNULL(r.Can_Requerida,i.Can_Requerida) Required,i.Can_Comprada TotalPurchased,
            CASE WHEN r.Sec_OrdComp IS NULL THEN 0 ELSE 1 END HasRequirement
          FROM LG_ORDCOMPITEM i LEFT JOIN LG_ITEM a ON a.Cod_Item=i.Cod_Item
          LEFT JOIN LG_ORDCOMPITEMREQ r ON r.Ser_OrdComp=i.Ser_OrdComp AND r.Cod_OrdComp=i.Cod_OrdComp AND r.Sec_OrdComp=i.Sec_OrdComp
          LEFT JOIN LG_ITEMCOMB ic ON ic.Cod_Item=i.Cod_Item AND ic.Cod_Comb=ISNULL(NULLIF(r.Cod_Comb,''),i.Cod_Comb)
          LEFT JOIN ES_ORDPRO op ON op.Cod_Fabrica=r.Cod_Fabrica AND op.Cod_OrdPro=r.Cod_OrdPro
          LEFT JOIN ES_ESTPROPRE ep ON ep.Cod_EstPro=op.Cod_EstPro AND ep.Cod_Present=r.Cod_Present
          LEFT JOIN LG_ITEMMED im ON im.Cod_Item=i.Cod_Item AND im.Cod_Medida=ISNULL(NULLIF(r.Cod_Medida,''),ISNULL(NULLIF(r.Cod_Talla,''),i.Cod_Talla))
          WHERE i.Ser_OrdComp=@Series AND i.Cod_OrdComp=@Number
          ORDER BY i.Sec_OrdComp,r.Cod_OrdPro,r.Cod_Present,r.Cod_Talla
          """;
        var rows=(await c.QueryAsync(new CommandDefinition(sql,new{order.Series,order.Number},cancellationToken:ct))).Select(x=>(IDictionary<string,object>)x).ToList();
        var output=new List<PrintBreakdown>();
        foreach(var group in rows.GroupBy(x=>RegistrationQueries.Text(x,"Sequence")))
        {
            var calculated=group.Any(x=>RegistrationQueries.Number(x,"HasRequirement")==1);
            var matches=group.Sum(x=>QuantityCalculator.RoundRequiredQuantity(RegistrationQueries.Number(x,"Required")))==RegistrationQueries.Number(group.First(),"TotalPurchased");
            foreach(var row in group)
            {
                var measure=string.Join(" - ",new[]{RegistrationQueries.Text(row,"MeasureCode"),RegistrationQueries.Text(row,"MeasureDescription")}.Where(x=>x.Length>0));
                decimal? purchased=!calculated ? RegistrationQueries.Number(row,"TotalPurchased") : matches ? QuantityCalculator.RoundRequiredQuantity(RegistrationQueries.Number(row,"Required")) : null;
                output.Add(new(group.Key,RegistrationQueries.Text(row,"Item"),RegistrationQueries.Text(row,"Combination"),measure,RegistrationQueries.Text(row,"CustomerStyle"),RegistrationQueries.Text(row,"ProductionOrder"),RegistrationQueries.Number(row,"Required"),purchased,calculated));
            }
        }
        return output;
    }

}



