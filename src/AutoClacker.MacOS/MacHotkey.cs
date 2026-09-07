using System.Runtime.InteropServices;
using AutoClacker.Core.Abstractions;
using AutoClacker.Core.Domain;

namespace AutoClacker.MacOS;

internal partial class MacHotkey : IHotkeyService
{
    public event Action? Pressed;

    private readonly ILog _log;
    private readonly object _gate = new();
    private IntPtr _eventTap;
    private IntPtr _runLoopSource;
    private IntPtr _runLoop;
    private Thread? _thread;
    private readonly ManualResetEventSlim _ready = new(false);
    private ushort _targetKeycode;
    private bool _disposed;
    private volatile bool _stop;

    private const string CoreGraphics = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";
    private const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

    private const int kCGSessionEventTap = 1;
    private const int kCGHeadInsertEventTap = 0;
    private const int kCGEventTapOptionListenOnly = 1;
    private const int kCGEventKeyDown = 10;
    private const int kCGEventTapDisabledByTimeout = -2;
    private const int kCGEventTapDisabledByUserInput = -1;
    private const int kCGKeyboardEventAutorepeat = 8;
    private const int kCGKeyboardEventKeycode = 9;

    // CFRunLoopRunInMode return codes
    private const int kCFRunLoopRunFinished = 1;
    private const int kCFRunLoopRunStopped = 2;

    [LibraryImport(CoreGraphics)]
    private static partial IntPtr CGEventTapCreate(int tap, int place, int options,
        ulong eventsOfInterest, IntPtr callback, IntPtr userInfo);

    [LibraryImport(CoreGraphics)]
    private static partial void CGEventTapEnable(IntPtr tap, [MarshalAs(UnmanagedType.U1)] bool enable);

    [LibraryImport(CoreGraphics)]
    private static partial long CGEventGetIntegerValueField(IntPtr eventRef, int field);

    [LibraryImport(CoreFoundation)]
    private static partial IntPtr CFMachPortCreateRunLoopSource(IntPtr allocator, IntPtr port, long order);

    [LibraryImport(CoreFoundation)]
    private static partial void CFRunLoopAddSource(IntPtr rl, IntPtr source, IntPtr mode);

    [LibraryImport(CoreFoundation)]
    private static partial void CFRunLoopRemoveSource(IntPtr rl, IntPtr source, IntPtr mode);

    [LibraryImport(CoreFoundation)]
    private static partial int CFRunLoopRunInMode(IntPtr mode, double seconds,
        [MarshalAs(UnmanagedType.U1)] bool returnAfterSourceHandled);

    [LibraryImport(CoreFoundation)]
    private static partial void CFRunLoopStop(IntPtr rl);

    [LibraryImport(CoreFoundation)]
    private static partial IntPtr CFRunLoopGetCurrent();

    [LibraryImport(CoreFoundation)]
    private static partial void CFRelease(IntPtr cf);

    [LibraryImport(CoreFoundation, StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr __CFStringMakeConstantString(string cStr);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr CGEventTapCallBack(IntPtr proxy, int type, IntPtr eventRef, IntPtr userInfo);

    private readonly CGEventTapCallBack _callback;
    private readonly IntPtr _callbackPtr;

    public MacHotkey(ILog log)
    {
        _log = log;
        _callback = TapCallback;
        _callbackPtr = Marshal.GetFunctionPointerForDelegate(_callback);
    }

    public bool TryRegister(KeyToken key)
    {
        Unregister();

        _targetKeycode = NativeKeys.ToKeycode(key);
        if (_targetKeycode == NativeKeys.Unsupported)
        {
            _log.Info($"MacHotkey: Unsupported key '{key}'");
            return false;
        }

        if (!MacOSPlatform.IsProcessTrusted())
        {
            // Creating a tap while untrusted makes macOS prompt again even if
            // Accessibility was already granted to a previous copy/signature.
            _log.Info("MacHotkey: skipped tap — process is not trusted for Accessibility");
            return false;
        }

        ulong eventMask = 1UL << kCGEventKeyDown;
        _eventTap = CGEventTapCreate(kCGSessionEventTap, kCGHeadInsertEventTap,
            kCGEventTapOptionListenOnly, eventMask, _callbackPtr, IntPtr.Zero);

        if (_eventTap == IntPtr.Zero)
        {
            _log.Info("MacHotkey: CGEventTapCreate failed (Accessibility permission needed)");
            return false;
        }

        _runLoopSource = CFMachPortCreateRunLoopSource(IntPtr.Zero, _eventTap, 0);
        if (_runLoopSource == IntPtr.Zero)
        {
            _log.Info("MacHotkey: CFMachPortCreateRunLoopSource failed");
            CFRelease(_eventTap);
            _eventTap = IntPtr.Zero;
            return false;
        }

        _stop = false;
        _ready.Reset();
        _thread = new Thread(RunLoopThread) { IsBackground = true, Name = "MacHotkeyListener" };
        _thread.Start();
        // Do not Wait/_ready on the UI thread — the tap thread can need the
        // main run loop, and Loaded calls TryRegister on that thread.
        _log.Info($"MacHotkey: Registered keycode={_targetKeycode} for '{key}'");
        return true;
    }

    public void Unregister()
    {
        _stop = true;
        var thread = Interlocked.Exchange(ref _thread, null);
        if (thread is not null)
        {
            _ready.Wait(2000);
            var rl = _runLoop;
            if (rl != IntPtr.Zero) CFRunLoopStop(rl);
            if (!thread.Join(2000))
                _log.Info("MacHotkey: Run loop did not stop in time — abandoning event tap");
            return;
        }

        lock (_gate)
            ReleaseTapUnlocked();
    }

    private void RunLoopThread()
    {
        var tap = _eventTap;
        var source = _runLoopSource;
        var mode = __CFStringMakeConstantString("kCFRunLoopDefaultMode");
        try
        {
            _runLoop = CFRunLoopGetCurrent();
            CFRunLoopAddSource(_runLoop, source, mode);
            CGEventTapEnable(tap, true);
            _ready.Set();
            _log.Info("MacHotkey: Run loop started");

            while (!_stop)
            {
                int result = CFRunLoopRunInMode(mode, 0.25, false);
                if (result == kCFRunLoopRunFinished)
                {
                    _log.Info("MacHotkey: run loop finished (no sources in kCFRunLoopDefaultMode)");
                    break;
                }

                if (result == kCFRunLoopRunStopped)
                    break;
            }

            CFRunLoopRemoveSource(CFRunLoopGetCurrent(), source, mode);
            CGEventTapEnable(tap, false);
        }
        catch (Exception ex)
        {
            _log.Info($"MacHotkey: run loop error: {ex}");
            try { _ready.Set(); } catch { /* ignore */ }
        }
        finally
        {
            lock (_gate)
                ReleaseTapUnlocked();
            _runLoop = IntPtr.Zero;
            _log.Info("MacHotkey: Run loop exited");
        }
    }

    void ReleaseTapUnlocked()
    {
        if (_runLoopSource != IntPtr.Zero)
        {
            try { CFRelease(_runLoopSource); } catch { /* ignore */ }
            _runLoopSource = IntPtr.Zero;
        }

        if (_eventTap != IntPtr.Zero)
        {
            try { CFRelease(_eventTap); } catch { /* ignore */ }
            _eventTap = IntPtr.Zero;
            _log.Info("MacHotkey: Unregistered");
        }
    }

    private IntPtr TapCallback(IntPtr proxy, int type, IntPtr eventRef, IntPtr userInfo)
    {
        if (type is kCGEventTapDisabledByTimeout or kCGEventTapDisabledByUserInput)
        {
            if (_eventTap != IntPtr.Zero) CGEventTapEnable(_eventTap, true);
            return eventRef;
        }

        if (type == kCGEventKeyDown
            && CGEventGetIntegerValueField(eventRef, kCGKeyboardEventAutorepeat) == 0
            && CGEventGetIntegerValueField(eventRef, kCGKeyboardEventKeycode) == _targetKeycode)
        {
            Pressed?.Invoke();
        }

        return eventRef;
    }

    public void Dispose()
    {
        if (_disposed) return;
        Unregister();
        _ready.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
