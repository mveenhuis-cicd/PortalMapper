using PortalMapper.Core.Model;
using PortalMapper.Core.Simplify;

namespace PortalMapper.Core.Tests;

public class LinkSimplifierTests
{
    private const string PageUrl = "https://www.example.de/";

    private static RawLink Link(string name, string href, string role = "main", string areaName = "", string heading = "")
        => new(name, href, role, areaName, heading);

    private static SimplifiedPage Simplify(params RawLink[] links)
        => new LinkSimplifier().Simplify(new RawPage(PageUrl, "Testseite", links));

    [Fact]
    public void Groups_by_area_and_heading_in_original_order()
    {
        var page = Simplify(
            Link("Startseite", "https://www.example.de/", "banner"),
            Link("Arbeitslos", "https://www.example.de/arbeitslos", "navigation", "Hauptnavigation"),
            Link("Arbeitslos melden", "https://www.example.de/melden", heading: "Häufig gesucht"),
            Link("Bürgergeld", "https://www.example.de/buergergeld", heading: "Häufig gesucht"),
            Link("Rückruf", "https://www.example.de/rueckruf", heading: "Service"),
            Link("Impressum", "https://www.example.de/impressum", "contentinfo"));

        Assert.Equal(
            new[] { "Kopfbereich", "Hauptnavigation", "Hauptinhalt", "Fußbereich" },
            page.Areas.Select(a => a.Title));

        var main = page.Areas[2];
        Assert.Equal(new[] { "Häufig gesucht", "Service" }, main.Sections.Select(s => s.Heading));
        Assert.Equal(2, main.Sections[0].Links.Count);
        Assert.Equal(6, page.LinkCount);
    }

    [Fact]
    public void Footer_is_collapsed_other_areas_are_open()
    {
        var page = Simplify(
            Link("Melden", "https://www.example.de/melden"),
            Link("Impressum", "https://www.example.de/impressum", "contentinfo"));

        Assert.False(page.Areas[0].Collapsed);
        Assert.True(page.Areas[1].Collapsed);
    }

    [Theory]
    [InlineData("")]                                        // role="link" without href
    [InlineData("javascript:void(0)")]
    [InlineData("https://www.example.de/#")]                // "#" link on the same page
    [InlineData("https://www.example.de/#main")]            // skip link
    [InlineData("data:text/html,hi")]
    public void Drops_links_without_real_target(string href)
    {
        var page = Simplify(Link("Irgendwas", href));

        Assert.Equal(0, page.LinkCount);
    }

    [Fact]
    public void Keeps_mailto_and_anchors_on_other_pages()
    {
        var page = Simplify(
            Link("E-Mail", "mailto:info@example.de"),
            Link("Fristen", "https://www.example.de/hilfe#fristen"));

        Assert.Equal(2, page.LinkCount);
    }

    [Fact]
    public void Merges_duplicate_targets_and_keeps_the_first()
    {
        var page = Simplify(
            Link("Schule", "https://www.example.de/schule", "navigation"),
            Link("Schule, Ausbildung", "https://www.example.de/schule#top"),
            Link("Schule", "https://www.example.de/schule", "contentinfo"));

        var link = Assert.Single(page.Areas.SelectMany(a => a.Sections).SelectMany(s => s.Links));
        Assert.Equal("Navigation", link.Path);
    }

    [Fact]
    public void Drops_links_without_name_and_cleans_names()
    {
        var page = Simplify(
            Link("", "https://www.example.de/icon-only"),
            Link(" Anmelden", "https://www.example.de/login", "banner"));

        var link = Assert.Single(page.Areas.SelectMany(a => a.Sections).SelectMany(s => s.Links));
        Assert.Equal("Anmelden", link.Name);
    }

    [Fact]
    public void Path_and_search_text_contain_area_and_heading()
    {
        var page = Simplify(Link("Bürgergeld beantragen", "https://www.example.de/bg", heading: "Häufig gesucht"));

        var link = page.Areas[0].Sections[0].Links[0];
        Assert.Equal("Hauptinhalt › Häufig gesucht", link.Path);
        Assert.Contains("buergergeld", link.SearchText);
        Assert.Contains("haeufig gesucht", link.SearchText);
    }

    [Fact]
    public void Heading_equal_to_area_title_is_not_repeated()
    {
        var page = Simplify(Link("Tipps", "https://www.example.de/tipps", "complementary", "Für Sie", "Für Sie"));

        Assert.Equal("", page.Areas[0].Sections[0].Heading);
        Assert.Equal("Für Sie", page.Areas[0].Sections[0].Links[0].Path);
    }
}
