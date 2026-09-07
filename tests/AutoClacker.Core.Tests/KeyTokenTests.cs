using AutoClacker.Core.Domain;

namespace AutoClacker.Core.Tests;

public class KeyTokenTests
{
    [Theory]
    [InlineData("F6", "F6")]
    [InlineData("f6", "F6")]
    [InlineData("Return", "ENTER")]
    [InlineData("ENTER", "ENTER")]
    [InlineData("Space", "SPACE")]
    [InlineData("D5", "5")]
    [InlineData("NumPad3", "3")]
    [InlineData("a", "A")]
    [InlineData("Escape", "ESCAPE")]
    public void TryParse_normalizes_supported_keys(string raw, string expected)
    {
        Assert.True(KeyToken.TryParse(raw, out var token));
        Assert.Equal(expected, token.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("F13")]
    [InlineData("LeftCtrl")]
    [InlineData("ArrowUp")]
    public void TryParse_rejects_unsupported(string raw)
    {
        Assert.False(KeyToken.TryParse(raw, out _));
    }
}
