namespace BuxferImporter.Core;

public interface IStatementParser
{
    IAsyncEnumerable<StatementEntry> ParseAsync(Stream source);

}
