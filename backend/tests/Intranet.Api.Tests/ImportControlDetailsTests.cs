using Intranet.Entity;
using Intranet.Service;
using Intranet.Data;
namespace Intranet.Api.Tests;
public sealed class ImportControlDetailsTests
{
 static ImportControlConcept Concept(string code="001")=>new(code,"VALOR FOB",100m,350m,3.5m,0m,"PR-001");
 [Fact]public void OlderClientsCanLeaveChildCollectionsUntouched()=>ImportControlService.ValidateChildren(new());
 [Fact]public void ConceptCodeCannotBeRepeated()=>Assert.Throws<ArgumentException>(()=>ImportControlService.ValidateChildren(new(){Concepts=[Concept(),Concept(" 001 ")]}));
 [Fact]public void NumericPrecisionIsNotSilentlyRounded(){Assert.Throws<ArgumentException>(()=>ImportControlService.ValidateChildren(new(){Concepts=[Concept() with{Dollars=1.001m}]}));Assert.Throws<ArgumentException>(()=>ImportControlService.ValidateChildren(new(){Concepts=[Concept() with{ExchangeRate=3.1234m}]}));}
 [Fact]public void AmountRequiresPositiveExchangeRate()=>Assert.Throws<ArgumentException>(()=>ImportControlService.ValidateChildren(new(){Concepts=[Concept() with{ExchangeRate=0}]}));
 [Fact]public void DescriptionDoesNotCauseAnUnnecessaryConceptWrite()=>Assert.True(ImportControlRepository.SameConcept(Concept(),Concept() with{Description="Descripción del catálogo actualizada"}));
 [Fact]public void ProformaNumberChangesArePersisted()=>Assert.False(ImportControlRepository.SameConcept(Concept(),Concept() with{DocumentNumber="PR-002"}));
 [Fact]public void ExistingPaymentSequencesCannotBeDuplicated()=>Assert.Throws<ArgumentException>(()=>ImportControlService.ValidateChildren(new(){Conditions=[new("001","Una"),new("001","Otra")]}));
 [Fact]public void MultipleNewPaymentRowsHaveNoAssignedSequence()=>ImportControlService.ValidateChildren(new(){Conditions=[new("","Una"),new("","Otra")]});
 [Fact]public void PaymentDescriptionIsRequired()=>Assert.Throws<ArgumentException>(()=>ImportControlService.ValidateChildren(new(){Conditions=[new("001","   ")]}));
 [Fact]public void PaymentDescriptionHonorsLegacyColumnLength()=>Assert.Throws<ArgumentException>(()=>ImportControlService.ValidateChildren(new(){Conditions=[new("001",new string('x',101))]}));
 [Fact]public void NullRowsAreRejectedBeforeRepositoryAccess(){Assert.Throws<ArgumentException>(()=>ImportControlService.ValidateChildren(new(){Concepts=[null!]}));Assert.Throws<ArgumentException>(()=>ImportControlService.ValidateChildren(new(){Conditions=[null!]}));}
}
