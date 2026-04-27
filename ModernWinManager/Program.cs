using System.Reflection;
using ModernWinManager.Models;
using ModernWinManager.Services;

var version = Assembly.GetExecutingAssembly()
    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";
var plusIdx = version.IndexOf('+');
if (plusIdx >= 0) version = version[..plusIdx];

if (args.Length > 0 && (args[0] == "--version" || args[0] == "-v"))
{
    Console.WriteLine($"ModernWinManager {version}");
    return;
}

var data = StorageService.Load();
var currentConfig = ScreenConfigService.GetCurrentConfig();

// Ensure current screen config is registered
if (!data.ScreenConfigs.Any(c => c.Fingerprint == currentConfig.Fingerprint))
{
    data.ScreenConfigs.Add(currentConfig);
    StorageService.Save(data);
}

if (args.Length > 0 && (args[0] == "--restore-all" || args[0] == "-r"))
{
    var positionsForCli = data.SavedPositions
        .Where(p => p.ScreenFingerprint == currentConfig.Fingerprint)
        .ToList();

    var (restoredCli, notFoundCli) = RestoreAllPositions(positionsForCli);

    var msg = $"Återställde {restoredCli} program.";
    if (notFoundCli > 0)
        msg += $" {notFoundCli} program hittades inte.";
    Console.WriteLine(msg);
    return;
}

bool editMode = false;

while (true)
{
    var savedConfig = data.ScreenConfigs.FirstOrDefault(c => c.Fingerprint == currentConfig.Fingerprint);
    var screenInfo = $"{savedConfig?.DisplayName ?? currentConfig.Description} [{currentConfig.Fingerprint[..8]}]";

    if (editMode)
        RunEditMode(ref editMode, screenInfo);
    else
        RunSetMode(ref editMode, screenInfo);
}

void RunSetMode(ref bool editMode, string screenInfo)
{
    MenuService.ShowHeader($"ModernWinManager {version} [SET]", screenInfo);

    var positions = data.SavedPositions
        .Where(p => p.ScreenFingerprint == currentConfig.Fingerprint)
        .ToList();

    var programs = positions
        .GroupBy(p => p.ProcessName)
        .OrderBy(g => g.First().DisplayName)
        .ToList();

    if (programs.Count == 0)
    {
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine("Inga sparade positioner för denna skärmkonfig.");
        Console.WriteLine("Tryck Ctrl+E för att gå till Edit-mode och spara positioner.");
        Console.ResetColor();
        Console.WriteLine();
        Console.Write("Väntar (Ctrl+E = Edit-mode, Esc = Avsluta)... ");
        var k = Console.ReadKey(intercept: true);
        Console.WriteLine();
        if (k.Modifiers.HasFlag(ConsoleModifiers.Control) && k.Key == ConsoleKey.E)
            editMode = true;
        else if (k.Key == ConsoleKey.Escape)
            Environment.Exit(0);
        return;
    }

    var options = programs.Select(g =>
    {
        var first = g.First();
        var label = first.CustomName != null ? $"{first.CustomName}  ({first.ProcessName})" : first.ProcessName;
        return $"{label}  ({g.Count()} position{(g.Count() > 1 ? "er" : "")})";
    }).ToList();

    options.Insert(0, "Återställ alla");

    Console.WriteLine("Sparade program för denna konfig:");
    int idx = MenuService.PickOption("Välj program (Ctrl+E = Edit-mode, Esc = Avsluta):", options, out var special);

    if (special.HasValue)
    {
        if (special.Value.Modifiers.HasFlag(ConsoleModifiers.Control) && special.Value.Key == ConsoleKey.E)
            editMode = true;
        else if (special.Value.Key == ConsoleKey.Escape)
            Environment.Exit(0);
        return;
    }

    if (idx < 0) return;

    if (idx == 0)
    {
        RestoreAll(positions);
        return;
    }

    var chosenGroup = programs[idx - 1];
    var savedForProcess = chosenGroup.ToList();

    SavedPosition chosen;
    if (savedForProcess.Count == 1)
    {
        chosen = savedForProcess[0];
    }
    else
    {
        MenuService.ShowHeader($"ModernWinManager {version} [SET]", screenInfo);
        Console.WriteLine($"Sparade positioner för \"{chosenGroup.First().DisplayName}\":");

        var posOptions = savedForProcess.Select(p =>
            $"\"{p.WindowTitle}\"  ({p.X},{p.Y} {p.Width}x{p.Height})  {p.SavedAt:yyyy-MM-dd HH:mm}").ToList();

        int posIdx = MenuService.PickOption("Välj position att återställa (Esc = tillbaka):", posOptions, out _);
        if (posIdx < 0) return;

        chosen = savedForProcess[posIdx];
    }
    var handles = WindowService.FindAllWindowsByProcessName(chosen.ProcessName);

    if (handles.Count == 0)
    {
        MenuService.SetPendingMessage($"Kunde inte hitta ett öppet fönster för \"{chosen.ProcessName}\".", MessageKind.Error);
        return;
    }

    foreach (var handle in handles)
        WindowService.SetPosition(handle, chosen.X, chosen.Y, chosen.Width, chosen.Height);

    MenuService.SetPendingMessage($"Återställde \"{chosen.DisplayName}\" ({handles.Count} fönster) till ({chosen.X},{chosen.Y} {chosen.Width}x{chosen.Height}).", MessageKind.Success);
}

void RunEditMode(ref bool editMode, string screenInfo)
{
    MenuService.ShowHeader($"ModernWinManager {version} [EDIT]", screenInfo);

    var options = new List<string>
    {
        "Spara ny fönsterposition",
        "Byt namn på ett sparat fönster",
        "Ta bort positioner för ett program",
        "Ta bort en enskild position",
        "Byt namn på skärmkonfig",
        "Tillbaka till Set-mode  (Esc)"
    };

    int idx = MenuService.PickOption("Välj:", options, out var special);

    if (special.HasValue)
    {
        if (special.Value.Key == ConsoleKey.Escape)
            editMode = false;
        else if (special.Value.Key == ConsoleKey.Q)
            Environment.Exit(0);
        return;
    }

    switch (idx)
    {
        case 0: SaveNewPosition(screenInfo); break;
        case 1: RenameWindow(screenInfo); break;
        case 2: DeleteAllForProgram(screenInfo); break;
        case 3: DeleteSinglePosition(screenInfo); break;
        case 4: RenameScreenConfig(screenInfo); break;
        case 5: editMode = false; break;
    }
}

void SaveNewPosition(string screenInfo)
{
    MenuService.ShowHeader("Spara ny position", screenInfo);

    var windows = WindowService.GetVisibleWindows();
    if (windows.Count == 0)
    {
        MenuService.SetPendingMessage("Inga öppna fönster hittades.", MessageKind.Warning);
        return;
    }

    var options = windows.Select(w => $"{w.ProcessName}  \"{w.Title}\"").ToList();
    int idx = MenuService.PickOption("Välj fönster att spara (Esc = tillbaka):", options, out _);
    if (idx < 0) return;

    var win = windows[idx];
    var (x, y, w2, h) = WindowService.GetPosition(win.Handle);

    var pos = new SavedPosition
    {
        ProcessName = win.ProcessName,
        WindowTitle = win.Title,
        ScreenFingerprint = currentConfig.Fingerprint,
        X = x,
        Y = y,
        Width = w2,
        Height = h
    };

    data.SavedPositions.Add(pos);
    StorageService.Save(data);

    MenuService.SetPendingMessage($"Sparade \"{win.ProcessName}\" vid ({x},{y} {w2}x{h}).", MessageKind.Success);
}

void DeleteAllForProgram(string screenInfo)
{
    MenuService.ShowHeader("Ta bort positioner för program", screenInfo);

    var programs = data.SavedPositions
        .GroupBy(p => p.ProcessName)
        .Select(g => g.Key)
        .OrderBy(n => n)
        .ToList();

    if (programs.Count == 0)
    {
        MenuService.SetPendingMessage("Inga sparade positioner.", MessageKind.Warning);
        return;
    }

    var options = programs.Select(p =>
    {
        var count = data.SavedPositions.Count(x => x.ProcessName == p);
        return $"{p}  ({count} position{(count > 1 ? "er" : "")})";
    }).ToList();

    int idx = MenuService.PickOption("Välj program (Esc = tillbaka):", options, out _);
    if (idx < 0) return;

    var chosen = programs[idx];
    int count2 = data.SavedPositions.Count(x => x.ProcessName == chosen);

    data.SavedPositions.RemoveAll(p => p.ProcessName == chosen);
    StorageService.Save(data);

    MenuService.SetPendingMessage($"Tog bort alla positioner för \"{chosen}\".", MessageKind.Success);
}

void DeleteSinglePosition(string screenInfo)
{
    MenuService.ShowHeader("Ta bort enskild position", screenInfo);

    if (data.SavedPositions.Count == 0)
    {
        MenuService.SetPendingMessage("Inga sparade positioner.", MessageKind.Warning);
        return;
    }

    var options = data.SavedPositions
        .OrderBy(p => p.ProcessName)
        .Select(p =>
        {
            var cfgName = data.ScreenConfigs.FirstOrDefault(c => c.Fingerprint == p.ScreenFingerprint)?.DisplayName
                          ?? p.ScreenFingerprint[..8];
            return $"{p.ProcessName}  \"{p.WindowTitle}\"  ({p.X},{p.Y} {p.Width}x{p.Height})  [{cfgName}]  {p.SavedAt:yyyy-MM-dd HH:mm}";
        }).ToList();

    var sorted = data.SavedPositions.OrderBy(p => p.ProcessName).ToList();

    int idx = MenuService.PickOption("Välj position att ta bort (Esc = tillbaka):", options, out _);
    if (idx < 0) return;

    var chosen = sorted[idx];
    data.SavedPositions.Remove(chosen);
    StorageService.Save(data);

    MenuService.SetPendingMessage("Positionen togs bort.", MessageKind.Success);
}

void RenameScreenConfig(string screenInfo)
{
    MenuService.ShowHeader("Byt namn på skärmkonfig", screenInfo);

    var cfg = data.ScreenConfigs.FirstOrDefault(c => c.Fingerprint == currentConfig.Fingerprint);
    if (cfg == null)
    {
        MenuService.SetPendingMessage("Skärmkonfig ej registrerad.", MessageKind.Error);
        return;
    }

    Console.WriteLine($"Nuvarande namn: {cfg.DisplayName}");
    Console.WriteLine($"Teknisk beskrivning: {cfg.Description}");
    Console.WriteLine();
    Console.Write("Nytt namn (tomt = återställ till standard): ");
    var name = Console.ReadLine()?.Trim();

    cfg.CustomName = string.IsNullOrEmpty(name) ? null : name;
    StorageService.Save(data);

    MenuService.SetPendingMessage($"Namnet uppdaterades till: {cfg.DisplayName}", MessageKind.Success);
}

void RenameWindow(string screenInfo)
{
    MenuService.ShowHeader("Byt namn på sparat fönster", screenInfo);

    var programs = data.SavedPositions
        .GroupBy(p => p.ProcessName)
        .OrderBy(g => g.First().DisplayName)
        .ToList();

    if (programs.Count == 0)
    {
        MenuService.SetPendingMessage("Inga sparade positioner.", MessageKind.Warning);
        return;
    }

    var options = programs.Select(g =>
    {
        var p = g.First();
        return p.CustomName != null ? $"{p.CustomName}  ({p.ProcessName})" : p.ProcessName;
    }).ToList();

    int idx = MenuService.PickOption("Välj program att byta namn på (Esc = tillbaka):", options, out _);
    if (idx < 0) return;

    var chosen = programs[idx];
    var current = chosen.First();

    Console.WriteLine($"Processnamn: {current.ProcessName}");
    Console.WriteLine($"Nuvarande namn: {current.DisplayName}");
    Console.WriteLine();
    Console.Write("Nytt namn (tomt = återställ till processnamn): ");
    var name = Console.ReadLine()?.Trim();
    var newName = string.IsNullOrEmpty(name) ? null : name;

    foreach (var pos in chosen)
        pos.CustomName = newName;

    StorageService.Save(data);
    MenuService.SetPendingMessage($"Döpte om \"{current.ProcessName}\" till \"{current.DisplayName}\".", MessageKind.Success);
}

void RestoreAll(List<SavedPosition> positions)
{
    var (restored, notFound) = RestoreAllPositions(positions);

    var msg = $"Återställde {restored} program.";
    if (notFound > 0)
        msg += $" {notFound} program hittades inte.";

    MenuService.SetPendingMessage(msg, notFound > 0 ? MessageKind.Warning : MessageKind.Success);
}

(int restored, int notFound) RestoreAllPositions(List<SavedPosition> positions)
{
    int restored = 0;
    int notFound = 0;

    foreach (var group in positions.GroupBy(p => p.ProcessName))
    {
        var chosen = group.First();
        var handles = WindowService.FindAllWindowsByProcessName(chosen.ProcessName);

        if (handles.Count == 0)
        {
            notFound++;
            continue;
        }

        foreach (var handle in handles)
            WindowService.SetPosition(handle, chosen.X, chosen.Y, chosen.Width, chosen.Height);

        restored++;
    }

    return (restored, notFound);
}
