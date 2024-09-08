using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuxferImporter.BuxferClient;
public static class RegisterDependencies
{
    private const string BuxferBaseAddressKey = "Buxfer:BaseAddress";

    public static void Register(IServiceCollection services, IConfiguration configuration)
    {
        var baseAddress = configuration.GetValue<string>(BuxferBaseAddressKey);
        if (string.IsNullOrWhiteSpace(baseAddress))
        {
            throw new InvalidOperationException($"Buxfer base address is not configured. Missing {BuxferBaseAddressKey}");
        }

        services.AddHttpClient<BuxferHttpClient>(client =>
        {
            client.BaseAddress = new Uri(baseAddress);
        });
        services.AddSingleton<BuxferHttpClient>();
    }
}
