using BuxferImporter.Core;
using BuxferImporter.Revolut;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var hostBuilder = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureAppConfiguration((context, builder) =>
    {
        if (context.HostingEnvironment.IsDevelopment())
        {
            _ = builder.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
            _ = builder.AddUserSecrets<Program>();
        }
    })
    .ConfigureServices((context, services) =>
    {
        _ = services.AddMemoryCache();
        _ = services.RegisterRevolutServices();
        _ = services.RegisterCoreServices(context.Configuration);
    });

// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();

hostBuilder.Build().Run();
