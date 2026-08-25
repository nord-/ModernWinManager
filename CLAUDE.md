# ModernWinManager

Konsolapp för Windows som sparar och återställer fönsterpositioner per skärmkonfiguration.

## Tech
- C# / .NET 10, `net10.0-windows`
- Inga NuGet-paket
- P/Invoke mot `user32.dll` för fönster- och monitorhantering
- JSON-lagring via `System.Text.Json` i `%LOCALAPPDATA%/ModernWinManager/positions.json`

## Bygg och kör
```
cd ModernWinManager
dotnet build
dotnet run
```

## Projektstruktur
```
ModernWinManager/
  Program.cs                      -- huvudloop, Set-mode / Edit-mode
  Models/
    WindowPosition.cs             -- MonitorInfo, ScreenConfig, SavedPosition, AppData
  Services/
    NativeInterop.cs              -- P/Invoke-signaturer (user32.dll)
    ScreenConfigService.cs        -- monitordetektering, SHA256-fingerprint
    WindowService.cs              -- fönsterenumeration, position get/set
    StorageService.cs             -- JSON load/save
    MenuService.cs                -- konsolmeny, paginering, pending messages
```

## Funktioner
- **Set-mode** (startar här): återställ sparade fönsterpositioner filtrerat på aktiv skärmkonfig
- **Edit-mode** (Ctrl+E): spara, byta namn, ta bort positioner; lista alla skärmkonfigurationer (aktiv markerad med `*`) och byta namn på valfri konfig i listan
- Listor i Edit-mode filtreras på aktiv skärmkonfig
- Skärmkonfig identifieras med SHA256-fingerprint av monitorernas layout
- Stöd för program med flera fönster (t.ex. qemu/Android-emulator) — alla fönster flyttas
- Listor: max 20 per sida, ↑↓ bläddrar, rader trunkeras med ellipsis

## Tangentkommandon
| Tangent | Funktion |
|---------|----------|
| `Ctrl+E` | Byt till Edit-mode |
| `Esc` | Tillbaka / Avsluta |
| `1`–`9` | Välj alternativ direkt |
| `1`/`2` + siffra | Välj alternativ 10–20 |
| `1`/`2` + Enter | Välj alternativ 1 eller 2 |
| `↑` / `↓` | Bläddra sidor i listor |

## Kommandoradsväxlar
| Växel | Funktion |
|-------|----------|
| `--restore-all`, `-r` | Återställ alla sparade positioner för aktiv skärmkonfig och avsluta direkt (ingen meny) |
| `--version`, `-v` | Skriv ut versionen och avsluta |

## Commit-meddelanden
Lägg inte till "Co-Authored-By: Claude" eller liknande i commit-meddelanden eller PR-kommentarer.
