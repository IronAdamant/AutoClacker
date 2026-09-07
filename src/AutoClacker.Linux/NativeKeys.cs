using AutoClacker.Core.Domain;

namespace AutoClacker.Linux;

internal static class NativeKeys
{
    public static string? ToKeysymName(KeyToken key)
    {
        var k = key.Value;
        return k switch
        {
            "SPACE" => "space",
            "ENTER" => "Return",
            "TAB" => "Tab",
            "ESCAPE" => "Escape",
            _ when k.Length == 1 && char.IsAsciiLetterUpper(k[0]) => k.ToLowerInvariant(),
            _ when k.Length == 1 && char.IsAsciiDigit(k[0]) => k,
            _ when k[0] == 'F' && int.TryParse(k[1..], out var n) && n is >= 1 and <= 12 => k,
            _ => null,
        };
    }
}
