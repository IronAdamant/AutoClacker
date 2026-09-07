using System.Runtime.InteropServices;

namespace AutoClacker.MacOS;

/// <summary>Sets the macOS process / menu name so the dock does not show "Avalonia Application".</summary>
public static partial class MacAppBranding
{
    private const string ObjC = "/usr/lib/libobjc.A.dylib";

    [LibraryImport(ObjC, StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr objc_getClass(string name);

    [LibraryImport(ObjC, StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr sel_registerName(string name);

    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial IntPtr intptr_objc_msgSend(IntPtr receiver, IntPtr selector);

    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial IntPtr intptr_objc_msgSend_ptr(IntPtr receiver, IntPtr selector, IntPtr arg);

    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial void void_objc_msgSend_ptr(IntPtr receiver, IntPtr selector, IntPtr arg);

    public static void Apply(string name)
    {
        if (!OperatingSystem.IsMacOS() || string.IsNullOrWhiteSpace(name))
            return;

        IntPtr utf8 = IntPtr.Zero;
        try
        {
            var nsStringClass = objc_getClass("NSString");
            var stringWithUtf8 = sel_registerName("stringWithUTF8String:");
            utf8 = Marshal.StringToCoTaskMemUTF8(name);
            var nsName = intptr_objc_msgSend_ptr(nsStringClass, stringWithUtf8, utf8);
            if (nsName == IntPtr.Zero) return;

            var processInfoClass = objc_getClass("NSProcessInfo");
            var processInfoSel = sel_registerName("processInfo");
            var setProcessNameSel = sel_registerName("setProcessName:");
            var info = intptr_objc_msgSend(processInfoClass, processInfoSel);
            if (info == IntPtr.Zero) return;
            void_objc_msgSend_ptr(info, setProcessNameSel, nsName);
        }
        catch
        {
            // Branding is best-effort; CFBundleName in the .app bundle is the durable path.
        }
        finally
        {
            if (utf8 != IntPtr.Zero) Marshal.FreeCoTaskMem(utf8);
        }
    }
}
