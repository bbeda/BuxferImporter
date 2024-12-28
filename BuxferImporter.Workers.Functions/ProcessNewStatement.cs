using Azure.Storage.Blobs;
using BuxferImporter.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ComponentModel.Design;
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
    public async Task Run([BlobTrigger("statements", Connection = "AzureWebJobsStorage")] BlobClient blobClient, FunctionContext functionContext)
    {
        var matchingMapping = _mappingOptions.Value.Entries.FirstOrDefault(x => x.File == blobClient.Name);

        if (matchingMapping == null)
        {
            _logger.LogWarning($"No mapping found for {blobClient.Name}");
            return;
        }

        var processor = serviceProvider.GetRequiredKeyedService<StatementMappingProcessor>(matchingMapping.StatementType);

        using var stream = await blobClient.OpenReadAsync();
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


        }
        var footer = $"Total:{totalCount}, Skipped:{skippedCount}";
        _logger.LogInformation(footer);
    }
}
