using BuxferImporter.Buxfer;
using BuxferImporter.Core;
using BuxferImporter.Revolut;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var appBuilder = Host.CreateApplicationBuilder();
appBuilder.Services.AddBuxferServices(appBuilder.Configuration);
appBuilder.Services.AddMemoryCache();
appBuilder.Services.RegisterCoreServices();
appBuilder.Services.RegisterRevolutServices();

var app = appBuilder.Build();

app.Start();

using var data = File.OpenRead(Path.Combine(@".\SampleFiles", "statement1.csv"));
var importer = app.Services.GetRequiredService<StatementMappingProcessor>();
await importer.ImportAsync(data);
