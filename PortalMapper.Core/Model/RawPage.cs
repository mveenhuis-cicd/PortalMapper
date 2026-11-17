namespace PortalMapper.Core.Model;

/// <summary>
/// What a reader (Playwright today, the browser extension later) delivers:
/// the page as seen in the browser, not yet cleaned or grouped.
/// </summary>
public sealed record RawPage(string Url, string Title, IReadOnlyList<RawLink> Links);

/// <param name="Name">Accessible name as computed in the browser (may contain icon characters).</param>
/// <param name="Href">Absolute target URL, empty for role="link" elements without href.</param>
/// <param name="AreaRole">Landmark role: banner, navigation, main, complementary, contentinfo, search or empty.</param>
/// <param name="AreaName">aria-label / aria-labelledby of the landmark, if any.</param>
/// <param name="Heading">Nearest heading before the link within the same landmark.</param>
public sealed record RawLink(string Name, string Href, string AreaRole, string AreaName, string Heading);
