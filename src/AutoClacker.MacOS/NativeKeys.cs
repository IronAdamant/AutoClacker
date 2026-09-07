using AutoClacker.Core.Domain;

namespace AutoClacker.MacOS;

internal static class NativeKeys
{
    static readonly ushort[] LetterCodes =
        [0, 11, 8, 2, 14, 3, 5, 4, 34, 38, 40, 37, 46, 45, 31, 35, 12, 15, 1, 17, 32, 9, 13, 7, 16, 6];
    static readonly ushort[] DigitCodes = [29, 18, 19, 20, 21, 23, 22, 26, 28, 25];
    static readonly ushort[] FunctionCodes = [122, 120, 99, 118, 96, 97, 98, 100, 101, 109, 103, 111];

    public const ushort Unsupported = 0xFFFF;

    public static ushort ToKeycode(KeyToken key)
    {
        var k = key.Value;
        return k switch
        {
            "SPACE" => 49,
            "ENTER" => 36,
            "TAB" => 48,
            "ESCAPE" => 53,
            _ when k.Length == 1 && char.IsAsciiLetterUpper(k[0]) => LetterCodes[k[0] - 'A'],
            _ when k.Length == 1 && char.IsAsciiDigit(k[0]) => DigitCodes[k[0] - '0'],
            _ when k[0] == 'F' && int.TryParse(k[1..], out var n) && n is >= 1 and <= 12 => FunctionCodes[n - 1],
            _ => Unsupported,
        };
    }
}
