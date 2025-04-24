using System.Windows.Forms;

namespace AutoClicker.Utilities
{
    public class KeyCaptureDialog : Form
    {
        private Label lblInstruction;
        private Keys selectedKey;

        public Keys SelectedKey => selectedKey;

        public KeyCaptureDialog()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Text = "Capture Key";
            this.Size = new System.Drawing.Size(300, 150);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            lblInstruction = new Label
            {
                Text = "Press a key to capture (Esc to cancel)",
                Location = new System.Drawing.Point(10, 20),
                AutoSize = true
            };

            this.Controls.Add(lblInstruction);
            this.KeyDown += KeyCaptureDialog_KeyDown;
        }

        private void KeyCaptureDialog_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
            else
            {
                selectedKey = e.KeyCode;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
    }
}