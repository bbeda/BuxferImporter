using BuxferImporter.Buxfer;
using BuxferImporter.Core;
using BuxferImporter.Revolut;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var appBuilder = Host.CreateApplicationBuilder();

appBuilder.Services.AddMemoryCache();
appBuilder.Services.RegisterRevolutServices();

appBuilder.Services.RegisterCoreServices(appBuilder.Configuration);

var app = appBuilder.Build();

app.Start();

using var data = File.OpenRead(Path.Combine(@".\SampleFiles", "statement1.csv"));
var importer = app.Services.GetRequiredKeyedService<StatementMappingProcessor>(StatementType.RevolutCsv);
await foreach (var result in importer.ImportAsync("1441844", data))
{
    Console.WriteLine(result);
}
