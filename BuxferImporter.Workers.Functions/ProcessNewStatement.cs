using Azure.Storage.Blobs;
using BuxferImporter.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO.Enumeration;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BuxferImporter.Workers.Functions;

public class ProcessNewStatement
{
    private readonly IServiceProvider serviceProvider;
    private readonly IOptions<StatementOptions> _mappingOptions;
    private readonly ILogger<ProcessNewStatement> _logger;

    public ProcessNewStatement(
        IServiceProvider serviceProvider,
        IOptions<StatementOptions> mappingOptions,
        ILogger<ProcessNewStatement> logger)
    {
        this.serviceProvider = serviceProvider;
        _mappingOptions = mappingOptions;
        _logger = logger;
    }

    [Function(nameof(ProcessNewStatement))]
    public async Task Run([BlobTrigger("statements", Connection = "AzureWebJobsStorage")] BlobClient inputBlobClient, FunctionContext functionContext)
    {
        var matchingMapping = _mappingOptions.Value.Entries.FirstOrDefault(x => FileSystemName.MatchesSimpleExpression(x.File, inputBlobClient.Name, true));

        var blobServiceClient = new BlobServiceClient("UseDevelopmentStorage=true");
        var containerClient = blobServiceClient.GetBlobContainerClient("output");
        _ = await containerClient.CreateIfNotExistsAsync();

        var fileName = $"{inputBlobClient.Name}-{DateTime.UtcNow:yyyyMMddHHmmss}.txt";
        var blobClientOutput = containerClient.GetBlobClient(fileName);

        using var outputStream = await blobClientOutput.OpenWriteAsync(true);
        using var writer = new StreamWriter(outputStream, leaveOpen: true);

        await writer.WriteLineAsync($"Processing {inputBlobClient.Name}");

        if (matchingMapping == null)
        {
            _logger.LogWarning($"No mapping found for {inputBlobClient.Name}");
            await writer.WriteLineAsync($"No mapping found");
            return;
        }
        await writer.WriteLineAsync(matchingMapping.ToString());
        await writer.FlushAsync();

        var processor = serviceProvider.GetRequiredKeyedService<StatementMappingProcessor>(matchingMapping.StatementType);

        using var stream = await inputBlobClient.OpenReadAsync();
        var skippedCount = 0;
        var totalCount = 0;

        await foreach (var result in processor.ImportAsync(matchingMapping.Account, stream))
        {
            totalCount++;
            if (result.ResultType == StatementMappingResultType.Skipped)
            {
                skippedCount++;
                continue;
            }
            _logger.LogInformation($"{result.ResultType} {result.StatementId} {result.BuxferId} {result.Details}");
            await writer.WriteLineAsync(result.ToString());
            await writer.FlushAsync();
        }
        var footer = $"Total:{totalCount}, Skipped:{skippedCount}";
        _logger.LogInformation(footer);
        await writer.WriteLineAsync(footer);
    }
}
