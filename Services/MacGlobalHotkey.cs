using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace AutoClacker.Services
{
    /// <summary>
    /// macOS global hotkey using CoreGraphics event tap.
    /// Requires Accessibility permissions (System Settings → Privacy → Accessibility).
    /// </summary>
    public class MacGlobalHotkey : IGlobalHotkey
    {
        public event Action? OnHotkeyPressed;

        private IntPtr _eventTap;
        private IntPtr _runLoopSource;
        private IntPtr _runLoop;
        private Thread? _thread;
        private ushort _targetKeycode;
        private bool _disposed;

        private const string CoreGraphics = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";
        private const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

        // CGEventTap constants
        private const int kCGSessionEventTap = 1;
        private const int kCGHeadInsertEventTap = 0;
        private const int kCGEventTapOptionListenOnly = 1;
        private const int kCGEventKeyDown = 10;

        [DllImport(CoreGraphics)]
        private static extern IntPtr CGEventTapCreate(int tap, int place, int options,
            ulong eventsOfInterest, CGEventTapCallBack callback, IntPtr userInfo);

        [DllImport(CoreGraphics)]
        private static extern void CGEventTapEnable(IntPtr tap, bool enable);

        [DllImport(CoreGraphics)]
        private static extern long CGEventGetIntegerValueField(IntPtr eventRef, int field);

        [DllImport(CoreFoundation)]
        private static extern IntPtr CFMachPortCreateRunLoopSource(IntPtr allocator, IntPtr port, long order);

        [DllImport(CoreFoundation)]
        private static extern void CFRunLoopAddSource(IntPtr rl, IntPtr source, IntPtr mode);

        [DllImport(CoreFoundation)]
        private static extern void CFRunLoopRemoveSource(IntPtr rl, IntPtr source, IntPtr mode);

        [DllImport(CoreFoundation)]
        private static extern void CFRunLoopRun();

        [DllImport(CoreFoundation)]
        private static extern void CFRunLoopStop(IntPtr rl);

        [DllImport(CoreFoundation)]
        private static extern IntPtr CFRunLoopGetCurrent();

        [DllImport(CoreFoundation)]
        private static extern void CFRelease(IntPtr cf);

        // kCFRunLoopCommonModes
        [DllImport(CoreFoundation)]
        private static extern IntPtr __CFStringMakeConstantString(string cStr);

        private delegate IntPtr CGEventTapCallBack(IntPtr proxy, int type, IntPtr eventRef, IntPtr userInfo);

        // Must be stored to prevent GC
        private CGEventTapCallBack? _callback;

        public void Register(string keyName)
        {
            Unregister();

            _targetKeycode = MapKeyToMacKeycode(keyName);
            if (_targetKeycode == 0xFFFF)
            {
                Logger.Log($"MacGlobalHotkey: Unknown key '{keyName}'");
                return;
            }

            _callback = TapCallback;

            // Event mask for kCGEventKeyDown
            ulong eventMask = 1UL << kCGEventKeyDown;

            _eventTap = CGEventTapCreate(kCGSessionEventTap, kCGHeadInsertEventTap,
                kCGEventTapOptionListenOnly, eventMask, _callback, IntPtr.Zero);

            if (_eventTap == IntPtr.Zero)
            {
                Logger.Log("MacGlobalHotkey: CGEventTapCreate failed (Accessibility permissions needed)");
                return;
            }

            _runLoopSource = CFMachPortCreateRunLoopSource(IntPtr.Zero, _eventTap, 0);

            _thread = new Thread(RunLoopThread) { IsBackground = true, Name = "MacHotkeyListener" };
            _thread.Start();

            Logger.Log($"MacGlobalHotkey: Registered keycode={_targetKeycode} for '{keyName}'");
        }

        public void Unregister()
        {
            if (_eventTap == IntPtr.Zero) return;

            if (_runLoop != IntPtr.Zero)
            {
                CFRunLoopStop(_runLoop);
            }

            _thread?.Join(2000);
            _thread = null;

            if (_eventTap != IntPtr.Zero)
            {
                CGEventTapEnable(_eventTap, false);
                CFRelease(_eventTap);
                _eventTap = IntPtr.Zero;
            }

            if (_runLoopSource != IntPtr.Zero)
            {
                CFRelease(_runLoopSource);
                _runLoopSource = IntPtr.Zero;
            }

            _runLoop = IntPtr.Zero;
            _callback = null;

            Logger.Log("MacGlobalHotkey: Unregistered");
        }

        private void RunLoopThread()
        {
            _runLoop = CFRunLoopGetCurrent();
            var commonModes = __CFStringMakeConstantString("kCFRunLoopCommonModes");
            CFRunLoopAddSource(_runLoop, _runLoopSource, commonModes);
            CGEventTapEnable(_eventTap, true);

            Logger.Log("MacGlobalHotkey: Run loop started");
            CFRunLoopRun();
            Logger.Log("MacGlobalHotkey: Run loop exited");
        }

        private IntPtr TapCallback(IntPtr proxy, int type, IntPtr eventRef, IntPtr userInfo)
        {
            if (type == kCGEventKeyDown)
            {
                // Field 9 = kCGKeyboardEventKeycode
                long keycode = CGEventGetIntegerValueField(eventRef, 9);
                if (keycode == _targetKeycode)
                {
                    Logger.Log($"MacGlobalHotkey: Key detected, keycode={keycode}");
                    OnHotkeyPressed?.Invoke();
                }
            }
            return eventRef;
        }

        private static ushort MapKeyToMacKeycode(string key)
        {
            var k = key.ToUpper();
            if (k.StartsWith("F") && int.TryParse(k[1..], out int n) && n >= 1 && n <= 12)
            {
                return n switch
                {
                    1 => 122, 2 => 120, 3 => 99, 4 => 118, 5 => 96, 6 => 97,
                    7 => 98, 8 => 100, 9 => 101, 10 => 109, 11 => 103, 12 => 111,
                    _ => 0xFFFF
                };
            }
            return k switch
            {
                "SPACE" => 49, "ENTER" => 36, "TAB" => 48, "ESCAPE" => 53,
                _ when k.Length == 1 && char.IsLetter(k[0]) =>
                    (ushort)(new byte[] { 0, 11, 8, 2, 14, 3, 5, 4, 34, 38, 40, 37, 46, 45, 31, 35, 12, 15, 1, 17, 32, 9, 13, 7, 16, 6 }[k[0] - 'A']),
                _ => 0xFFFF
            };
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Unregister();
                _disposed = true;
            }
        }
    }
}
