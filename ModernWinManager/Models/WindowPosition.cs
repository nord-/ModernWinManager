namespace ModernWinManager.Models;

public class MonitorInfo
{
    public string DeviceName { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

public class ScreenConfig
{
    public string Fingerprint { get; set; } = "";
    public string? CustomName { get; set; }
    public string Description { get; set; } = "";
    public List<MonitorInfo> Monitors { get; set; } = [];

    public string DisplayName => CustomName ?? Description;
}

public class SavedPosition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string ProcessName { get; set; } = "";
    public string? CustomName { get; set; }
    public string WindowTitle { get; set; } = "";
    public DateTime SavedAt { get; set; } = DateTime.Now;
    public string ScreenFingerprint { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public string DisplayName => CustomName ?? ProcessName;
}

public class AppData
{
    public List<ScreenConfig> ScreenConfigs { get; set; } = [];
    public List<SavedPosition> SavedPositions { get; set; } = [];
}
