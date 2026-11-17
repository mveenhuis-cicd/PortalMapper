() => {
  // Reads all visible links of the current page, together with their landmark
  // (area) and the nearest heading before them. Runs inside the browser:
  // today via Playwright (page.EvaluateAsync), later in the browser extension.
  // Returns plain data only; cleaning and grouping happen in PortalMapper.Core.
  const LANDMARK_BY_TAG = {
    header: 'banner',
    nav: 'navigation',
    main: 'main',
    aside: 'complementary',
    footer: 'contentinfo',
  };
  const LANDMARK_ROLES = new Set(['banner', 'navigation', 'main', 'complementary', 'contentinfo', 'search']);

  const textById = (id) => document.getElementById(id)?.textContent ?? '';
  const labelledBy = (el) =>
    (el.getAttribute('aria-labelledby') ?? '').split(/\s+/).filter(Boolean).map(textById).join(' ');

  const landmarkRole = (el) => {
    const role = el.getAttribute('role');
    if (role) return LANDMARK_ROLES.has(role) ? role : null;
    const tag = el.tagName.toLowerCase();
    // header/footer only count as page landmarks when not nested in a sectioning element
    if ((tag === 'header' || tag === 'footer') && el.parentElement?.closest('article, aside, main, nav, section')) {
      return null;
    }
    return LANDMARK_BY_TAG[tag] ?? null;
  };

  const findLandmark = (el) => {
    for (let node = el.parentElement; node; node = node.parentElement) {
      if (landmarkRole(node)) return node;
    }
    return null;
  };

  const isVisible = (el) => {
    if (el.closest('[aria-hidden="true"], [hidden], [inert]')) return false;
    if (typeof el.checkVisibility === 'function') {
      return el.checkVisibility({ visibilityProperty: true, opacityProperty: false });
    }
    return el.getClientRects().length > 0;
  };

  const accessibleName = (el) => {
    const byId = labelledBy(el).trim();
    if (byId) return byId;
    const label = el.getAttribute('aria-label')?.trim();
    if (label) return label;
    const text = (el.innerText ?? el.textContent ?? '').trim();
    if (text) return text;
    const img = el.querySelector('img[alt]');
    if (img?.alt.trim()) return img.alt.trim();
    return el.getAttribute('title')?.trim() ?? '';
  };

  const lastHeading = new Map(); // landmark element (or null) -> heading text
  const links = [];

  const selector = 'h1, h2, h3, h4, h5, h6, [role="heading"], a[href], [role="link"]';
  for (const el of document.querySelectorAll(selector)) {
    const landmark = findLandmark(el);
    const isHeading = /^H[1-6]$/.test(el.tagName) || el.getAttribute('role') === 'heading';

    if (isHeading) {
      if (isVisible(el)) lastHeading.set(landmark, accessibleName(el));
      continue;
    }
    if (!isVisible(el)) continue;

    links.push({
      name: accessibleName(el),
      href: el.href ?? el.getAttribute('href') ?? '',
      areaRole: landmark ? landmarkRole(landmark) : '',
      areaName: landmark ? (labelledBy(landmark).trim() || landmark.getAttribute('aria-label')?.trim() || '') : '',
      heading: lastHeading.get(landmark) ?? '',
    });
  }

  return { url: location.href, title: document.title, links };
}
