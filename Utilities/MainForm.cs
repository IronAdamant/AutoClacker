using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using AutoClicker.Utilities;

namespace AutoClicker.Utilities
{
    public partial class MainForm : Form
    {
        private Settings settings;
        private AutomationController automationController;
        private HotkeyManager hotkeyManager;
        private ApplicationDetector applicationDetector;

        // Controls
        private RadioButton rbGlobal, rbRestricted;
        private ComboBox cbApplications;
        private Button btnRefreshApps;
        private RadioButton rbMouse, rbKeyboard;
        private GroupBox gbMouseSettings, gbKeyboardSettings;
        private RadioButton rbLeft, rbRight;
        private RadioButton rbClick, rbHold;
        private NumericUpDown nudMouseHoldH, nudMouseHoldM, nudMouseHoldS, nudMouseHoldMs;
        private Button btnSetKey;
        private Label lblSelectedKey;
        private RadioButton rbPress, rbHoldKey;
        private NumericUpDown nudKeyHoldH, nudKeyHoldM, nudKeyHoldS, nudKeyHoldMs;
        private Button btnSetTriggerKey;
        private Label lblTriggerKey;
        private NumericUpDown nudIntervalH, nudIntervalM, nudIntervalS, nudIntervalMs;
        private RadioButton rbConstant, rbTimer;
        private NumericUpDown nudTotalH, nudTotalM, nudTotalS, nudTotalMs;
        private Label lblStatus;
        private Button btnReset, btnOptions;

        public bool IsAutomationRunning => automationController != null && automationController.GetType().GetField("cts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(automationController) != null;

        public Settings Settings => settings;

        public MainForm()
        {
            InitializeComponent();
            settings = SettingsManager.LoadSettings();
            automationController = new AutomationController(this);
            hotkeyManager = new HotkeyManager(this);
            applicationDetector = new ApplicationDetector();

            InitializeUI();
            ApplySettings();
            ApplyTheme();
        }

        private void InitializeComponent()
        {
            this.Text = "Automatic Mouse & Keyboard Clicker";
            this.FormClosing += (s, e) => hotkeyManager?.Dispose();
            this.Size = new Size(600, 750); // Increased height to fit all controls
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Font = new Font("Segoe UI", 9F);
            this.BackColor = Color.FromArgb(240, 240, 240);

            // Click Scope
            var gbScope = new GroupBox { Text = "Click Scope", Location = new Point(10, 10), Size = new Size(260, 120), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(245, 245, 245) };
            rbGlobal = new RadioButton { Text = "Global", Location = new Point(15, 25), Checked = true, FlatStyle = FlatStyle.Flat };
            rbGlobal.Tag = "Select to apply automation globally across the system.";
            rbRestricted = new RadioButton { Text = "Restricted to Application", Location = new Point(15, 45), FlatStyle = FlatStyle.Flat };
            rbRestricted.Tag = "Select to restrict automation to a specific application.";
            cbApplications = new ComboBox { Location = new Point(15, 85), Size = new Size(150, 25), Enabled = false, DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat };
            cbApplications.Tag = "Select the application to restrict automation to.";
            btnRefreshApps = new Button { Text = "Refresh", Location = new Point(175, 85), Size = new Size(70, 25), Enabled = false, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(100, 150, 200), ForeColor = Color.White };
            btnRefreshApps.Tag = "Refresh the list of running applications.";
            btnRefreshApps.MouseEnter += (s, e) => btnRefreshApps.BackColor = Color.FromArgb(120, 170, 220);
            btnRefreshApps.MouseLeave += (s, e) => btnRefreshApps.BackColor = Color.FromArgb(100, 150, 200);
            gbScope.Controls.AddRange(new Control[] { rbGlobal, rbRestricted, cbApplications, btnRefreshApps });
            this.Controls.Add(gbScope);

            // Action Type
            var gbActionType = new GroupBox { Text = "Action Type", Location = new Point(280, 10), Size = new Size(260, 120), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(245, 245, 245) };
            rbMouse = new RadioButton { Text = "Mouse", Location = new Point(15, 25), Checked = true, FlatStyle = FlatStyle.Flat };
            rbMouse.Tag = "Automate mouse actions (click or hold).";
            rbKeyboard = new RadioButton { Text = "Keyboard", Location = new Point(135, 25), FlatStyle = FlatStyle.Flat };
            rbKeyboard.Tag = "Automate keyboard actions (press or hold).";
            gbActionType.Controls.AddRange(new Control[] { rbMouse, rbKeyboard });
            this.Controls.Add(gbActionType);

            // Mouse Settings
            gbMouseSettings = new GroupBox { Text = "Mouse Settings", Location = new Point(10, 140), Size = new Size(260, 170), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(245, 245, 245) };
            rbLeft = new RadioButton { Text = "Left", Location = new Point(15, 25), Checked = true, FlatStyle = FlatStyle.Flat };
            rbLeft.Tag = "Use the left mouse button.";
            rbRight = new RadioButton { Text = "Right", Location = new Point(115, 25), FlatStyle = FlatStyle.Flat };
            rbRight.Tag = "Use the right mouse button.";
            rbClick = new RadioButton { Text = "Click", Location = new Point(15, 55), Checked = true, FlatStyle = FlatStyle.Flat };
            rbClick.Tag = "Perform a single click.";
            rbHold = new RadioButton { Text = "Hold", Location = new Point(115, 55), FlatStyle = FlatStyle.Flat };
            rbHold.Tag = "Hold the mouse button for a specified duration.";
            var lblHoldDuration = new Label { Text = "Hold Duration:", Location = new Point(15, 85), AutoSize = true };
            nudMouseHoldH = new NumericUpDown { Location = new Point(15, 105), Size = new Size(40, 25), Enabled = false };
            var lblMouseHoldH = new Label { Text = "h", Location = new Point(60, 108), AutoSize = true };
            nudMouseHoldM = new NumericUpDown { Location = new Point(75, 105), Size = new Size(40, 25), Enabled = false };
            var lblMouseHoldM = new Label { Text = "m", Location = new Point(120, 108), AutoSize = true };
            nudMouseHoldS = new NumericUpDown { Location = new Point(135, 105), Size = new Size(40, 25), Enabled = false };
            var lblMouseHoldS = new Label { Text = "s", Location = new Point(180, 108), AutoSize = true };
            nudMouseHoldMs = new NumericUpDown { Location = new Point(195, 105), Size = new Size(50, 25), Enabled = false };
            var lblMouseHoldMs = new Label { Text = "ms", Location = new Point(250, 108), AutoSize = true };
            gbMouseSettings.Controls.AddRange(new Control[] { rbLeft, rbRight, rbClick, rbHold, lblHoldDuration, nudMouseHoldH, lblMouseHoldH, nudMouseHoldM, lblMouseHoldM, nudMouseHoldS, lblMouseHoldS, nudMouseHoldMs, lblMouseHoldMs });
            this.Controls.Add(gbMouseSettings);

            // Keyboard Settings
            gbKeyboardSettings = new GroupBox { Text = "Keyboard Settings", Location = new Point(280, 140), Size = new Size(260, 190), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(245, 245, 245), Visible = false };
            btnSetKey = new Button { Text = "Set Key", Location = new Point(15, 25), Size = new Size(80, 25), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(100, 150, 200), ForeColor = Color.White };
            btnSetKey.Tag = "Set the key to automate.";
            btnSetKey.MouseEnter += (s, e) => btnSetKey.BackColor = Color.FromArgb(120, 170, 220);
            btnSetKey.MouseLeave += (s, e) => btnSetKey.BackColor = Color.FromArgb(100, 150, 200);
            lblSelectedKey = new Label { Text = "None", Location = new Point(105, 30), Size = new Size(140, 25), BorderStyle = BorderStyle.FixedSingle };
            rbPress = new RadioButton { Text = "Press", Location = new Point(15, 55), Checked = true, FlatStyle = FlatStyle.Flat };
            rbPress.Tag = "Perform a single key press.";
            rbHoldKey = new RadioButton { Text = "Hold", Location = new Point(135, 55), FlatStyle = FlatStyle.Flat };
            rbHoldKey.Tag = "Hold the key for a specified duration.";
            var lblKeyHoldDuration = new Label { Text = "Hold Duration:", Location = new Point(35, 85), AutoSize = true };
            nudKeyHoldH = new NumericUpDown { Location = new Point(35, 105), Size = new Size(40, 25), Enabled = false };
            var lblKeyHoldH = new Label { Text = "h", Location = new Point(80, 108), AutoSize = true };
            nudKeyHoldM = new NumericUpDown { Location = new Point(95, 105), Size = new Size(40, 25), Enabled = false };
            var lblKeyHoldM = new Label { Text = "m", Location = new Point(140, 108), AutoSize = true };
            nudKeyHoldS = new NumericUpDown { Location = new Point(155, 105), Size = new Size(40, 25), Enabled = false };
            var lblKeyHoldS = new Label { Text = "s", Location = new Point(200, 108), AutoSize = true };
            nudKeyHoldMs = new NumericUpDown { Location = new Point(215, 105), Size = new Size(50, 25), Enabled = false };
            var lblKeyHoldMs = new Label { Text = "ms", Location = new Point(270, 108), AutoSize = true };
            gbKeyboardSettings.Controls.AddRange(new Control[] { btnSetKey, lblSelectedKey, rbPress, rbHoldKey, lblKeyHoldDuration, nudKeyHoldH, lblKeyHoldH, nudKeyHoldM, lblKeyHoldM, nudKeyHoldS, lblKeyHoldS, nudKeyHoldMs, lblKeyHoldMs });
            this.Controls.Add(gbKeyboardSettings);

            // Toggle Key (Start/Stop)
            var gbTriggerKey = new GroupBox { Text = "Toggle Key (Start/Stop)", Location = new Point(10, 320), Size = new Size(260, 80), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(245, 245, 245) };
            btnSetTriggerKey = new Button { Text = "Set Toggle Key", Location = new Point(15, 35), Size = new Size(120, 25), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(100, 150, 200), ForeColor = Color.White };
            btnSetTriggerKey.Tag = "Set the key to start/stop automation.";
            btnSetTriggerKey.MouseEnter += (s, e) => btnSetTriggerKey.BackColor = Color.FromArgb(120, 170, 220);
            btnSetTriggerKey.MouseLeave += (s, e) => btnSetTriggerKey.BackColor = Color.FromArgb(100, 150, 200);
            lblTriggerKey = new Label { Text = "F5", Location = new Point(145, 40), Size = new Size(100, 25), BorderStyle = BorderStyle.FixedSingle };
            gbTriggerKey.Controls.AddRange(new Control[] { btnSetTriggerKey, lblTriggerKey });
            this.Controls.Add(gbTriggerKey);

            // Speed
            var gbSpeed = new GroupBox { Text = "Speed", Location = new Point(280, 340), Size = new Size(260, 120), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(245, 245, 245) };
            gbSpeed.Tag = "Set how often the automation repeats (interval between actions).";
            var lblInterval = new Label { Text = "Interval:", Location = new Point(15, 25), AutoSize = true };
            nudIntervalH = new NumericUpDown { Location = new Point(15, 45), Size = new Size(40, 25) };
            var lblIntervalH = new Label { Text = "h", Location = new Point(60, 48), AutoSize = true };
            nudIntervalM = new NumericUpDown { Location = new Point(75, 45), Size = new Size(40, 25) };
            var lblIntervalM = new Label { Text = "m", Location = new Point(120, 48), AutoSize = true };
            nudIntervalS = new NumericUpDown { Location = new Point(135, 45), Size = new Size(40, 25) };
            var lblIntervalS = new Label { Text = "s", Location = new Point(180, 48), AutoSize = true };
            nudIntervalMs = new NumericUpDown { Location = new Point(195, 45), Size = new Size(50, 25) };
            var lblIntervalMs = new Label { Text = "ms", Location = new Point(250, 48), AutoSize = true };
            gbSpeed.Controls.AddRange(new Control[] { lblInterval, nudIntervalH, lblIntervalH, nudIntervalM, lblIntervalM, nudIntervalS, lblIntervalS, nudIntervalMs, lblIntervalMs });
            this.Controls.Add(gbSpeed);

            // Mode
            var gbMode = new GroupBox { Text = "Mode", Location = new Point(10, 410), Size = new Size(260, 180), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(245, 245, 245) };
            gbMode.Tag = "Choose whether automation runs indefinitely (Constant) or for a set time (Timer).";
            rbConstant = new RadioButton { Text = "Constant", Location = new Point(15, 25), Checked = true, FlatStyle = FlatStyle.Flat };
            rbConstant.Tag = "Run automation indefinitely.";
            rbTimer = new RadioButton { Text = "Timer", Location = new Point(15, 55), FlatStyle = FlatStyle.Flat };
            rbTimer.Tag = "Run automation for a specified duration.";
            var lblTotalDuration = new Label { Text = "Total Duration:", Location = new Point(15, 85), Enabled = false, AutoSize = true };
            nudTotalH = new NumericUpDown { Location = new Point(15, 105), Size = new Size(40, 25), Enabled = false };
            var lblTotalH = new Label { Text = "h", Location = new Point(60, 108), AutoSize = true, Enabled = false };
            nudTotalM = new NumericUpDown { Location = new Point(75, 105), Size = new Size(40, 25), Enabled = false };
            var lblTotalM = new Label { Text = "m", Location = new Point(120, 108), AutoSize = true, Enabled = false };
            nudTotalS = new NumericUpDown { Location = new Point(135, 105), Size = new Size(40, 25), Enabled = false };
            var lblTotalS = new Label { Text = "s", Location = new Point(180, 108), AutoSize = true, Enabled = false };
            nudTotalMs = new NumericUpDown { Location = new Point(195, 105), Size = new Size(50, 25), Enabled = false };
            var lblTotalMs = new Label { Text = "ms", Location = new Point(250, 108), AutoSize = true, Enabled = false };
            gbMode.Controls.AddRange(new Control[] { rbConstant, rbTimer, lblTotalDuration, nudTotalH, lblTotalH, nudTotalM, lblTotalM, nudTotalS, lblTotalS, nudTotalMs, lblTotalMs });
            this.Controls.Add(gbMode);

            // Status Indicator
            lblStatus = new Label { Text = "Not running", ForeColor = Color.Red, Location = new Point(10, 620), Size = new Size(100, 25), TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.FromArgb(245, 245, 245) };
            lblStatus.Tag = "Current status of the automation.";
            this.Controls.Add(lblStatus);

            // Buttons
            btnReset = new Button { Text = "Reset to Default", Location = new Point(280, 620), Size = new Size(120, 30), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(100, 150, 200), ForeColor = Color.White };
            btnReset.Tag = "Reset all settings to their default values.";
            btnReset.MouseEnter += (s, e) => btnReset.BackColor = Color.FromArgb(120, 170, 220);
            btnReset.MouseLeave += (s, e) => btnReset.BackColor = Color.FromArgb(100, 150, 200);
            btnOptions = new Button { Text = "Options", Location = new Point(410, 620), Size = new Size(120, 30), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(100, 150, 200), ForeColor = Color.White };
            btnOptions.Tag = "Configure additional options like theme.";
            btnOptions.MouseEnter += (s, e) => btnOptions.BackColor = Color.FromArgb(120, 170, 220);
            btnOptions.MouseLeave += (s, e) => btnOptions.BackColor = Color.FromArgb(100, 150, 200);
            this.Controls.AddRange(new Control[] { btnReset, btnOptions });

            // Add Tooltips
            var toolTip = new ToolTip();
            foreach (Control c in this.Controls)
            {
                if (c.Tag != null) toolTip.SetToolTip(c, c.Tag.ToString());
                foreach (Control subC in c.Controls)
                {
                    if (subC.Tag != null) toolTip.SetToolTip(subC, subC.Tag.ToString());
                }
            }

            // Event Handlers - Attach after all controls are initialized
            rbGlobal.CheckedChanged += RbScope_CheckedChanged;
            rbRestricted.CheckedChanged += RbScope_CheckedChanged;
            btnRefreshApps.Click += BtnRefreshApps_Click;
            rbMouse.CheckedChanged += RbActionType_CheckedChanged;
            rbKeyboard.CheckedChanged += RbActionType_CheckedChanged;
            rbHold.CheckedChanged += RbMouseMode_CheckedChanged;
            rbHoldKey.CheckedChanged += RbKeyboardMode_CheckedChanged;
            btnSetKey.Click += BtnSetKey_Click;
            btnSetTriggerKey.Click += BtnSetTriggerKey_Click;
            rbTimer.CheckedChanged += RbMode_CheckedChanged;
            btnReset.Click += BtnReset_Click;
            btnOptions.Click += BtnOptions_Click;
        }

        private void InitializeUI()
        {
            // Populate application dropdown
            cbApplications.DataSource = applicationDetector.GetRunningApplications();
            // Set default values for NumericUpDowns
            SetNumericUpDownDefaults();
        }

        private void ApplySettings()
        {
            // Apply settings to controls
            rbGlobal.Checked = settings.ClickScope == "Global";
            rbRestricted.Checked = settings.ClickScope == "Restricted";
            if (!string.IsNullOrEmpty(settings.TargetApplication))
                cbApplications.SelectedItem = settings.TargetApplication;
            rbMouse.Checked = settings.ActionType == "Mouse";
            rbKeyboard.Checked = settings.ActionType == "Keyboard";
            rbLeft.Checked = settings.MouseButton == "Left";
            rbRight.Checked = settings.MouseButton == "Right";
            rbClick.Checked = settings.MouseMode == "Click";
            rbHold.Checked = settings.MouseMode == "Hold";
            SetTimeSpanToNumericUpDowns(nudMouseHoldH, nudMouseHoldM, nudMouseHoldS, nudMouseHoldMs, settings.MouseHoldDuration);
            lblSelectedKey.Text = settings.KeyboardKey == Keys.None ? "None" : settings.KeyboardKey.ToString();
            rbPress.Checked = settings.KeyboardMode == "Press";
            rbHoldKey.Checked = settings.KeyboardMode == "Hold";
            SetTimeSpanToNumericUpDowns(nudKeyHoldH, nudKeyHoldM, nudKeyHoldS, nudKeyHoldMs, settings.KeyboardHoldDuration);
            lblTriggerKey.Text = settings.TriggerKey.ToString();
            SetTimeSpanToNumericUpDowns(nudIntervalH, nudIntervalM, nudIntervalS, nudIntervalMs, settings.Interval);
            rbConstant.Checked = settings.Mode == "Constant";
            rbTimer.Checked = settings.Mode == "Timer";
            SetTimeSpanToNumericUpDowns(nudTotalH, nudTotalM, nudTotalS, nudTotalMs, settings.TotalDuration);

            // Ensure "Refresh" button state matches the "Restricted" radio button
            cbApplications.Enabled = rbRestricted.Checked;
            btnRefreshApps.Enabled = rbRestricted.Checked;
        }

        private void ApplyTheme()
        {
            this.BackColor = settings.Theme == "Light" ? Color.FromArgb(240, 240, 240) : Color.FromArgb(30, 30, 30);
            this.ForeColor = settings.Theme == "Light" ? Color.Black : Color.White;
            foreach (Control c in this.Controls)
            {
                c.BackColor = settings.Theme == "Light" ? Color.FromArgb(240, 240, 240) : Color.FromArgb(30, 30, 30);
                c.ForeColor = settings.Theme == "Light" ? Color.Black : Color.White;
                if (c is GroupBox)
                {
                    c.BackColor = settings.Theme == "Light" ? Color.FromArgb(245, 245, 245) : Color.FromArgb(40, 40, 40);
                }
                else if (c is Button)
                {
                    ((Button)c).BackColor = Color.FromArgb(100, 150, 200);
                    ((Button)c).ForeColor = Color.White;
                }
                else if (c == lblStatus)
                {
                    c.BackColor = settings.Theme == "Light" ? Color.FromArgb(245, 245, 245) : Color.FromArgb(40, 40, 40);
                }
            }
        }

        // Event Handlers
        private void RbScope_CheckedChanged(object sender, EventArgs e)
        {
            bool isRestricted = rbRestricted.Checked;
            cbApplications.Enabled = isRestricted;
            btnRefreshApps.Enabled = isRestricted;
            settings.ClickScope = isRestricted ? "Restricted" : "Global";
        }

        private void BtnRefreshApps_Click(object sender, EventArgs e)
        {
            cbApplications.DataSource = applicationDetector.GetRunningApplications();
        }

        private void RbActionType_CheckedChanged(object sender, EventArgs e)
        {
            gbMouseSettings.Visible = rbMouse.Checked;
            gbKeyboardSettings.Visible = rbKeyboard.Checked;
            settings.ActionType = rbMouse.Checked ? "Mouse" : "Keyboard";
        }

        private void RbMouseMode_CheckedChanged(object sender, EventArgs e)
        {
            bool isHold = rbHold.Checked;
            nudMouseHoldH.Enabled = isHold;
            nudMouseHoldM.Enabled = isHold;
            nudMouseHoldS.Enabled = isHold;
            nudMouseHoldMs.Enabled = isHold;
            settings.MouseMode = isHold ? "Hold" : "Click";
        }

        private void RbKeyboardMode_CheckedChanged(object sender, EventArgs e)
        {
            bool isHold = rbHoldKey.Checked;
            nudKeyHoldH.Enabled = isHold;
            nudKeyHoldM.Enabled = isHold;
            nudKeyHoldS.Enabled = isHold;
            nudKeyHoldMs.Enabled = isHold;
            settings.KeyboardMode = isHold ? "Hold" : "Press";
        }

        private void BtnSetKey_Click(object sender, EventArgs e)
        {
            // Logic to set keyboard key
            var keyDialog = new KeyCaptureDialog();
            if (keyDialog.ShowDialog() == DialogResult.OK)
            {
                settings.KeyboardKey = keyDialog.SelectedKey;
                lblSelectedKey.Text = settings.KeyboardKey == Keys.None ? "None" : settings.KeyboardKey.ToString();
            }
        }

        private void BtnSetTriggerKey_Click(object sender, EventArgs e)
        {
            // Logic to set trigger key
            var keyDialog = new KeyCaptureDialog();
            if (keyDialog.ShowDialog() == DialogResult.OK)
            {
                settings.TriggerKey = keyDialog.SelectedKey;
                lblTriggerKey.Text = settings.TriggerKey.ToString();
                hotkeyManager.RegisterTriggerHotkey(settings.TriggerKey, settings.TriggerKeyModifiers);
            }
        }

        private void RbMode_CheckedChanged(object sender, EventArgs e)
        {
            bool isTimer = rbTimer.Checked;
            nudTotalH.Enabled = isTimer;
            nudTotalM.Enabled = isTimer;
            nudTotalS.Enabled = isTimer;
            nudTotalMs.Enabled = isTimer;
            settings.Mode = isTimer ? "Timer" : "Constant";
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            settings = Settings.Default;
            ApplySettings();
        }

        private void BtnOptions_Click(object sender, EventArgs e)
        {
            // Open options dialog
            var optionsDialog = new OptionsDialog(settings);
            if (optionsDialog.ShowDialog() == DialogResult.OK)
            {
                settings = optionsDialog.Settings;
                ApplyTheme();
            }
        }

        // Helper Methods
        private void SetNumericUpDownDefaults()
        {
            // Set min/max values for NumericUpDowns
            foreach (var nud in new NumericUpDown[] { nudMouseHoldH, nudMouseHoldM, nudMouseHoldS, nudKeyHoldH, nudKeyHoldM, nudKeyHoldS, nudIntervalH, nudIntervalM, nudIntervalS, nudTotalH, nudTotalM, nudTotalS })
            {
                nud.Minimum = 0;
                nud.Maximum = 59;
            }
            foreach (var nud in new NumericUpDown[] { nudMouseHoldMs, nudKeyHoldMs, nudIntervalMs, nudTotalMs })
            {
                nud.Minimum = 0;
                nud.Maximum = 999;
            }
        }

        private void SetTimeSpanToNumericUpDowns(NumericUpDown h, NumericUpDown m, NumericUpDown s, NumericUpDown ms, TimeSpan time)
        {
            h.Value = time.Hours;
            m.Value = time.Minutes;
            s.Value = time.Seconds;
            ms.Value = time.Milliseconds;
        }

        public void StartAutomation()
        {
            SettingsManager.SaveSettings(settings);
            Task.Run(() => automationController.StartAutomation(settings));
            UpdateStatus("Running", Color.Green);
        }

        public void StopAutomation()
        {
            automationController.StopAutomation();
            UpdateStatus("Not running", Color.Red);
        }

        public void UpdateStatus(string text, Color color)
        {
            lblStatus.Text = text;
            lblStatus.ForeColor = color;
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (hotkeyManager != null)
            {
                hotkeyManager.ProcessHotkey(m);
            }
        }
    }
}