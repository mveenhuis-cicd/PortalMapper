using PortalMapper.Core.Model;
using PortalMapper.Core.Text;

namespace PortalMapper.Core.Simplify;

/// <summary>
/// Turns the raw link list into the simplified tree:
/// clean names, drop unusable links, merge duplicates, group by area and heading,
/// keep the original order.
/// </summary>
public sealed class LinkSimplifier
{
    private const string PathSeparator = " › ";

    public SimplifiedPage Simplify(RawPage page)
    {
        ArgumentNullException.ThrowIfNull(page);

        var seenTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var areas = new List<AreaBuilder>();

        foreach (var raw in page.Links)
        {
            var name = TextCleaner.Clean(raw.Name);
            if (name.Length == 0) continue;
            if (!IsUsableTarget(raw.Href, page.Url)) continue;
            if (!seenTargets.Add(WithoutFragment(raw.Href))) continue; // same target already listed

            var kind = ToAreaKind(raw.AreaRole);
            var areaTitle = TextCleaner.Clean(raw.AreaName) is { Length: > 0 } label ? label : DefaultTitle(kind);

            // Original order: a new area group starts whenever the area changes.
            var area = areas.LastOrDefault();
            if (area is null || area.Kind != kind || area.Title != areaTitle)
            {
                area = areas.FirstOrDefault(a => a.Kind == kind && a.Title == areaTitle)
                       ?? AddArea(areas, kind, areaTitle);
            }

            var heading = TextCleaner.Clean(raw.Heading);
            if (heading == areaTitle) heading = string.Empty; // e.g. aside labelled by its own heading

            var path = heading.Length > 0 ? areaTitle + PathSeparator + heading : areaTitle;
            var entry = new LinkEntry(
                name,
                raw.Href,
                path,
                TextCleaner.NormalizeForSearch(name + " " + path));

            area.Add(heading, entry);
        }

        return new SimplifiedPage(page.Url, TextCleaner.Clean(page.Title), areas.Select(a => a.Build()).ToList());
    }

    internal static bool IsUsableTarget(string? href, string pageUrl)
    {
        if (string.IsNullOrWhiteSpace(href)) return false;
        if (!Uri.TryCreate(href, UriKind.Absolute, out var uri)) return false;

        if (uri.Scheme is not ("http" or "https" or "mailto" or "tel")) return false; // javascript:, data:, ...

        // Jump marks on the same page ("#", "#main", skip links) are no destinations.
        if (href.Contains('#') && WithoutFragment(href) == WithoutFragment(pageUrl)) return false;

        return true;
    }

    internal static string WithoutFragment(string url)
    {
        var index = url.IndexOf('#');
        return index < 0 ? url : url[..index];
    }

    internal static AreaKind ToAreaKind(string? role) => role switch
    {
        "banner" => AreaKind.Header,
        "navigation" => AreaKind.Navigation,
        "search" => AreaKind.Search,
        "main" => AreaKind.Main,
        "complementary" => AreaKind.Complementary,
        "contentinfo" => AreaKind.Footer,
        _ => AreaKind.Other,
    };

    internal static string DefaultTitle(AreaKind kind) => kind switch
    {
        AreaKind.Header => "Kopfbereich",
        AreaKind.Navigation => "Navigation",
        AreaKind.Search => "Suche",
        AreaKind.Main => "Hauptinhalt",
        AreaKind.Complementary => "Zusatzinformationen",
        AreaKind.Footer => "Fußbereich",
        _ => "Weitere Links",
    };

    private static AreaBuilder AddArea(List<AreaBuilder> areas, AreaKind kind, string title)
    {
        var area = new AreaBuilder(kind, title);
        areas.Add(area);
        return area;
    }

    private sealed class AreaBuilder(AreaKind kind, string title)
    {
        private readonly List<(string Heading, List<LinkEntry> Links)> _sections = [];

        public AreaKind Kind { get; } = kind;
        public string Title { get; } = title;

        public void Add(string heading, LinkEntry entry)
        {
            var index = _sections.FindIndex(s => s.Heading == heading);
            if (index < 0)
            {
                _sections.Add((heading, new List<LinkEntry> { entry }));
            }
            else
            {
                _sections[index].Links.Add(entry);
            }
        }

        public Area Build() => new(
            Kind,
            Title,
            Collapsed: Kind == AreaKind.Footer,
            _sections.Select(s => new Section(s.Heading, s.Links)).ToList());
    }
}
