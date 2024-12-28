using BuxferImporter.Buxfer;

namespace BuxferImporter.Core;
public class StatementMappingProcessor(BuxferClient httpClient, IStatementParser statementParser)
{
    private readonly Dictionary<DateOnly, List<BuxferTransaction>> DailyBuxferTransactions = new();
    private readonly List<StatementMappingOperation> statementMappingOperations = new();

    public async IAsyncEnumerable<StatementMappingResult> ImportAsync(string accountId, Stream source)
    {
        await foreach (var entry in statementParser.ParseAsync(source))
        {
            if (entry.State != TransactionState.Completed)
            {
                yield return StatementMappingResult.Skipped(entry, default, $"State: {(entry.State?.ToString() ?? "Missing")}");
                continue;
            }

            var transactionDate = DateOnly.FromDateTime(entry.StartDate!.Value.Date);
            var day = transactionDate.Day;
            var month = transactionDate.Month;
            if (!DailyBuxferTransactions.ContainsKey(transactionDate))
            {
                var transactions = await httpClient.LoadAllTransactionsAsync(accountId, transactionDate, transactionDate).ToListAsync();
                DailyBuxferTransactions[transactionDate] = transactions;
            }

            var todayTransactions = DailyBuxferTransactions[transactionDate];
            var matchingTransaction = FindMatchingTransaction(entry, todayTransactions);

            switch (matchingTransaction)
            {
                case null:
                    statementMappingOperations.Add(new StatementMappingOperation
                    {
                        StatementEntry = entry,
                        Action = StatementMappingAction.Create
                    });
                    break;
                default:
                    statementMappingOperations.Add(new StatementMappingOperation
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
                statementMappingOperations.Add(new StatementMappingOperation
                {
                    BuxferTransaction = transaction,
                    Action = StatementMappingAction.Delete
                });
                removedTransactions.Add(transaction);
            }

            DailyBuxferTransactions[key].RemoveAll(t => removedTransactions.Contains(t));
        }
;
        foreach (var operation in statementMappingOperations)
        {
            switch (operation.Action)
            {
                case StatementMappingAction.Create:
                    var createResponse = await httpClient.CreateTransactionAsync(new NewBuxferTransaction()
                    {
                        AccountId = accountId,
                        Amount = operation.StatementEntry.Amount!.Value,
                        Date = DateOnly.FromDateTime(operation.StatementEntry.StartDate!.Value.DateTime),
                        Description = operation.StatementEntry.Description!,
                        Status = BuxferTransactionStatus.Cleared,
                        Type = operation.StatementEntry.Amount!.Value > 0 ? BuxferTransactionType.Income : BuxferTransactionType.Expense,
                        FromAccountId = default,
                        ToAccountId = default,
                    });
                    yield return MapBuxferResponse(operation, createResponse, () => StatementMappingResult.Created(createResponse.Id!, operation.StatementEntry));
                    break;
                case StatementMappingAction.Update:
                    var updateResponse = await httpClient.UpdateTransactionAsync(new UpdateBuxferTransaction()
                    {
                        AccountId = accountId,
                        Description = operation.StatementEntry.Description,
                        Id = operation.BuxferTransaction!.Id
                    });

                    IReadOnlyCollection<StatementMappingResult.UpdatedValueInfo> updatedProperties = [new StatementMappingResult.UpdatedValueInfo(operation.BuxferTransaction.Description, operation.StatementEntry.Description)];

                    yield return MapBuxferResponse(operation, updateResponse, () => StatementMappingResult.Updated(
                        updateResponse.Id!,
                        operation.StatementEntry,
                        updatedProperties));
                    break;
                case StatementMappingAction.Delete:
                    var deleteResponse = await httpClient.DeleteTransactionAsync(operation.BuxferTransaction!.Id.ToString());
                    yield return MapBuxferResponse(operation, deleteResponse, () => StatementMappingResult.Deleted(operation.BuxferTransaction.Id.ToString()));
                    break;
                case StatementMappingAction.None:
                    yield return StatementMappingResult.Skipped(operation.StatementEntry, operation.BuxferTransaction?.Id.ToString(), "Nothing changed");
                    break;
                default:
                    throw new InvalidOperationException($"Unknown action: {operation.Action}");
            }
        }

        static StatementMappingResult MapBuxferResponse(StatementMappingOperation result, BuxferResponse response, Func<StatementMappingResult> success)
        {
            return response.Status switch
            {
                ResponseStatus.Success => success(),
                ResponseStatus.Error => StatementMappingResult.Error(result.StatementEntry, null, [response.Message]),
                _ => throw new InvalidOperationException($"Unknown response status: {response.Status}")//TODO: handle this case
            };
        }
    }

    private BuxferTransaction? FindMatchingTransaction(StatementEntry entry, IEnumerable<BuxferTransaction> transactions)
    {
        var similarTransactions = transactions.Where(t => entry.IsEquivalentWith(t));
        var maxSimilarity = 0d;
        var bestMatch = default(BuxferTransaction);
        foreach (var transaction in similarTransactions)
        {
            if (entry.Description == transaction.Description)
            {
                return transaction;
            }

            var similarity = CalculateSimilarity(entry.Description ?? string.Empty, transaction.Description ?? string.Empty);

            if (similarity >= maxSimilarity)
            {
                maxSimilarity = similarity;
                bestMatch = transaction;
            }
        }

        return bestMatch;
    }

    private static int GetLevenshteinDistance(string s, string t)
    {
        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];

        if (n == 0)
        {
            return m;
        }

        if (m == 0)
        {
            return n;
        }

        for (int i = 0; i <= n; i++)
        {
            d[i, 0] = i;
        }

        for (int j = 0; j <= m; j++)
        {
            d[0, j] = j;
        }

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }

    private static double CalculateSimilarity(string s, string t)
    {
        s = s.ToLowerInvariant();
        t = t.ToLowerInvariant();
        int maxLen = Math.Max(s.Length, t.Length);
        if (maxLen == 0)
        {
            return 1.0; // Both strings are empty
        }

        int distance = GetLevenshteinDistance(s, t);
        return (1.0 - (double)distance / maxLen);
    }

    internal record StatementMappingOperation
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
}
