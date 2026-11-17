using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PortalMapper.Core.Model;
using PortalMapper.Core.Reading;
using PortalMapper.Core.Simplify;

namespace PortalMapper.Pages;

public class IndexModel(IPageReader reader, LinkSimplifier simplifier, ILogger<IndexModel> logger) : PageModel
{
    public const string DefaultAddress = "https://www.arbeitsagentur.de/";

    /// <summary>Address of the page to read (query parameter "address").</summary>
    [BindProperty(SupportsGet = true)]
    public string? Address { get; set; }

    public SimplifiedPage? Result { get; private set; }

    public string? Error { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Address))
        {
            Address = DefaultAddress;
            return;
        }

        if (!Uri.TryCreate(Address.Trim(), UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
        {
            Error = "Bitte eine vollständige Adresse eingeben, die mit http:// oder https:// beginnt.";
            return;
        }

        try
        {
            var raw = await reader.ReadAsync(uri, cancellationToken);
            Result = simplifier.Simplify(raw);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Reading {Address} failed", uri);
            Error = $"Die Seite konnte nicht gelesen werden: {ex.Message}";
        }
    }
}
