using Intranet.Entity;
using Dapper;

using Microsoft.Extensions.Options;

namespace Intranet.Data;

public sealed class RegistrationReadRepository(GestionConnectionFactory factory, IOptions<GestionOptions> options) : IRegistrationReadRepository
{
    private static IReadOnlyList<LookupOption> Lookup(IEnumerable<dynamic> rows) => rows.Select(raw => { var r = (IDictionary<string, object>)raw; return new LookupOption(RegistrationQueries.Text(r, "Code"), RegistrationQueries.Text(r, "Description")); }).ToList();
    public async Task<object> List(string user, string? windows, int page, string search, CancellationToken ct)
    {
        await using var connection = await factory.OpenAsync(ct);
        var term = search.Trim();
        var readAll = windows is not null && options.Value.ReadAllWindowsUsers.Contains(windows, StringComparer.OrdinalIgnoreCase);
        using var result = await connection.QueryMultipleAsync(new CommandDefinition("dbo.PA_LG_ORDCOMP_S0005", new { User = user, ReadAll = readAll, Search = term, Offset = (page - 1) * 50, End = page * 50 + 1 }, commandTimeout: options.Value.CommandTimeoutSeconds, cancellationToken: ct, commandType: System.Data.CommandType.StoredProcedure));
        var rows = (await result.ReadAsync()).ToList(); var totalRows = await result.ReadSingleAsync<int>(); var totalPages = Math.Max(1, (int)Math.Ceiling(totalRows / 50d));
        return new { rows = rows.Take(50).Select(x => new { series = (string)x.Series, number = (string)x.Number, supplier = (string?)x.Supplier, status = (string)x.Status, @class = (string)x.Class, createdAt = (DateTime?)x.CreatedAt }), hasMore = page < totalPages, page, totalRows, totalPages };
    }
    public async Task<RegistrationCatalogs> Catalogs(CancellationToken ct)
    {
        await using var connection = await factory.OpenAsync(ct);
        async Task<IReadOnlyList<LookupOption>> ReadCatalog(string name) => Lookup(await connection.QueryAsync(new CommandDefinition(name, commandTimeout: options.Value.CommandTimeoutSeconds, cancellationToken: ct, commandType: System.Data.CommandType.StoredProcedure)));
        return new(await ReadCatalog("dbo.PA_LG_CONDVENT_S0001"), await ReadCatalog("dbo.PA_LG_DSCTOS_S0001"), await ReadCatalog("dbo.PA_TG_MONEDA_S0001"), await ReadCatalog("dbo.PA_LG_LUGENTR_S0001"), await ReadCatalog("dbo.PA_TG_CENCOSTO_S0001"), await ReadCatalog("dbo.PA_LG_CLAORDCOMP_S0001"));
    }

    public async Task<IReadOnlyList<LookupOption>> DetailGroups(OrderReference order, CancellationToken ct)
    {
        await using var c = await factory.OpenAsync(ct);
        const string sql = "dbo.PA_ES_GRUPOLOG_S0001";
        return Lookup(await c.QueryAsync(new CommandDefinition(sql, order, cancellationToken: ct, commandType: System.Data.CommandType.StoredProcedure)));
    }
    public async Task<IReadOnlyList<LookupOption>> DetailOps(OrderReference order, string group, CancellationToken ct)
    {
        await using var c = await factory.OpenAsync(ct);
        const string sql = "dbo.PA_ES_ORDPRO_S0002";
        return Lookup(await c.QueryAsync(new CommandDefinition(sql, new { order.Series, order.Number, Group = group }, cancellationToken: ct, commandType: System.Data.CommandType.StoredProcedure)));
    }
    public async Task<IReadOnlyList<LookupOption>> DetailItems(OrderReference order, string group, string op, CancellationToken ct)
    {
        if (!(await DetailOps(order, group, ct)).Any(x => x.Code == op)) throw new RegistrationConflictException("La OP no pertenece al grupo seleccionado de esta OC.");
        await using var c = await factory.OpenAsync(ct); var header = await RegistrationQueries.Header(c, order, null, false, ct);
        var rows = await c.QueryAsync(new CommandDefinition("dbo.UP_SEL_REQUEXCOMPRARLOG", new { COD_GRUPOLOG = group, COD_ORDPRO = op, COD_FAMITEM = "", COD_PROVEEDOR = header.SupplierCode, COD_ITEM = "", SER_ORDCOMP = order.Series, COD_ORDCOMP = order.Number, DES_PRESENTACION = "" }, commandType: System.Data.CommandType.StoredProcedure, commandTimeout: options.Value.CommandTimeoutSeconds, cancellationToken: ct));
        return rows.Select(raw => (IDictionary<string, object>)raw).Where(r => RegistrationQueries.Number(r, "CANTXCOMPRAR") > 0 && RegistrationQueries.Text(r, "COD_ORDPRO") == op).Select(r => new LookupOption(RegistrationQueries.Text(r, "COD_ITEM"), RegistrationQueries.Text(r, "ITEM"))).Where(x => x.Code.Length > 0).DistinctBy(x => x.Code).OrderBy(x => x.Code).ToList();
    }
    public async Task<object> LinkedRequirements(OrderReference order, string sequence, CancellationToken ct)
    {
        await using var c = await factory.OpenAsync(ct); var rows = await OrderRequirementLinks.Read(c, null, order, sequence, ct);
        return rows.Select(r => new { key = OrderRequirementLinks.Key(r), factory = RegistrationQueries.Text(r, "Cod_Fabrica"), productionOrder = RegistrationQueries.Text(r, "Cod_OrdPro"), presentation = RegistrationQueries.Text(r, "Presentation"), itemCode = RegistrationQueries.Text(r, "Cod_Item"), size = RegistrationQueries.Text(r, "Cod_Talla"), quantity = RegistrationQueries.Number(r, "Can_Requerida") }).ToList();
    }
    public async Task<UpdateOrderItemRequest?> EditableItem(OrderReference order, string sequence, CancellationToken ct)
    {
        await using var c = await factory.OpenAsync(ct);
        return await c.QuerySingleOrDefaultAsync<UpdateOrderItemRequest>(new CommandDefinition("dbo.PA_LG_ORDCOMPITEM_S0001", new { order.Series, order.Number, Sequence = sequence }, commandTimeout: options.Value.CommandTimeoutSeconds, cancellationToken: ct, commandType: System.Data.CommandType.StoredProcedure));
    }
    public async Task<object> EditableHeader(OrderReference order, CancellationToken ct)
    {
        await using var c = await factory.OpenAsync(ct);
        const string sql = "dbo.PA_LG_ORDCOMP_S0006";
        using var result = await c.QueryMultipleAsync(new CommandDefinition(sql, order, commandTimeout: options.Value.CommandTimeoutSeconds, cancellationToken: ct, commandType: System.Data.CommandType.StoredProcedure));
        var header = await result.ReadSingleAsync<UpdateOrderRequest>(); var supplierName = await result.ReadFirstOrDefaultAsync<string>();
        return new { header, supplierName };
    }
    public async Task<IReadOnlyList<LookupOption>> OrderGroups(OrderReference order, CancellationToken ct)
    {
        await using var c = await factory.OpenAsync(ct);
        var rows = await c.QueryAsync(new CommandDefinition("dbo.LG_MUESTRA_ORDCOMP_GRUPOS", new { SER_ORDCOMP = order.Series, COD_ORDCOMP = order.Number }, commandType: System.Data.CommandType.StoredProcedure, commandTimeout: options.Value.CommandTimeoutSeconds, cancellationToken: ct));
        return rows.Select(raw => new LookupOption(RegistrationQueries.Text((IDictionary<string, object>)raw, "Cod_Grupo"), "")).ToList();
    }
    public async Task<IReadOnlyList<LookupOption>> Factories(CancellationToken ct)
    {
        await using var c = await factory.OpenAsync(ct);
        return Lookup(await c.QueryAsync(new CommandDefinition("dbo.PA_TG_FABRICA_S0001", commandTimeout: options.Value.CommandTimeoutSeconds, cancellationToken: ct, commandType: System.Data.CommandType.StoredProcedure)));
    }
    public async Task<IReadOnlyList<LookupOption>> GroupsByOp(string factoryAbbreviation, string productionOrder, CancellationToken ct)
    {
        await using var c = await factory.OpenAsync(ct);
        const string sql = "dbo.PA_ES_GRUPOLOG_S0002";
        return Lookup(await c.QueryAsync(new CommandDefinition(sql, new { Factory = factoryAbbreviation, Op = productionOrder }, commandTimeout: options.Value.CommandTimeoutSeconds, cancellationToken: ct, commandType: System.Data.CommandType.StoredProcedure)));
    }
    public async Task<SupplierDefaults?> SupplierDefaults(string code, CancellationToken ct)
    {
        await using var c = await factory.OpenAsync(ct);
        const string sql = "dbo.PA_LG_PROVEEDOR_S0001";
        return await c.QuerySingleOrDefaultAsync<SupplierDefaults>(new CommandDefinition(sql, new { Code = code }, commandTimeout: options.Value.CommandTimeoutSeconds, cancellationToken: ct, commandType: System.Data.CommandType.StoredProcedure));
    }
    public async Task<IReadOnlyList<LookupOption>> Search(string kind, string search, CancellationToken ct)
    {
        var sql = kind switch
        {
            "proveedores" => "dbo.PA_LG_PROVEEDOR_S0002",
            "grupos" => "dbo.PA_ES_GRUPOLOG_S0003",
            _ => throw new ArgumentException("Catálogo no válido.")
        };
        await using var connection = await factory.OpenAsync(ct);
        return Lookup(await connection.QueryAsync(new CommandDefinition(sql, new { Search = "%" + search.Trim() + "%" }, cancellationToken: ct, commandType: System.Data.CommandType.StoredProcedure)));
    }
    public async Task<IReadOnlyList<PendingRequirement>> Pending(OrderReference order, string op, string item, CancellationToken ct)
    {
        await using var connection = await factory.OpenAsync(ct);
        var header = await RegistrationQueries.Header(connection, order, null, false, ct);
        if (header.Status != "P" || header.ClassCode != "01") throw new RegistrationConflictException("Agrega detalle únicamente a una OC de avíos Planeada.");
        return await RegistrationQueries.Pending(connection, header, op, item, null, options.Value.CommandTimeoutSeconds, ct);
    }
    public async Task<PrintOrder> Print(PurchaseOrder order, IReadOnlyList<OrderItem> items, CancellationToken ct)
    {
        await using var connection = await factory.OpenAsync(ct);
        const string sql = "dbo.PA_LG_ORDCOMP_S0007";
        using var results = await connection.QueryMultipleAsync(new CommandDefinition(sql, new { order.Series, order.Number }, cancellationToken: ct, commandType: System.Data.CommandType.StoredProcedure));
        var header = (IDictionary<string, object>)(await results.ReadSingleAsync());
        var lines = (await results.ReadAsync()).Select(raw => { var r = (IDictionary<string, object>)raw; var qty = RegistrationQueries.Number(r, "Can_Comprada"); var price = RegistrationQueries.Number(r, "Pre_Unitario"); return new PrintLine(RegistrationQueries.Text(r, "Sec_OrdComp"), RegistrationQueries.Text(r, "Cod_Item"), RegistrationQueries.Text(r, "Des_Item"), RegistrationQueries.Text(r, "Cod_Talla"), qty, price, qty * price); }).ToList();
        DateTime? Date(string key) => header.TryGetValue(key, out var value) && value is DateTime d ? d : null;
        var breakdown = await PrintBreakdown(connection, order, ct);
        var reportRaw = await connection.QueryAsync(new CommandDefinition("dbo.UP_SEL_ORDCOMPITEM", new { Tip_Item = "I", Ser_OrdComp = order.Series, Cod_OrdComp = order.Number }, commandType: System.Data.CommandType.StoredProcedure, commandTimeout: options.Value.CommandTimeoutSeconds, cancellationToken: ct));
        var reportRows = reportRaw.Select(raw =>
        {
            var d = (IDictionary<string, object>)raw;
            string T(params string[] keys) => RegistrationQueries.Text(d, keys);
            decimal N(string key) => RegistrationQueries.Number(d, key);
            string Pair(string code, string desc) => string.Join(" - ", new[] { T(code), T(desc) }.Where(x => x.Length > 0));
            return new GestionPrintLine(T("ITEM"), Pair("cod_comb", "des_comb"), Pair("cod_color", "des_color"), T("medida"), T("cod_destino"), T("estilo_cli"), T("cod_unimed"), N("pre_unitario"), N("can_requerida"), N("can_comprada"), N("can_recibida"), N("Fac_Equiprov"), T("cod_itemprov"), T("cod_prov"), string.Join(", ", breakdown.Where(x => x.Sequence == T("Sec_OrdComp")).Select(x => x.ProductionOrder).Where(x => x.Length > 0).Distinct()));
        }).ToList();
        return new(order, items, RegistrationQueries.Text(header, "Cod_Moneda"), RegistrationQueries.Text(header, "Des_CondVent"), RegistrationQueries.Text(header, "Des_LugEntr"), RegistrationQueries.Text(header, "Cod_Grupo"), RegistrationQueries.Text(header, "Observaciones"), RegistrationQueries.Number(header, "Porc_IGV"), Date("Fec_Entrega_Inicio"), Date("Fec_Entrega_Fin"), lines, breakdown, reportRows);
    }
    private static async Task<IReadOnlyList<PrintBreakdown>> PrintBreakdown(Microsoft.Data.SqlClient.SqlConnection c, PurchaseOrder order, CancellationToken ct)
    {
        const string sql = "dbo.PA_LG_ORDCOMPITEM_S0002";
        var rows = (await c.QueryAsync(new CommandDefinition(sql, new { order.Series, order.Number }, cancellationToken: ct, commandType: System.Data.CommandType.StoredProcedure))).Select(x => (IDictionary<string, object>)x).ToList();
        var output = new List<PrintBreakdown>();
        foreach (var group in rows.GroupBy(x => RegistrationQueries.Text(x, "Sequence")))
        {
            var calculated = group.Any(x => RegistrationQueries.Number(x, "HasRequirement") == 1);
            var matches = group.Sum(x => QuantityRules.RoundRequiredQuantity(RegistrationQueries.Number(x, "Required"))) == RegistrationQueries.Number(group.First(), "TotalPurchased");
            foreach (var row in group)
            {
                var measure = string.Join(" - ", new[] { RegistrationQueries.Text(row, "MeasureCode"), RegistrationQueries.Text(row, "MeasureDescription") }.Where(x => x.Length > 0));
                decimal? purchased = !calculated ? RegistrationQueries.Number(row, "TotalPurchased") : matches ? QuantityRules.RoundRequiredQuantity(RegistrationQueries.Number(row, "Required")) : null;
                output.Add(new(group.Key, RegistrationQueries.Text(row, "Item"), RegistrationQueries.Text(row, "Combination"), measure, RegistrationQueries.Text(row, "CustomerStyle"), RegistrationQueries.Text(row, "ProductionOrder"), RegistrationQueries.Number(row, "Required"), purchased, calculated));
            }
        }
        return output;
    }

}



