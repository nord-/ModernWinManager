# ModernWinManager

A Windows console app for saving and restoring window positions across different monitor configurations.

## Features

- **Save** window positions tagged to the current monitor layout
- **Restore** windows with one keypress — filtered to your active screen setup
- **Multi-window support** — apps like the Android emulator (qemu) that spawn multiple windows are all moved together
- **Custom names** — rename cryptic process names like `qemu-system-x86_64` to something readable
- **Rename monitor configs** — give your setups friendly names like "Home ultrawide" or "Laptop only"
- **Paginated lists** — up to 20 items per page, navigate with arrow keys

## Requirements

- Windows 10/11
- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Usage

```
cd ModernWinManager
dotnet run
```

The app starts in **Set-mode** for quick restores. Press `Ctrl+E` to enter **Edit-mode** for saving and managing positions.

### Command-line flags

| Flag | Action |
|------|--------|
| `--restore-all`, `-r` | Restore all saved positions for the current screen config and exit immediately (no menu) |
| `--version`, `-v` | Print the version and exit |

### Keyboard shortcuts

| Key | Action |
|-----|--------|
| `Ctrl+E` | Switch to Edit-mode |
| `Esc` | Back / Exit |
| `1`–`9` | Select option immediately |
| `1`/`2` + digit | Select options 10–20 |
| `1`/`2` + Enter | Select option 1 or 2 |
| `↑` / `↓` | Page through lists |

## Data

Positions are stored in `%LOCALAPPDATA%\ModernWinManager\positions.json`.

## License

MIT
