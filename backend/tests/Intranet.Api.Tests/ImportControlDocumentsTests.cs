using Intranet.Entity;using Intranet.Service;
namespace Intranet.Api.Tests;
public sealed class ImportControlDocumentsTests {
 static ImportControlDocument Doc()=>new(){Kind="PR",Type="PR",Series="001",Number="123",AnnexType="P",Annex="8342",ItemType="603",Currency="USD",Description="ETIQUETAS",ExchangeRate=3.5m};
 [Fact]public void DocumentDraftsAreOptionalForExistingClients()=>ImportControlService.ValidateDocuments(new());
 [Fact]public void ValidAuxiliaryDocumentIsAccepted()=>ImportControlService.ValidateDocuments(new(){DocumentEntries=[Doc()]});
 [Fact]public void ADocumentCannotBeRoutedToAnotherProcedure()=>Assert.Throws<ArgumentException>(()=>ImportControlService.ValidateDocuments(new(){DocumentEntries=[Doc() with{Type="FI"}]}));
 [Fact]public void MultipleDraftsOfTheSameKindAreRejected()=>Assert.Throws<ArgumentException>(()=>ImportControlService.ValidateDocuments(new(){DocumentEntries=[Doc(),Doc()]}));
 [Theory][InlineData(-1)][InlineData(1.001)]public void AmountsMustMatchAccountingPrecision(decimal amount)=>Assert.Throws<ArgumentException>(()=>ImportControlService.ValidateDocuments(new(){DocumentEntries=[Doc() with{Dollars=amount}]}));
 [Fact]public void ExchangeRateIsRequired()=>Assert.Throws<ArgumentException>(()=>ImportControlService.ValidateDocuments(new(){DocumentEntries=[Doc() with{ExchangeRate=0}]}));
 [Fact]public void AuxiliarySeriesIsLimitedToThreeWhileInvoiceAllowsFour(){Assert.Throws<ArgumentException>(()=>ImportControlService.ValidateDocuments(new(){DocumentEntries=[Doc() with{Series="1234"}]}));ImportControlService.ValidateDocuments(new(){DocumentEntries=[Doc() with{Kind="FI",Type="FI",Series="1234"}]});}
 [Fact]public void AnArbitraryPolicyIdentifierIsRejected()=>Assert.Throws<ArgumentException>(()=>ImportControlService.ValidateDocuments(new(){Policy=new("malformed")}));
 [Fact]public void OrderRequiresWarehouseAndCompleteIdentity()=>Assert.Throws<ArgumentException>(()=>ImportControlService.ValidateDocuments(new(){Order=new("","001","123456")}));
}
