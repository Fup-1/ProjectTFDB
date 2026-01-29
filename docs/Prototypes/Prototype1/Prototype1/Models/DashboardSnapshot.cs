using System;
using System.Collections.Generic;

namespace ProjectTFDB.Models;

public sealed class DashboardSnapshot
{
    public DateTimeOffset SavedAt { get; init; }

    public string SteamStatus { get; init; } = "";
    public string PricesStatus { get; init; } = "";
    public string SchemaStatus { get; init; } = "";

    public double KeyRef { get; init; }
    public double TotalRef { get; init; }
    public int TotalValueKeysPart { get; init; }
    public double TotalValueRefPart { get; init; }
    public int TotalStacks { get; init; }
    public int TotalItems { get; init; }
    public int PricedStacks { get; init; }
    public int DealsCount { get; init; }

    public int KeysCount { get; init; }
    public int RefinedCount { get; init; }

    public string? SchemaPath { get; init; }
    public int SchemaCount { get; init; }

    public List<EnrichedItem> Items { get; init; } = [];
    public List<DealItem> Deals { get; init; } = [];
}

public sealed class EnrichedItem
{
    public int Defindex { get; init; }
    public int Quality { get; init; }
    public string QualityName { get; init; } = "";
    public int Quantity { get; init; }

    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public string? IconPath { get; init; }

    public double? BuyKeys { get; init; }
    public double? SellKeys { get; init; }
    public double? BuyRef { get; init; }
    public double? SellRef { get; init; }
}

public sealed class DealItem
{
    public int Defindex { get; init; }
    public string Name { get; init; } = "";
    public int Quantity { get; init; }
    public double BuyKeys { get; init; }
    public double SellKeys { get; init; }
    public double SpreadKeys { get; init; }
}

