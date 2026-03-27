namespace ModernWinManager.Services;

using System.Diagnostics;
using System.Text;

internal record WindowInfo(IntPtr Handle, string ProcessName, string Title);

internal static class WindowService
{
    public static List<WindowInfo> GetVisibleWindows()
    {
        var windows = new List<WindowInfo>();

        NativeInterop.EnumWindows((hWnd, _) =>
        {
            if (!NativeInterop.IsWindowVisible(hWnd))
                return true;

            var titleLength = NativeInterop.GetWindowTextLength(hWnd);
            if (titleLength == 0)
                return true;

            var sb = new StringBuilder(titleLength + 1);
            NativeInterop.GetWindowText(hWnd, sb, sb.Capacity);
            var title = sb.ToString();

            NativeInterop.GetWindowThreadProcessId(hWnd, out var pid);
            try
            {
                var process = Process.GetProcessById((int)pid);
                var name = process.ProcessName;

                // Skip common system windows
                if (name is "explorer" or "TextInputHost" or "SearchHost" or "ShellExperienceHost")
                    return true;

                windows.Add(new WindowInfo(hWnd, name, title));
            }
            catch
            {
                // Process may have exited
            }

            return true;
        }, IntPtr.Zero);

        return windows;
    }

    public static (int X, int Y, int Width, int Height) GetPosition(IntPtr hWnd)
    {
        NativeInterop.GetWindowRect(hWnd, out var rect);
        return (rect.Left, rect.Top, rect.Width, rect.Height);
    }

    public static void SetPosition(IntPtr hWnd, int x, int y, int width, int height)
    {
        // Restore window if minimized
        NativeInterop.ShowWindow(hWnd, NativeInterop.SW_RESTORE);
        NativeInterop.SetWindowPos(hWnd, NativeInterop.HWND_TOP,
            x, y, width, height, NativeInterop.SWP_NOZORDER | NativeInterop.SWP_NOACTIVATE);
    }

    public static List<IntPtr> FindAllWindowsByProcessName(string processName)
    {
        var windows = GetVisibleWindows();
        return windows
            .Where(w => w.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase))
            .Select(w => w.Handle)
            .ToList();
    }
}
