using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ProjectTFDB.Helpers;
using ProjectTFDB.Models;

namespace ProjectTFDB.Services;

public sealed class CacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public Task WriteDashboardAsync(DashboardSnapshot snapshot)
        => JsonFile.WriteAsync(AppPaths.DashboardCachePath, snapshot, JsonOptions);

    public async Task<DashboardSnapshot?> ReadDashboardAsync()
    {
        try
        {
            return await JsonFile.ReadAsync<DashboardSnapshot>(AppPaths.DashboardCachePath, JsonOptions).ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }

    public void OpenCacheFolder()
    {
        var dir = AppPaths.CacheRootDir;
        Directory.CreateDirectory(dir);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = dir,
            UseShellExecute = true
        });
    }
}

