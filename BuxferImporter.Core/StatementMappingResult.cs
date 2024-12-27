namespace BuxferImporter.Core;
public record StatementMappingResult
{
    private StatementMappingResult(
        StatementMappingResultType statementMappingResultType,
        string? buxferId,
        string? statementId,
        IReadOnlyCollection<string>? errors,
        IReadOnlyCollection<UpdatedValueInfo>? updatedValues)
    {
        ResultType = statementMappingResultType;
        BuxferId = buxferId;
        StatementId = statementId;
        Errors = errors;
        UpdatedValues = updatedValues;
    }

    public StatementMappingResultType ResultType { get; init; }

    public string? BuxferId { get; init; }

    public string? StatementId { get; init; }

    public IReadOnlyCollection<string>? Errors { get; init; }

    public IReadOnlyCollection<UpdatedValueInfo>? UpdatedValues { get; init; }

    public record UpdatedValueInfo(string? OldValue, string? NewValue);

    public static StatementMappingResult Created(string buxferId, string statementId)
    {
        return new StatementMappingResult(StatementMappingResultType.Added, buxferId, statementId, null, null);
    }

    public static StatementMappingResult Updated(string buxferId, string statementId, IReadOnlyCollection<UpdatedValueInfo> updatedValues)
    {
        return new StatementMappingResult(StatementMappingResultType.Updated, buxferId, statementId, null, updatedValues);
    }

    public static StatementMappingResult Skipped(string statementId)
    {
        return new StatementMappingResult(StatementMappingResultType.Skipped, null, statementId, null, null);
    }

    public static StatementMappingResult NoAction(string statementId, string buxferId)
    {
        return new StatementMappingResult(StatementMappingResultType.NoAction, buxferId, statementId, null, null);
    }

    public static StatementMappingResult Deleted(string buxferId)
    {
        return new StatementMappingResult(StatementMappingResultType.Deleted, buxferId, null, null, null);
    }

    public static StatementMappingResult Error(string? statementId, string? buxferId, IReadOnlyCollection<string> errors)
    {
        return new StatementMappingResult(StatementMappingResultType.Error, buxferId, statementId, errors, null);
    }
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
