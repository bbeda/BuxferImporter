using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;

namespace BuxferImporter.Buxfer.Tests;

[TestClass]
internal class BuxferHttpClientTests
{
    private readonly HttpClient httpClient;
    private readonly IOptions<BuxferOptions> buxferOptions = Options.Create(new BuxferOptions()
    {
        Email = "email",
        Password = "password"
    });
    private readonly IMemoryCache memoryCache;

    private readonly BuxferHttpClient buxferHttpClient;

    public BuxferHttpClientTests()
    {
        var config=new ConfigurationBuilder()
            .AddUserSecrets<BuxferHttpClientTests>()
            .Build();

        httpClient = new HttpClient()
        {
            BaseAddress = new Uri("https://www.buxfer.com/api/")
        };

        memoryCache = new Mock<IMemoryCache>().Object;

        buxferHttpClient = new BuxferHttpClient(httpClient, buxferOptions, memoryCache);
    }

    [TestMethod]
    public async Task LoadAllTransactionsAsync()
    {
        var transactions = buxferHttpClient.LoadAllTransactionsAsync("product", DateOnly.FromDateTime(DateTime.Now), DateOnly.FromDateTime(DateTime.Now)).ToListAsync();
        Assert.IsNotNull(transactions);
    }
}
