using System;
using System.Windows.Forms;
using AutoClicker;

namespace AutoClicker.Utilities
{
    public class OptionsDialog : Form
    {
        private Settings settings;
        private RadioButton rbLightMode, rbDarkMode;
        private Button btnOk, btnCancel;

        public Settings Settings => settings;

        public OptionsDialog(Settings currentSettings)
        {
            settings = new Settings
            {
                ClickScope = currentSettings.ClickScope,
                TargetApplication = currentSettings.TargetApplication,
                ActionType = currentSettings.ActionType,
                MouseButton = currentSettings.MouseButton,
                MouseMode = currentSettings.MouseMode,
                MouseHoldDuration = currentSettings.MouseHoldDuration,
                KeyboardKey = currentSettings.KeyboardKey,
                KeyboardMode = currentSettings.KeyboardMode,
                KeyboardHoldDuration = currentSettings.KeyboardHoldDuration,
                TriggerKey = currentSettings.TriggerKey,
                TriggerKeyModifiers = currentSettings.TriggerKeyModifiers,
                Interval = currentSettings.Interval,
                Mode = currentSettings.Mode,
                TotalDuration = currentSettings.TotalDuration,
                Theme = currentSettings.Theme
            };
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Text = "Options";
            this.Size = new System.Drawing.Size(320, 160); // Increased width to accommodate Dark Mode shift
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            // Theme Selection
            var lblTheme = new Label
            {
                Text = "Theme:",
                Location = new System.Drawing.Point(10, 20),
                AutoSize = true
            };
            rbLightMode = new RadioButton
            {
                Text = "Light Mode",
                Location = new System.Drawing.Point(100, 20),
                Checked = settings.Theme == "Light"
            };
            rbDarkMode = new RadioButton
            {
                Text = "Dark Mode",
                Location = new System.Drawing.Point(200, 20), // Moved to the right
                Checked = settings.Theme == "Dark"
            };

            // Buttons
            btnOk = new Button
            {
                Text = "OK",
                Location = new System.Drawing.Point(120, 80),
                DialogResult = DialogResult.OK
            };
            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new System.Drawing.Point(200, 80),
                DialogResult = DialogResult.Cancel
            };

            btnOk.Click += (s, e) =>
            {
                settings.Theme = rbLightMode.Checked ? "Light" : "Dark";
            };

            this.Controls.AddRange(new Control[] { lblTheme, rbLightMode, rbDarkMode, btnOk, btnCancel });
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }
    }
}