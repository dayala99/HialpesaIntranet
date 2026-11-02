using Intranet.Data;
namespace Intranet.Api.Tests;
public sealed class AnalisisRbHistorySearchTests {
 [Theory][InlineData(null)][InlineData("")][InlineData("broken")][InlineData("[]")][InlineData("null")][InlineData("{\"source\":null}")][InlineData("{\"source\":{}}")][InlineData("{\"source\":{\"garmentType\":123}}")]
 public void MissingOrInvalidTypeStaysUnknown(string? json)=>Assert.Null(AnalisisRbHistorySearchRepository.SavedType(json));
 [Theory][InlineData("{\"source\":{\"garmentType\":\" T-SHIRT \"}}")][InlineData("{\"Source\":{\"GarmentType\":\"T-SHIRT\"}}")]
 public void TypeComesFromSavedSnapshot(string json)=>Assert.Equal("T-SHIRT",AnalisisRbHistorySearchRepository.SavedType(json));
}
