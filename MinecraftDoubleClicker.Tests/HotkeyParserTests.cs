using MinecraftDoubleClicker.Models;
using MinecraftDoubleClicker.Services;

namespace MinecraftDoubleClicker.Tests;

public sealed class HotkeyParserTests
{
    [Theory]
    [InlineData("F8", HotkeyModifiers.None, "F8")]
    [InlineData("Ctrl+Shift+f8", HotkeyModifiers.Control | HotkeyModifiers.Shift, "F8")]
    [InlineData("option+cmd+return", HotkeyModifiers.Alt | HotkeyModifiers.Command, "ENTER")]
    [InlineData("win+esc", HotkeyModifiers.Command, "ESCAPE")]
    [InlineData("control+pgdn", HotkeyModifiers.Control, "PAGEDOWN")]
    public void Parse_NormalizesSupportedSyntax(
        string text,
        HotkeyModifiers expectedModifiers,
        string expectedKey)
    {
        HotkeyDefinition result = HotkeyParser.Parse(text);

        Assert.Equal(expectedModifiers, result.Modifiers);
        Assert.Equal(expectedKey, result.Key);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Ctrl+Shift")]
    [InlineData("A+B")]
    public void Parse_RejectsMissingOrMultipleKeys(string text)
    {
        Assert.Throws<ArgumentException>(() => HotkeyParser.Parse(text));
    }
}
