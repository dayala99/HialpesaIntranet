using System.Text.Json;
using Intranet.Api.Contracts;
using Intranet.Api.Services;
using Xunit;
namespace Intranet.Api.Tests;
public class PoQuantitySourceTests
{
 [Fact]public void UsesRoundedDetailAmountsForBothItemsAndCombinesLots()
 {
  var a=new PoDetailLine("001","STYLE1","","C1","BLACK","L / G",153,0,"");
  var b=a with{Lot="002",Required=6};
  Assert.Equal(155,a.Purchased);Assert.Equal(10,b.Purchased);
  using var json=JsonDocument.Parse(JsonSerializer.Serialize(PoQuantitySource.Create("PO-000001",new[]{a,b})));
  var rows=json.RootElement.EnumerateArray().ToArray();Assert.Equal(2,rows.Length);
  Assert.All(rows,r=>{Assert.Equal(165,r.GetProperty("purchased").GetDecimal());Assert.Equal("L",r.GetProperty("measure").GetString());Assert.Equal("PO-000001 COLOR: BLACK",r.GetProperty("combination").GetString());});
 }
 [Fact]public void RejectsMissingDetailsOrNegativeRequirements()
 {
  Assert.Throws<ArgumentException>(()=>PoQuantitySource.Create("PO-000001",Array.Empty<PoDetailLine>()));
  Assert.Throws<ArgumentException>(()=>PoQuantitySource.Create("PO-000001",new[]{new PoDetailLine("001","STYLE","","C","BLACK","L",-1,0,"")}));
 }
}
