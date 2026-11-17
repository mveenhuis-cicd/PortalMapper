using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using PortalMapper.Core.Model;
using PortalMapper.Core.Reading;

namespace PortalMapper.Reader;

/// <summary>
/// Opens a page in a headless browser and reads the links with Reader/extract-links.js.
/// For public pages only: the browser has no login session of the user.
/// One browser is shared; every call gets its own, empty browser context.
/// </summary>
public sealed class PlaywrightPageReader(
    IOptions<PlaywrightReaderOptions> options,
    ILogger<PlaywrightPageReader> logger) : IPageReader, IAsyncDisposable
{
    private static readonly Lazy<string> ExtractScript = new(LoadScript);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly PlaywrightReaderOptions _options = options.Value;
    private readonly SemaphoreSlim _startLock = new(1, 1);
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    //public async Task<RawPage> ReadAsync(Uri url, CancellationToken cancellationToken = default)
    public async Task<RawPage> ReadAsync(Uri url, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(url);
        if (url.Scheme is not ("http" or "https"))
        {
            throw new ArgumentException("Only http and https addresses can be read.", nameof(url));
        }

        var browser = await GetBrowserAsync(cancellationToken);
        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            Locale = "de-DE",
            ViewportSize = new ViewportSize { Width = 1280, Height = 900 },
        });
        var page = await context.NewPageAsync();

        logger.LogInformation("Reading {Url}", url);
        await page.GotoAsync(url.AbsoluteUri, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = _options.TimeoutMs,
        });

        try
        {
            //await page.WaitForPageSettledAsync(new PageReaderOptions
            //{
            //    NavigationTimeoutMs = _options.NavigationTimeoutMs,
            //    LoadTimeoutMs = _options.LoadTimeoutMs,
            //    SettleTimeoutMs = _options.SettleTimeoutMs,
            //    QuietWindowMs = _options.QuietWindowMs,
            //}, cancellationToken);

            await page.GotoAsync(url.AbsoluteUri, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = _options.TimeoutMs
            });
            await WaitForPageSettledAsync(page, url);
        }
        catch (PlaywrightException)
        {
            // Pages with tracking or polling never become idle; read what is there.
            logger.LogDebug("{Url} did not become idle, reading anyway", url);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var json = await page.EvaluateAsync<JsonElement>(ExtractScript.Value);
        var result = json.Deserialize<RawPage>(JsonOptions)
                     ?? throw new InvalidOperationException("The page returned no data.");

        logger.LogInformation("Read {Count} links from {Url}", result.Links.Count, result.Url);
        return result;
    }

    private async Task<IBrowser> GetBrowserAsync(CancellationToken cancellationToken)
    {
        if (_browser is { IsConnected: true }) return _browser;

        await _startLock.WaitAsync(cancellationToken);
        try
        {
            if (_browser is { IsConnected: true }) return _browser;

            _playwright ??= await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = _options.Headless,
                Channel = string.IsNullOrWhiteSpace(_options.Channel) ? null : _options.Channel,
            });
            return _browser;
        }
        finally
        {
            _startLock.Release();
        }
    }

    private static string LoadScript()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("extract-links.js")
                           ?? throw new InvalidOperationException("Embedded resource extract-links.js not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null) await _browser.DisposeAsync();
        _playwright?.Dispose();
        _startLock.Dispose();
    }

    private async Task WaitForPageSettledAsync(IPage page, Uri url)
    {
        try
        {
            await page.WaitForLoadStateAsync(LoadState.Load,
                new PageWaitForLoadStateOptions { Timeout = _options.LoadTimeoutMs });
        }
        catch (TimeoutException)
        {
            logger.LogDebug("{Url} did not fire load within {Ms} ms, continuing", url, _options.LoadTimeoutMs);
        }

        try
        {
            var quiet = await page.EvaluateAsync<bool>(@"([quietMs, maxMs]) => new Promise(resolve => {
            let quietTimer;
            const finish = ok => { observer.disconnect(); clearTimeout(quietTimer); clearTimeout(capTimer); resolve(ok); };
            const observer = new MutationObserver(() => {
                clearTimeout(quietTimer);
                quietTimer = setTimeout(() => finish(true), quietMs);
            });
            observer.observe(document, { childList: true, subtree: true });
            quietTimer = setTimeout(() => finish(true), quietMs);
            const capTimer = setTimeout(() => finish(false), maxMs);
        })", new[] { _options.QuietWindowMs, _options.SettleTimeoutMs });

            if (!quiet)
                logger.LogDebug("{Url} DOM still changing after {Ms} ms, reading anyway", url, _options.SettleTimeoutMs);
        }
        catch (PlaywrightException ex)
        {
            // z. B. Weiterleitung während des Wartens: Kontext zerstört
            logger.LogDebug(ex, "{Url} navigated while settling, reading current state", url);
        }
    }

  
    //Task<RawPage> IPageReader.ReadAsync(Uri url, CancellationToken cancellationToken)
    //{
    //    throw new NotImplementedException();
    //}
}
