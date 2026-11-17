namespace PortalMapper.Core.Model;

/// <summary>The simplified tree: Page → Area → Section → Link.</summary>
public sealed record SimplifiedPage(string Url, string Title, IReadOnlyList<Area> Areas)
{
    public int LinkCount => Areas.Sum(a => a.Sections.Sum(s => s.Links.Count));
}

public enum AreaKind
{
    Header,
    Navigation,
    Search,
    Main,
    Complementary,
    Footer,
    Other,
}

/// <param name="Collapsed">Shown collapsed by default (e.g. footer with legal links).</param>
public sealed record Area(AreaKind Kind, string Title, bool Collapsed, IReadOnlyList<Section> Sections);

/// <param name="Heading">Section heading, empty when the links have no heading.</param>
public sealed record Section(string Heading, IReadOnlyList<LinkEntry> Links);

/// <param name="Path">Human-readable location, e.g. "Hauptinhalt › Häufig gesucht".</param>
/// <param name="SearchText">Normalized text (name + path) the search compares against.</param>
public sealed record LinkEntry(string Name, string Href, string Path, string SearchText);
