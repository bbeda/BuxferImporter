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

using var data = File.OpenRead(Path.Combine(@".\SampleFiles", "real_statement.csv"));
var importer = app.Services.GetRequiredService<Importer>();
await importer.ImportAsync(data);
