using Intranet.Entity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
namespace Intranet.Api.Tests;
public sealed class ImportControlMvcValidationTests
{
    private static ModelStateDictionary Validate(object model)
    {
        var services=new ServiceCollection();services.AddLogging();services.AddControllers();
        using var provider=services.BuildServiceProvider();
        var context=new ActionContext(new DefaultHttpContext {RequestServices=provider},new RouteData(),new ActionDescriptor(),new ModelStateDictionary());
        provider.GetRequiredService<IObjectModelValidator>().Validate(context,null,"",model);
        return context.ModelState;
    }
    [Fact]
    public void ImportWithAssociatedOrderAndPolicyPassesMvcValidation()
    {
        var request=new ImportControlSave {
            Values=new(){Annex="9DO0",ItemType="371",Currency="USD",Origin="HK"},
            Order=new("C","002","350064"),Policy=new("000000012345")
        };
        Assert.True(Validate(request).IsValid);
    }
    [Fact]
    public void InvalidOrderAndPolicyReturnFieldErrorsInsteadOfThrowing()
    {
        var order=Validate(new ImportControlOrder("","1234","1234567"));
        Assert.False(order.IsValid);Assert.True(order.ContainsKey("Warehouse"));Assert.True(order.ContainsKey("Series"));Assert.True(order.ContainsKey("Number"));
        var policy=Validate(new ImportControlPolicy(""));Assert.False(policy.IsValid);Assert.True(policy.ContainsKey("Correlative"));
    }
    [Fact]
    public void ShipmentAndProvisionUseConstructorValidation()
    {
        Assert.True(Validate(new ImportControlShipmentSave("I","APP","AÉREO PRE PAGADO","")).IsValid);
        Assert.False(Validate(new ImportControlShipmentSave("X","","","")).IsValid);
        Assert.True(Validate(new ImportControlProvision(new string('A',64),"provision")).IsValid);
        var invalid=Validate(new ImportControlProvision("invalid","other"));Assert.False(invalid.IsValid);Assert.True(invalid.ContainsKey("Version"));Assert.True(invalid.ContainsKey("Action"));
    }
}
