using BuxferImporter.Buxfer;

namespace BuxferImporter.Core;
public class StatementMappingProcessor(BuxferHttpClient httpClient, IStatementParser statementParser)
{
    private const string AccountId = "1441844";
    private readonly Dictionary<DateOnly, List<BuxferTransaction>> DailyBuxferTransactions = new();
    private readonly List<StatementMappingResult> statementMappingResults = new();

    public async Task ImportAsync(Stream source)
    {
        await foreach (var entry in statementParser.ParseAsync(source))
        {
            var transactionDate = DateOnly.FromDateTime(entry.StartDate!.Value.Date);
            var day = transactionDate.Day;
            var month = transactionDate.Month;
            if (!DailyBuxferTransactions.ContainsKey(transactionDate))
            {
                var transactions = await httpClient.LoadAllTransactionsAsync(AccountId, transactionDate, transactionDate).ToListAsync();
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

        var removedTransactions = new List<BuxferTransaction>();
        foreach (var key in DailyBuxferTransactions.Keys)
        {
            removedTransactions.Clear();
            foreach (var transaction in DailyBuxferTransactions[key])
            {
                statementMappingResults.Add(new StatementMappingResult
                {
                    BuxferTransaction = transaction,
                    Action = StatementMappingAction.Delete
                });
                removedTransactions.Add(transaction);
            }

            DailyBuxferTransactions[key].RemoveAll(t => removedTransactions.Contains(t));
        }

        foreach (var result in statementMappingResults)
        {
            switch (result.Action)
            {
                case StatementMappingAction.Create:
                    await httpClient.CreateTransactionAsync(AccountId, new NewBuxferTransaction()
                    {
                        AccountId = AccountId,
                        Amount = result.StatementEntry.Amount!.Value,
                        Date = DateOnly.FromDateTime(result.StatementEntry.StartDate!.Value.DateTime),
                        Description = result.StatementEntry.Description!,
                        Status = BuxferTransactionStatus.Cleared,
                        Type = result.StatementEntry.Amount!.Value > 0 ? BuxferTransactionType.Income : BuxferTransactionType.Expense
                    });
                    break;
                case StatementMappingAction.Update:
                    await httpClient.UpdateTransactionAsync(AccountId, result.BuxferTransaction!.Id, result.StatementEntry);
                    break;
                case StatementMappingAction.Delete:
                    await httpClient.DeleteTransactionAsync(result.BuxferTransaction!.Id.ToString());
                    break;
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
        Update,
        Delete
    }
