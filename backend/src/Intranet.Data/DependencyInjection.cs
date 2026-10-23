using Intranet.Entity;
using Microsoft.Extensions.DependencyInjection;
namespace Intranet.Data;

public static class DependencyInjection
{
    public static IServiceCollection AddIntranetData(this IServiceCollection services)
    {
        services.AddScoped<GestionConnectionFactory>();
        services.AddScoped<IGestionLoginRepository, GestionLoginRepository>();
        services.AddScoped<IGestionAccessRepository, GestionAccessRepository>();
        services.AddScoped<IAviosRepository, AviosRepository>();
        services.AddScoped<IRegistrationReadRepository, RegistrationReadRepository>();
        services.AddScoped<IImportsRepository, ImportsRepository>();
        services.AddScoped<IDyeingRepository, DyeingRepository>();
        services.AddScoped<IQualityControlRepository, QualityControlRepository>();
        services.AddScoped<IQualityEventRepository, QualityEventRepository>();
        services.AddScoped<IOrderWriteGateway, SqlOrderWriteGateway>(); return services;
    }
}
