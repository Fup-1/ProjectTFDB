namespace ProjectTFDB.Models;

public sealed class InventoryItemStack
{
    public int Defindex { get; init; }
    public int Quality { get; init; } = 6;
    public int Quantity { get; set; } = 1;
    public string? CustomName { get; init; }
    public string? CustomDesc { get; init; }
}

