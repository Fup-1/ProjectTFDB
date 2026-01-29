using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace ProjectTFDB.Helpers;

public static class JsonFile
{
    public static async Task<T?> ReadAsync<T>(string path, JsonSerializerOptions? options = null)
    {
        if (!File.Exists(path)) return default;
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream, options).ConfigureAwait(false);
    }

    public static async Task WriteAsync<T>(string path, T value, JsonSerializerOptions? options = null)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, value, options).ConfigureAwait(false);
        await stream.FlushAsync().ConfigureAwait(false);
    }
}

