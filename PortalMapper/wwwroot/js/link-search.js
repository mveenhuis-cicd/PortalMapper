// Local search over the simplified link tree. No request leaves the browser.
// normalize() must match TextCleaner.NormalizeForSearch in PortalMapper.Core.
(() => {
  const input = document.getElementById('pm-search-input');
  if (!input) return;

  const status = document.getElementById('pm-search-status');
  const noResults = document.getElementById('pm-no-results');
  const total = Number(status.dataset.total);
  const areas = [...document.querySelectorAll('.pm-area')];

  const normalize = (text) =>
    text
      .replace(/[-​-‍­﻿]/g, '')
      .toLowerCase()
      .replace(/ä/g, 'ae')
      .replace(/ö/g, 'oe')
      .replace(/ü/g, 'ue')
      .replace(/ß/g, 'ss')
      .normalize('NFD')
      .replace(/[̀-ͯ]/g, '')
      .replace(/\s+/g, ' ')
      .trim();

  const apply = () => {
    const words = normalize(input.value).split(' ').filter(Boolean);
    const searching = words.length > 0;
    document.body.classList.toggle('pm-searching', searching);
    let hits = 0;

    for (const area of areas) {
      let areaHits = 0;
      for (const section of area.querySelectorAll('.pm-section')) {
        let sectionHits = 0;
        for (const item of section.querySelectorAll('.pm-link')) {
          const match = words.every((w) => item.dataset.search.includes(w));
          item.hidden = !match;
          if (match) sectionHits++;
        }
        section.hidden = sectionHits === 0;
        areaHits += sectionHits;
      }
      area.hidden = areaHits === 0;
      // While searching, open every area with hits; afterwards restore the original state.
      area.open = searching ? areaHits > 0 : area.dataset.collapsed !== 'true';
      hits += areaHits;
    }

    status.textContent = searching ? `${hits} von ${total} Links` : `${total} Links`;
    noResults.hidden = hits > 0;
  };

  input.addEventListener('input', apply);
  input.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') {
      input.value = '';
      apply();
    }
  });
  input.focus();
})();
