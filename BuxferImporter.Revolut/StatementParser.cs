using BuxferImporter.Core;
using CsvHelper.Configuration;
using System.Globalization;

namespace BuxferImporter.Revolut;
internal class StatementParser : IStatementParser
{
    public async IAsyncEnumerable<StatementEntry> ParseAsync(Stream source)
    {
        var csvParser = new CsvHelper.CsvParser(new StreamReader(source), new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ",",
            HasHeaderRecord = true,
            IgnoreBlankLines = true,
            TrimOptions = TrimOptions.Trim,
        });

        await csvParser.ReadAsync();

        var index = 0;
        while (await csvParser.ReadAsync())
        {
            var transactionType = MapTransactionType(csvParser[0]);
            var product = csvParser[1].ToString();
            var startDate = ParseDate(csvParser[2]);
            var completedDate = ParseDate(csvParser[3]);
            var description = csvParser[4].ToString();
            var amount = ParseDecimal(csvParser[5]);
            var fee = ParseDecimal(csvParser[6]);
            var currency = csvParser[7].ToString();
            var state = MapTransactionState(csvParser[8]);
            var balance = ParseDecimal(csvParser[9]);


            yield return new StatementEntry()
            {
                Id = index.ToString(),
                TransactionType = transactionType,
                Product = product,
                StartDate = startDate,
                CompletedDate = completedDate,
                Description = description,
                Amount = amount,
                Fee = fee,
                Currency = currency,
                State = state,
                Balance = balance
            };

            index++;
        }

        static DateTimeOffset? ParseDate(string input)
        {
            if (DateTimeOffset.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
            {
                return parsed;
            }

            return default;
        }

        static decimal? ParseDecimal(string input)
        {
            if (decimal.TryParse(input, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }

            return default;
        }

        static TransactionType MapTransactionType(string input)
        {
            return input switch
            {
                "CARD_PAYMENT" => TransactionType.CardPayment,
                "CARD_REFUND" => TransactionType.Refund,
                "TRANSFER" => TransactionType.Transfer,
                _ => throw new NotSupportedException($"Transaction type {input} is not supported")
            };
        }

        static TransactionState MapTransactionState(string input)
        {
            return input switch
            {
                "REVERTED" => TransactionState.Reverted,
                "COMPLETED" => TransactionState.Completed,
                "PENDING" => TransactionState.Pending,
                _ => throw new NotSupportedException($"Transaction state {input} is not supported")
            };
        }
    }
}
