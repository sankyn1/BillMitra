namespace BillMitra.Domain.ValueObjects;

public readonly struct Money : IEquatable<Money>, IComparable<Money>
{
    public decimal Amount { get; }
    public string Currency { get; }

    public static Money Zero(string currency = "INR") => new(0m, currency);

    public Money(decimal amount, string currency = "INR")
    {
        // Enforce 2 decimal places standard for Indian currency display, while keeping exact decimal precision
        Amount = Math.Round(amount, 4, MidpointRounding.AwayFromZero);
        Currency = string.IsNullOrWhiteSpace(currency) ? "INR" : currency.ToUpperInvariant();
    }

    public static Money operator +(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount - right.Amount, left.Currency);
    }

    public static Money operator *(Money left, decimal multiplier) =>
        new(left.Amount * multiplier, left.Currency);

    public static Money operator /(Money left, decimal divisor)
    {
        if (divisor == 0m)
            throw new DivideByZeroException("Cannot divide money amount by zero.");
        return new Money(left.Amount / divisor, left.Currency);
    }

    private static void EnsureSameCurrency(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException($"Cannot operate on money with mismatched currencies: {left.Currency} vs {right.Currency}");
    }

    public bool Equals(Money other) => Amount == other.Amount && Currency == other.Currency;
    public override bool Equals(object? obj) => obj is Money other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Amount, Currency);
    public int CompareTo(Money other)
    {
        EnsureSameCurrency(this, other);
        return Amount.CompareTo(other.Amount);
    }

    public static bool operator ==(Money left, Money right) => left.Equals(right);
    public static bool operator !=(Money left, Money right) => !left.Equals(right);
    public static bool operator <(Money left, Money right) => left.CompareTo(right) < 0;
    public static bool operator <=(Money left, Money right) => left.CompareTo(right) <= 0;
    public static bool operator >(Money left, Money right) => left.CompareTo(right) > 0;
    public static bool operator >=(Money left, Money right) => left.CompareTo(right) >= 0;

    public override string ToString() => $"{Currency} {Amount:N2}";
}

