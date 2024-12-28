namespace BuxferImporter.Core;
public record StatementMappingResult
{
    private StatementMappingResult(
        StatementMappingResultType statementMappingResultType,
        string? buxferId,
        StatementEntry? statementEntry,
        IReadOnlyCollection<string>? errors,
        IReadOnlyCollection<UpdatedValueInfo>? updatedValues,
        string? details)
    {
        ResultType = statementMappingResultType;

        BuxferId = buxferId;
        StatementEntry = statementEntry;

        Errors = errors;
        UpdatedValues = updatedValues;
        Details = details;
    }

    public StatementMappingResultType ResultType { get; init; }

    public StatementEntry? StatementEntry { get; init; }

    public string? BuxferId { get; init; }

    public string? StatementId => StatementEntry?.Id;

    public string? Details { get; init; }

    public IReadOnlyCollection<string>? Errors { get; init; }

    public IReadOnlyCollection<UpdatedValueInfo>? UpdatedValues { get; init; }

    public record UpdatedValueInfo(string? OldValue, string? NewValue);

    public static StatementMappingResult Created(string buxferId, StatementEntry statementEntry) => new(StatementMappingResultType.Added, buxferId, statementEntry, null, null, null);

    public static StatementMappingResult Updated(string buxferId, StatementEntry statemenEntry, IReadOnlyCollection<UpdatedValueInfo> updatedValues) => new(StatementMappingResultType.Updated, buxferId, statemenEntry, null, updatedValues, null);

    public static StatementMappingResult Skipped(StatementEntry statementEntry, string? buxferId, string details) => new(StatementMappingResultType.Skipped, buxferId, statementEntry, null, null, details);

    public static StatementMappingResult NoAction(StatementEntry statementEntry, string buxferId, string details) => new(StatementMappingResultType.NoAction, buxferId, statementEntry, null, null, details);

    public static StatementMappingResult Deleted(string buxferId) => new(StatementMappingResultType.Deleted, buxferId, null, null, null, null);

    public static StatementMappingResult Error(StatementEntry statementEntry, string? buxferId, IReadOnlyCollection<string> errors) => new(StatementMappingResultType.Error, buxferId, statementEntry, errors, null, null);
}

public enum StatementMappingResultType
{
    Added,
    Updated,
    Skipped,
    NoAction,
    Deleted,
    Error
}
