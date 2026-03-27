namespace ModernWinManager.Services;

using System.Security.Cryptography;
using System.Text;
using ModernWinManager.Models;

internal static class ScreenConfigService
{
    public static ScreenConfig GetCurrentConfig()
    {
        var monitors = new List<MonitorInfo>();

        NativeInterop.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr _, ref NativeInterop.RECT _, IntPtr _) =>
        {
            var info = NativeInterop.MONITORINFOEX.Create();
            if (NativeInterop.GetMonitorInfo(hMonitor, ref info))
            {
                monitors.Add(new MonitorInfo
                {
                    DeviceName = info.szDevice,
                    X = info.rcMonitor.Left,
                    Y = info.rcMonitor.Top,
                    Width = info.rcMonitor.Width,
                    Height = info.rcMonitor.Height
                });
            }
            return true;
        }, IntPtr.Zero);

        monitors = monitors.OrderBy(m => m.X).ThenBy(m => m.Y).ToList();

        var fingerprint = GenerateFingerprint(monitors);
        var description = $"{monitors.Count} monitor(s): " +
            string.Join("+", monitors.Select(m => $"{m.Width}x{m.Height}"));

        return new ScreenConfig
        {
            Fingerprint = fingerprint,
            Description = description,
            Monitors = monitors
        };
    }

    private static string GenerateFingerprint(List<MonitorInfo> monitors)
    {
        var sb = new StringBuilder();
        foreach (var m in monitors)
            sb.Append($"{m.DeviceName}:{m.X},{m.Y},{m.Width},{m.Height};");

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(hash)[..16].ToLowerInvariant();
    }
}
