using Microsoft.Extensions.DependencyInjection;
namespace Intranet.Service;

public static class DependencyInjection
{
    public static IServiceCollection AddIntranetServices(this IServiceCollection services)
    {
        services.AddScoped<GestionIdentity>(); services.AddScoped<IntranetLogin>();
        services.AddScoped<IAviosService, AviosService>();
        services.AddScoped<IOrderRegistrationApplicationService, OrderRegistrationApplicationService>();
        services.AddScoped<IProviderService, ProviderService>(); services.AddScoped<IImportsService, ImportsService>();
        services.AddScoped<OrderRegistrationService>(); services.AddSingleton<QuantityCalculator>(); services.AddSingleton<ProviderAgentTickets>(); return services;
    }
}
