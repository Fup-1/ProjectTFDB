using System;
using System.IO;

namespace ProjectTFDB.Services;

public static class AppPaths
{
    public static string AppBaseDir => AppContext.BaseDirectory;

    public static string CacheRootDir
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ProjectTFDB");

    public static string SettingsPath => Path.Combine(CacheRootDir, "settings.json");

    public static string DashboardCachePath => Path.Combine(CacheRootDir, "dashboard_cache.json");

    public static string PricesRawPath => Path.Combine(CacheRootDir, "prices_v4.json");
    public static string PricesMetaPath => Path.Combine(CacheRootDir, "prices_v4.meta.json");

    public static string SchemaCacheDir => Path.Combine(CacheRootDir, "tf2_schema_dump");
    public static string SchemaCachePath => Path.Combine(SchemaCacheDir, "schema_items.json");
    public static string SchemaCacheIconsDir => Path.Combine(SchemaCacheDir, "icons");

    public static string PortableSchemaDir => Path.Combine(AppBaseDir, "tf2_schema_dump");
    public static string PortableSchemaPath => Path.Combine(PortableSchemaDir, "schema_items.json");
    public static string PortableSchemaIconsDir => Path.Combine(PortableSchemaDir, "icons");
}

