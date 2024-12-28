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

using var data = File.OpenRead(Path.Combine(@".\SampleFiles", "statement3.csv"));
var importer = app.Services.GetRequiredKeyedService<StatementMappingProcessor>(StatementType.RevolutCsv);

var outputFileName = $".\\out{DateTime.Now:yyyyMMddhhmm}.txt";
using var file = File.CreateText(outputFileName);

var stats = new Dictionary<DateOnly, Dictionary<StatementMappingResultType, int>>();
var skippedCount = 0;
var totalCount = 0;
await foreach (var result in importer.ImportAsync("1441844", data))
{
    totalCount++;
    if (result.ResultType == StatementMappingResultType.Skipped)
    {
        skippedCount++;
        continue;
    }
    Console.WriteLine($"{result.ResultType} {result.StatementId} {result.BuxferId} {result.Details}");

    await file.WriteLineAsync(result.ToString());
}
var footer = $"Total:{totalCount}, Skipped:{skippedCount}";
await file.WriteLineAsync(footer);

Console.WriteLine(footer);
