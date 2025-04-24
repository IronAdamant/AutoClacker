# Automatic Mouse & Keyboard Clicker

This application automates mouse clicks and keyboard key presses on Windows. It can operate globally or be restricted to a specific application, making it ideal for offline tasks such as automating actions in offline games or applications.

**Important Note:** This tool is intended for offline use only. Do not use it in online games or MMOs to avoid the risk of bans or violations of terms of service.

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
3. **Set Trigger Key**: Define a hotkey to start the automation.
4. **Adjust Speed and Duration**: Customize the interval between actions and the total duration of automation.
5. **Start/Stop Automation**: Use the trigger key to start and the stop key (default: Esc) to stop the automation.

## Project Structure
- `MainForm.cs`: The main GUI form that coordinates all components.
- `Models/Settings.cs`: Defines the configuration settings for the application.
- `Utilities/SettingsManager.cs`: Handles loading and saving settings to JSON.
- `Utilities/ApplicationDetector.cs`: Detects running applications for restricted mode.
- `Utilities/HotkeyManager.cs`: Manages global hotkeys and keyboard hooks.
- `Controllers/AutomationController.cs`: Controls the automation logic.

## License
This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.