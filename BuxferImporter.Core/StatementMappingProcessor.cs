﻿using BuxferImporter.Buxfer;

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
            if (entry.State != TransactionState.Completed)
            {
                continue;
            }

            var transactionDate = DateOnly.FromDateTime(entry.StartDate!.Value.Date);
            var day = transactionDate.Day;
            var month = transactionDate.Month;
            if (!DailyBuxferTransactions.ContainsKey(transactionDate))
            {
                var transactions = await httpClient.LoadAllTransactionsAsync(AccountId, transactionDate, transactionDate).ToListAsync();
                DailyBuxferTransactions[transactionDate] = transactions;
            }

            var todayTransactions = DailyBuxferTransactions[transactionDate];
            var matchingTransaction = FindMatchingTransaction(entry, todayTransactions);

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
;
        foreach (var result in statementMappingResults)
        {
            switch (result.Action)
            {
                case StatementMappingAction.Create:
                    await httpClient.CreateTransactionAsync(new NewBuxferTransaction()
                    {
                        AccountId = AccountId,
                        Amount = result.StatementEntry.Amount!.Value,
                        Date = DateOnly.FromDateTime(result.StatementEntry.StartDate!.Value.DateTime),
                        Description = result.StatementEntry.Description!,
                        Status = BuxferTransactionStatus.Cleared,
                        Type = result.StatementEntry.Amount!.Value > 0 ? BuxferTransactionType.Income : BuxferTransactionType.Expense,
                        FromAccountId = default,
                        ToAccountId = default,
                    });
                    break;
                case StatementMappingAction.Update:
                    await httpClient.UpdateTransactionAsync(new UpdateBuxferTransaction()
                    {
                        AccountId = AccountId,
                        Description = result.StatementEntry.Description,
                        Id = result.BuxferTransaction!.Id
                    });
                    break;
                case StatementMappingAction.Delete:
                    await httpClient.DeleteTransactionAsync(result.BuxferTransaction!.Id.ToString());
                    break;
            }
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
}
