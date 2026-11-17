using PortalMapper.Core.Text;

namespace PortalMapper.Core.Tests;

public class TextCleanerTests
{
    [Theory]
    [InlineData(" Anmelden", "Anmelden")]               // icon-font glyph before the text
    [InlineData("  Arbeitslos   und\nArbeit finden ", "Arbeitslos und Arbeit finden")]
    [InlineData("Bürger­geld", "Bürgergeld")]             // soft hyphen
    [InlineData("Zero​Width", "ZeroWidth")]
    [InlineData("Non breaking", "Non breaking")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void Clean_removes_icons_and_collapses_whitespace(string? input, string expected)
    {
        Assert.Equal(expected, TextCleaner.Clean(input));
    }

    [Theory]
    [InlineData("Bürgergeld beantragen", "buergergeld beantragen")]
    [InlineData("Straße", "strasse")]
    [InlineData("ÄRZTE", "aerzte")]
    [InlineData("Café", "cafe")]
    [InlineData(" Öffnungszeiten", "oeffnungszeiten")]
    public void NormalizeForSearch_is_case_and_umlaut_insensitive(string input, string expected)
    {
        Assert.Equal(expected, TextCleaner.NormalizeForSearch(input));
    }
}
