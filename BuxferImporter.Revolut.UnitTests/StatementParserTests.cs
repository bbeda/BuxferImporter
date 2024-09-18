namespace BuxferImporter.Revolut.UnitTests;

[TestClass]
public sealed class StatementParserTests
{
    [TestMethod]
    public async Task TestMethod1()
    {
        var stream = File.OpenRead(@".\SampleFiles\real_statement.csv");
        var parser = new StatementParser();

        var entries = await parser.ParseAsync(stream).ToListAsync();
        var types = entries.Select(e => e.TransactionType).Distinct();
    }
}
