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
      if (markData.cfxEmpty === 'true' || markData.cfxSeries !== data.cfxSeries || values.has(markData.cfxPoint) || mark.closest('[data-cfx-role="legend-item"]')) return;
      const rawValue = markData.cfxY !== undefined ? markData.cfxY : markData.cfxValue;
      if (rawValue === undefined || rawValue === '') return;
      const value = Number(rawValue);
      if (!Number.isFinite(value)) return;
      values.set(markData.cfxPoint, { point: Number(markData.cfxPoint), value, label: markData.cfxXLabel || markData.cfxCategory || '',
        formatted: markData.cfxFormattedValue, valueLabel: rowName(mark, 'value', 'Value') });
    });
    return Array.from(values.values()).sort((a, b) => a.point - b.point);
  };
  const summaryValue = (value) => value.toLocaleString(undefined, { maximumFractionDigits: 12 });
  const comparisonLegendRows = (node) => {
    const data = node.dataset || {}, kind = (data.cfxKind || '').toLowerCase();
    if (!['dumbbell', 'errorbar'].includes(kind)) return null;
    const category = data.cfxCategoryLabel !== undefined ? data.cfxCategoryLabel : data.cfxXLabel || data.cfxX;
    const rows = [{ name: rowName(node, 'category', 'Category'), value: category }];
    if (kind === 'dumbbell') rows.push({ name: rowName(node, 'start', 'Start'), value: capturedText(data, 'cfxFormattedStart', data.cfxStart) },
      { name: rowName(node, 'end', 'End'), value: capturedText(data, 'cfxFormattedEnd', data.cfxEnd) }, { name: rowName(node, 'delta', 'Delta'), value: data.cfxDelta });
    else rows.push({ name: rowName(node, 'value', 'Value'), value: capturedText(data, 'cfxFormattedValue', data.cfxValue) },
      { name: rowName(node, 'lower', 'Lower'), value: data.cfxLower }, { name: rowName(node, 'upper', 'Upper'), value: data.cfxUpper });
    return rows.filter((row) => row.value !== undefined && row.value !== null && row.value !== '');
  };
  const legendSummaryRows = (item) => {
    const data = item.dataset || {};
    if (data.cfxWaterfallKind) return waterfallTooltipRows(item);
    const reference = legendTarget(item);
    const pointReference = reference || (data.cfxPoint !== undefined ? { targetKind: 'point', targetId: pointTargetId(item) } : null);
    const svg = item.closest('svg');
    const mark = svg && pointReference && referencedTargetNode(svg, { ...pointReference, seriesKey: seriesKey(item) });
    const rangeBarRows = mark && rangeBarTooltipRows(mark);
    if (rangeBarRows) return rangeBarRows.concat(dimensionTooltipRows(mark));
    const boxPlotRows = mark && boxPlotTooltipRows(mark);
    if (boxPlotRows) return boxPlotRows.concat(dimensionTooltipRows(mark));
    const comparisonRows = mark && comparisonLegendRows(mark);
    if (comparisonRows) return comparisonRows.concat(dimensionTooltipRows(mark));
    const financialRows = mark && financialTooltipRows(mark);
    if (financialRows) return financialRows.concat(dimensionTooltipRows(mark));
    if (reference) {
      const source = mark || item, facts = source.dataset || {};
      const value = capturedText(facts, 'cfxFormattedValue', facts.cfxValue);
      const rows = facts.cfxEmpty === 'true' || value === undefined ? [] : [{ name: rowName(source, 'value', 'Value'), value }];
      return rows.concat(mark ? dimensionTooltipRows(mark) : []);
    }
    const values = legendSeriesValues(item);
    if (!values.length) return [];
    if (data.cfxPoint !== undefined) {
      const entry = values.find((candidate) => String(candidate.point) === data.cfxPoint);
      return entry ? [{ name: entry.valueLabel, value: entry.formatted !== undefined ? entry.formatted : summaryValue(entry.value) }]
        .concat(mark ? dimensionTooltipRows(mark) : []) : [];
    }
    const kind = (data.cfxKind || '').toLowerCase();
    if (totalSeriesKinds.has(kind)) return [{ name: 'Total', value: summaryValue(values.reduce((sum, entry) => sum + entry.value, 0)) }];
    if (!trendSeriesKinds.has(kind)) return [];
    const latest = values[values.length - 1];
    return [{ name: latest.label ? 'Latest (' + latest.label + ')' : 'Latest',
      value: latest.formatted !== undefined ? latest.formatted : summaryValue(latest.value) }];
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
