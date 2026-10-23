using Intranet.Entity;
using Intranet.Data;
using Intranet.Service;
using Intranet.Api.Controllers;
using System.Reflection;
namespace Intranet.Api.Tests;

public sealed class LayerDependenciesTests
{
    [Fact] public void EntityDoesNotReferenceOtherLayers() => Assert.DoesNotContain(typeof(PurchaseOrder).Assembly.GetReferencedAssemblies(), a => a.Name is "Intranet.Api" or "Intranet.Data" or "Intranet.Service");
    [Fact] public void DataDoesNotReferenceServiceOrApi() => Assert.DoesNotContain(typeof(AviosRepository).Assembly.GetReferencedAssemblies(), a => a.Name is "Intranet.Api" or "Intranet.Service");
    [Fact] public void ServiceDoesNotReferenceConcreteDataOrHttp() => Assert.DoesNotContain(typeof(ProviderService).Assembly.GetReferencedAssemblies(), a => a.Name is "Intranet.Api" or "Intranet.Data" || a.Name!.StartsWith("Microsoft.AspNetCore"));
    [Theory]
    [InlineData(typeof(AviosController))]
    [InlineData(typeof(OrderRegistrationController))]
    [InlineData(typeof(ImportsController))]
    [InlineData(typeof(DyeingController))]
    [InlineData(typeof(QualityControlController))]
    [InlineData(typeof(ProviderController))]
    public void ControllersUseServiceInterfaces(Type controller) => Assert.All(controller.GetConstructors().Single().GetParameters(), p => { Assert.True(p.ParameterType.IsInterface); Assert.Equal("Intranet.Service", p.ParameterType.Assembly.GetName().Name); });
}
