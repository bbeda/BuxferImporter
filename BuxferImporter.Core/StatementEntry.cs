using BuxferImporter.Buxfer;

namespace BuxferImporter.Core;
public class StatementEntry : IEquatable<BuxferTransaction>
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

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(Description);
        hashCode.Add(Amount);
        hashCode.Add(StartDate);
        return hashCode.ToHashCode();
    }

    public override bool Equals(object? obj)
    {
        if (obj is not StatementEntry other)
        {
            return false;
        }

        if (obj is BuxferTransaction buxferTransaction)
        {
            return obj.Equals(buxferTransaction);
        }

        return Description == other.Description &&
            Amount == other.Amount &&
            StartDate == other.StartDate;
    }

    public bool Equals(BuxferTransaction? other)
    {
        if (other is null)
        {
            return false;
        }

        var startDate = DateOnly.FromDateTime(StartDate!.Value.DateTime);
        return Description == other.Description &&
            Amount == other.Amount &&
            startDate == other.Date;
    }

    public static bool operator ==(StatementEntry? left, StatementEntry? right) => Equals(left, right);

    public static bool operator !=(StatementEntry? left, StatementEntry? right) => !Equals(left, right);
}