using Intranet.Api.Controllers;
using Xunit;
namespace Intranet.Api.Tests;
public class ImportParametersTests
{
 [Fact] public void ServerSourceDefinesIdentityAndNumberIsAllocatedByGestion(){
 var r=new ImportRequest{ItemType="371",Origin="HK",RegisteredAt=new DateTime(2026,10,19),Guide=" AB "};
 var s=new ImportSource("supplier","Name","P","4815","USD","","E");
 var p=ImportParameters.Create(r,s,"programador04");
 Assert.Equal("",p.Get<string>("Num_Importacion"));Assert.Equal("",p.Get<string>("Ser_OrdComp"));
 Assert.Equal("4815",p.Get<string>("Cod_Anxo"));Assert.Equal("USD",p.Get<string>("Cod_Moneda"));
 Assert.Equal("programador04",p.Get<string>("cod_usuario"));Assert.Equal("AB",p.Get<string>("Num_Guia"));
 Assert.Equal("N",p.Get<string>("Flg_Liquidacion_Parcial"));Assert.Null(p.Get<DateTime?>("Fecha_Guia"));
 Assert.Equal(new DateTime(2026,10,19),p.Get<DateTime>("Fec_RegDoc"));
 }
}
