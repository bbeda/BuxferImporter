using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BuxferImporter.Buxfer;
public static class RegisterDependencies
{
    public static void AddBuxferServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();

        services.Configure<BuxferOptions>(configuration.GetSection(BuxferOptions.SectionName));

        services.AddHttpClient(BuxferHttpClient.HttpClientName, (services, client) =>
        {
            var buxferOptions = services.GetRequiredService<IOptions<BuxferOptions>>();
            client.BaseAddress = new Uri(buxferOptions.Value.BaseAddress);
        });
        services.AddSingleton<BuxferHttpClient>();
    }
}
