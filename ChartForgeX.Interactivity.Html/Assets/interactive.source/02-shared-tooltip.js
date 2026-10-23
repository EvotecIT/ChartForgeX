
  const sharedXSeriesKinds = new Set(['line', 'stepline', 'area', 'steparea', 'stackedarea', 'bar', 'horizontalbar', 'lollipop', 'scatter', 'bubble', 'errorbar', 'slope', 'trendline', 'waterfall', 'differencearea']);
  const sharedXObservation = (data) => sharedXSeriesKinds.has((data.cfxKind || '').toLowerCase()) && !data.cfxDerived;
  const sharedXValue = (data) => (data.cfxKind || '').toLowerCase() === 'differencearea' ? data.cfxFirst : data.cfxY;
  const renderSharedXTip = (tip, node, root) => {
    const data = node.dataset || {};
    const svg = node.closest('svg');
    if (!svg || !sharedXObservation(data) || !tooltipNumber(data.cfxX) || !tooltipNumber(sharedXValue(data))) return false;
    const points = new Map();
    const x = Number(data.cfxX), styles = new Map();
    const addObservation = (point) => {
      const candidate = point.dataset;

      if (!sharedXObservation(candidate) || !tooltipNumber(candidate.cfxX) || !tooltipNumber(sharedXValue(candidate))
        || Number(candidate.cfxX) !== x || points.has(candidate.cfxSeries) || !isInteractiveTarget(point)
        || point.closest('.cfx-series-muted,[data-cfx-role="legend-item"]')) return;
      const paint = pointerTargetPaint(point, styles);
      if (!paint) return;
      const index = candidate.cfxSeries;
      points.set(index, { point, index, key: seriesKey(point), source: sourcePointIndex(point), name: seriesLabel(point),
        state: candidate.cfxState || svg.getAttribute('data-cfx-series-state-' + index) || 'none',
        value: Number(sharedXValue(candidate)), rawValue: sharedXValue(candidate), colour: paint.colour || paintColour(point, styles) });
    };
    // Keep duplicate-X identity.
    addObservation(node);
    svg.querySelectorAll('[data-cfx-point][data-cfx-series][data-cfx-x]').forEach(addObservation);
    const priority = { danger: 5, warning: 4, info: 3, none: 2, neutral: 1, quiet: 0, success: 0 };
    const rows = Array.from(points.values()).sort((a, b) => (priority[b.state] || 0) - (priority[a.state] || 0) || b.value - a.value);
    if (!rows.length) return false;
    tip.replaceChildren();
    const header = document.createElement('div');
    header.className = 'cfx-tooltip__title'; header.textContent = categoryReadoutText(node, capturedText(data, 'cfxXLabel', capturedText(data, 'cfxDisplayLabel', numericFactText(data, 'X')))); tip.appendChild(header);
    const list = document.createElement('dl'); list.className = 'cfx-tooltip__meta';
    rows.forEach((row) => {
      const name = document.createElement('dt'); const value = document.createElement('dd');
      name.dataset.cfxTooltipSeries = row.index; name.dataset.cfxTooltipSeriesKey = row.key;
      name.dataset.cfxTooltipPoint = row.point.dataset.cfxPoint; name.dataset.cfxTooltipSourcePoint = row.source;
      const difference = differenceTooltipRows(row.point);
      name.textContent = row.name + (difference ? ' · ' + difference[0].name : '');
      value.textContent = difference ? difference[0].value : capturedText(row.point.dataset, 'cfxFormattedValue', numericDisplayText(row.rawValue));
      const swatch = document.createElement('span'); swatch.className = 'cfx-tooltip__swatch';
      if (row.colour) swatch.style.backgroundColor = row.colour;
      swatch.setAttribute('aria-hidden', 'true'); name.prepend(swatch);
      if (row.state === 'quiet' || row.state === 'success') { name.className = 'cfx-tooltip__quiet'; value.className = 'cfx-tooltip__quiet'; }
      list.append(name, value);
      const dimensions = dimensionTooltipRows(row.point);
      if (difference) dimensions.unshift({ ...difference[1], dimension: 'second' });
      dimensions.forEach((dimension) => {
        const term = document.createElement('dt'), measurement = document.createElement('dd');
        term.dataset.cfxTooltipDimension = dimension.dimension;
        term.dataset.cfxTooltipObservation = row.key;
        term.dataset.cfxTooltipPoint = row.point.dataset.cfxPoint;
        term.dataset.cfxTooltipSourcePoint = row.source;
        term.textContent = row.name + ' · ' + dimension.name; measurement.textContent = dimension.value;
        if (row.state === 'quiet' || row.state === 'success') { term.className = 'cfx-tooltip__quiet'; measurement.className = 'cfx-tooltip__quiet'; }
        list.append(term, measurement);
      });
    });
    tip.appendChild(list);
    return true;
  };
