using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ProjectTFDB.Models;

namespace ProjectTFDB.Services;

public sealed class SteamInventoryService
{
    private readonly HttpService _http;

    public SteamInventoryService(HttpService http)
    {
        _http = http;
    }

    public async Task<(List<InventoryItemStack> Items, string Status)> FetchAndParseAsync(
        string steamApiKey,
        string steamId64,
        CancellationToken cancellationToken)
    {
        steamApiKey = (steamApiKey ?? "").Trim();
        steamId64 = (steamId64 ?? "").Trim();

        if (steamId64.Length == 0) return ([], "SteamID64 missing (using cache if available)");
        if (steamApiKey.Length == 0) return ([], "Steam API key missing (using cache if available)");

        var url = $"https://api.steampowered.com/IEconItems_440/GetPlayerItems/v1/?key={Uri.EscapeDataString(steamApiKey)}&steamid={Uri.EscapeDataString(steamId64)}";
        var (status, body) = await _http.GetStringAsync(url, cancellationToken).ConfigureAwait(false);
        if (status != 200) return ([], $"Steam HTTP {status} (using cache if available)");

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        if (!root.TryGetProperty("result", out var result) || result.ValueKind != JsonValueKind.Object)
            return ([], "Steam response missing result (using cache if available)");

        if (result.TryGetProperty("status", out var steamStatus) && steamStatus.ValueKind == JsonValueKind.Number)
        {
            var st = steamStatus.GetInt32();
            if (st != 1) return ([], $"Steam API status {st} (using cache if available)");
        }

        if (!result.TryGetProperty("items", out var itemsEl) || itemsEl.ValueKind != JsonValueKind.Array)
            return ([], "Steam response missing items (using cache if available)");

        var map = new Dictionary<string, InventoryItemStack>();
        foreach (var it in itemsEl.EnumerateArray())
        {
            if (it.ValueKind != JsonValueKind.Object) continue;
            if (!TryGetInt(it, "defindex", out var defindex)) continue;
            var quality = TryGetInt(it, "quality", out var q) ? q : 6;
            var quantity = TryGetInt(it, "quantity", out var qty) ? qty : 1;
            if (quantity <= 0) quantity = 1;

            var customName = TryGetString(it, "custom_name");
            var customDesc = TryGetString(it, "custom_desc");

            var key = $"{defindex}|{quality}|{customName ?? ""}|{customDesc ?? ""}";
            if (map.TryGetValue(key, out var prev))
            {
                prev.Quantity += quantity;
            }
            else
            {
                map[key] = new InventoryItemStack
                {
                    Defindex = defindex,
                    Quality = quality,
                    Quantity = quantity,
                    CustomName = customName,
                    CustomDesc = customDesc
                };
            }
        }

        return ([.. map.Values], "inventory loaded");
    }

    private static bool TryGetInt(JsonElement obj, string prop, out int value)
    {
        value = default;
        if (!obj.TryGetProperty(prop, out var el)) return false;
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

    private static string? TryGetString(JsonElement obj, string prop)
    {
        if (!obj.TryGetProperty(prop, out var el)) return null;
        return el.ValueKind == JsonValueKind.String ? el.GetString() : null;
    }
}

