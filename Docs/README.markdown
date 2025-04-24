# Automatic Mouse & Keyboard Clicker

This application automates mouse clicks and keyboard key presses on Windows. It can operate globally or be restricted to a specific application, making it ideal for offline tasks such as automating actions in offline games or applications.

## Features

- **Global or Restricted Mode**: Choose to automate actions across the entire system or limit them to a specific application.
- **Mouse and Keyboard Automation**: Configure actions for mouse clicks or keyboard key presses, with options to hold keys or buttons.
- **Customizable Settings**: Adjust intervals, durations, and hotkeys to suit your needs.
- **Persistent Settings**: Settings are saved in a JSON file for easy persistence between sessions.
- **User-Friendly GUI**: A graphical interface makes it easy to configure and control the automation.

## Installation

1. Clone or download the repository to your local machine.
2. Open the solution in Visual Studio (or your preferred C# IDE).
3. Build the project to ensure all dependencies are resolved.
4. Run the application from the IDE or by executing the compiled executable.

## Usage

1. **Select Scope**: Choose between "Global" or "Restricted to Application" mode.
2. **Configure Actions**: Set up mouse or keyboard actions, including hold durations if applicable.
3. **Set Toggle Key**: Define a hotkey to start/stop the automation (default: F5).
4. **Adjust Speed and Duration**: Customize the interval between actions ("Speed") and the total duration of automation ("Mode"). Tooltips in the UI explain the difference between these settings.
5. **Start/Stop Automation**: Use the toggle key (default: F5) to start or stop the automation.

### Settings File

A default `settings.json` file is provided in the `configs/` directory. When you run the application, runtime settings are saved to `configs/settings.json` in the application directory (e.g., `bin/Debug/configs/`). These runtime files are not tracked in the repository to avoid conflicts.

## Project Structure

- `MainForm.cs`: The main GUI form that coordinates all components.
- `Models/Settings.cs`: Defines the configuration settings for the application.
- `Utilities/SettingsManager.cs`: Handles loading and saving settings to JSON.
- `Utilities/ApplicationDetector.cs`: Detects running applications for restricted mode.
- `Utilities/HotkeyManager.cs`: Manages global hotkeys.
- `Utilities/KeyCaptureDialog.cs`: Dialog for capturing keyboard input.
- `Utilities/OptionsDialog.cs`: Dialog for additional options like theme selection.
- `Controllers/AutomationController.cs`: Controls the automation logic.

## License

This project is licensed under the MIT License. See the `Docs/LICENSE.txt` file for details.