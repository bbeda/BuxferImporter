﻿using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
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

    private record TokenResponse(string Status, string Token);

    private record BuxferResponse<T>
    {
        public T Response { get; init; } = default!;
    }
}

public record BuxferOptions
{
    public static string SectionName { get; } = "Buxfer";

    public required string Email { get; init; }

    public required string Password { get; init; }

    public required string BaseAddress { get; init; }
}

public record BuxferTransaction(
    decimal Id,
    string Description,
    decimal Amount,
    string Date,
    string Type,
    decimal AccountId,
    string Tags)
{
    public string DuplicationKey
    {
        get
        {
            using var memoryStream = new MemoryStream();
            using var binaryWriter = new BinaryWriter(memoryStream);

            binaryWriter.Write(Description);
            binaryWriter.Write(Amount);
            binaryWriter.Write(Date);

            return Convert.ToBase64String(memoryStream.ToArray());
        }
    }
}

internal record TransactionsListResponse(
    string Status,
    int NumTransactions,
    BuxferTransaction[] Transactions);