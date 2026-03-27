namespace ModernWinManager.Services;

using System.Text.Json;
using ModernWinManager.Models;

internal static class StorageService
{
    private static readonly string DataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ModernWinManager");

    private static readonly string FilePath = Path.Combine(DataDir, "positions.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static AppData Load()
    {
        if (!File.Exists(FilePath))
            return new AppData();

        var json = File.ReadAllText(FilePath);
        return JsonSerializer.Deserialize<AppData>(json, JsonOptions) ?? new AppData();
    }

    public static void Save(AppData data)
    {
        Directory.CreateDirectory(DataDir);
        var json = JsonSerializer.Serialize(data, JsonOptions);
        File.WriteAllText(FilePath, json);
    }
}
