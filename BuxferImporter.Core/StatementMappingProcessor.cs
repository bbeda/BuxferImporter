using BuxferImporter.Buxfer;

namespace BuxferImporter.Core;
public class StatementMappingProcessor(BuxferHttpClient httpClient, IStatementParser statementParser)
{
    private readonly Dictionary<DateOnly, List<BuxferTransaction>> DailyBuxferTransactions = new();
    private readonly List<StatementMappingResult> statementMappingResults = new();

    public async Task ImportAsync(Stream source)
    {
        await foreach (var entry in statementParser.ParseAsync(source))
        {
            var transactionDate = DateOnly.FromDateTime(entry.StartDate!.Value.Date);
            if (!DailyBuxferTransactions.ContainsKey(transactionDate))
            {
                var transactions = await httpClient.LoadAllTransactionsAsync("1441844", transactionDate, transactionDate).ToListAsync();
                DailyBuxferTransactions[transactionDate] = transactions;
            }

            var todayTransactions = DailyBuxferTransactions[transactionDate];
            var matchingTransaction = todayTransactions.FirstOrDefault(t => entry.IsEquivalentWith(t));

            switch (matchingTransaction)
            {
                case null:
                    statementMappingResults.Add(new StatementMappingResult
                    {
                        StatementEntry = entry,
                        Action = StatementMappingAction.Create
                    });
                    break;
                default:
                    statementMappingResults.Add(new StatementMappingResult
                    {
                        StatementEntry = entry,
                        BuxferTransaction = matchingTransaction,
                        Action = entry.Description == matchingTransaction.Description ? StatementMappingAction.None : StatementMappingAction.Update
                    });
                    todayTransactions.Remove(matchingTransaction);
                    break;
            }

        }
    }
}

internal record StatementMappingResult
{
    public StatementEntry StatementEntry { get; init; } = default!;

    public BuxferTransaction? BuxferTransaction { get; init; }

    public StatementMappingAction Action { get; init; }
}

internal enum StatementMappingAction
{
    None,
    Create,
    Update
}
