namespace ProjectTFDB.Models;

public sealed class SchemaItem
{
    public int Defindex { get; init; }
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public string IconUrl { get; init; } = "";
}

