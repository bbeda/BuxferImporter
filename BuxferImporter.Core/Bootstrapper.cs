using BuxferImporter.Buxfer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuxferImporter.Core;
public static class Bootstrapper
{
    public static IServiceCollection RegisterCoreServices(this IServiceCollection services, IConfiguration configuration)
    {
        _ = services.AddBuxferServices(configuration);
        _ = services.Configure<StatementOptions>(configuration.GetSection("Statements"));


        _ = services.AddKeyedSingleton(StatementType.RevolutCsv, (svcs, _) =>
        {
            var revolutParser = svcs.GetRequiredKeyedService<IStatementParser>(StatementType.RevolutCsv);
            var client = svcs.GetRequiredService<BuxferClient>();
            return new StatementMappingProcessor(client, revolutParser);
        });

        return services;
    }
}
