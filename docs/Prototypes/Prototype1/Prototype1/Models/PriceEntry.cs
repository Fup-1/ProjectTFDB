namespace ProjectTFDB.Models;

public sealed class PriceEntry
{
    public string Name { get; init; } = "";
    public Price Buy { get; init; }
    public Price Sell { get; init; }
}

