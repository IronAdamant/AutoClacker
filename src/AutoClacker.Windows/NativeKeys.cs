using AutoClacker.Core.Domain;

namespace AutoClacker.Windows;

internal static class NativeKeys
{
    public static ushort ToVirtualKey(KeyToken key)
    {
        var k = key.Value;
        return k switch
        {
            "SPACE" => 0x20,
            "ENTER" => 0x0D,
            "TAB" => 0x09,
            "ESCAPE" => 0x1B,
            _ when k.Length == 1 && char.IsAsciiLetterUpper(k[0]) => (ushort)(0x41 + k[0] - 'A'),
            _ when k.Length == 1 && char.IsAsciiDigit(k[0]) => (ushort)(0x30 + k[0] - '0'),
            _ when k[0] == 'F' && int.TryParse(k[1..], out var n) => (ushort)(0x6F + n),
            _ => 0,
        };
    }
}
