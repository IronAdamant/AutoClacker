using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AutoClicker.Utilities
{
    public class HotkeyManager : IDisposable
    {
        private const int WM_HOTKEY = 0x0312;
        private const int MOD_NONE = 0x0000;
        private const int DebounceIntervalMs = 500; // Debounce interval in milliseconds

        private MainForm form;
        private int triggerHotkeyId = 1;
        private DateTime lastTriggerTime = DateTime.MinValue;

        public HotkeyManager(MainForm form)
        {
            this.form = form;
            RegisterTriggerHotkey(form.Settings.TriggerKey, form.Settings.TriggerKeyModifiers);
        }

        public void RegisterTriggerHotkey(Keys key, int modifiers)
        {
            UnregisterHotKey(form.Handle, triggerHotkeyId);
            bool success = RegisterHotKey(form.Handle, triggerHotkeyId, modifiers, (int)key);
            if (!success)
            {
                MessageBox.Show($"Failed to register hotkey {key}. It may be in use by another application.", "Hotkey Registration Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public void ProcessHotkey(Message m)
        {
            if (m.Msg == WM_HOTKEY && (int)m.WParam == triggerHotkeyId)
            {
                // Debounce: Ignore if the last trigger was less than 500ms ago
                DateTime now = DateTime.Now;
                if ((now - lastTriggerTime).TotalMilliseconds < DebounceIntervalMs)
                    return;

                lastTriggerTime = now;

                // Toggle automation
                if (!form.IsAutomationRunning)
                {
                    form.StartAutomation();
                }
                else
                {
                    form.StopAutomation();
                }
            }
        }

        public void Dispose()
        {
            UnregisterHotKey(form.Handle, triggerHotkeyId);
        }

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}