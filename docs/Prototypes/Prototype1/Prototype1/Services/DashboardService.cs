using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ProjectTFDB.Models;

namespace ProjectTFDB.Services;

public sealed class DashboardService
{
    private readonly SettingsService _settings;
    private readonly SchemaService _schema;
    private readonly SteamInventoryService _steam;
    private readonly BackpackPricesService _prices;
    private readonly CacheService _cache;

    public DashboardService(
        SettingsService settings,
        SchemaService schema,
        SteamInventoryService steam,
        BackpackPricesService prices,
        CacheService cache)
    {
        _settings = settings;
        _schema = schema;
        _steam = steam;
        _prices = prices;
        _cache = cache;
    }

    public async Task<DashboardSnapshot> RefreshAsync(CancellationToken cancellationToken)
    {
        var settings = await _settings.LoadAsync().ConfigureAwait(false);

        var schemaIndex = await _schema.LoadIndexAsync().ConfigureAwait(false);
        var schemaStatus = schemaIndex is null ? "schema missing (import schema_items.json)" : "schema loaded";

        var (pricesRaw, pricesStatus) = await _prices.FetchPricesV4RawAsync(settings.BackpackTfApiKey, cancellationToken).ConfigureAwait(false);
        var priceMap = new Dictionary<int, PriceEntry>();
        var skippedComplex = 0;
        if (!string.IsNullOrWhiteSpace(pricesRaw))
        {
            (priceMap, skippedComplex) = _prices.ParsePricesV4(pricesRaw);
        }

        var keyRef = BackpackPricesService.FindKeyPriceRef(priceMap, fallback: 60);

        var (inv, steamStatus) = await _steam.FetchAndParseAsync(settings.SteamApiKey, settings.SteamId64, cancellationToken).ConfigureAwait(false);
        List<InventoryItemStack> stacked;
        if (inv.Count > 0)
        {
            stacked = inv;
        }
        else
        {
            var cached = await _cache.ReadDashboardAsync().ConfigureAwait(false);
            stacked = cached?.Items
                .Select(i => new InventoryItemStack { Defindex = i.Defindex, Quality = i.Quality, Quantity = i.Quantity })
                .ToList() ?? [];
            if (stacked.Count > 0) steamStatus = $"{steamStatus} (loaded cached items)";
        }

        var items = new List<EnrichedItem>(stacked.Count);
        foreach (var it in stacked)
        {
            SchemaItem? si = null;
            if (schemaIndex is not null)
            {
                schemaIndex.Map.TryGetValue(it.Defindex, out si);
            }
            var name = si?.Name ?? $"Item {it.Defindex}";
            var desc = si?.Description ?? "";
            var iconPath = _schema.TryGetLocalIconPath(it.Defindex);

            var qualityName = QualityName(it.Quality);

            double? buyRef = null;
            double? sellRef = null;
            double? buyKeys = null;
            double? sellKeys = null;

            if (priceMap.TryGetValue(it.Defindex, out var p))
            {
                buyRef = ToRef(p.Buy, keyRef);
                sellRef = ToRef(p.Sell, keyRef);
                buyKeys = buyRef is not null ? ToKeys(buyRef.Value, keyRef) : null;
                sellKeys = sellRef is not null ? ToKeys(sellRef.Value, keyRef) : null;
            }

            items.Add(new EnrichedItem
            {
                Defindex = it.Defindex,
                Quality = it.Quality,
                QualityName = qualityName,
                Quantity = it.Quantity,
                Name = name,
                Description = desc,
                IconPath = iconPath,
                BuyRef = RoundNullable(buyRef, 2),
                SellRef = RoundNullable(sellRef, 2),
                BuyKeys = RoundNullable(buyKeys, 4),
                SellKeys = RoundNullable(sellKeys, 4),
            });
        }

        var keysCount = items.Where(i => i.Defindex == 5021).Sum(i => i.Quantity);
        var refinedCount = items.Where(i => i.Defindex == 5002).Sum(i => i.Quantity);

        var totalStacks = items.Count;
        var totalItems = items.Sum(i => i.Quantity);
        var pricedStacks = items.Count(i => i.SellRef is not null);

        var totalRef = items.Where(i => i.SellRef is not null).Sum(i => i.SellRef!.Value * i.Quantity);
        var (keysPart, refPart) = SplitKeysRef(totalRef, keyRef);

        var deals = items
            .Where(i => i.BuyKeys is not null && i.SellKeys is not null && i.BuyKeys > 0 && i.SellKeys > i.BuyKeys)
            .Select(i => new DealItem
            {
                Defindex = i.Defindex,
                Name = i.Name,
                Quantity = i.Quantity,
                BuyKeys = i.BuyKeys!.Value,
                SellKeys = i.SellKeys!.Value,
                SpreadKeys = i.SellKeys!.Value - i.BuyKeys!.Value
            })
            .OrderByDescending(d => d.SpreadKeys)
            .ToList();

        var snapshot = new DashboardSnapshot
        {
            SavedAt = DateTimeOffset.UtcNow,
            SteamStatus = steamStatus,
            PricesStatus = string.IsNullOrWhiteSpace(pricesRaw) ? pricesStatus : $"{pricesStatus} (map={priceMap.Count}, skippedComplex={skippedComplex})",
            SchemaStatus = schemaStatus,
            KeyRef = keyRef,
            TotalRef = Round(totalRef, 2),
            TotalValueKeysPart = keysPart,
            TotalValueRefPart = Round(refPart, 2),
            TotalStacks = totalStacks,
            TotalItems = totalItems,
            PricedStacks = pricedStacks,
            DealsCount = deals.Count,
            KeysCount = keysCount,
            RefinedCount = refinedCount,
            SchemaPath = schemaIndex?.Path,
            SchemaCount = schemaIndex?.Map.Count ?? 0,
            Items = items,
            Deals = deals
        };

        await _cache.WriteDashboardAsync(snapshot).ConfigureAwait(false);
        return snapshot;
    }

    private static string QualityName(int q) => q switch
    {
        0 => "Normal",
        1 => "Genuine",
        2 => "Vintage",
        3 => "Unusual",
        5 => "Community",
        6 => "Unique",
        7 => "Valve",
        8 => "Self-Made",
        9 => "Customized",
        10 => "Strange",
        11 => "Haunted",
        12 => "Collector's",
        _ => $"Quality {q.ToString(CultureInfo.InvariantCulture)}"
    };

    private static double? ToRef(Price p, double keyPriceRef)
    {
        if (keyPriceRef <= 0) return null;
        if (double.IsNaN(p.Keys) || double.IsNaN(p.Metal)) return null;
        return (p.Keys * keyPriceRef) + p.Metal;
    }

    private static double ToKeys(double refValue, double keyPriceRef)
    {
        if (keyPriceRef <= 0) return 0;
        return refValue / keyPriceRef;
    }

    private static (int KeysPart, double RefPart) SplitKeysRef(double totalRef, double keyPriceRef)
    {
        if (keyPriceRef <= 0) return (0, 0);
        var keysPart = (int)Math.Floor(totalRef / keyPriceRef);
        var refPart = totalRef - (keysPart * keyPriceRef);
        return (keysPart, refPart);
    }

    private static double Round(double v, int decimals) => Math.Round(v, decimals, MidpointRounding.AwayFromZero);

    private static double? RoundNullable(double? v, int decimals)
        => v is null ? null : Round(v.Value, decimals);
}
