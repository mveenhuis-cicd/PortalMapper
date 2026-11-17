using System.Globalization;
using System.Text;

namespace PortalMapper.Core.Text;

public static class TextCleaner
{
    /// <summary>
    /// Cleans an accessible name for display: removes icon-font glyphs (Unicode private use area),
    /// zero-width and control characters, and collapses whitespace.
    /// " Anmelden" → "Anmelden"
    /// </summary>
    public static string Clean(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        var sb = new StringBuilder(text.Length);
        var lastWasSpace = true; // trims the start
        foreach (var rune in text.EnumerateRunes())
        {
            var category = Rune.GetUnicodeCategory(rune);
            if (category is UnicodeCategory.PrivateUse
                or UnicodeCategory.Format       // zero-width space/joiner, soft hyphen, BOM
                or UnicodeCategory.Surrogate)
            {
                continue;
            }

            if (Rune.IsWhiteSpace(rune) || category == UnicodeCategory.Control)
            {
                if (!lastWasSpace) sb.Append(' ');
                lastWasSpace = true;
                continue;
            }

            sb.Append(rune.ToString());
            lastWasSpace = false;
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Normalizes text for searching: lower case, German umlauts spelled out (ä → ae, ß → ss),
    /// other accents removed. "Bürgergeld" and "buergergeld" give the same result.
    /// Must stay in sync with normalize() in wwwroot/js/link-search.js.
    /// </summary>
    public static string NormalizeForSearch(string? text)
    {
        var cleaned = Clean(text).ToLowerInvariant();
        if (cleaned.Length == 0) return cleaned;

        cleaned = cleaned
            .Replace("ä", "ae")
            .Replace("ö", "oe")
            .Replace("ü", "ue")
            .Replace("ß", "ss");

        var decomposed = cleaned.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
