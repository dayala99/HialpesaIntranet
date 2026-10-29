using Intranet.Entity;
using Intranet.Service;
namespace Intranet.Api.Tests;
public sealed class AnalisisRbHeaderTests {
 [Fact]public void SnapshotPreservesHeaderAsSaved(){var request=new AnalisisRbSave{RequestId=Guid.NewGuid(),Source=new("J1907",438,1.305m,12.05m){ClientName="LILLY",LineName="GAMBOA",FabricDescription="INTERLOCK",ColorName="FRESH WATER",SewingStartDate=new(2026,11,5),SewingEndDate=new(2026,11,6),DispatchDate=new(2026,11,9)},UsedTime=12.05m,ServicePrice=1,OtherExpenses=.03m};var snapshot=AnalisisRbHistoryService.BuildSnapshot(request,new(2026,10,28));var restored=System.Text.Json.JsonSerializer.Deserialize<AnalisisRbSnapshot>(System.Text.Json.JsonSerializer.Serialize(snapshot));Assert.Equal("FRESH WATER",restored!.Source.ColorName);Assert.Equal(new DateTime(2026,11,5),restored.Source.SewingStartDate);Assert.Equal(new DateTime(2026,11,6),restored.Source.SewingEndDate);Assert.Equal("INTERLOCK",restored.Source.FabricDescription);Assert.Equal("GAMBOA",restored.Source.LineName);}
 [Fact]public void OldSnapshotCanOmitNewFields(){var source=System.Text.Json.JsonSerializer.Deserialize<AnalisisRbRow>("{\"Np\":\"J1825\",\"ProgrammedQuantity\":217}");Assert.Null(source!.ColorName);Assert.Null(source.SewingStartDate);Assert.Null(source.SewingEndDate);}
}
