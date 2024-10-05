using BuxferImporter.Buxfer;

namespace BuxferImporter.Core;
public class Importer(BuxferHttpClient httpClient, IStatementParser statementParser)
{
    private readonly Dictionary<DateOnly, BuxferTransaction[]> DailyBuxferTransactions = new();

    public async Task ImportAsync(Stream source)
    {
        await foreach (var entry in statementParser.ParseAsync(source))
        {
            var transactionDate = DateOnly.FromDateTime(entry.StartDate!.Value.Date);
            if (!DailyBuxferTransactions.ContainsKey(transactionDate))
            {
                var transactions = await httpClient.LoadAllTransactionsAsync("1441844", transactionDate, transactionDate).ToListAsync();
                DailyBuxferTransactions[transactionDate] = transactions.ToArray();
            }
        }
    }



}
