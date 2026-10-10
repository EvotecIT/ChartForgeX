  // The prepared kind names distinguish source quantities from map/grid coordinates and tuple summaries.
  const sharedXSeriesKinds = new Set(['line', 'stepline', 'area', 'steparea', 'stackedarea', 'bar', 'horizontalbar', 'lollipop', 'scatter', 'bubble', 'errorbar', 'slope', 'trendline', 'waterfall']);
  const sharedXObservation = (data) => sharedXSeriesKinds.has((data.cfxKind || '').toLowerCase()) && !data.cfxDerived;
  const tooltipNumber = (value) => value !== undefined && value !== null && String(value).trim() !== '' && Number.isFinite(Number(value));
  const renderSharedXTip = (tip, node, root) => {
    const data = node.dataset || {};
    const svg = node.closest('svg');
    if (!svg || !sharedXObservation(data) || !tooltipNumber(data.cfxX) || !tooltipNumber(data.cfxY)) return false;
    const points = new Map();
    const x = Number(data.cfxX), styles = new Map();
    const addObservation = (point) => {
      const candidate = point.dataset;
      // Reject unrelated coordinates and duplicate series before resolving any computed mark styles.
      if (!sharedXObservation(candidate) || !tooltipNumber(candidate.cfxX) || !tooltipNumber(candidate.cfxY)
        || Number(candidate.cfxX) !== x || points.has(candidate.cfxSeries) || !isInteractiveTarget(point)
        || point.closest('.cfx-series-muted,[data-cfx-role="legend-item"]')) return;
      const paint = pointerTargetPaint(point, styles);
      if (!paint) return;
      const index = candidate.cfxSeries;
      points.set(index, { point, index, key: seriesKey(point), source: sourcePointIndex(point), name: seriesLabel(point),
        state: candidate.cfxState || svg.getAttribute('data-cfx-series-state-' + index) || 'none',
        value: Number(candidate.cfxY), rawValue: candidate.cfxY, colour: paint.colour || paintColour(point, styles) });
    };
    // Duplicate x coordinates are valid: retain the observation the reader actually interacted with.
    addObservation(node);
    svg.querySelectorAll('[data-cfx-point][data-cfx-series][data-cfx-x][data-cfx-y]').forEach(addObservation);
    const priority = { danger: 5, warning: 4, info: 3, none: 2, neutral: 1, quiet: 0, success: 0 };
    const rows = Array.from(points.values()).sort((a, b) => (priority[b.state] || 0) - (priority[a.state] || 0) || b.value - a.value);
    if (!rows.length) return false;
    tip.replaceChildren();
    const header = document.createElement('div');
    header.className = 'cfx-tooltip__title'; header.textContent = data.cfxXLabel || data.cfxX; tip.appendChild(header);
    const list = document.createElement('dl'); list.className = 'cfx-tooltip__meta';
    rows.forEach((row) => {
      const name = document.createElement('dt'); const value = document.createElement('dd');
      name.dataset.cfxTooltipSeries = row.index; name.dataset.cfxTooltipSeriesKey = row.key;
      name.dataset.cfxTooltipPoint = row.point.dataset.cfxPoint; name.dataset.cfxTooltipSourcePoint = row.source;
      name.textContent = row.name; value.textContent = row.rawValue;
      const swatch = document.createElement('span'); swatch.className = 'cfx-tooltip__swatch';
      if (row.colour) swatch.style.backgroundColor = row.colour;
      swatch.setAttribute('aria-hidden', 'true'); name.prepend(swatch);
      if (row.state === 'quiet' || row.state === 'success') { name.className = 'cfx-tooltip__quiet'; value.className = 'cfx-tooltip__quiet'; }
      list.append(name, value);
    });
    tip.appendChild(list);
    return true;
  };
