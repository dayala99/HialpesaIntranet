namespace Intranet.Entity;

public static class QuantityRules
{
    public static decimal RoundRequiredQuantity(decimal required)
    {
        if (required < 0 || required > 100000000) throw new ArgumentOutOfRangeException(nameof(required));
        var integer = decimal.Truncate(required);
        // For integer parts ending in 0 or 5, discard fractions below 0.50.
        if (integer % 5m == 0m && required - integer < 0.5m) return integer;
        return decimal.Ceiling(required / 5m) * 5m;
    }
}
