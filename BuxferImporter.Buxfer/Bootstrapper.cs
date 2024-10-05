using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BuxferImporter.Buxfer;
public static class Bootstrapper
{
    public static IServiceCollection AddBuxferServices(this IServiceCollection services, IConfiguration configuration)
    {
        _ = services.AddMemoryCache();

        _ = services.Configure<BuxferOptions>(configuration.GetSection(BuxferOptions.SectionName));

        _ = services.AddHttpClient(BuxferHttpClient.HttpClientName, (services, client) =>
        {
            var buxferOptions = services.GetRequiredService<IOptions<BuxferOptions>>();
            client.BaseAddress = new Uri(buxferOptions.Value.BaseAddress);
        });
        _ = services.AddSingleton<BuxferHttpClient>();

        return services;
    }
}
