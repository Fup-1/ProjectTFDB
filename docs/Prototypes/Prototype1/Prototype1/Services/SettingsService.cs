using System.Text.Json;
using ProjectTFDB.Helpers;
using ProjectTFDB.Models;

namespace ProjectTFDB.Services;

public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public async Task<AppSettings> LoadAsync()
    {
        var settings = await JsonFile.ReadAsync<AppSettings>(AppPaths.SettingsPath, JsonOptions).ConfigureAwait(false);
        return settings ?? new AppSettings();
    }

    public Task SaveAsync(AppSettings settings)
        => JsonFile.WriteAsync(AppPaths.SettingsPath, settings, JsonOptions);
}

