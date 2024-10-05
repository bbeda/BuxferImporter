using Microsoft.Extensions.DependencyInjection;

namespace BuxferImporter.Core;
public static class Bootstrapper
{
    public static IServiceCollection RegisterCoreServices(this IServiceCollection services)
    {
        _ = services.AddSingleton<StatementMappingProcessor>();

        return services;
    }
}
