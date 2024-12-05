using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.ComponentModel;
using System.Net.Http.Json;

namespace BuxferImporter.Buxfer;
public class BuxferHttpClient(
    IHttpClientFactory httpClientFactory,
    IOptions<BuxferOptions> buxferOptions,
    IMemoryCache memoryCache)
{
    public const string HttpClientName = "BuxferApi";

    private const string BuxferTokenKey = "BuxferToken";

    private async Task<string> GetTokenAsync()
    {
        using var httpClient = CreateHttpClient();
        var response = await httpClient.PostAsync($"login?email={buxferOptions.Value.Email}&password={buxferOptions.Value.Password}", null);
        _ = response.EnsureSuccessStatusCode();
        var tokenResponse = await response.Content.ReadFromJsonAsync<BuxferResponse<TokenResponse>>();
        return tokenResponse?.Response.Token!;
    }

    private HttpClient CreateHttpClient() => httpClientFactory.CreateClient(HttpClientName);

    private async Task<string> LoadTokenAsync()
    {
        if (memoryCache.TryGetValue(BuxferTokenKey, out string cachedToken))
        {
            return cachedToken!;
        }

        var token = await GetTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("Failed to get token from Buxfer");
        }

        memoryCache.Set(BuxferTokenKey, token, TimeSpan.FromHours(5));

        return token;
    }

    internal async Task<TransactionsListResponse> LoadTransactionsAsync(
        string accountId,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        int page = 1)
    {
        var token = await LoadTokenAsync();

        var query = $"transactions?token={token}&page={page}&accountId={accountId}";
        query = AddQueryDate(query, "startDate", startDate);
        query = AddQueryDate(query, "endDate", endDate);

        using var httpClient = CreateHttpClient();
        var response = await httpClient.GetAsync(query);
        return (await response!.Content!.ReadFromJsonAsync<BuxferResponse<TransactionsListResponse>>())?.Response!;

        static string AddQueryDate(string query, string key, DateOnly? date)
        {
            if (date.HasValue)
            {
                query += $"&{key}={date.Value:yyyy-MM-dd}";
            }
            return query;
        }
    }

    public async IAsyncEnumerable<BuxferTransaction> LoadAllTransactionsAsync(string accountId, DateOnly? startDate = null, DateOnly? endDate = null)
    {
        var page = 1;
        TransactionsListResponse? response;
        do
        {
            response = await LoadTransactionsAsync(accountId, startDate, endDate, page++);
            foreach (var transaction in response.Transactions)
            {
                yield return transaction;
            }

            if (response.Transactions.Length == 0)
            {
                break;
            }
        } while (true);
    }

    public async Task DeleteTransactionAsync(string transactionId)
    {
        using var httpClient = CreateHttpClient();
        var token = await LoadTokenAsync();
        var response = await httpClient.PostAsync($"delete_transaction?token={token}&id={transactionId}", new StringContent(""));
        _ = response.EnsureSuccessStatusCode();
    }

    public async Task CreateTransactionAsync(NewBuxferTransaction transaction)
    {
        using var httpClient = CreateHttpClient();
        var token = await LoadTokenAsync();

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "description", transaction.Description },
            { "amount", transaction.Amount.ToString() },
            { "date", transaction.Date.ToString("yyyy-MM-dd") },
            { "type", transaction.Type.ToString() },
            { "status", transaction.Status.ToString() },
            { "accountId", transaction.AccountId }
        });

        var response = await httpClient.PostAsync($"transaction_add?token={token}", content);
        _ = response.EnsureSuccessStatusCode();
    }

    public async Task UpdateTransactionAsync(UpdateBuxferTransaction transaction)
    {
        using var httpClient = CreateHttpClient();
        var token = await LoadTokenAsync();

        var content = new Dictionary<string, string?>
        {
            { "Description", transaction.Description },
            { "AccountId", transaction.AccountId },
            { "Id", transaction.Id.ToString()}
        };

        var response = await httpClient.PostAsync(QueryHelpers.AddQueryString($"transaction_edit?token={token}", content), new FormUrlEncodedContent(Enumerable.Empty<KeyValuePair<string, string>>()));
        _ = response.EnsureSuccessStatusCode();
    }

    private record TokenResponse(string Status, string Token);

    private record BuxferResponse<T>
    {
        public T Response { get; init; } = default!;
    }
}

public class UpdateBuxferTransaction
{
    public required string Description { get; init; }

    public required string AccountId { get; init; }

    public required decimal Id { get; init; }
}

public record BuxferOptions
{
    public static string SectionName { get; } = "Buxfer";

    public required string Email { get; init; }

    public required string Password { get; init; }

    public required string BaseAddress { get; init; }
}

public class NewBuxferTransaction
{
    public required string Description { get; init; }

    public required decimal Amount { get; init; }

    public required DateOnly Date { get; init; }

    public required BuxferTransactionType Type { get; init; }

    public required BuxferTransactionStatus Status { get; init; }

    public required string AccountId { get; init; }

    public required string? FromAccountId { get; init; }

    public required string? ToAccountId { get; init; }
}

public class BuxferTransaction
{
    public required decimal Id { get; init; }

    public required string? Description { get; init; }

    public required decimal Amount { get; init; }

    public required DateOnly Date { get; init; }

    public required string Type { get; init; }

    public required decimal AccountId { get; init; }

    public required string? Tags { get; init; }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(Description);
        hashCode.Add(Amount);
        hashCode.Add(Date);
        return hashCode.ToHashCode();
    }

    public override bool Equals(object? obj)
    {
        if (obj is not BuxferTransaction other)
        {
            return false;
        }
        return Description == other.Description &&
            Amount == other.Amount &&
            Date == other.Date;
    }

    public static bool operator ==(BuxferTransaction? left, BuxferTransaction? right) => Equals(left, right);

    public static bool operator !=(BuxferTransaction? left, BuxferTransaction? right) => !Equals(left, right);
}

internal record TransactionsListResponse(
string Status,
int NumTransactions,
BuxferTransaction[] Transactions);

public enum BuxferTransactionType
{
    Income,
    Expense,
    Refund,
    Payment,
    Transfer,
    InvestmentBuy,
    InvestmentSell,
    InvestmentDividend,
    CapitalGain,
    CapitalLoss,
    SharedBill,
    PaidForFriend,
    Settlement,
    Loan
}

public enum BuxferTransactionStatus
{
    Cleared,
    Pending
}