using Microsoft.Extensions.DependencyInjection;
namespace Intranet.Service;

public static class DependencyInjection
{
    public static IServiceCollection AddIntranetServices(this IServiceCollection services)
    {
        services.AddScoped<GestionIdentity>();
        services.AddScoped<IAnalisisRbOperationsService, AnalisisRbOperationsService>();
        services.AddScoped<IAnalisisRbImageService, AnalisisRbImageService>();
        services.AddScoped<IAnalisisRbService, AnalisisRbService>();
        services.AddScoped<IAnalisisRbHistoryService, AnalisisRbHistoryService>();
        services.AddScoped<IPortalAccessService, PortalAccessService>(); services.AddScoped<IntranetLogin>();
        services.AddScoped<IAviosService, AviosService>();
        services.AddScoped<IDyeingService, DyeingService>();
        services.AddScoped<IQualityControlService, QualityControlService>();
        services.AddScoped<IQualityEventService, QualityEventService>();
        services.AddScoped<IOrderRegistrationApplicationService, OrderRegistrationApplicationService>();
        services.AddScoped<IProviderService, ProviderService>(); services.AddScoped<IImportsService, ImportsService>();
        services.AddScoped<IImportControlService, ImportControlService>();
        services.AddScoped<OrderRegistrationService>(); services.AddSingleton<QuantityCalculator>(); services.AddSingleton<ProviderAgentTickets>(); return services;
    }
}
