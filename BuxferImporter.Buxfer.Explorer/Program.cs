using BuxferImporter.Buxfer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var appBuilder = Host.CreateApplicationBuilder();
appBuilder.Services.AddBuxferServices(appBuilder.Configuration);
appBuilder.Services.AddMemoryCache();

var app = appBuilder.Build();

app.Start();

var buxferHttpClient = app.Services.GetRequiredService<BuxferHttpClient>();
var transactions = buxferHttpClient.LoadAllTransactionsAsync("1441844", new DateOnly(2024,04,19), new DateOnly(2024, 04, 19));

await foreach (var transaction in transactions)
{
    Console.WriteLine(transaction);
}
