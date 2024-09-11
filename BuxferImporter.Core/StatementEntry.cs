namespace BuxferImporter.Core;
public record StatementEntry
{
    public required string? TransactionType { get; init; }

    public required string? Product { get; init; }

    public required string? Description { get; init; }

    public required string? Amount { get; init; }

    public required string? Fee { get; init; }

    public required string? Currency { get; init; }

    public required string? State { get; init; }

    public required string? Balance { get; init; }

    public required string? StartDate { get; init; }

    public required string? CompletedDate { get; init; }

}
