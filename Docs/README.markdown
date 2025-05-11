# AutoClacker

## Version
1.0.0.1 (as of May 11, 2025)

AutoClacker is a Windows application that automates mouse clicks and keyboard key presses. It supports global or application-restricted modes, making it suitable for offline tasks like automating actions in games or applications.

![AutoClacker Screenshot](Images/AutoClacker_Not_Running.png)

## Features

- **Global or Restricted Mode**: Automate actions system-wide or limit them to a specific application.
- **Mouse and Keyboard Automation**: Configure mouse clicks (single/double, click/hold) or keyboard key presses (press/hold).
- **Customizable Settings**: Adjust intervals, durations, and hotkeys via a user-friendly GUI.
- **Persistent Settings**: Settings are saved for consistency across sessions.
- **Hotkey Support**: Toggle automation with a configurable hotkey (default: F5).

## Installation

1. Clone or download the repository to your local machine.
2. Open the solution (`AutoClacker.sln`) in Visual Studio 2022 or a compatible C# IDE.
3. Build the project to resolve dependencies.
4. Run the application from the IDE or the compiled executable (`bin\Debug\AutoClacker.exe`).

## Usage

1. **Select Scope**: Choose "Global" or "Restricted to Application" mode.
2. **Configure Actions**: Set mouse (button, click type, mode) or keyboard (key, mode) actions.
3. **Set Toggle Key**: Define a hotkey to start/stop automation (default: F5).
4. **Adjust Speed and Duration**: Customize the interval between actions and total duration (if applicable).
5. **Start/Stop Automation**: Press the toggle key to begin or end automation.

## Project Structure

- `Views/MainWindow.xaml`: Main GUI for configuring and controlling automation.
- `Models/Settings.cs`: Defines configuration settings.
- `Utilities/SettingsManager.cs`: Manages loading and saving settings.
- `Utilities/ApplicationDetector.cs`: Detects running applications for restricted mode.
- `Utilities/HotkeyManager.cs`: Handles global hotkeys.
- `Controllers/AutomationController.cs`: Core logic for mouse/keyboard automation.
- `ViewModels/MainViewModel.cs`: Coordinates UI and automation logic.

## License

Licensed under the MIT License. See `Docs/LICENSE.txt` for details.