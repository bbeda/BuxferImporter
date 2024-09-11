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

        while (await csvParser.ReadAsync())
        {

            var transactionType = csvParser[0].ToString();
            var product = csvParser[1].ToString();
            var startDate = csvParser[2].ToString();
            var completedDate = csvParser[3].ToString();
            var description = csvParser[4].ToString();
            var amount = csvParser[5].ToString();
            var fee = csvParser[6].ToString();
            var currency = csvParser[7].ToString();
            var state = csvParser[8].ToString();
            var balance = csvParser[9].ToString();


            yield return new StatementEntry()
            {
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

        }
    }
}
