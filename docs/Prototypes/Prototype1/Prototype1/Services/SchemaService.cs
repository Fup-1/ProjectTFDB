using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ProjectTFDB.Models;

namespace ProjectTFDB.Services;

public sealed class SchemaService
{
    private static readonly JsonDocumentOptions DocOptions = new()
    {
        AllowTrailingCommas = true
    };

    public sealed record SchemaIndex(string Path, IReadOnlyDictionary<int, SchemaItem> Map);

    public string? ResolveSchemaPath()
    {
        if (File.Exists(AppPaths.PortableSchemaPath)) return AppPaths.PortableSchemaPath;
        if (File.Exists(AppPaths.SchemaCachePath)) return AppPaths.SchemaCachePath;
        return null;
    }

    public async Task<SchemaIndex?> LoadIndexAsync()
    {
        var schemaPath = ResolveSchemaPath();
        if (schemaPath is null) return null;

        try
        {
            await using var stream = File.OpenRead(schemaPath);
            using var doc = await JsonDocument.ParseAsync(stream, DocOptions).ConfigureAwait(false);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return null;

            var map = new Dictionary<int, SchemaItem>(capacity: doc.RootElement.GetArrayLength());
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (el.ValueKind != JsonValueKind.Object) continue;
                if (!TryGetDefindex(el, out var defindex)) continue;

                var name = GetString(el, "item_name") ?? GetString(el, "name") ?? $"Item {defindex}";
                var desc = GetString(el, "item_description")
                           ?? GetString(el, "description")
                           ?? GetString(el, "item_desc")
                           ?? GetString(el, "item_type_name")
                           ?? "";
                var iconUrl = GetString(el, "image_url")
                              ?? GetString(el, "image_url_large")
                              ?? GetString(el, "icon_url")
                              ?? "";

                map[defindex] = new SchemaItem
                {
                    Defindex = defindex,
                    Name = name,
                    Description = desc,
                    IconUrl = iconUrl
                };
            }

            return new SchemaIndex(schemaPath, map);
        }
        catch
        {
            return null;
        }
    }

    public string? TryGetLocalIconPath(int defindex)
    {
        static string? FindWithExts(string dir, int di)
        {
            var exts = new[] { ".png", ".webp", ".jpg", ".jpeg" };
            foreach (var ext in exts)
            {
                var p = Path.Combine(dir, $"{di}{ext}");
                if (File.Exists(p)) return p;
            }
            return null;
        }

        var p1 = FindWithExts(AppPaths.PortableSchemaIconsDir, defindex);
        if (p1 is not null) return p1;

        var p2 = FindWithExts(AppPaths.SchemaCacheIconsDir, defindex);
        return p2;
    }

    public Task ImportSchemaFromFileAsync(string schemaItemsJsonPath)
    {
        if (string.IsNullOrWhiteSpace(schemaItemsJsonPath)) throw new ArgumentException("Path is required.", nameof(schemaItemsJsonPath));
        if (!File.Exists(schemaItemsJsonPath)) throw new FileNotFoundException("schema_items.json not found.", schemaItemsJsonPath);

        Directory.CreateDirectory(AppPaths.SchemaCacheDir);
        File.Copy(schemaItemsJsonPath, AppPaths.SchemaCachePath, overwrite: true);

        // Optional: copy icons if sibling 'icons' exists.
        var schemaDir = Path.GetDirectoryName(schemaItemsJsonPath);
        if (!string.IsNullOrWhiteSpace(schemaDir))
        {
            var iconsDir = Path.Combine(schemaDir, "icons");
            if (Directory.Exists(iconsDir))
            {
                CopyDirectory(iconsDir, AppPaths.SchemaCacheIconsDir);
            }
        }

        return Task.CompletedTask;
    }

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);
        foreach (var file in Directory.EnumerateFiles(sourceDir))
        {
            var dest = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, dest, overwrite: true);
        }
    }

    private static string? GetString(JsonElement obj, string propertyName)
    {
        if (!obj.TryGetProperty(propertyName, out var el)) return null;
        return el.ValueKind == JsonValueKind.String ? el.GetString() : null;
    }

    private static bool TryGetDefindex(JsonElement obj, out int defindex)
    {
        defindex = default;
        if (!obj.TryGetProperty("defindex", out var diEl)) return false;
        if (diEl.ValueKind == JsonValueKind.Number && diEl.TryGetInt32(out var diN))
        {
            defindex = diN;
            return true;
        }
        if (diEl.ValueKind == JsonValueKind.String && int.TryParse(diEl.GetString(), out var diS))
        {
            defindex = diS;
            return true;
        }
        return false;
    }
}

