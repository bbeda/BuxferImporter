using BuxferImporter.Buxfer;

namespace BuxferImporter.Core;
public record StatementEntry
{
    public required TransactionType? TransactionType { get; init; }

    public required string? Product { get; init; }

    public required string? Description { get; init; }

    public required decimal? Amount { get; init; }

    public required decimal? Fee { get; init; }

    public required string? Currency { get; init; }

    public required TransactionState? State { get; init; }

    public required decimal? Balance { get; init; }

    public required DateTimeOffset? StartDate { get; init; }

    public required DateTimeOffset? CompletedDate { get; init; }

    public bool IsEquivalentWith(BuxferTransaction buxferTransaction)
    {
        var startDate = DateOnly.FromDateTime(StartDate!.Value.DateTime);

        double similarity = 1;

        if (Description == buxferTransaction.Description)
        {
            similarity = 1;
        }
        else
        {
            similarity = CalculateSimilarity(Description ?? string.Empty, buxferTransaction?.Description ?? string.Empty);
        }

        return Math.Abs(Amount!.Value) == buxferTransaction.Amount &&
            startDate == buxferTransaction.Date;
    }

    public static int GetLevenshteinDistance(string s, string t)
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

    public static double CalculateSimilarity(string s, string t)
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
}