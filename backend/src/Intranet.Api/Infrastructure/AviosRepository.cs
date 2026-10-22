using System.Data;
using System.Globalization;
using Dapper;
using Intranet.Api.Contracts;
using Microsoft.Extensions.Options;

namespace Intranet.Api.Infrastructure;

public sealed class AviosRepository(GestionConnectionFactory factory, IOptions<GestionOptions> options)
{
    public async Task<object> ListPos(string po, string search, int page, CancellationToken ct)
    {
        await using var connection = await factory.OpenAsync(ct);
        const string source = """
            FROM dbo.TG_PurOrd p
            LEFT JOIN dbo.TG_Cliente c ON c.Cod_Cliente=p.Cod_Cliente
            WHERE (@Po='' OR p.Cod_PurOrd LIKE @PoPattern ESCAPE '~')
              AND (@Search='' OR p.Cod_PurOrd LIKE @Pattern ESCAPE '~' OR c.Nom_Cliente LIKE @Pattern ESCAPE '~' OR p.Des_General LIKE @Pattern ESCAPE '~')
            """;
        static string Pattern(string text) => "%" + text.Replace("~","~~").Replace("%","~%").Replace("_","~_").Replace("[","~[") + "%";
        var sql = "SELECT COUNT(*) " + source + "; WITH Paged AS (SELECT ROW_NUMBER() OVER(ORDER BY p.Fec_Creacion DESC,p.Cod_PurOrd,p.Cod_Cliente) AS RowNumber,RTRIM(p.Cod_PurOrd) AS Po,RTRIM(p.Cod_Cliente) AS ClientCode,RTRIM(c.Nom_Cliente) AS Client,RTRIM(p.Des_General) AS Description,p.Fec_Creacion AS CreatedAt " + source + ") SELECT Po,ClientCode,Client,Description,CreatedAt FROM Paged WHERE RowNumber>@Offset AND RowNumber<=@Offset+50 ORDER BY RowNumber;";
        using var result = await connection.QueryMultipleAsync(new CommandDefinition(sql,new {Po=po,PoPattern=Pattern(po),Search=search,Pattern=Pattern(search),Offset=(page-1)*50},commandTimeout:options.Value.CommandTimeoutSeconds,cancellationToken:ct));
        var totalRows=await result.ReadSingleAsync<int>();
        var rows=(await result.ReadAsync<PoSummary>()).ToList();
        var totalPages=Math.Max(1,(int)Math.Ceiling(totalRows/50d));
        return new {rows,page,totalRows,totalPages,hasMore=page<totalPages};
    }

    public async Task<bool> PoExists(string po,string clientCode,CancellationToken ct)
    {
        await using var connection=await factory.OpenAsync(ct);
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition("SELECT COUNT(*) FROM dbo.TG_PurOrd WHERE Cod_PurOrd=@Po AND Cod_Cliente=@ClientCode",new{Po=po,ClientCode=clientCode},commandTimeout:options.Value.CommandTimeoutSeconds,cancellationToken:ct))==1;
    }
    public async Task<IReadOnlyList<PoDetailLine>> PoDetails(string po,string clientCode,CancellationToken ct)
    {
        await using var connection=await factory.OpenAsync(ct);
        const string sql="""
            SELECT RTRIM(d.Cod_LotPurOrd) Lot,RTRIM(d.Cod_EstCli) Style,RTRIM(e.Des_EstCli) Description,
                RTRIM(d.Cod_ColCli) ColorCode,RTRIM(c.Nom_ColCli) Color,RTRIM(d.Cod_Talla) Size,
                CAST(ISNULL(d.Num_PreReq,0) AS decimal(18,5)) Required,CAST(ISNULL(d.Num_PreDes,0) AS decimal(18,5)) Shipped,RTRIM(d.UPC) Upc
            FROM dbo.TG_LotColTal d
            LEFT JOIN dbo.TG_EstCli e ON e.Cod_Cliente=d.Cod_Cliente AND e.Cod_EstCli=d.Cod_EstCli
            LEFT JOIN dbo.TG_ColCli c ON c.Cod_Cliente=d.Cod_Cliente AND c.Cod_ColCli=d.Cod_ColCli
            WHERE d.Cod_PurOrd=@Po AND d.Cod_Cliente=@ClientCode
            ORDER BY d.Cod_LotPurOrd,d.Cod_EstCli,d.Cod_ColCli,d.Cod_Talla
            """;
        return (await connection.QueryAsync<PoDetailLine>(new CommandDefinition(sql,new{Po=po,ClientCode=clientCode},commandTimeout:options.Value.CommandTimeoutSeconds,cancellationToken:ct))).ToList();
    }

    private static string Text(IDictionary<string, object> row, params string[] names)
    {
        foreach (var name in names)
        {
            var pair = row.FirstOrDefault(p => string.Equals(p.Key, name, StringComparison.OrdinalIgnoreCase));
            if (pair.Value is not null and not DBNull) return Convert.ToString(pair.Value, CultureInfo.InvariantCulture)?.Trim() ?? "";
        }
        return "";
    }
    private static decimal Number(IDictionary<string, object> row, params string[] names) => decimal.TryParse(Text(row, names), NumberStyles.Any, CultureInfo.InvariantCulture, out var value) ? value : 0;
    private CommandDefinition Command(string name, object parameters, CancellationToken ct) => new(name, parameters, commandType: CommandType.StoredProcedure, commandTimeout: options.Value.CommandTimeoutSeconds, cancellationToken: ct);

    public async Task<PurchaseOrder?> FindOrder(string series, string number, string user, CancellationToken ct, string? readAllWindowsUser = null)
    {
        await using var connection = await factory.OpenAsync(ct);
        // Existing procedure applies LG_SEGORDCOMP permissions for the mapped Gestion user.
        var rows = await connection.QueryAsync(Command("dbo.UP_SEL_ORDCOMP", new {
            TIPO_ELECCION = 1, SER_ORDCOMP = series, COD_ORDCOMP = number,
            COD_PROVEEDOR = "", COD_STAORDCOMP = "", COD_GRUPO = "", COD_USUARIO = user,
            COD_FABRICA = "", COD_ORDPRO = "", NUM_REQ = 0
        }, ct));
        var raw = rows.FirstOrDefault();
        if (raw is null && readAllWindowsUser is not null
            && options.Value.ReadAllWindowsUsers.Contains(readAllWindowsUser, StringComparer.OrdinalIgnoreCase))
        {
            // The authenticated application identity must be explicitly allowed by the server.
            const string adminRead = """
                    SELECT o.Cod_StaOrdComp, o.Cod_ClaOrdComp, o.Fec_Creacion,
                           p.Des_Proveedor AS PROVEEDOR, c.Nom_Cliente AS NOM_CLIENTE
                    FROM dbo.LG_ORDCOMP o
                    LEFT JOIN dbo.LG_PROVEEDOR p ON p.Cod_Proveedor = o.Cod_Proveedor
                    LEFT JOIN dbo.TG_CLIENTE c ON c.Cod_Cliente = o.COD_CLIENTE
                    WHERE o.Ser_OrdComp = @Series AND o.Cod_OrdComp = @Number
                """;
            raw = (await connection.QueryAsync(new CommandDefinition(adminRead,
                new { WindowsIdentity = readAllWindowsUser, Series = series, Number = number },
                commandTimeout: options.Value.CommandTimeoutSeconds, cancellationToken: ct))).FirstOrDefault();
        }
        if (raw is null) return null;
        var row = (IDictionary<string, object>)raw;
        DateTime? date = DateTime.TryParse(Text(row, "FEC_CREACION"), CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) ? parsed : null;
        return new(series, number, Text(row, "PROVEEDOR"), Text(row, "COD_STAORDCOMP", "ESTADOOC"), Text(row, "COD_CLAORDCOMP", "CLASEOC"), Text(row, "NOM_CLIENTE", "ABR_CLIENTE"), date);
    }

    public async Task<IReadOnlyList<OrderItem>> Items(string series, string number, CancellationToken ct)
    {
        await using var connection = await factory.OpenAsync(ct);
        var rows = await connection.QueryAsync(Command("dbo.UP_SEL_ORDCOMPITEM", new { TIP_ITEM = "I", SER_ORDCOMP = series, COD_ORDCOMP = number }, ct));
        const string opSql="SELECT Sec_OrdComp,Cod_OrdPro FROM dbo.LG_ORDCOMPITEMREQ WHERE Ser_OrdComp=@Series AND Cod_OrdComp=@Number UNION SELECT Sec_OrdComp,Cod_OrdPro FROM dbo.LG_ORDCOMPITEM WHERE Ser_OrdComp=@Series AND Cod_OrdComp=@Number";
        var links=await connection.QueryAsync(new CommandDefinition(opSql,new{Series=series,Number=number},commandTimeout:options.Value.CommandTimeoutSeconds,cancellationToken:ct));
        var productionOrders=links.Select(x=>(IDictionary<string,object>)x).GroupBy(x=>Text(x,"Sec_OrdComp")).ToDictionary(g=>g.Key,g=>string.Join(", ",g.Select(x=>Text(x,"Cod_OrdPro")).Where(x=>x.Length>0).Distinct().Order()));
        return rows.Select(raw => {
            var r = (IDictionary<string, object>)raw;
            return new OrderItem(Text(r,"SEC_ORDCOMP"), Text(r,"CODIGO","COD_ITEM"), Text(r,"DESCRIPCION","DES_ITEM"), Text(r,"COD_TALLA"), Text(r,"COD_ESTCLI"), Number(r,"CAN_REQUERIDA"), Number(r,"CAN_COMPRADA"), Number(r,"CAN_RECIBIDA"), productionOrders.GetValueOrDefault(Text(r,"SEC_ORDCOMP"),""));
        }).ToList();
    }

    public async Task<IReadOnlyList<Requirement>> Requirements(string series, string number, string sequence, CancellationToken ct)
    {
        await using var connection = await factory.OpenAsync(ct);
        var rows = await connection.QueryAsync(Command("dbo.UP_SEL_ORDCOMPITEMREQ", new { Tip_Item = "I", Ser_OrdComp = series, Cod_OrdComp = number, Sec_OrdComp = sequence }, ct));
        return rows.Select(raw => {
            var r = (IDictionary<string, object>)raw;
            return new Requirement(Text(r,"COD_FABRICA"), Text(r,"COD_ORDPRO"), Text(r,"COD_PRESENT"), Text(r,"DES_PRESENT"), Text(r,"COD_COMPEST"), Text(r,"COD_ITEM"), Text(r,"COD_ESTCLI"), Text(r,"DES_COLOR"), Text(r,"COD_TALLA"), Text(r,"COD_DESTINO"), Number(r,"CAN_REQUERIDA"));
        }).ToList();
    }
}


