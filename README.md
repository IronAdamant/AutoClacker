# AutoClacker

A cross-platform auto-clicker built with Avalonia UI and .NET 9.

![AutoClacker Screenshot](images/AutoClacker.png)

## Features
- **Cross-platform**: Works on Windows, Linux, and macOS
- **Global hotkey**: Works even when app is not focused
- **Debug console**: Optional live click counter
- **Self-contained**: No runtime installation required
- **Lightweight UI**: Modern Fluent design
- **Configurable**: Adjustable click interval, button, and type

## Download

Get the latest release from the [Releases](../../releases) page.

## Build from Source

```bash
# Windows (single-file)
dotnet-warp . -r win-x64 -o publish/AutoClacker.exe

# Windows (folder)
dotnet publish -c Release -r win-x64 -o publish/win-x64

# Linux
dotnet publish -c Release -r linux-x64 -o publish/linux-x64

# macOS
dotnet publish -c Release -r osx-arm64 -o publish/osx-arm64
```

## Linux Requirements

On Linux, install `xdotool` for input simulation:
```bash
sudo apt install xdotool  # Debian/Ubuntu
sudo pacman -S xdotool    # Arch
```

## Project Structure

```
AutoClacker/
├── Services/           # Platform-specific input simulators
├── ViewModels/         # MVVM ViewModels
├── Models/             # Data models
└── MainWindow.axaml    # UI definition
```

## License

MIT License - Creation of IronAdamant, 2025

