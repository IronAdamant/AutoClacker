# AutoClacker

Cross-platform mouse and keyboard click automation, built with Avalonia UI and .NET 10.

**Open source** under the [MIT License](Docs/LICENSE.txt) (copyright Aron Amos). Creation of IronAdamant, 2025.

<p align="center">
  <img src="Images/icon.png" width="160" alt="AutoClacker emblem"/>
</p>

![AutoClacker Screenshot](Images/AutoClacker.png)

Windows and macOS have official portable builds. There is **no installer**: run from source with `dotnet run`, or download / publish a self-contained binary and run that file. Linux is in the source tree for anyone who wants to fork and publish their own build.

Building from source requires the **.NET 10 SDK** (the .NET 10 *runtime* alone is not enough). A published Windows `.exe` is self-contained and does **not** need the SDK to run. Windows portable builds are on [Releases](https://github.com/IronAdamant/AutoClacker/releases). Apple Silicon macOS builds (`.app` inside a zip) are on the same [Releases](https://github.com/IronAdamant/AutoClacker/releases) page.

## Features

- **Mouse mode** — left / right / middle button, single or double click
- **Keyboard mode** — repeat a chosen key
- **Optional action counter** — a checkbox plus a count field:
  - **Unchecked**, or the count field **empty / unset** → the session runs until you stop it (infinite)
  - **Enabled** with a positive integer **N** → remaining counts down by one per action; when remaining reaches **0**, all automation stops
- Global hotkey start/stop (default **F6**)
- Interval 10–2000 ms
- Settings persisted under the OS app-data directory as `AutoClacker/settings.json`
- **Optional debug log** — Settings checkbox **Write debug log** (visible on Windows, macOS, and Linux). Unchecked: no debug log writes. Checked: Info lines are written to `AutoClacker/debug.log` under app data. Windows may also show a debug console (separate checkbox, Windows only).
- Native P/Invoke per OS (no helper tools like xdotool)
- Capability-aware UI: will not pretend to “Run” when input is unavailable

## Status

| Platform | Input | Global hotkey | Notes |
|----------|-------|---------------|--------|
| **Windows** | Supported | Supported | Primary / best-tested path |
| **macOS** | Requires Accessibility | Requires Accessibility | Official **Apple Silicon** `.app` on [Releases](https://github.com/IronAdamant/AutoClacker/releases). Grant **AutoClacker** in System Settings → Privacy & Security → Accessibility |
| **Linux** | X11 / XWayland | X11 / XWayland | No official portable build. Fork and publish if you need one. Pure Wayland without XWayland is **not** supported |

## Layout

One solution (`AutoClacker.slnx`), shared UI, three OS platform modules:

```text
src/
  AutoClacker.Core/       # Contracts, settings, click session (no Avalonia)
  AutoClacker.App/        # Avalonia UI + view models
  AutoClacker.Windows/    # WindowsPlatform facade
  AutoClacker.MacOS/      # MacOSPlatform facade
  AutoClacker.Linux/      # LinuxPlatform facade
  AutoClacker.Desktop/    # Host / composition root
tests/
  AutoClacker.Core.Tests/
```

The app depends only on `IPlatformServices`. OS selection happens in `Desktop` only.

## Build, test, run

Requires the **.NET 10 SDK**, not only the runtime. Confirm with `dotnet --list-sdks` — a `10.x` SDK must be listed. A `9.x` SDK cannot target `net10.0` and fails with `NETSDK1045`.

Install from [https://aka.ms/dotnet/download](https://aka.ms/dotnet/download), or on Windows:

```powershell
winget install Microsoft.DotNet.SDK.10
```

Then:

```bash
dotnet build AutoClacker.slnx -c Release
dotnet test  AutoClacker.slnx -c Release

# Run from source (repo root)
dotnet run --project src/AutoClacker.Desktop -c Release
```

## Portable publish (no installer)

Self-contained, single-file output. Copy the published folder and run the binary — no MSI, DMG, pkg, or setup wizard.

The **host RID** is the runtime identifier of the machine you are building on (`dotnet --info`, look for `RID:`). Publishing with that RID produces a binary for this computer. You can also pass another RID to cross-publish.

```bash
# Host RID (this machine)
./scripts/publish.sh

# Explicit targets
./scripts/publish.sh win-x64
./scripts/publish.sh linux-x64
./scripts/publish.sh osx-arm64
```

Equivalent `dotnet publish` (output under `publish/`, which is gitignored):

```bash
dotnet publish src/AutoClacker.Desktop -c Release -r win-x64   --self-contained -p:PublishSingleFile=true -o publish/win-x64
dotnet publish src/AutoClacker.Desktop -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true -o publish/linux-x64
dotnet publish src/AutoClacker.Desktop -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=true -o publish/osx-arm64
```

Then run `publish/win-x64/AutoClacker.exe` on Windows. On macOS, `scripts/publish.sh osx-arm64` wraps `publish/osx-arm64/AutoClacker.app` so the menu bar and Dock say **AutoClacker**, not “Avalonia Application”. Official Apple Silicon zips are on [Releases](https://github.com/IronAdamant/AutoClacker/releases). Intel Macs can publish `osx-x64` themselves.

`scripts/publish.sh` takes an optional RID argument and defaults to the current host RID. A `linux-x64` RID exists in the script for forks; there is no official Linux download.

## Platform notes

- **Windows:** Desktop input injection needs no extra permission for normal use. Live runs are a GUI app (`WinExe`) with **no console window** unless you enable **Show Debug Console** in Settings (requires restart). Optional **Write debug log** writes to `%AppData%\AutoClacker\debug.log`.
- **macOS (Apple Silicon):** Download `AutoClacker-osx-arm64.zip` from [Releases](https://github.com/IronAdamant/AutoClacker/releases), unzip, then **move `AutoClacker.app` into `/Applications`** and open it from there. Opening from Downloads can make macOS treat each launch as a new app and ask for Accessibility again. No .NET SDK is required to run it. Not notarized: if Gatekeeper blocks it, right-click → **Open**. Grant **AutoClacker** (the `.app` in Applications, not Terminal) in System Settings → Privacy & Security → Accessibility — injection and the global hotkey both need it. After you tick the checkbox, quit and reopen once. If old AutoClacker rows are listed, remove them and enable only the current app. After **Start**, click the target app so injected keys do not land on AutoClacker. Optional **Write debug log** writes to `~/Library/Application Support/AutoClacker/debug.log`. If you `dotnet run` from a terminal or IDE, grant Accessibility to that host instead — it is a different identity from the `.app`.
- **Linux:** Not an official portable release. The tree still has an X11 / XWayland module (`libX11` / `libXtst`) for anyone who wants to fork and publish. Pure Wayland without XWayland is not supported.

## License

MIT — see [`Docs/LICENSE.txt`](Docs/LICENSE.txt). Copyright (c) 2025 Aron Amos.

Creation of IronAdamant, 2025.
