using Intranet.Data;using Intranet.Entity;
namespace Intranet.Api.Tests;
public sealed class AnalisisRbVariantMessagesTests {
 private static AnalisisRbVariant[] Variants=>[new("C","SHORT","P")];
 [Fact]public void J3237AllowsManualChoiceWithoutAssignment()=>Assert.Equal("",AnalisisRbOperationsRepository.ValidateVariant("J3237","B4902"," ",null,Variants));
 [Fact]public void AssignedVariantLoadsByDefault()=>Assert.Equal("C",AnalisisRbOperationsRepository.ValidateVariant("J3237","B4902","C",null,Variants));

 [Fact]public void MissingTariffIsExplained(){var e=Assert.Throws<ArgumentException>(()=>AnalisisRbOperationsRepository.ValidateVariant("J3237"," ","",null,[]));Assert.Contains("no tiene un tarifado asociado",e.Message);}
 [Fact]public void MissingVariantCatalogIsExplained(){var e=Assert.Throws<ArgumentException>(()=>AnalisisRbOperationsRepository.ValidateVariant("J3237","B4902","C",null,[]));Assert.Contains("no tiene variantes registradas",e.Message);}
 [Fact]public void InvalidAssignedVariantIsExplained(){var e=Assert.Throws<ArgumentException>(()=>AnalisisRbOperationsRepository.ValidateVariant("J3237","B4902","0",null,Variants));Assert.Contains("variante asignada 0",e.Message);Assert.Contains("no existe",e.Message);}
 [Fact]public void ProvisionalValidVariantIsStillAllowed()=>Assert.Equal("C",AnalisisRbOperationsRepository.ValidateVariant("J3237","B4902","C",null,Variants));
}
