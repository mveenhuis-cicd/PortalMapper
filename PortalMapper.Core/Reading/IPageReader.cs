using PortalMapper.Core.Model;

namespace PortalMapper.Core.Reading;

/// <summary>
/// Reads a page as the browser renders it.
/// Implemented by the Playwright reader (public pages) and later by the browser extension.
/// </summary>
public interface IPageReader
{
    Task<RawPage> ReadAsync(Uri url, CancellationToken cancellationToken = default);
}
