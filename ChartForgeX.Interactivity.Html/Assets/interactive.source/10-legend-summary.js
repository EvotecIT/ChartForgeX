  // Legend items summarize their series for readers instead of exposing renderer metadata such as role or kind.
  const trendSeriesKinds = new Set(['line', 'stepline', 'area', 'steparea', 'stackedarea', 'rangearea', 'slope', 'trendline']);
  const totalSeriesKinds = new Set(['bar', 'horizontalbar', 'lollipop', 'radialbar', 'radialcolumn']);
  const legendSeriesValues = (item) => {
    const data = item.dataset || {};
    const svg = item.closest('svg');
    if (!svg || data.cfxSeries === undefined) return [];
    const values = new Map();
    svg.querySelectorAll('[data-cfx-series][data-cfx-point]').forEach((mark) => {
      const markData = mark.dataset || {};
      if (markData.cfxSeries !== data.cfxSeries || values.has(markData.cfxPoint) || mark.closest('[data-cfx-role="legend-item"]')) return;
      const value = Number(markData.cfxY !== undefined ? markData.cfxY : markData.cfxValue);
      if (!Number.isFinite(value)) return;
      values.set(markData.cfxPoint, { point: Number(markData.cfxPoint), value, label: markData.cfxXLabel || markData.cfxCategory || '' });
    });
    return Array.from(values.values()).sort((a, b) => a.point - b.point);
  };
  const summaryValue = (value) => value.toLocaleString(undefined, { maximumFractionDigits: 12 });
  const legendSummaryRows = (item) => {
    const data = item.dataset || {};
    const reference = legendTarget(item);
    const pointReference = reference || (data.cfxPoint !== undefined ? { targetKind: 'point', targetId: pointTargetId(item) } : null);
    const svg = item.closest('svg');
    const mark = svg && pointReference && referencedTargetNode(svg, { ...pointReference, seriesKey: seriesKey(item) });
    const financialRows = mark && financialTooltipRows(mark);
    if (financialRows) return financialRows.concat(colorTooltipRows(mark));
    if (reference) {
      const rows = data.cfxValue === undefined ? [] : [{ name: 'Value', value: data.cfxValue }];
      return rows.concat(mark ? colorTooltipRows(mark) : []);
    }
    const values = legendSeriesValues(item);
    if (!values.length) return [];
    if (data.cfxPoint !== undefined) {
      const entry = values.find((candidate) => String(candidate.point) === data.cfxPoint);
      return entry ? [{ name: 'Value', value: summaryValue(entry.value) }] : [];
    }
    const kind = (data.cfxKind || '').toLowerCase();
    if (totalSeriesKinds.has(kind)) return [{ name: 'Total', value: summaryValue(values.reduce((sum, entry) => sum + entry.value, 0)) }];
    if (!trendSeriesKinds.has(kind)) return [];
    const latest = values[values.length - 1];
    return [{ name: latest.label ? 'Latest (' + latest.label + ')' : 'Latest', value: summaryValue(latest.value) }];
  };
  const renderLegendTip = (tip, item) => {
    const data = item.dataset || {};
    const name = data.cfxPoint !== undefined || legendTarget(item) ? data.cfxLabel || seriesLabel(item) : seriesLabel(item) || data.cfxLabel || '';
    if (!name) return false;
    tip.replaceChildren();
    const title = document.createElement('div');
    title.className = 'cfx-tooltip__title cfx-tooltip__title--series';
    const swatch = document.createElement('span');
    swatch.className = 'cfx-tooltip__swatch';
    swatch.setAttribute('aria-hidden', 'true');
    const colour = paintColour(item);
    if (colour) swatch.style.backgroundColor = colour;
    title.append(swatch, document.createTextNode(name));
    tip.appendChild(title);
    const rows = legendSummaryRows(item);
    if (!rows.length) return true;
    const list = document.createElement('dl');
    list.className = 'cfx-tooltip__meta';
    rows.forEach((row) => {
      const term = document.createElement('dt');
      term.textContent = row.name;
      const value = document.createElement('dd');
      value.textContent = row.value;
      list.append(term, value);
    });
    tip.appendChild(list);
    return true;
  };
