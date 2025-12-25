# AutoClacker

A cross-platform auto-clicker built with Avalonia UI and .NET 9.

## Features
- **Cross-platform**: Works on Windows, Linux, and macOS
- **Self-contained**: No runtime installation required
- **Lightweight UI**: Modern Fluent design
- **Configurable**: Adjustable click interval, button, and type

## Download

Pre-built self-contained executables:
- `publish/win-x64/AutoClacker.exe` - Windows
- `publish/linux-x64/AutoClacker` - Linux (requires xdotool)
- `publish/osx-x64/AutoClacker` - macOS Intel
- `publish/osx-arm64/AutoClacker` - macOS Apple Silicon

## Build from Source

```bash
# Windows
dotnet publish -c Release -r win-x64 -o publish/win-x64

# Linux
dotnet publish -c Release -r linux-x64 -o publish/linux-x64

# macOS Intel
dotnet publish -c Release -r osx-x64 -o publish/osx-x64

# macOS Apple Silicon
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
├── MainWindow.axaml    # UI definition
└── wiki-local/         # Documentation
```

## License

MIT License - Creation of IronAdamant, 2025
