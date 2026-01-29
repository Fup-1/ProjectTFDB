using System;
using System.IO;
using System.Text;

namespace ProjectTFDB.Services;

public static class AppLogger
{
    private static readonly object Gate = new();

    public static string LogsDir => Path.Combine(AppPaths.CacheRootDir, "logs");

    public static string CurrentLogPath => Path.Combine(LogsDir, $"{DateTime.Now:yyyy-MM-dd}.log");

    public static void Initialize()
    {
        Directory.CreateDirectory(LogsDir);
    }

    public static void Log(string message)
    {
        try
        {
            Directory.CreateDirectory(LogsDir);
            var line = $"{DateTime.Now:O} {message}{Environment.NewLine}";
            lock (Gate)
            {
                File.AppendAllText(CurrentLogPath, line, Encoding.UTF8);
            }
        }
        catch
        {
        }
    }

    public static void Log(Exception ex, string context)
    {
        Log($"{context}: {ex}");
    }
}
