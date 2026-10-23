using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Intranet.Entity;
namespace Intranet.Data;

public sealed class QualityControlRepository(GestionConnectionFactory factory) : IQualityControlRepository
{
    private const string Reader = "dbo.PA_TI_ORDTRA_TINTORERIA_ITEMS_S0002";
    private static CommandDefinition Query(string mode,string batch,int sequence,string user,CancellationToken ct,SqlTransaction? tx=null,bool locked=false,string search="") => new(Reader,new { MODO=mode,COD_ORDTRA=batch,NUM_SECUENCIA=sequence,COD_USUARIO=user,BLOQUEAR=locked,BUSQUEDA=search },transaction:tx,commandType:CommandType.StoredProcedure,commandTimeout:60,cancellationToken:ct);
    private static Dictionary<string,object?> Dictionary(object row) => ((IDictionary<string,object>)row).ToDictionary(e=>e.Key.ToLowerInvariant(),e=>e.Value is string s?(object?)s.Trim():e.Value);
    private static string Text(Dictionary<string,object?> row,string key) => Convert.ToString(row.GetValueOrDefault(key))?.Trim() ?? "";
    private static decimal Number(Dictionary<string,object?> row,string key) => row.GetValueOrDefault(key) is {} value ? Convert.ToDecimal(value) : 0;
    private static async Task<QualityControlDetail?> ReadDetail(SqlConnection c,string batch,int sequence,string user,CancellationToken ct,SqlTransaction? tx=null,bool locked=false)
    {
        var data=await c.QuerySingleOrDefaultAsync(Query("D",batch,sequence,user,ct,tx,locked));
        if(data is null)return null;
        var row=Dictionary((object)data);
        var recommendations=await c.QueryAsync(new CommandDefinition("dbo.TI_RECOMENDACIONES_CONTROL_CALIDAD_select",new {COD_ORDTRA=batch,NUM_SECUENCIA=sequence},transaction:tx,commandType:CommandType.StoredProcedure,cancellationToken:ct));
        var list=recommendations.Select(r=>{var x=Dictionary((object)r);return new QualityRecommendation(Text(x,"cod_recomendacion"),Text(x,"des_recomendacion"),Convert.ToBoolean(x["sel"]));}).ToList();
        var values=new QualityControlValues {
            Weight = Number(row, "gramaje_acab"),
            Width = Number(row, "ancho_acab_informado"),
            ShrinkWidth = Number(row, "encog_ancho"),
            ShrinkLength = Number(row, "encog_largo"),
            Twist = Number(row, "revirado"),
            ThirdShrinkWidth = Number(row, "encog_ancho_3ralav"),
            ThirdShrinkLength = Number(row, "encog_largo_3ralav"),
            ThirdTwist = Number(row, "revirado_3ralav"),
            WashedWeight = Number(row, "gramaje_lavado"),
            LaundryWeight = Number(row, "gramaje_lavanderia"),
            ElongationLength = Number(row, "elongacionlargo"),
            ElongationWidth = Number(row, "elongacionancho"),
            RejectedWeight = Number(row, "kgs_rechazados"),
            PlantWidth = Number(row, "qcplantwidth"),
            Resistance = Number(row, "resistencia_tela_real"),
            ResidualShrinkWidth = Number(row, "qcresidualshrinkwidth"),
            ResidualShrinkLength = Number(row, "qcresidualshrinklength"),
            ResidualTwist = Number(row, "qcresidualtwist"),
            RamaWeight = Number(row, "gramaje_real_rama"),
            RamaWidth = Number(row, "ancho_real_rama"),
            RamaShrinkWidth = Number(row, "encog_ancho_real_rama"),
            RamaShrinkLength = Number(row, "encog_largo_real_rama"),
            RamaTwist = Number(row, "revirado_rama"),
            RamaWashedWeight = Number(row, "gramaje_lavado_rama"),
            WidthType=Text(row,"tip_ancho_informado"),ShrinkType=Text(row,"cod_tipenc"),RamaType=Text(row,"tipo_rama"),
            Approval=Text(row,"flg_status_apro"),Observations=Text(row,"observaciones"),RejectionReason=Text(row,"cod_motrechazo"),
            AlternativeFabric=Text(row,"cod_tela_relacionada"),Skew=Text(row,"skew"),Inclination=Text(row,"inclinacion_columna"),WashType=Text(row,"tipo_lavado")
        };
        if(values.Approval=="")values.Approval="A";
        var reference=new QualityReference(Number(row,"qcprogrammedweight"),Number(row,"qcprogrammedwidth"),Number(row,"qcprogrammedshrinkwidth"),Number(row,"qcprogrammedshrinklength"),Number(row,"qcfabrictwist"),Text(row,"qctechnicalwidthtype"),Text(row,"qcalternativefabricdescription"));
        return new(batch,sequence,Text(row,"cod_tela"),Text(row,"cod_comb"),Text(row,"cod_color"),Text(row,"qcfabricdescription"),values,reference,row.GetValueOrDefault("fec_ultmoddattec") as DateTime?,Convert.ToBoolean(row["qccanedit"]),list);
    }
    public async Task<QualityControlScreen?> Read(string batch,int sequence,string user,CancellationToken ct)
    {
        await using var c=await factory.OpenAsync(ct);
        try {
            var detail=await ReadDetail(c,batch,sequence,user,ct);if(detail is null)return null;
            var catalogs=(await c.QueryAsync<DyeingCatalogOption>(Query("C",batch,sequence,user,ct))).ToList();
            return new(detail,catalogs);
        } catch(SqlException ex) when(ex.Number==2812){throw new QualityControlPendingException();}
    }
    public async Task<IReadOnlyList<DyeingCatalogOption>> Fabrics(string search,CancellationToken ct)
    {
        await using var c=await factory.OpenAsync(ct);
        try { return (await c.QueryAsync<DyeingCatalogOption>(Query("T","",0,"",ct,search:search))).ToList(); }
        catch(SqlException ex) when(ex.Number==2812){throw new QualityControlPendingException();}
    }
    public async Task<IReadOnlyList<Dictionary<string,object?>>> Consumption(string batch,int sequence,string user,CancellationToken ct)
    {
        await using var c=await factory.OpenAsync(ct);
        try {
            var detail=await ReadDetail(c,batch,sequence,user,ct);if(detail is null)throw new ArgumentException("El detalle de OT ya no existe.");
            var rows=await c.QueryAsync(new CommandDefinition("dbo.ti_muestra_tendidos_donde_se_uso_partida",new {cod_ordtra=batch,cod_tela=detail.Fabric,cod_comb=detail.Combination},commandType:CommandType.StoredProcedure,commandTimeout:60,cancellationToken:ct));
            return rows.Select(row=>Dictionary((object)row)).ToList();
        } catch(SqlException ex) when(ex.Number==2812){throw new QualityControlPendingException();}
    }
    public static DynamicParameters WriteParameters(QualityControlDetail current,QualityControlValues v,string user)
    {
        var p=new DynamicParameters();
        p.Add("COD_TIPORDTRA","TI");p.Add("COD_ORDTRA",current.Batch);p.Add("NUM_SECUENCIA",current.Sequence);
        p.Add("COD_TELA",current.Fabric);p.Add("COD_COMB",current.Combination);p.Add("COD_COLOR",current.Color);p.Add("COD_USUARIO",user);
        p.Add("GRAMAJE_ACAB", v.Weight);
        p.Add("ANCHO_ACAB", v.Width);
        p.Add("ENCOG_ANCHO", v.ShrinkWidth);
        p.Add("ENCOG_LARGO", v.ShrinkLength);
        p.Add("REVIRADO", v.Twist);
        p.Add("ENCOG_ANCHO_3RALAV", v.ThirdShrinkWidth);
        p.Add("ENCOG_LARGO_3RALAV", v.ThirdShrinkLength);
        p.Add("REVIRADO_3RALAV", v.ThirdTwist);
        p.Add("GRAMAJE_LAVADO", v.WashedWeight);
        p.Add("GRAMAJE_LAVANDERIA", v.LaundryWeight);
        p.Add("ELONGACIONLARGO", v.ElongationLength);
        p.Add("ELONGACIONANCHO", v.ElongationWidth);
        p.Add("KGS_RECHAZADOS", v.RejectedWeight);
        p.Add("ANCHO_PLANTA", v.PlantWidth);
        p.Add("RESISTENCIA_TELA_REAL", v.Resistance);
        p.Add("ENCOG_ANCHO_RESIDUALES", v.ResidualShrinkWidth);
        p.Add("ENCOG_LARGO_RESIDUALES", v.ResidualShrinkLength);
        p.Add("REVIRADO_RESIDUALES", v.ResidualTwist);
        p.Add("Gramaje_Real_Rama", v.RamaWeight);
        p.Add("Ancho_Real_Rama", v.RamaWidth);
        p.Add("encog_ancho_real_Rama", v.RamaShrinkWidth);
        p.Add("encog_largo_real_Rama", v.RamaShrinkLength);
        p.Add("Revirado_Rama", v.RamaTwist);
        p.Add("Gramaje_Lavado_Rama", v.RamaWashedWeight);
        p.Add("TIP_ANCHO",v.WidthType);p.Add("COD_TIPENC",v.ShrinkType);p.Add("Tipo_Rama",v.RamaType);p.Add("FLG_STATUS_APRO",v.Approval);
        p.Add("OBSERVACIONES",v.Observations);p.Add("COD_MOTRECHAZO",v.RejectionReason);p.Add("COD_TELA_RELACIONDA",v.AlternativeFabric);
        p.Add("SKEW",v.Skew);p.Add("INCLINACION_COLUMNA",v.Inclination);p.Add("Tipo_Lavado",v.WashType);
        return p;
    }
    public async Task Save(string batch,int sequence,QualityControlUpdate request,string user,CancellationToken ct)
    {
        await using var c=await factory.OpenAsync(ct);
        using var tx=c.BeginTransaction();
        try {
            var current=await ReadDetail(c,batch,sequence,user,ct,tx,true) ?? throw new ArgumentException("El detalle de OT ya no existe.");
            if(!current.CanEdit)throw new UnauthorizedAccessException("Gestión no autoriza a este usuario a cambiar los datos técnicos de esta OT.");
            if(current.ModifiedAt!=request.ExpectedModifiedAt)throw new RegistrationConflictException("Los datos cambiaron desde que abriste el formulario. Ciérralo y vuelve a abrirlo.");
            var catalogs=(await c.QueryAsync<DyeingCatalogOption>(Query("C",batch,sequence,user,ct,tx))).ToList();
            bool Has(string kind,string code)=>catalogs.Any(o=>o.Kind==kind && o.Code.Trim()==code);
            var v=request.Values;
            if(!Has("widthTypes",v.WidthType)||!Has("shrinkTypes",v.ShrinkType)||!Has("shrinkTypes",v.RamaType))throw new ArgumentException("Selecciona tipos de ancho y encogimiento válidos.");
            if(v.RejectionReason!=""&&!Has("rejectionReasons",v.RejectionReason))throw new ArgumentException("El motivo de rechazo no existe.");
            var selected=request.Recommendations.ToHashSet(StringComparer.Ordinal);
            if(selected.Any(code=>!current.Recommendations.Any(r=>r.Code==code)))throw new ArgumentException("Recomendación no válida.");
            await c.ExecuteAsync(new CommandDefinition("dbo.TI_UP_ACT_DATOSTECNICOS_TINTO_ITEM",WriteParameters(current,v,user),transaction:tx,commandType:CommandType.StoredProcedure,commandTimeout:90,cancellationToken:ct));
            foreach(var recommendation in current.Recommendations)await c.ExecuteAsync(new CommandDefinition("dbo.UP_MAN_TI_ORDTRA_TINTORERIA_ITEMS_RECOMENDACIONES",new {OPCION="A",COD_ORDTRA=batch,NUM_SECUENCIA=sequence,COD_RECOMENDACION=recommendation.Code,CHK=selected.Contains(recommendation.Code)},transaction:tx,commandType:CommandType.StoredProcedure,cancellationToken:ct));
            tx.Commit();
        } catch(SqlException ex) when(ex.Number==2812){try{tx.Rollback();}catch{} throw new QualityControlPendingException();}
        catch {try{tx.Rollback();}catch{} throw;}
    }
}
