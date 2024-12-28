using Azure.Storage.Blobs;
using BuxferImporter.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BuxferImporter.Workers.Functions;

public class ProcessNewStatement
{
    private readonly StatementMappingProcessor _statementMappingProcessor;
    private readonly IOptions<StatementOption[]> _mappingOptions;
    private readonly ILogger<ProcessNewStatement> _logger;

    public ProcessNewStatement(
        [FromKeyedServices(StatementType.RevolutCsv)] StatementMappingProcessor statementMappingProcessor,
        IOptions<StatementOption[]> mappingOptions,
        ILogger<ProcessNewStatement> logger)
    {
        _statementMappingProcessor = statementMappingProcessor;
        _mappingOptions = mappingOptions;
        _logger = logger;
    }

    [Function(nameof(ProcessNewStatement))]
    public async Task Run([BlobTrigger("statements", Connection = "AzureWebJobsStorage")] BlobClient blobClient, FunctionContext functionContext)
    {
        using var stream = await blobClient.OpenReadAsync();
        var skippedCount = 0;
        var totalCount = 0;
        await foreach (var result in _statementMappingProcessor.ImportAsync("1441844", stream))
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
