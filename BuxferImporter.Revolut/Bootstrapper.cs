using BuxferImporter.Core;
using Microsoft.Extensions.DependencyInjection;

namespace BuxferImporter.Revolut;
public static class Bootstrapper
{
    public static IServiceCollection RegisterRevolutServices(this IServiceCollection services) => services.AddKeyedSingleton<IStatementParser, StatementParser>(StatementType.RevolutCsv);
}
