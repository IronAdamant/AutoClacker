namespace AutoClacker.Core.Domain;

/// <summary>
/// Canonical key token shared across UI and platforms.
/// Supported: A–Z, 0–9, SPACE, ENTER, TAB, ESCAPE, F1–F12.
/// </summary>
public readonly record struct KeyToken(string Value)
{
    public override string ToString() => Value;

    /// <summary>Parse a raw name (Avalonia Key.ToString or settings value) to a token.</summary>
    public static bool TryParse(string? raw, out KeyToken token)
    {
        token = default;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var k = raw.Trim().ToUpperInvariant();
        string? normalized = k switch
        {
            "RETURN" or "ENTER" => "ENTER",
            "SPACE" => "SPACE",
            "TAB" => "TAB",
            "ESCAPE" or "ESC" => "ESCAPE",
            _ when k.Length == 1 && (char.IsAsciiLetterUpper(k[0]) || char.IsAsciiDigit(k[0])) => k,
            _ when k.Length == 2 && k[0] == 'D' && char.IsAsciiDigit(k[1]) => k[1..],
            _ when k.Length == 7 && k.StartsWith("NUMPAD", StringComparison.Ordinal) && char.IsAsciiDigit(k[6]) => k[6..],
            _ when IsFunctionKey(k) => k,
            _ => null,
        };
        if (normalized is null) return false;
        token = new KeyToken(normalized);
        return true;
    }

    public static KeyToken Parse(string raw) =>
        TryParse(raw, out var t) ? t : throw new ArgumentException($"Unsupported key '{raw}'", nameof(raw));

    static bool IsFunctionKey(string k) =>
        k.Length is 2 or 3 && k[0] == 'F' && int.TryParse(k[1..], out var n) && n is >= 1 and <= 12;
}
