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

}
