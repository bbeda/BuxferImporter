using BuxferImporter.Core;
using Microsoft.Extensions.DependencyInjection;

namespace BuxferImporter.Revolut;
public static class Bootstrapper
{
    public static IServiceCollection RegisterRevolutServices(this IServiceCollection services) => services.AddSingleton<IStatementParser, StatementParser>();
}
