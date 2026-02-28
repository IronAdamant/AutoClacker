using System;

namespace AutoClacker.Services
{
    /// <summary>
    /// Cross-platform interface for global hotkey detection.
    /// </summary>
    public interface IGlobalHotkey : IDisposable
    {
        event Action? OnHotkeyPressed;
        void Register(string keyName);
        void Unregister();
    }
}
