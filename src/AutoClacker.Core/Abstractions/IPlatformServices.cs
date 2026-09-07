using AutoClacker.Core.Domain;

namespace AutoClacker.Core.Abstractions;

/// <summary>OS facade the app depends on. Constructed only in the Desktop composition root.</summary>
public interface IPlatformServices : IDisposable
{
    string PlatformId { get; }
    PlatformCapabilities Capabilities { get; }
    IInputSimulation Input { get; }
    IHotkeyService Hotkey { get; }
}

public sealed record PlatformCapabilities(
    bool InputAvailable,
    bool HotkeyAvailable,
    string? UnavailableReason,
    bool SupportsDebugConsole);

public interface IInputSimulation : IDisposable
{
    void MouseClick(MouseButton button, ClickKind kind);
    void KeyPress(KeyToken key);
}

public interface IHotkeyService : IDisposable
{
    event Action? Pressed;
    bool TryRegister(KeyToken key);
    void Unregister();
}
