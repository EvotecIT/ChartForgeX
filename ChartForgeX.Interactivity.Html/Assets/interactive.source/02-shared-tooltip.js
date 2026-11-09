
  const sharedXSeriesKinds = new Set(['line', 'stepline', 'area', 'steparea', 'stackedarea', 'bar', 'horizontalbar', 'lollipop', 'scatter', 'bubble', 'errorbar', 'slope', 'trendline', 'waterfall', 'differencearea', 'radar']);
  const automaticSharedKinds = new Set(['line', 'stepline', 'area', 'steparea', 'stackedarea', 'bar', 'horizontalbar', 'lollipop', 'slope', 'trendline', 'radar']);
  const sharedXObservation = (data) => sharedXSeriesKinds.has((data.cfxKind || '').toLowerCase()) && !data.cfxDerived;
  const sharedXValue = (data) => (data.cfxKind || '').toLowerCase() === 'differencearea' ? data.cfxFirst : data.cfxY ?? data.cfxValue;
  const renderSharedXTip = (tip, node, root) => {
    const data = node.dataset || {};
    const svg = node.closest('svg');
    if (!svg || !sharedXObservation(data) || !tooltipNumber(data.cfxX) || !tooltipNumber(sharedXValue(data))) return false;
    const points = new Map();
    const x = Number(data.cfxX), styles = new Map();
    const addObservation = (point) => {
      const candidate = point.dataset;

      if (candidate.cfxTooltipText === '' || !sharedXObservation(candidate) || !tooltipNumber(candidate.cfxX) || !tooltipNumber(sharedXValue(candidate))
        || Number(candidate.cfxX) !== x || points.has(candidate.cfxSeries) || !isInteractiveTarget(point)
        || point.closest('.cfx-series-muted,[data-cfx-role="legend-item"]')) return;
      const paint = pointerTargetPaint(point, styles);
      if (!paint) return;
      const index = candidate.cfxSeries;
      points.set(index, { point, index, key: seriesKey(point), source: sourcePointIndex(point), name: seriesLabel(point),
        state: candidate.cfxState || svg.getAttribute('data-cfx-series-state-' + index) || 'none',
        value: Number(sharedXValue(candidate)), rawValue: sharedXValue(candidate), colour: tooltipSolidColour(point, paint.colour || paintColour(point, styles)),
        active: candidate.cfxSeries === data.cfxSeries, end: Number(candidate.cfxStackEnd), group: candidate.cfxStackGroup });
    };

    addObservation(node);
    // Only observations at the same x can join the readout; the chart model groups them by x once.
    (modelled(root, 'sharedX', () => modelGroups(Array.from(svg.querySelectorAll('[data-cfx-point][data-cfx-series][data-cfx-x]')), point => [Number(point.dataset.cfxX)])).get(x) || []).forEach(addObservation);
    const priority = { danger: 5, warning: 4, info: 3, none: 2, neutral: 1, quiet: 0, success: 0 };
    const stacked = data.cfxStackTotal !== undefined;
    const rows = Array.from(points.values()).sort((a, b) => {
      if (stacked && a.group === b.group && Number.isFinite(a.end) && Number.isFinite(b.end)) {
        const ab = a.point.getBoundingClientRect?.(), bb = b.point.getBoundingClientRect?.();
        const horizontal = root._cfxDataProjection?.horizontal;
        const delta = ab && bb ? horizontal ? bb.right - ab.right : ab.top - bb.top : 0;
        return delta || b.end - a.end;
      }
      return stacked ? Number(a.index) - Number(b.index) : (priority[b.state] || 0) - (priority[a.state] || 0) || b.value - a.value;
    });
    if (!rows.length) return false;
    const title = categoryReadoutText(node, data.cfxCategoryLabel ?? capturedText(data, 'cfxXLabel', capturedText(data, 'cfxDisplayLabel', numericFactText(data, 'X'))));
    const content = [];
    rows.forEach((row) => {
      const captured = row.point.dataset.cfxTooltipText;
      if (captured !== undefined) { content.push({ ...row, value: captured }); return; }
      if (row.point.dataset.cfxWaterfallKind) {
        waterfallTooltipRows(row.point).forEach(fact => content.push({ ...row, ...fact,
          name: rows.length > 1 ? row.name + ' · ' + fact.name : fact.name }));
        return;
      }
      const difference = differenceTooltipRows(row.point);
      content.push({ ...row, name: row.name + (difference ? ' · ' + difference[0].name : ''),
        value: difference ? difference[0].value : tooltipValueText(row.point) });
      const dimensions = dimensionTooltipRows(row.point);
      if (difference) dimensions.unshift({ ...difference[1], dimension: 'second' });
      dimensions.forEach((dimension) => {
        content.push({ ...row, ...dimension, name: row.name + ' · ' + dimension.name, secondary: true });
      });
    });
    if (stacked) {
      const group = data.cfxStackGroup, groupRows = rows.filter(row => row.group === group);
      const total = groupRows.reduce((sum, row) => sum + row.value, 0);
      if (Number.isFinite(total)) content.push({ name: rowName(node, 'total', 'Total'), value: tooltipNumberText(node, total), total: true, secondary: true });
    }
    return renderTooltipContent(tip, title, content, node);
  };
