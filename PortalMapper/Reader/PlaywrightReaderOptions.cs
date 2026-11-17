namespace PortalMapper.Reader;

/// <summary>Settings from appsettings.json, section "PlaywrightReader".</summary>
public sealed class PlaywrightReaderOptions
{
    /// <summary>
    /// Empty: Playwright's own Chromium (install once with playwright.ps1 install chromium).
    /// "chrome" or "msedge": use the browser already installed on this computer.
    /// </summary>
    public string? Channel { get; set; }

    public bool Headless { get; set; } = true;

    /// <summary>Maximum time for loading the page.</summary>
    public int TimeoutMs { get; set; } = 30_000;

    /// <summary>Extra wait for content loaded by scripts after the page itself has loaded.</summary>
    public int NetworkIdleTimeoutMs { get; set; } = 5_000;

    public int NavigationTimeoutMs { get; set; } = 30_000;
    public int LoadTimeoutMs { get; set; } = 5_000;
    public int SettleTimeoutMs { get; set; } = 5_000;
    public int QuietWindowMs { get; set; } = 500;
}

