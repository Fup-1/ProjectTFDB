using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ProjectTFDB.Helpers;
using ProjectTFDB.Models;

namespace ProjectTFDB.Services;

public sealed class BackpackPricesService
{
    private readonly HttpService _http;

    public BackpackPricesService(HttpService http)
    {
        _http = http;
    }

    private sealed class PricesMeta
    {
        public DateTimeOffset SavedAt { get; init; }
        public string Source { get; init; } = "";
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public async Task<(string? RawJson, string Status)> FetchPricesV4RawAsync(string? apiKey, CancellationToken cancellationToken)
    {
        apiKey = (apiKey ?? "").Trim();

        // 1) backpack.tf (preferred if apiKey exists)
        if (apiKey.Length > 0)
        {
            var url = $"https://backpack.tf/api/IGetPrices/v4?appid=440&key={Uri.EscapeDataString(apiKey)}";
            try
            {
                var (status, body) = await _http.GetStringAsync(url, cancellationToken).ConfigureAwait(false);
                if (status == 200)
                {
                    await WritePricesCacheAsync(body, new PricesMeta { SavedAt = DateTimeOffset.UtcNow, Source = "backpack.tf" }).ConfigureAwait(false);
                    return (body, "prices loaded (backpack.tf)");
                }
            }
            catch
            {
                // ignore; fall through
            }
        }

        // 2) mirror (optional)
        try
        {
            var (status, body) = await _http.GetStringAsync("https://prices.tf/api/IGetPrices/v4?appid=440", cancellationToken).ConfigureAwait(false);
            if (status == 200)
            {
                await WritePricesCacheAsync(body, new PricesMeta { SavedAt = DateTimeOffset.UtcNow, Source = "prices.tf" }).ConfigureAwait(false);
                return (body, "prices loaded (prices.tf)");
            }
        }
        catch
        {
            // ignore; fall through
        }

        // 3) cache fallback
        var cached = await ReadPricesCacheAsync().ConfigureAwait(false);
        if (cached.RawJson is not null) return (cached.RawJson, $"prices loaded (cache: {cached.Meta?.Source ?? "unknown"})");
        return (null, "prices missing (check key / cache)");
    }

    public (Dictionary<int, PriceEntry> PriceMap, int SkippedComplex) ParsePricesV4(string rawJson)
    {
        using var doc = JsonDocument.Parse(rawJson);
        var root = doc.RootElement;
        JsonElement itemsEl;

        if (root.TryGetProperty("items", out var items1) && items1.ValueKind == JsonValueKind.Object)
        {
            itemsEl = items1;
        }
        else if (root.TryGetProperty("response", out var resp) && resp.ValueKind == JsonValueKind.Object
                 && resp.TryGetProperty("items", out var items2) && items2.ValueKind == JsonValueKind.Object)
        {
            itemsEl = items2;
        }
        else
        {
            return (new Dictionary<int, PriceEntry>(), 0);
        }

        var map = new Dictionary<int, PriceEntry>(capacity: 16384);
        var skippedComplex = 0;

        foreach (var itemProp in itemsEl.EnumerateObject())
        {
            var name = itemProp.Name;
            var item = itemProp.Value;
            if (item.ValueKind != JsonValueKind.Object) continue;

            if (!item.TryGetProperty("defindex", out var defEl) || defEl.ValueKind != JsonValueKind.Array) continue;
            if (!item.TryGetProperty("prices", out var pricesEl) || pricesEl.ValueKind != JsonValueKind.Object) continue;

            // Only Unique (quality 6) for now
            if (!pricesEl.TryGetProperty("6", out var qBlock) || qBlock.ValueKind != JsonValueKind.Object) continue;
            if (!qBlock.TryGetProperty("Tradable", out var tradable) || tradable.ValueKind != JsonValueKind.Object) continue;
            if (!tradable.TryGetProperty("Craftable", out var craftable)) continue;

            JsonElement entryEl;
            if (craftable.ValueKind == JsonValueKind.Array)
            {
                entryEl = craftable.GetArrayLength() > 0 ? craftable[0] : default;
                if (entryEl.ValueKind != JsonValueKind.Object) continue;
            }
            else
            {
                // Complex items; skip but count.
                skippedComplex++;
                continue;
            }

            if (!TryGetPrice(entryEl, "buy", out var buy) || !TryGetPrice(entryEl, "sell", out var sell)) continue;

            foreach (var diEl in defEl.EnumerateArray())
            {
                if (!TryGetInt(diEl, out var defindex)) continue;
                map[defindex] = new PriceEntry { Name = name, Buy = buy, Sell = sell };
            }
        }

        return (map, skippedComplex);
    }

    public static double FindKeyPriceRef(Dictionary<int, PriceEntry> priceMap, double fallback = 60)
    {
        if (priceMap.TryGetValue(5021, out var keyEntry))
        {
            var k = keyEntry.Sell.Keys;
            var m = keyEntry.Sell.Metal;
            if (m > 0 && Math.Abs(k) < 0.000001) return m;
            if (k > 0) return (k * fallback) + m;
        }
        return fallback;
    }

    private static bool TryGetPrice(JsonElement obj, string prop, out Price price)
    {
        price = default;
        if (!obj.TryGetProperty(prop, out var el) || el.ValueKind != JsonValueKind.Object) return false;

        var keys = 0.0;
        var metal = 0.0;

        if (el.TryGetProperty("keys", out var kEl) && kEl.ValueKind == JsonValueKind.Number) keys = kEl.GetDouble();
        if (el.TryGetProperty("metal", out var mEl) && mEl.ValueKind == JsonValueKind.Number) metal = mEl.GetDouble();

        price = new Price(keys, metal);
        return true;
    }

    private static bool TryGetInt(JsonElement el, out int value)
    {
        value = default;
        if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var n))
        {
            value = n;
            return true;
        }
        if (el.ValueKind == JsonValueKind.String && int.TryParse(el.GetString(), out var s))
        {
            value = s;
            return true;
        }
        return false;
    }

    private static async Task WritePricesCacheAsync(string rawJson, PricesMeta meta)
    {
        Directory.CreateDirectory(AppPaths.CacheRootDir);
        await File.WriteAllTextAsync(AppPaths.PricesRawPath, rawJson).ConfigureAwait(false);
        await JsonFile.WriteAsync(AppPaths.PricesMetaPath, meta, JsonOptions).ConfigureAwait(false);
    }

    private static async Task<(string? RawJson, PricesMeta? Meta)> ReadPricesCacheAsync()
    {
        if (!File.Exists(AppPaths.PricesRawPath)) return (null, null);
        try
        {
            var raw = await File.ReadAllTextAsync(AppPaths.PricesRawPath).ConfigureAwait(false);
            var meta = await JsonFile.ReadAsync<PricesMeta>(AppPaths.PricesMetaPath, JsonOptions).ConfigureAwait(false);
            return (string.IsNullOrWhiteSpace(raw) ? null : raw, meta);
        }
        catch
        {
            return (null, null);
        }
    }
}

