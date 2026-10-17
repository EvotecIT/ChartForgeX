(() => {
  if (window.ChartForgeXCharts) {
    document.querySelectorAll('.cfx-interactive-chart').forEach(root => window.ChartForgeXCharts.mount(root));
    return;
  }
  const featureAliases = {
    ReportReview: ['Tooltips', 'Selection', 'LegendToggles', 'KeyboardNavigation', 'Crosshair', 'CompareMarkers']
  };
  const featureTokens = (root) => (root.dataset.cfxInteractionFeatures || '')
    .split(',')
    .map((feature) => feature.trim())
    .filter(Boolean);
  const hasFeature = (root, name) => featureTokens(root).some((feature) => feature.toLowerCase() === name.toLowerCase()
    || (featureAliases[feature] || []).some((alias) => alias.toLowerCase() === name.toLowerCase()));
  const targetSelector = '.cfx-interactive-region,[data-cfx-target-kind],[data-cfx-label],[data-cfx-point],[data-cfx-series],[data-cfx-region],[data-cfx-node],[data-cfx-role="legend-item"]';
  const lassoSelector = '.cfx-interactive-region,[data-cfx-target-kind]:not([data-cfx-target-kind="legend"]),[data-cfx-label],[data-cfx-point],[data-cfx-region],[data-cfx-node]';
  const renderedTargetSelector = '.cfx-interactive-region,[data-cfx-label],[data-cfx-series],[data-cfx-point],[data-cfx-region],[data-cfx-node],[data-cfx-source][data-cfx-target],[data-cfx-role="legend-item"],[data-cfx-role^="annotation"]';
  const isPassiveTarget = (node) => !!node.closest('[data-cfx-passive="true"]');
  const isInteractiveTarget = (node) => {
    if (isPassiveTarget(node)) return false;
    const role = (node.dataset || {}).cfxRole;
    if (role === 'legend-entry-omitted') return false;
    if (role === 'legend-item') return true;
    if (node.closest('[data-cfx-role="legend-item"]')) return false;
    // Bind a semantic group once; a nested group with its own explicit identity remains a target.
    const owner = node.parentElement && node.parentElement.closest('[data-cfx-point],[data-cfx-region],[data-cfx-node],[data-cfx-series][data-cfx-value]:not([data-cfx-point])');
    if (!owner) return true;
    const data = node.dataset || {}, ownerData = owner.dataset || {};
    return !!(data.cfxTargetKind && data.cfxTargetId
      && (data.cfxTargetKind !== ownerData.cfxTargetKind || data.cfxTargetId !== ownerData.cfxTargetId));
  };
  const interactiveTargets = (root, selector = targetSelector) => Array.from(root.querySelectorAll(selector)).filter(isInteractiveTarget);
  const targetFocusNode = (node) => {
    // Only the renderer-owned cell link is a mark's alternate keyboard target.
    const link = node.parentElement;
    if (link && link.matches('a[data-cfx-role="heatmap-cell-link"][href]')) return link;
    // Prepared marks put identity on the outer group and their cell link inside it.
    return Array.from(node.children).find((child) => child.matches('a[data-cfx-role="heatmap-cell-link"][href]')) || node;
  };
  // A legend may refer to an authored native item while retaining its own legend identity.
  const legendTarget = (node) => {
    const data = node.dataset || {};
    return data.cfxLegendTargetKind && data.cfxLegendTargetId
      ? { targetKind: data.cfxLegendTargetKind, targetId: data.cfxLegendTargetId } : null;
  };
  const referencedTargetNode = (root, target) => Array.from(root.querySelectorAll('[data-cfx-target-kind][data-cfx-target-id]'))
    .find((node) => isInteractiveTarget(node) && (node.dataset || {}).cfxRole !== 'legend-item'
      && node.dataset.cfxTargetKind === target.targetKind && node.dataset.cfxTargetId === target.targetId
      && (!target.seriesKey || seriesKey(node) === target.seriesKey));
  const seriesLegend = (node) => {
    const data = node.dataset || {};
    if (data.cfxSeries === undefined) return null;
    const root = node.closest('.cfx-interactive-chart');
    if (root) {
      const legendItems = Array.from(root.querySelectorAll('[data-cfx-role="legend-item"][data-cfx-series]'));
      const sameSeries = (item) => (item.dataset || {}).cfxSeries === data.cfxSeries;
      const itemId = data.cfxTargetKind === 'node' ? data.cfxTargetId : undefined;
      if (itemId !== undefined) return legendItems.find((item) => sameSeries(item)
        && (item.dataset || {}).cfxLegendTargetKind === 'node' && item.dataset.cfxLegendTargetId === itemId)
        || legendItems.find((item) => sameSeries(item) && !legendTarget(item));
      return data.cfxPoint === undefined
        ? legendItems.find((item) => sameSeries(item) && (item.dataset || {}).cfxPoint === undefined) || legendItems.find(sameSeries)
        : legendItems.find((item) => sameSeries(item) && (item.dataset || {}).cfxPoint === data.cfxPoint)
          || legendItems.find((item) => sameSeries(item) && (item.dataset || {}).cfxPoint === undefined);
    }
    return null;
  };
  const seriesLabel = (node) => {
    const data = node.dataset || {};
    if (data.cfxSeriesName) return data.cfxSeriesName;
    const legend = seriesLegend(node);
    if (legend) return (legend.dataset || {}).cfxSeriesName || (legend.dataset || {}).cfxLabel || legend.getAttribute('aria-label') || legend.textContent || '';
    const svg = node.closest('svg');
    const mapped = svg && data.cfxSeries !== undefined ? svg.getAttribute('data-cfx-series-name-' + data.cfxSeries) : '';
    if (mapped) return mapped;
    if (data.cfxSeries === undefined) return '';
    return 'Series ' + data.cfxSeries;
  };
  const seriesKey = (node) => {
    const data = node.dataset || {};
    if (data.cfxSeriesKey) return data.cfxSeriesKey;
    const legend = seriesLegend(node);
    if (legend) return (legend.dataset || {}).cfxSeriesKey || seriesLabel(legend);
    const svg = node.closest('svg');
    return svg && data.cfxSeries !== undefined ? svg.getAttribute('data-cfx-series-key-' + data.cfxSeries) || '' : '';
  };
  // Series groups use source-points as a count; marks use it as an authored collection.
  const hasSourcePointCollection = (node) => {
    const data = node.dataset || {};
    return data.cfxSourcePoints !== undefined && data.cfxRole !== 'series';
  };
  const sourcePointCollection = (node) => {
    if (!hasSourcePointCollection(node)) return undefined;
    return node.dataset.cfxSourcePoints.split(',').filter((value) => value.trim() !== '')
      .map(Number).filter((value) => Number.isSafeInteger(value) && value >= 0);
  };
  const hasDerivedPointIdentity = (node) => hasSourcePointCollection(node) || (node.dataset || {}).cfxDerived !== undefined;
  const sourcePointIndex = (node) => {
    const data = node.dataset || {};
    if (hasDerivedPointIdentity(node)) return undefined;
    if (data.cfxSourcePoint !== undefined) return data.cfxSourcePoint;
    if (data.cfxPoint === undefined || data.cfxSeries === undefined) return data.cfxPoint;
    const svg = node.closest('svg');
    const sourceIndices = svg ? svg.getAttribute('data-cfx-series-source-indices-' + data.cfxSeries) : '';
    if (!sourceIndices) return data.cfxPoint;
    const index = Number(data.cfxPoint);
    const mapped = sourceIndices.split(',')[index];
    return mapped === undefined || mapped === '' ? data.cfxPoint : mapped;
  };
  const derivedPointIdentity = (node) => {
    const data = node.dataset || {};
    if (data.cfxDerivedIdentity !== undefined) return data.cfxDerivedIdentity;
    const derived = data.cfxDerived || 'aggregate';
    // Layout ordinals do not identify equivalent summaries in charts with different partitions.
    if (derived === 'histogram-bin' && data.cfxBinLower !== undefined && data.cfxBinUpper !== undefined)
      return [derived, String(Number(data.cfxBinLower)), String(Number(data.cfxBinUpper)), data.cfxBinUpperInclusive || 'false',
        data.cfxHistogramAggregation || 'count', data.cfxHistogramEncoding || 'value'].join(':');
    if (derived === 'timeline-run' && data.cfxStart !== undefined && data.cfxEnd !== undefined)
      return [derived, String(Number(data.cfxStart)), String(Number(data.cfxEnd))].join(':');
    if (derived === 'radial-slice' && data.cfxPoint === '-1' && hasSourcePointCollection(node))
      return derived + ':sources:' + sourcePointCollection(node).slice().sort((a, b) => a - b).join(',');
    return derived + ':' + (data.cfxPoint ?? '0');
  };
  const pointTargetId = (node) => {
    const data = node.dataset || {};
    if (data.cfxObservationId !== undefined) return JSON.stringify([seriesKey(node), data.cfxObservationId]);
    const derived = hasDerivedPointIdentity(node);
    const point = derived ? 'derived:' + derivedPointIdentity(node) : sourcePointIndex(node) ?? '0';
    return `${seriesKey(node) || data.cfxSeries || 'series'}:${point}`;
  };
  const renderedTargetKind = (node) => {
    const data = node.dataset || {};
    const role = data.cfxRole || '';
    if (data.cfxTargetKind) return data.cfxTargetKind;
    if (role === 'legend-item') return 'legend';
    if (role.indexOf('annotation') === 0) return 'annotation';
    if (data.cfxRegion !== undefined) return 'region';
    if (data.cfxNode !== undefined) return 'node';
    if (role.indexOf('link') >= 0 || role.indexOf('connector') >= 0 || role.indexOf('edge') >= 0) return 'link';
    if (data.cfxPoint !== undefined) return 'point';
    if (data.cfxSeries !== undefined) return 'series';
    return 'element';
  };
  const renderedTargetId = (node, kind) => {
    const data = node.dataset || {};
    if (data.cfxTargetId) return data.cfxTargetId;
    if (kind === 'series') return seriesKey(node) || data.cfxSeries || '';
    if (kind === 'point') return pointTargetId(node);
    if (kind === 'region') return data.cfxRegion || data.cfxId || data.cfxLabel || '';
    if (kind === 'node') return data.cfxNode || data.cfxId || data.cfxLabel || '';
    if (kind === 'link') return data.cfxId || [data.cfxSource, data.cfxTarget].filter(Boolean).join('->') || data.cfxLabel || '';
    if (kind === 'legend') {
      const reference = legendTarget(node);
      if (reference) return JSON.stringify([seriesKey(node) || data.cfxSeries || 'series', reference.targetKind, reference.targetId]);
      return data.cfxPoint === undefined ? seriesKey(node) || data.cfxSeries || data.cfxLabel || '' : pointTargetId(node);
    }
    if (kind === 'annotation') return data.cfxId || data.cfxLabel || [data.cfxKind, data.cfxValue].filter(Boolean).join(':');
    return data.cfxId || node.id || data.cfxLabel || data.cfxRole || '';
  };
  const applyRenderedTargetContract = (root) => {
    prepareChartTargets(root);
    Array.from(root.querySelectorAll(renderedTargetSelector)).filter(isInteractiveTarget).forEach((node) => {
      if (node.closest('defs')) return;
      const kind = renderedTargetKind(node);
      const id = renderedTargetId(node, kind);
      if (!kind || !id) return;
      node.setAttribute('data-cfx-target-kind', kind);
      node.setAttribute('data-cfx-target-id', id);
    });
  };
  // Captured display text may deliberately be empty; only an absent attribute falls back to raw facts.
  const capturedText = (data, key, fallback) => data[key] !== undefined ? data[key] : fallback;
  const tooltipNumber = (value) => value !== undefined && value !== null && String(value).trim() !== '' && Number.isFinite(Number(value));
  // Source metadata stays exact; fallback captions use the browser's shortest round-trip number.
  const numericDisplayText = (value) => tooltipNumber(value) ? String(Number(value)) : value;
  const numericFactText = (data, member) => capturedText(data, 'cfxFormatted' + member, numericDisplayText(data['cfx' + member]));
  const text = (node, tooltipTitle = false) => {
    const data = node.dataset || {};
    if (tooltipTitle && ['rangebar', 'boxplot'].includes((data.cfxKind || '').toLowerCase()))
      return [seriesLabel(node), data.cfxAuthoredLabel ?? targetCategory(node) ?? data.cfxLabel].filter(Boolean).join(': ');
    if (isDataTarget(node)) return data.cfxAuthoredLabel ?? targetCategory(node) ?? data.cfxDisplayLabel ?? (data.cfxLabel || data.cfxText || seriesLabel(node) || data.cfxRole || '');
    const aria = node.getAttribute('aria-label');
    if (aria) return aria;
    const label = data.cfxLabel || data.cfxText || '';
    const parts = [];
    if (label) parts.push(label);
    else if (data.cfxRole) parts.push(data.cfxRole.replace(/-/g, ' '));
    if (data.cfxSeries !== undefined && !label) parts.push(seriesLabel(node));
    if (data.cfxPoint !== undefined && (!tooltipTitle || !label)) parts.push('Point ' + data.cfxPoint);
    const value = capturedText(data, 'cfxFormattedValue', data.cfxValue || data.cfxY || data.cfxEnd || data.cfxTarget || '');
    if (value) parts.push(rowName(node, 'value', 'Value') + ' ' + value);
    return parts.join(' / ');
  };
  const targetIdentity = (node) => {
    const data = node.dataset || {};
    const targetKind = renderedTargetKind(node);
    const state = (data.cfxKind || '').toLowerCase() === 'statetimeline';
    const rawValue = (data.cfxKind || '').toLowerCase() === 'boxplot' ? data.cfxMedian : data.cfxValue ?? data.cfxY ?? data.cfxEnd;
    return {
      targetKind,
      targetId: renderedTargetId(node, targetKind),
      id: data.cfxId || node.id || '',
      role: data.cfxRole || '',
      label: data.cfxAuthoredLabel ?? targetCategory(node) ?? data.cfxDisplayLabel ?? (data.cfxLabel || data.cfxText || node.getAttribute('aria-label') || ''),
      rangeLabel: data.cfxRangeLabel,
      series: data.cfxSeries,
      seriesKey: seriesKey(node),
      seriesScope: data.cfxSeriesScope,
      observationId: data.cfxObservationId,
      observationIds: contributorObservationIds(node),
      category: targetCategory(node),
      x: targetRawX(node),
      numericValue: !state && tooltipNumber(rawValue) ? Number(rawValue) : undefined,
      range: targetDataRange(node),
      point: data.cfxPoint,
      sourcePoint: sourcePointIndex(node),
      sourcePoints: sourcePointCollection(node),
      legendTargetKind: data.cfxLegendTargetKind,
      legendTargetId: data.cfxLegendTargetId,
      value: state ? data.cfxStateLabel ?? data.cfxLabel ?? '' : capturedText(data, 'cfxFormattedValue', rawValue ?? ''),
      kind: data.cfxKind || ''
    };
  };
  const metaLabel = (attributeName) => attributeName
    .replace(/^data-cfx-meta-/i, '')
    .split('-')
    .filter(Boolean)
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(' ');
  const metadataRows = (node) => Array.from(node.attributes || [])
    .filter((attribute) => attribute.name.toLowerCase().indexOf('data-cfx-meta-') === 0 && attribute.value !== '')
    .map((attribute) => ({ name: metaLabel(attribute.name), value: attribute.value }));
  const rowName = (node, key, fallback) => {
    const owner = node.closest ? node.closest('[data-cfx-label-' + key + ']') : null;
    return (owner && owner.getAttribute('data-cfx-label-' + key)) || fallback;
  };
  const percentFormat = new Intl.NumberFormat('en', { style: 'percent', maximumFractionDigits: 2 });
  const percentText = (value) => value === undefined || value === '' ? value : percentFormat.format(Number(value));
  const colorTooltipRows = (node) => {
    const data = node.dataset || {};
    const value = data.cfxColorMissing === 'true' ? rowName(node, 'no-data', 'No data')
      : capturedText(data, 'cfxFormattedColorValue', data.cfxColorValue);
    return value === undefined ? [] : [{ name: rowName(node, 'color', 'Color'), value }];
  };
  const sizeTooltipRows = (node) => {
    const value = (node.dataset || {}).cfxSize;
    return value === undefined || value === null || value === '' ? [] : [{ name: rowName(node, 'size', 'Size'), value }];
  };
  const dimensionTooltipRows = (node) => sizeTooltipRows(node).map((row) => ({ ...row, dimension: 'size' }))
    .concat(colorTooltipRows(node).map((row) => ({ ...row, dimension: 'color' })));
  // A financial Y is the tuple's opening member; all readouts use the complete prices.
  const financialTooltipRows = (node) => {
    const data = node.dataset || {};
    if (!['candlestick', 'ohlc'].includes((data.cfxKind || '').toLowerCase())) return null;
    return ['Open', 'High', 'Low', 'Close'].filter((part) => {
      const value = data['cfx' + part];
      return value !== undefined && value !== null && value !== '';
    }).map((part) => ({ name: rowName(node, part.toLowerCase(), part), value: numericFactText(data, part) }));
  };
  const waterfallTooltipRows = (node) => {
    const data = node.dataset || {};
    if (!data.cfxWaterfallKind) return [];
    const fallback = data.cfxWaterfallKind === 'total' ? 'Total' : data.cfxWaterfallKind === 'subtotal' ? 'Subtotal' : data.cfxWaterfallKind === 'opening-balance' ? 'Opening balance' : 'Change';
    return [{ name: rowName(node, 'waterfall-value', fallback), value: numericFactText(data, 'Value') },
      { name: 'Start', value: numericFactText(data, 'Start') }, { name: 'End', value: numericFactText(data, 'End') }]
      .filter((row) => row.value !== undefined && row.value !== null);
  };
  const boxPlotTooltipRows = (node) => {
    const data = node.dataset || {};
    if ((data.cfxKind || '').toLowerCase() !== 'boxplot') return null;
    return [{ name: rowName(node, 'category', ''), value: data.cfxCategoryLabel !== undefined ? data.cfxCategoryLabel : data.cfxX }]
      .concat(['minimum', 'q1', 'median', 'q3', 'maximum'].map((key) => ({
        name: rowName(node, key, ''), value: numericFactText(data, key.charAt(0).toUpperCase() + key.slice(1))
      }))).filter((row) => row.value !== undefined && row.value !== null);
  };
  const rangeBarTooltipRows = (node) => {
    const data = node.dataset || {};
    if ((data.cfxKind || '').toLowerCase() !== 'rangebar') return null;
    return [{ name: rowName(node, 'category', ''), value: data.cfxCategoryLabel !== undefined ? data.cfxCategoryLabel : data.cfxX },
      { name: rowName(node, 'start', ''), value: numericFactText(data, 'Start') },
      { name: rowName(node, 'end', ''), value: numericFactText(data, 'End') }]
      .filter((row) => row.value !== undefined && row.value !== null);
  };
  const differenceTooltipRows = (node) => {
    const data = node.dataset || {};
    if ((data.cfxKind || '').toLowerCase() !== 'differencearea') return null;
    return ['First', 'Second'].map((part) => ({
      name: rowName(node, part.toLowerCase(), part),
      value: numericFactText(data, part)
    }));
  };
  const tooltipRows = (node) => {
    const data = node.dataset || {};
    const rows = [];
    const push = (name, value) => {
      if (value !== undefined && value !== null && value !== '') rows.push({ name, value: String(value) });
    };
    const pushX = () => {
      const value = capturedText(data, 'cfxXLabel', capturedText(data, 'cfxFormattedX', numericDisplayText(data.cfxX) || data.cfxCategory || data.cfxDate || capturedText(data, 'cfxFormattedStart', data.cfxStart)));
      if (data.cfxXLabel !== undefined || data.cfxFormattedX !== undefined) rows.push({ name: 'X', value });
      else push('X', value);
    };
    push('Series', seriesLabel(node));
    const boxPlotRows = boxPlotTooltipRows(node);
    if (boxPlotRows) return rows.concat(boxPlotRows, metadataRows(node));
    const rangeBarRows = rangeBarTooltipRows(node);
    if (rangeBarRows) return rows.concat(rangeBarRows, metadataRows(node));
    const differenceRows = differenceTooltipRows(node);
    if (differenceRows) {
      pushX();
      rows.push(...differenceRows);
      metadataRows(node).forEach((row) => push(row.name, row.value));
      return rows;
    }
    if (data.cfxWaterfallKind) push('Category', data.cfxCategoryLabel !== undefined ? data.cfxCategoryLabel : data.cfxXLabel || data.cfxX);
    else pushX();
    const financialRows = financialTooltipRows(node);
    if (financialRows) rows.push(...financialRows);
    else if (data.cfxWaterfallKind) rows.push(...waterfallTooltipRows(node));
    else if (data.cfxEmpty !== 'true') {
      const name = data.cfxValue !== undefined ? rowName(node, 'value', 'Value') : 'Y';
      const value = capturedText(data, 'cfxFormattedValue', numericDisplayText(data.cfxValue !== undefined ? data.cfxValue : data.cfxY));
      if (data.cfxFormattedValue !== undefined) rows.push({ name, value });
      else push(name, value);
      push('End', capturedText(data, 'cfxFormattedEnd', data.cfxEnd));
    }
    sizeTooltipRows(node).forEach((row) => push(row.name, row.value));
    push('Target', data.cfxTarget);
    push('Status', data.cfxStatus);
    push(rowName(node, 'level', 'Level'), data.cfxLevel);
    rows.push(...colorTooltipRows(node));
    if (data.cfxLeaf === 'false' && data.cfxAuthoredValue !== undefined && Number(data.cfxAuthoredValue) !== Number(data.cfxValue))
      push(rowName(node, 'authored-value', 'Provided value'), data.cfxAuthoredValue);
    if (Number(data.cfxRemainderValue) > 0)
      push(rowName(node, 'remainder', 'Remainder'), data.cfxRemainderValue);
    push('Percent', percentText(data.cfxPercent));
    if (!data.cfxWaterfallKind) push('Delta', numericFactText(data, 'Delta'));
    if (tooltipNumber(data.cfxLower) && tooltipNumber(data.cfxUpper)) {
      const lower = numericFactText(data, 'Lower'), upper = numericFactText(data, 'Upper');
      rows.push({ name: 'Range', value: lower === '' && upper === '' ? '' : lower + ' - ' + upper });
    }
    metadataRows(node).forEach((row) => push(row.name, row.value));
    return rows;
  };
  const renderTip = (tip, node) => {
    if ((node.dataset || {}).cfxRole === 'legend-item') return renderLegendTip(tip, node);
    const root = node.closest && node.closest('.cfx-interactive-chart');
    if (root && root.dataset.cfxTooltipMode === 'shared-x' && renderSharedXTip(tip, node, root)) return true;
    const label = text(node, true);
    // An explicitly captured empty caption remains a valid readout of the source fields.
    if (label === undefined || label === null) return false;
    tip.replaceChildren();
    const title = document.createElement('div');
    title.className = 'cfx-tooltip__title';
    title.textContent = label;
    const colour = paintColour(node);
    if (colour) {
      const swatch = document.createElement('span');
      swatch.className = 'cfx-tooltip__swatch'; swatch.style.backgroundColor = colour;
      swatch.setAttribute('aria-hidden', 'true'); title.prepend(swatch);
      title.classList.add('cfx-tooltip__title--series');
    }
    tip.appendChild(title);
    const secondary = (node.dataset || {}).cfxSecondaryLabel;
    if (secondary) {
      const caption = document.createElement('div');
      caption.className = 'cfx-tooltip__secondary';
      caption.textContent = secondary;
      tip.appendChild(caption);
    }
    const rows = tooltipRows(node);
    if (rows.length) {
      const list = document.createElement('dl');
      list.className = 'cfx-tooltip__meta';
      rows.forEach((row) => {
        const term = document.createElement('dt');
        term.textContent = row.name;
        const value = document.createElement('dd');
        value.textContent = row.value;
        list.appendChild(term);
        list.appendChild(value);
      });
      tip.appendChild(list);
    }
    return true;
  };
  const showTip = (root, tip, node, event) => {
    if (!hasFeature(root, 'Tooltips') || root.dataset.cfxTooltipPinned === 'true') { cancelTooltipRequest(root); return; }
    if (!tooltipReadoutAvailable(node, event)) { hideTip(root, tip, false); return; }
    if (event instanceof PointerEvent && tooltipDelay(root) > 0) { requestPointerTip(root, tip, node, event); return; }
    cancelTooltipRequest(root);
    if (displayTip(tip, node, event)) rememberImmediateTooltip(root, tip, node, event);
  };
  const displayTip = (tip, node, event) => {
    if (!renderTip(tip, node)) return false;
    tip.hidden = false;
    moveTip(tip, event, node);
    return true;
  };
  const moveTip = (tip, event, node) => {
    updateTooltipPointer(tip, node, event);
    positionTooltip(tip, event, node);
  };
  const targetKey = (target) => target ? [target.targetKind || '', target.targetId || '', target.id || '', target.role || '', target.label || '', target.series ?? '', target.point ?? '', target.value || '', target.kind || ''].join('|') : '';
  const compareItems = (root) => {
    const items = [];
    const seen = new Set();
    dataTargets(root).filter(node => node.classList.contains('cfx-selected')).forEach((node) => {
      const target = targetIdentity(node);
      const key = targetKey(target);
      if (!key || seen.has(key)) return;
      seen.add(key);
      const data = node.dataset || {};
      let label = data.cfxAuthoredLabel ?? targetCategory(node) ?? data.cfxRangeLabel
        ?? (timeTarget(node) ? data.cfxDisplayLabel : undefined) ?? seriesLabel(node);
      const series = seriesLabel(node);
      if (timeTarget(node) && label && series && label !== series) label = series + ' · ' + label;
      items.push({ label: label ?? target.label ?? target.role ?? target.id ?? 'Target', target });
    });
    return items;
  };
  const selectedTargets = (root) => compareItems(root).map((item) => item.target);
  const renderCompare = (root) => {
    (root._cfxCompareCleanup || []).forEach(release => release());
    root._cfxCompareCleanup = [];
    const tray = root.querySelector('[data-cfx-compare-tray]');
    if (!tray || !hasFeature(root, 'CompareMarkers')) return [];
    const items = compareItems(root);
    tray.replaceChildren();
    if (!items.length) {
      tray.hidden = true;
      root.removeAttribute('data-cfx-compare-count');
      return items;
    }
    tray.hidden = false;
    root.dataset.cfxCompareCount = String(items.length);
    const summary = document.createElement('div');
    summary.className = 'cfx-compare-tray__summary';
    summary.textContent = 'Compare ' + items.length;
    tray.appendChild(summary);
    items.slice(0, 6).forEach((item, index) => {
      const chip = document.createElement('button');
      chip.className = 'cfx-compare-chip';
      chip.type = 'button';
      chip.textContent = item.label;
      chip.dataset.cfxCompareIndex = String(index);
      chip.title = item.label;
      root._cfxCompareCleanup.push(listenChart(root, chip, 'click', () => {
        clearHover(root, false, false);
        if (applyHoverByTarget(root, item.target)) {
          recordFocusTrail(root, item.target, true, true);
          emitHostEvent(root, 'cfxhover', { label: item.label, target: item.target });
        }
      }));
      tray.appendChild(chip);
    });
    if (items.length > 6) {
      const more = document.createElement('span');
      more.className = 'cfx-compare-tray__more';
      more.textContent = '+' + (items.length - 6);
      tray.appendChild(more);
    }
    const clear = document.createElement('button');
    clear.className = 'cfx-compare-clear';
    clear.type = 'button';
    clear.textContent = 'Clear';
    clear.setAttribute('data-cfx-compare-clear', 'true');
    root._cfxCompareCleanup.push(listenChart(root, clear, 'click', () => {
      const returnFocus = tray.contains(document.activeElement);
      clearSelections(root);
      publishSelectionChange(root, 'clear');
      publishCompare(root, true);
      if (returnFocus) {
        if (!root.hasAttribute('tabindex')) root.setAttribute('tabindex', '-1');
        try { root.focus({ preventScroll: true }); } catch { root.focus(); }
      }
    }));
    tray.appendChild(clear);
    return items;
  };
  const publishCompare = (root, sync) => {
    const items = renderCompare(root);
    if (!hasFeature(root, 'CompareMarkers')) return items;
    const targets = items.map((item) => item.target);
    emitHostEvent(root, 'cfxcompare', { count: targets.length, targets });
    if (sync !== false) emitSync(root, { action: 'compare', count: targets.length, targets });
    return items;
  };
  const hideTip = (root, tip, force) => {
    // A delayed pointer handoff can hide an active readout; a force-dismissed readout has no request to restore.
    const restoreKeyboardReadout = tip && (!tip.hidden || tooltipRequests.has(root));
    cancelTooltipRequest(root);
    if (!tip || (!force && root.dataset.cfxTooltipPinned === 'true')) return;
    // Pointer exit can follow host reflow while a keyboard target still owns focus, even in a closed tree.
    const active = !force && hasFeature(root, 'Tooltips') && root.getRootNode().activeElement;
    const focused = active && root.contains(active) && interactiveTargets(root).find(node => targetFocusNode(node) === active);
    if (restoreKeyboardReadout && focused && active.matches(':focus-visible') && tooltipReadoutAvailable(focused)) {
      if (root.dataset.cfxHoverKey !== targetKey(targetIdentity(focused))) setHover(root, focused, true, true);
      showTip(root, tip, focused);
      return;
    }
    forgetTooltipPosition(tip);
    tip.hidden = true;
    tip.classList.remove('cfx-tooltip--pinned');
    root.removeAttribute('data-cfx-tooltip-pinned');
    root.removeAttribute('data-cfx-pinned-target');
  };
  const pinTip = (root, tip, node, event) => {
    cancelTooltipRequest(root);
    if (root.dataset.cfxPinOnSelect === 'false') return;
    if (!hasFeature(root, 'Tooltips') || !tooltipReadoutAvailable(node, event) || !renderTip(tip, node)) return;
    const target = targetIdentity(node);
    const key = targetKey(target);
    const pinned = root.dataset.cfxTooltipPinned === 'true' && root.dataset.cfxPinnedTarget === key;
    if (pinned) {
      hideTip(root, tip, true);
      emitHostEvent(root, 'cfxtooltip', { pinned: false, target });
      return;
    }
    tip.hidden = false;
    tip.classList.add('cfx-tooltip--pinned');
    root.dataset.cfxTooltipPinned = 'true';
    root.dataset.cfxPinnedTarget = key;
    moveTip(tip, event, node);
    emitHostEvent(root, 'cfxtooltip', { pinned: true, label: text(node), target });
  };
  const unpinTooltip = root => {
    const key = root.dataset.cfxPinnedTarget;
    const target = key && interactiveTargets(root).find(node => targetKey(targetIdentity(node)) === key);
    hideTip(root, root.querySelector('.cfx-tooltip'), true);
    if (key) emitHostEvent(root, 'cfxtooltip', { pinned: false, ...(target ? { target: targetIdentity(target) } : {}) });
  };
  // SVG coordinates have three decimals. Bound screen-space comparisons by one logical
  // quantization step, projected through the live transform, plus a small screen rounding allowance.
  const svgCoordinatePrecision = matrix => ({
    x: .001 * (Math.abs(matrix.a) + Math.abs(matrix.c)) + .0001,
    y: .001 * (Math.abs(matrix.b) + Math.abs(matrix.d)) + .0001
  });
  const cartesianFillObservation = (node, subject) => subject !== node && subject.hasAttribute('data-cfx-point')
    && ['area', 'steparea', 'stackedarea', 'differencearea', 'rangearea', 'rangeband'].includes((subject.dataset.cfxKind || '').toLowerCase())
    && /^(area|difference-(positive|negative)|range-(area|band))(-pattern)?$/.test(node.dataset.cfxRole || '');
  const cartesianFillOwnsObservation = (node, subject) => {
    // Fill and hatch share non-paint ownership by observation index, independently of their coordinates.
    const segment = node.closest('[data-cfx-role="fill-segment"]');
    if (!segment) return false;
    const first = Number(segment.dataset.cfxFillPointStart), count = Number(segment.dataset.cfxFillPointCount);
    const index = Number(subject.dataset.cfxPoint);
    return Number.isInteger(first) && Number.isInteger(count) && Number.isInteger(index)
      && first >= 0 && count > 0 && index >= first && index - first < count;
  };
  const cartesianFillStrokeInk = (node, subject, paint) => {
    if (!cartesianFillOwnsObservation(node, subject)) return false;
    if (pairedExtentObservation(subject)) return pairedBoundaryInk(node, subject, paint);
    const region = subject._cfxNativeRegion;
    // Reuse the source-bounded stroke/clip query with this scalar's captured centre as both endpoints.
    return region && pairedBoundaryInk(node, subject, paint, {
      x: region.x + region.width / 2, y: region.y + region.height / 2, width: 0, height: 0
    });
  };
  let nativeFillClipContext;
  const nativeFillEnvelope = (node, styles) => {
    const role = node.dataset.cfxRole || '';
    if (!role.endsWith('-pattern')) return typeof node.isPointInFill === 'function'
      ? { key: node, matrix: node.getScreenCTM(), contains: point => node.isPointInFill(point) } : null;
    // Native hatches are open strokes clipped by an envelope. The browser's native path parser
    // queries that clip with its clip-rule, independently of the base fill's visibility or fill-rule.
    for (let parent = node.parentElement; parent && parent.ownerSVGElement; parent = parent.parentElement) {
      const reference = /^url\(#([^)]*)\)$/.exec(parent.getAttribute('clip-path') || '');
      const clip = reference && parent.ownerSVGElement.getElementById(reference[1]);
      if (!clip || clip.getAttribute('clipPathUnits') !== 'userSpaceOnUse' || clip.children.length !== 1
        || !clip.firstElementChild.matches('path')) continue;
      const shape = clip.firstElementChild;
      let matrix = parent.getScreenCTM();
      if (!matrix) return null;
      for (const element of [clip, shape]) for (let i = 0; i < element.transform.baseVal.numberOfItems; i++)
        matrix = matrix.multiply(element.transform.baseVal.getItem(i).matrix);
      const paths = styles._cfxFillClipPaths ||= new WeakMap();
      let path = paths.get(shape);
      if (!path) {
        path = { geometry: new Path2D(shape.getAttribute('d') || ''), rule: paintStyle(shape, styles).clipRule };
        paths.set(shape, path);
      }
      if (!nativeFillClipContext) {
        const canvas = document.createElement('canvas'); canvas.width = canvas.height = 1;
        nativeFillClipContext = canvas.getContext('2d');
      }
      return { key: shape, matrix, contains: point => nativeFillClipContext.isPointInPath(path.geometry, point.x, point.y, path.rule) };
    }
    return null;
  };
  const cartesianFillInk = (node, subject, styles) => {
    if (!cartesianFillOwnsObservation(node, subject)) return false;
    const surface = nativeFillEnvelope(node, styles), region = subject._cfxNativeRegion;
    const sourceMatrix = subject.getScreenCTM(), shapeMatrix = surface && surface.matrix, clips = nativePaintClips(node);
    if (!surface || !region || !sourceMatrix || !shapeMatrix || !clips) return false;
    // This cache lives only as long as the current paint/style probe. Hatches share one native envelope.
    const cache = styles._cfxFillInk ||= new WeakMap();
    let subjects = cache.get(surface.key);
    if (!subjects) { subjects = new WeakMap(); cache.set(surface.key, subjects); }
    if (subjects.has(subject)) return subjects.get(subject);
    const sourceInverse = sourceMatrix.inverse(), shapeInverse = shapeMatrix.inverse();
    const paired = pairedExtentObservation(subject), x = region.x + region.width / 2;
    // Scalar sources retain their source-location clip contract, even when their footprint overlaps paint.
    if (!paired && !paintWithinNativeClips(node, subject)) return false;
    const low = paired ? region.y + region.width / 2 : region.y + region.height / 2;
    const high = paired ? region.y + region.height - region.width / 2 : low;
    // Native SVG uses three decimals. Interior probes below that quantum distinguish a real held
    // surface from a zero-width endpoint stem, which isPointInFill alone considers inside the path.
    const inset = .00025;
    const contains = (px, py) => {
      const screen = new DOMPoint(px, py).matrixTransform(sourceMatrix);
      if (clips.some(({ rect, matrix }) => {
        const point = screen.matrixTransform(matrix.inverse());
        return point.x <= rect.x || point.x >= rect.x + rect.width || point.y <= rect.y || point.y >= rect.y + rect.height;
      })) return false;
      return surface.contains(screen.matrixTransform(shapeInverse));
    };
    const atY = (y, halfHeight) => [x, Number(x.toFixed(3))].some(px => [-1, 1].some(direction =>
      [px + direction * inset / 2, px + direction * inset].every(sx =>
        [y - halfHeight, y + halfHeight].every(sy => contains(sx, sy)))));
    let visible = false;
    if (!paired) {
      visible = [low, Number(low.toFixed(3))].some(y => atY(y - inset, inset / 2) || atY(y + inset, inset / 2));
      if (!visible) visible = (subject._cfxFillInteriors || []).some(([px, py, radius]) =>
        [px, py, radius].every(Number.isFinite) && radius >= inset / 2
        && px - radius >= region.x && px + radius <= region.x + region.width
        && py - radius >= region.y && py + radius <= region.y + region.height
        && contains(px, py) && [-1, 1].every(dx => [-1, 1].every(dy => contains(px + dx * radius, py + dy * radius))));
    } else if (high > low) {
      // Most observations contain their own middle. A shifted Step hold can overlap only a different
      // part of the paired extent, so partition it using captured neighboring boundaries and clips.
      visible = atY(low / 2 + high / 2, Math.min(inset, (high - low) / 4));
      if (!visible) {
        const knots = new Set([low, high]);
        const add = (y) => { for (const value of [y, Number(y.toFixed(3))]) if (value > low && value < high) knots.add(value); };
        const owner = subject.closest('[data-cfx-role="series"]'), index = Number(subject.dataset.cfxPoint);
        const connected = point => point && point.getAttribute('data-cfx-break-before') !== 'true';
        for (const offset of [-1, 1]) {
          const neighbor = owner && owner.querySelector('[data-cfx-role="point"][data-cfx-point="' + (index + offset) + '"]');
          if (!neighbor || !connected(offset < 0 ? subject : neighbor) || !neighbor._cfxNativeRegion) continue;
          const bounds = neighbor._cfxNativeRegion, matrix = neighbor.getScreenCTM();
          if (!matrix) continue;
          for (const y of [bounds.y + bounds.width / 2, bounds.y + bounds.height - bounds.width / 2])
            add(new DOMPoint(bounds.x + bounds.width / 2, y).matrixTransform(matrix).matrixTransform(sourceInverse).y);
        }
        for (const { rect, matrix } of clips) for (const cy of [rect.y, rect.y + rect.height])
          add(new DOMPoint(rect.x, cy).matrixTransform(matrix).matrixTransform(sourceInverse).y);
        const sorted = Array.from(knots).sort((a, b) => a - b);
        for (let i = 1; i < sorted.length && !visible; i++)
          visible = atY(sorted[i - 1] / 2 + sorted[i] / 2, Math.min(inset, (sorted[i] - sorted[i - 1]) / 4));
      }
    }
    subjects.set(subject, visible);
    return visible;
  };
  // Semantic groups do not paint. Resolve the real mark before using a series or legend fallback.
  const paintShapes = 'rect,circle,ellipse,line,polyline,path,polygon';
  // Both readout lifetime and keyboard availability follow the same native paint inputs.
  // Adapter state and paint-probe attributes are excluded so observing paint cannot observe itself.
  const paintAvailabilityAttributes = ['class', 'style', 'hidden', 'aria-hidden', 'display', 'visibility',
    'fill', 'fill-opacity', 'stroke', 'stroke-width', 'stroke-opacity', 'opacity', 'clip-path', 'clipPathUnits',
    'x', 'y', 'x1', 'x2', 'y1', 'y2', 'width', 'height', 'cx', 'cy', 'r', 'rx', 'ry', 'd', 'points', 'transform',
    'stop-color', 'stop-opacity', 'font-size', 'fill-rule', 'clip-rule', 'slot', 'name'];
  // Paint, clipping and focus share the browser's accessible flattened ancestry.
  const composedParent = (node) => node.assignedSlot || node.parentElement || node.getRootNode().host;
  const composedAncestry = (root) => {
    const nodes = [], trees = new Set();
    for (let node = root; node; node = composedParent(node)) {
      nodes.push(node); trees.add(node.getRootNode());
    }
    return { nodes, trees: Array.from(trees) };
  };
  // Cache only for this render: host CSS can change between successive focus and pointer events.
  const paintStyle = (node, styles) => {
    if (styles.has(node)) return styles.get(node);
    const style = getComputedStyle(node); styles.set(node, style); return style;
  };
  const paintAncestorsVisible = (node, styles) => {
    if (!node || node.closest('defs,[hidden]')) return false;
    for (let ancestor = node; ancestor; ancestor = composedParent(ancestor)) {
      const style = paintStyle(ancestor, styles);
      if (style.display === 'none' || Number(style.opacity) === 0) return false;
    }
    // aria-hidden changes accessibility exposure, not whether SVG marks are painted.
    return true;
  };
  const paintNodeVisible = (node, styles) => {
    if (!paintAncestorsVisible(node, styles)) return false;
    // Visibility is inherited, and a painted descendant can explicitly restore it.
    const style = paintStyle(node, styles);
    return style.visibility !== 'hidden' && style.visibility !== 'collapse';
  };
  const solidPaintColour = (value, opacity) => value && value !== 'none' && value !== 'transparent'
    && !/^url\(/i.test(value) && !/^rgba\(.*[,]\s*0(?:\.0+)?\s*\)$|\/\s*0(?:\.0+)?%?\s*\)$/i.test(value)
    && Number(opacity) > 0 ? value : '';
  const paintValue = (node, value, opacity, styles) => {
    const colour = solidPaintColour(value, opacity);
    if (colour) return { colour };
    if (Number(opacity) <= 0) return null;
    const reference = /^url\(\s*["']?([^"')]+)["']?\s*\)$/i.exec(value || '');
    if (!reference || !node.ownerSVGElement) return null;
    // Read only paint servers in this SVG. One visible stop represents a gradient; never follow linked servers.
    let server;
    try {
      const url = new URL(reference[1], node.ownerDocument.baseURI);
      if (url.href.split('#')[0] !== node.ownerDocument.URL.split('#')[0]) return null;
      server = node.ownerSVGElement.getElementById(decodeURIComponent(url.hash.slice(1)));
    } catch (_) { return null; }
    // A pattern is a painted surface even though it has no single representative swatch colour.
    if (server && server.matches('pattern')) return { colour: '' };
    if (!server || !server.matches('linearGradient,radialGradient')) return null;
    const stops = server.querySelectorAll('stop');
    for (let index = 0; index < Math.min(stops.length, 32); index++) {
      const stop = paintStyle(stops[index], styles);
      const paint = solidPaintColour(stop.stopColor, stop.stopOpacity);
      if (paint) return { colour: paint };
    }
    return null;
  };
  // These Cartesian observations describe both boundaries, rather than one source Y location.
  const pairedExtentObservation = (node) => node.hasAttribute('data-cfx-point')
    && ['differencearea', 'rangearea', 'rangeband'].includes((node.dataset.cfxKind || '').toLowerCase());
  const pairedConnectedSpan = (node, owner) => {
    const index = Number(node.dataset.cfxPoint);
    const followsPrevious = (point) => point
      && point.getAttribute('data-cfx-break-before') !== 'true';
    const previous = owner.querySelector('[data-cfx-role="point"][data-cfx-point="' + (index - 1) + '"]');
    const next = owner.querySelector('[data-cfx-role="point"][data-cfx-point="' + (index + 1) + '"]');
    // Native paired fills need a connected span; isolated observations can still paint their own markers or caps.
    return !!(previous && followsPrevious(node) || followsPrevious(next));
  };
  const pairedExtentScreenBox = (node) => {
    const region = node._cfxPairedRegion, matrix = node.getScreenCTM();
    if (!region || !matrix) return null;
    // Use the captured native extent, excluding the minimum-size transparent browser hit area.
    const corners = [[region.x, region.y], [region.x + region.width, region.y],
      [region.x, region.y + region.height], [region.x + region.width, region.y + region.height]]
      .map(([x, y]) => new DOMPoint(x, y).matrixTransform(matrix));
    return { left: Math.min(...corners.map(p => p.x)), right: Math.max(...corners.map(p => p.x)),
      top: Math.min(...corners.map(p => p.y)), bottom: Math.max(...corners.map(p => p.y)) };
  };
  // Native producers use rectangular user-space clips, in the live transform of their owning group.
  const nativePaintClips = (node) => {
    const clips = [];
    for (let parent = node; parent && parent.ownerSVGElement; parent = parent.parentElement) {
      const reference = /^url\(#([^)]*)\)$/.exec(parent.getAttribute('clip-path') || '');
      if (!reference) continue;
      const clip = parent.ownerSVGElement.getElementById(reference[1]);
      if (!clip || clip.getAttribute('clipPathUnits') !== 'userSpaceOnUse' || clip.children.length !== 1
        || !clip.firstElementChild.matches('rect')) continue;
      const matrix = parent.getScreenCTM(), rect = clip.firstElementChild.getBBox();
      if (!matrix || !(rect.width > 0 && rect.height > 0)) return null;
      clips.push({ rect, matrix });
    }
    return clips;
  };
  // A retained fact or a path's box can lie outside the native clips.
  const paintWithinNativeClips = (node, subject = node) => {
    const paired = subject !== node && pairedExtentObservation(subject);
    const clips = nativePaintClips(node);
    if (!clips) return false;
    let box;
    for (const { rect, matrix } of clips) {
      const corners = [[rect.x, rect.y], [rect.x + rect.width, rect.y], [rect.x, rect.y + rect.height], [rect.x + rect.width, rect.y + rect.height]]
        .map(([x, y]) => new DOMPoint(x, y).matrixTransform(matrix));
      const left = Math.min(...corners.map(p => p.x)), right = Math.max(...corners.map(p => p.x));
      const top = Math.min(...corners.map(p => p.y)), bottom = Math.max(...corners.map(p => p.y));
      // Native SVG coordinates use three decimals; prepared fact locations retain full precision.
      const allowance = svgCoordinatePrecision(matrix), precision = Math.max(allowance.x, allowance.y);
      box ||= paired ? pairedExtentScreenBox(subject) : subject.getBoundingClientRect();
      if (!box) return false;
      if (subject !== node) {
        // Connected paired fills use their own extent. Single-point line/radar paint keeps its source-location contract.
        const x = (box.left + box.right) / 2, y = (box.top + box.bottom) / 2;
        if (x < left - precision || x > right + precision
          || (paired ? box.bottom < top - precision || box.top > bottom + precision : y < top - precision || y > bottom + precision)) return false;
      } else if (box.right < left - precision || box.left > right + precision || box.bottom < top - precision || box.top > bottom + precision) return false;
    }
    return true;
  };
  const pairedBoundaryRole = (node) => /^(difference-(first|second)|range-(upper|lower|midline))$/.test(node.dataset.cfxRole || '');
  const pairedBoundaryInk = (node, subject, paint, region = subject._cfxPairedRegion) => {
    const sourceMatrix = subject.getScreenCTM(), shapeMatrix = node.getScreenCTM();
    const clips = nativePaintClips(node);
    if (!region || !sourceMatrix || !shapeMatrix || !clips || typeof node.isPointInStroke !== 'function') return false;
    // Captured paired extents include equal marker inflation on each side. Remove it to locate the actual boundaries.
    const x = region.x + region.width / 2;
    const ys = node.dataset.cfxRole === 'range-midline' ? [Number(subject.dataset.cfxMidlineY)]
      : [region.y + region.width / 2, region.y + region.height - region.width / 2];
    for (const y of ys) {
      if (!Number.isFinite(y)) continue;
      // Query the exported three-decimal geometry as well as the full-precision captured location.
      for (const [px, py] of [[x, y], [Number(x.toFixed(3)), Number(y.toFixed(3))]]) {
        let point = new DOMPoint(px, py).matrixTransform(sourceMatrix);
        const source = point.matrixTransform(shapeMatrix.inverse());
        // The nearest point inside the clip can still be real cap ink when its source centre lies just outside.
        for (const { rect, matrix } of clips) {
          const local = point.matrixTransform(matrix.inverse());
          point = new DOMPoint(Math.max(rect.x, Math.min(rect.x + rect.width, local.x)),
            Math.max(rect.y, Math.min(rect.y + rect.height, local.y))).matrixTransform(matrix);
        }
        if (clips.some(({ rect, matrix }) => {
          const local = point.matrixTransform(matrix.inverse());
          // Use the same native three-decimal coordinate precision as the clip availability check.
          return local.x < rect.x - .001 || local.x > rect.x + rect.width + .001
            || local.y < rect.y - .001 || local.y > rect.y + rect.height + .001;
        })) continue;
        const sample = point.matrixTransform(shapeMatrix.inverse());
        const radius = parseFloat(paint.strokeWidth) / 2 * (paint.strokeLinecap === 'square' ? Math.SQRT2 : 1);
        // Clamping must stay within this source endpoint's stroke footprint, not follow a remote connected span.
        if (Math.hypot(sample.x - source.x, sample.y - source.y) <= radius + .001 && node.isPointInStroke(sample)) return true;
      }
    }
    return false;
  };
  const shapePaint = (node, styles, subject = node) => {
    if (!node || !node.matches(paintShapes) || node.closest('[data-cfx-browser-hit-area]') || node.classList.contains('cfx-prepared-point-marker')) return null;
    const pairedBoundary = subject !== node && pairedExtentObservation(subject) && pairedBoundaryRole(node);
    if (/-(highlight|halo|shadow(?:-soft)?)$/.test((node.dataset || {}).cfxRole || '')
      || !paintNodeVisible(node, styles) || !pairedBoundary && !paintWithinNativeClips(node)) return null;
    // SVG omits zero-radius circles and ellipses, including their otherwise visible stroke paint.
    if (node.matches('circle,ellipse')) {
      const box = node.getBBox();
      if (!(box.width > 0 && box.height > 0)) return null;
    }
    const paint = paintStyle(node, styles);
    const stroke = parseFloat(paint.strokeWidth) > 0 ? paintValue(node, paint.stroke, paint.strokeOpacity, styles) : null;
    // A series-wide boundary box cannot establish paint at this observation, connected or isolated.
    if (pairedBoundary) return stroke && pairedBoundaryInk(node, subject, paint) ? stroke : null;
    // Open line marks never paint their inherited default black fill.
    const fill = /^(line|polyline)$/i.test(node.tagName) || node.dataset.cfxFillArea === 'false' ? null
      : paintValue(node, paint.fill, paint.fillOpacity, styles);
    if (cartesianFillObservation(node, subject)) {
      if ((node.dataset.cfxRole || '').endsWith('-pattern')) return stroke && cartesianFillInk(node, subject, styles) ? stroke : null;
      if (fill && cartesianFillInk(node, subject, styles)) return fill;
      return stroke && cartesianFillStrokeInk(node, subject, paint) ? stroke : null;
    }
    const resolved = fill || stroke;
    return resolved && (subject === node || paintWithinNativeClips(node, subject)) ? resolved : null;
  };
  const primaryTextPaint = (node, decoration, styles) => {
    const role = (node.dataset || {}).cfxRole;
    // Text is the mark for a word-cloud term or annotation caption; ordinary point labels are decoration.
    if (role !== 'word-cloud-term' && role !== 'annotation' && !(decoration && role === 'legend-item')) return null;
    for (const text of node.querySelectorAll('text')) {
      if (!text.textContent.trim() || !paintNodeVisible(text, styles) || !paintWithinNativeClips(text)
        || !decoration && text.closest('[data-cfx-label-decoration]')) continue;
      const style = paintStyle(text, styles);
      if (!(parseFloat(style.fontSize) > 0)) continue;
      const stroke = parseFloat(style.strokeWidth) > 0 ? paintValue(text, style.stroke, style.strokeOpacity, styles) : null;
      const paint = paintValue(text, style.fill, style.fillOpacity, styles) || stroke;
      if (paint) return paint;
    }
    return null;
  };
  const childPaint = (node, decoration, styles, subject) => {
    if (!node) return null;
    const shapes = node.matches(paintShapes) ? [node] : Array.from(node.querySelectorAll(paintShapes));
    const primary = shapes.filter((shape) => shape.matches('[data-cfx-role^="circle-value"],[data-cfx-role^="gauge-value"],[data-cfx-role="gauge-needle"],[data-cfx-role="bullet-value"]'));
    const authoredPattern = (shape) => /-pattern$/.test(shape.dataset.cfxRole || '');
    const patterns = shapes.filter(authoredPattern);
    for (const shape of primary.concat(shapes.filter((shape) => !authoredPattern(shape)))) {
      if (!decoration && shape.closest('[data-cfx-label-decoration]')) continue;
      const paint = shapePaint(shape, styles, subject || shape);
      if (paint) return paint;
    }
    const text = primaryTextPaint(node, decoration, styles);
    if (text) return text;
    // Authored hatches can be the only mark ink; captions and ordinary surfaces keep their paint precedence.
    for (const shape of patterns) {
      if (!decoration && shape.closest('[data-cfx-label-decoration]')) continue;
      const paint = shapePaint(shape, styles, subject || shape);
      if (paint) return paint;
    }
    return null;
  };
  const seriesPaint = (node, styles) => {
    const owner = node.closest('[data-cfx-role="series"],[data-cfx-role="radar-series"],[data-cfx-role="polar-series"]');
    if (!owner) return null;
    const data = node.dataset || {};
    const paired = (data.cfxKind || '').toLowerCase() === 'differencearea' && data.cfxPoint !== undefined;
    const connected = !pairedExtentObservation(node) || pairedConnectedSpan(node, owner);
    const relevant = (layer) => {
      const role = layer.dataset.cfxRole || '';
      if (!connected && /^(difference-(positive|negative)|range-(area|band))(-pattern)?$/.test(role)) return false;
      if (!paired) return true;
      if (role === 'difference-positive' || role === 'difference-positive-pattern') return Number(data.cfxFirst) > Number(data.cfxSecond);
      if (role === 'difference-negative' || role === 'difference-negative-pattern') return Number(data.cfxFirst) < Number(data.cfxSecond);
      return true;
    };
    for (const layer of owner.querySelectorAll('[data-cfx-role="line"],[data-cfx-role="trend-line"],[data-cfx-role="slope-line"],[data-cfx-role="area"],[data-cfx-role="range-area"],[data-cfx-role="range-band"],[data-cfx-role="range-upper"],[data-cfx-role="range-lower"],[data-cfx-role="range-midline"],[data-cfx-role="difference-positive"],[data-cfx-role="difference-negative"],[data-cfx-role="difference-first"],[data-cfx-role="difference-second"],[data-cfx-role="radar-outline"],[data-cfx-role="radar-area"],[data-cfx-role="polar-line"]')) {
      if (!relevant(layer)) continue;
      const paint = childPaint(layer, false, styles, node.hasAttribute('data-cfx-point') ? node : undefined);
      if (paint) return paint;
    }
    for (const layer of owner.querySelectorAll('[data-cfx-role="area-pattern"],[data-cfx-role="range-area-pattern"],[data-cfx-role="range-band-pattern"],[data-cfx-role="difference-positive-pattern"],[data-cfx-role="difference-negative-pattern"],[data-cfx-role="radar-pattern"]')) {
      if (!relevant(layer)) continue;
      const paint = childPaint(layer, false, styles, node.hasAttribute('data-cfx-point') ? node : undefined);
      if (paint) return paint;
    }
    return null;
  };
  const observationPaint = (node, styles) => withNativeHighlightPaint(node, () => {
    // A probe suspends adapter opacity only; use fresh live style values after the suspension.
    if (node.closest('[data-cfx-highlight-managed]')) styles = new Map();
    return paintAncestorsVisible(node, styles) ? childPaint(node, false, styles) || seriesPaint(node, styles) : null;
  });
  const retainedGeometryFact = (node) => ['zero', 'precision-collapse', 'layout-collapse'].includes((node.dataset || {}).cfxGeometryStatus);
  // Geometry alone is not evidence of paint: transparent browser hit areas and retained facts have boxes too.
  // Legend summaries remain usable when their data is muted; data targets obey ancestor muting.
  const pointerTargetPaint = (node, styles = new Map()) => {
    if (!node || !isInteractiveTarget(node)) return null;
    const legend = (node.dataset || {}).cfxRole === 'legend-item';
    if (legend) return childPaint(node, true, styles);
    if (node.dataset.cfxVisible === 'false' || node.closest('.cfx-series-muted') || retainedGeometryFact(node)) return null;
    return observationPaint(node, styles);
  };
  const tooltipReadoutAvailable = (node, event, styles = new Map()) => {
    if (pointerTargetPaint(node, styles)) return true;
    // Authored facts without painted geometry remain a keyboard readout, without becoming pointer targets.
    const pointer = event instanceof PointerEvent || event instanceof MouseEvent && event.detail > 0;
    return !pointer && retainedGeometryFact(node)
      && paintAncestorsVisible(node, styles) && keyboardTargetAvailable(node);
  };
  const paintColour = (node, styles = new Map()) => {
    if (!node) return '';
    const legend = (node.dataset || {}).cfxRole === 'legend-item';
    const paint = legend ? childPaint(node, true, styles) : observationPaint(node, styles);
    if (paint && paint.colour) return paint.colour;
    const fallback = !legend && childPaint(seriesLegend(node), true, styles);
    return fallback && fallback.colour || '';
  };
  // The prepared kind names distinguish source quantities from map/grid coordinates and tuple summaries.
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
      // Reject unrelated coordinates and duplicate series before resolving any computed mark styles.
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
    // Duplicate x coordinates are valid: retain the observation the reader actually interacted with.
    addObservation(node);
    svg.querySelectorAll('[data-cfx-point][data-cfx-series][data-cfx-x]').forEach(addObservation);
    const priority = { danger: 5, warning: 4, info: 3, none: 2, neutral: 1, quiet: 0, success: 0 };
    const rows = Array.from(points.values()).sort((a, b) => (priority[b.state] || 0) - (priority[a.state] || 0) || b.value - a.value);
    if (!rows.length) return false;
    tip.replaceChildren();
    const header = document.createElement('div');
    header.className = 'cfx-tooltip__title'; header.textContent = capturedText(data, 'cfxXLabel', capturedText(data, 'cfxDisplayLabel', numericFactText(data, 'X'))); tip.appendChild(header);
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
  // Timing is scoped to one chart and semantic target; hover and guide notifications stay immediate.
  const tooltipRequests = new WeakMap();
  const pendingTooltips = new Set();
  let tooltipObserver = null;
  const tooltipObservedNodes = new Map();
  const pointerReadoutSuspended = root => root._cfxPointerReadoutSuspended === true;
  const resumePointerReadout = root => { delete root._cfxPointerReadoutSuspended; };
  const suspendPointerReadout = root => {
    // Reset can animate native marks beneath a stationary pointer. Passive entry
    // must not undo its dismissal; only fresh pointer or keyboard input resumes it.
    root._cfxPointerReadoutSuspended = true;
    hideTip(root, root.querySelector('.cfx-tooltip'), true);
  };
  // A closed shadow tree is still reachable from its chart. Keep every enclosing tree in the lifetime boundary.
  const tooltipRootTrees = (root) => {
    const trees = [];
    for (let tree = root.getRootNode(); tree; tree = tree.host && tree.host.getRootNode()) trees.push(tree);
    return trees;
  };
  const tooltipLifetimeNodes = (root) => {
    const nodes = [];
    for (let node = root; node; node = node.parentNode || node.host) nodes.push(node);
    return nodes;
  };
  const tooltipDelay = (root) => {
    const value = Number(root.dataset.cfxTooltipDelay || 0);
    return Number.isInteger(value) && value >= 0 && value <= 2147483647 ? value : 0;
  };
  const removePendingTooltip = (request) => {
    if (!pendingTooltips.delete(request)) return;
    if (!pendingTooltips.size && tooltipObserver) {
      tooltipObserver.disconnect(); tooltipObserver = null; tooltipObservedNodes.clear();
    } else if (tooltipObserver) {
      // A shared observer cannot unobserve one node or downgrade its subtree boundary. Release canceled hosts.
      // Preserve queued removals for surviving requests before disconnect discards the native record queue.
      checkPendingTooltipMutations(tooltipObserver.takeRecords());
      if (!tooltipObserver) return;
      tooltipObserver.disconnect(); tooltipObservedNodes.clear();
      for (const pending of pendingTooltips) watchPendingTooltips(pending);
    }
  };
  const cancelTooltipRequest = (root) => {
    // A host may restore paint before a staged move runs. Retire that old sample
    // with its request so it cannot acquire a new deadline without fresh input.
    root._cfxCancelPointerHover?.();
    const request = tooltipRequests.get(root);
    if (!request) return;
    if (request.timer !== undefined) window.clearTimeout(request.timer);
    removePendingTooltip(request);
    tooltipRequests.delete(root);
  };
  const tooltipRequestAvailable = ({ root, tip, node }) => root.isConnected && tip.isConnected && root.contains(tip)
    && node.isConnected && root.contains(node) && hasFeature(root, 'Tooltips')
    && root.dataset.cfxTooltipPinned !== 'true' && !!pointerTargetPaint(node);
  const currentPointerTooltip = (root, event) => {
    if (!event || !Number.isFinite(event.clientX) || !Number.isFinite(event.clientY) || !hasFeature(root, 'Tooltips')) return null;
    // The event's old hit can outlive a scroll, reflow or host update. Resolve the actual native surface again.
    let hit = root.ownerDocument.elementFromPoint(event.clientX, event.clientY);
    const trees = tooltipRootTrees(root);
    // Descend only through the actual enclosing host hit: outer dialogs must continue to obscure the chart.
    for (let index = trees.length - 2; index >= 0; index--) {
      if (hit !== trees[index].host) return null;
      hit = trees[index].elementFromPoint(event.clientX, event.clientY);
    }
    if (!hit || !root.contains(hit)) return null;
    const legend = hit && hit.closest('[data-cfx-role="legend-item"]');
    if (legend && root.contains(legend) && pointerTargetPaint(legend)) return legend;
    const candidates = pointerCandidates(root, { target: hit, clientX: event.clientX, clientY: event.clientY }, root.dataset.cfxTooltipRange !== 'exact');
    const point = acquiredTooltipPoint(root, candidates);
    return point && point.node;
  };
  const checkPendingTooltipMutations = (records) => {
    // Native paint probes are synchronous reads, not host changes. Observing their own suspension
    // would queue another availability probe indefinitely while a pointer request is pending.
    records = records.filter(record => record.type !== 'attributes' || record.attributeName !== 'data-cfx-highlight-probe');
    if (!records.length) return;
    for (const pending of Array.from(pendingTooltips)) {
      const removed = records.some(record => record.type === 'childList'
        && Array.from(record.removedNodes).some(node => node.contains(pending.root) || node.contains(pending.node) || node.contains(pending.tip)
          || pending.nodes.some(boundary => node.contains(boundary))));
      // Ancestor child-list delivery is needed for detach, but unrelated dashboard updates need no paint scan.
      const paintChanged = records.some(record => pending.root.contains(record.target)
        || record.type === 'attributes' && pending.nodes.includes(record.target));
      if (removed || paintChanged && !tooltipRequestAvailable(pending)) cancelTooltipRequest(pending.root);
    }
  };
  const watchPendingTooltips = (request) => {
    // Observe only while a request is queued, so a detached or briefly replaced host cannot retain a long timer.
    if (!tooltipObserver) tooltipObserver = new MutationObserver(checkPendingTooltipMutations);
    for (const node of request.nodes) {
      const subtree = node === request.root;
      if (tooltipObservedNodes.has(node) && (tooltipObservedNodes.get(node) || !subtree)) continue;
      // Descendants belong to the chart. Enclosing elements and shadow hosts contribute only their own style and detach.
      tooltipObserver.observe(node, { childList: true, attributes: node instanceof Element, subtree });
      tooltipObservedNodes.set(node, subtree);
    }
  };
  const requestPointerTip = (root, tip, node, event) => {
    if (pointerReadoutSuspended(root)) return;
    const key = targetKey(targetIdentity(node));
    const previous = tooltipRequests.get(root);
    if (previous && previous.key === key && (previous.pointerId === undefined || previous.pointerId === event.pointerId)) {
      previous.node = node; previous.event = event; previous.pointerId = event.pointerId;
      if (previous.shown) displayTip(tip, node, event);
      return;
    }
    cancelTooltipRequest(root);
    tip.hidden = true;
    const request = { root, tip, node, key, event, pointerId: event.pointerId, shown: false, nodes: tooltipLifetimeNodes(root) };
    tooltipRequests.set(root, request);
    pendingTooltips.add(request);
    watchPendingTooltips(request);
    request.timer = window.setTimeout(() => {
      removePendingTooltip(request);
      request.timer = undefined;
      if (tooltipRequests.get(root) !== request) return;
      const acquired = tooltipRequestAvailable(request) && currentPointerTooltip(root, request.event);
      if (!acquired || targetKey(targetIdentity(acquired)) !== request.key) { cancelTooltipRequest(root); return; }
      request.node = acquired;
      request.shown = true;
      displayTip(tip, acquired, request.event);
    }, tooltipDelay(root));
  };
  // An immediate focus readout is already shown for this identity; pointer handoff must not hide it again.
  const rememberImmediateTooltip = (root, tip, node, event) => {
    if (tooltipDelay(root) > 0) tooltipRequests.set(root, { root, tip, node, event, key: targetKey(targetIdentity(node)), shown: true });
  };
  const ownsTooltipPointer = (root, event) => {
    const request = tooltipRequests.get(root);
    return !(event instanceof PointerEvent) || !request || request.pointerId === undefined || request.pointerId === event.pointerId;
  };
  const updateTooltipPointer = (tip, node, event) => {
    if (!(event instanceof PointerEvent)) return;
    const root = tip.closest('.cfx-interactive-chart'), request = root && tooltipRequests.get(root);
    if (request && !request.shown && ownsTooltipPointer(root, event) && request.key === targetKey(targetIdentity(node))) request.event = event;
  };
  const retainPointerTip = (root, event) => {
    const request = tooltipRequests.get(root);
    if (!request || !tooltipRequestAvailable(request)) return false;
    const node = currentPointerTooltip(root, event);
    if (!node || targetKey(targetIdentity(node)) !== request.key) return false;
    request.node = node; request.event = event;
    return true;
  };
  // Screen placement belongs to the Html adapter; target acquisition supplies the canonical native node.
  const tooltipPositions = new WeakMap();
  const activeTooltipPositions = new Set();
  const tooltipPositionTrees = new Set();
  let tooltipPositionFrame = 0, tooltipPositionObserver = null, tooltipPositionResize = null;
  const tooltipDirections = {
    top: [0, -1], 'top-right': [1, -1], right: [1, 0], 'bottom-right': [1, 1],
    bottom: [0, 1], 'bottom-left': [-1, 1], left: [-1, 0], 'top-left': [-1, -1]
  };
  const unwatchTooltipPositionTrees = () => {
    for (const tree of tooltipPositionTrees) {
      tree.removeEventListener('scroll', queueTooltipPositionRefresh, true);
      tree.removeEventListener('transitionend', queueTooltipPositionRefresh, true);
      tree.removeEventListener('slotchange', queueTooltipPositionRefresh, true);
    }
    tooltipPositionTrees.clear();
  };
  const forgetTooltipPosition = (tip) => {
    const state = tooltipPositions.get(tip);
    tooltipPositions.delete(tip);
    if (state?.intersectionObserver) state.intersectionObserver.disconnect();
    if (state) activeTooltipPositions.delete(state);
    if (tooltipPositionResize && state) tooltipPositionResize.unobserve(state.stage);
    if (activeTooltipPositions.size) {
      if (tooltipPositionObserver && Array.from(tooltipPositionTrees).some(tree =>
        !Array.from(activeTooltipPositions).some(current => current.trees.includes(tree)))) {
        // Drain surviving hosts' removals before rebuilding registrations to release unused shadow trees.
        checkTooltipPositionMutations(tooltipPositionObserver.takeRecords());
        if (!tooltipPositionObserver) return;
        tooltipPositionObserver.disconnect(); unwatchTooltipPositionTrees();
        for (const current of activeTooltipPositions) watchTooltipPositionTrees(current);
      }
      return;
    }
    if (tooltipPositionObserver) { tooltipPositionObserver.disconnect(); tooltipPositionObserver = null; unwatchTooltipPositionTrees(); }
    if (tooltipPositionResize) { tooltipPositionResize.disconnect(); tooltipPositionResize = null; }
    window.removeEventListener('resize', queueTooltipPositionRefresh);
    window.visualViewport?.removeEventListener('scroll', queueTooltipPositionRefresh);
    window.visualViewport?.removeEventListener('resize', queueTooltipPositionRefresh);
    if (tooltipPositionFrame) { cancelAnimationFrame(tooltipPositionFrame); tooltipPositionFrame = 0; }
  };
  const tooltipSourceRect = (node) => {
    if (node._cfxPairedRegion) return pairedExtentScreenBox(node);
    const region = node._cfxNativeRegion, matrix = region && node.getScreenCTM();
    if (matrix) {
      const corners = [[region.x, region.y], [region.x + region.width, region.y],
        [region.x, region.y + region.height], [region.x + region.width, region.y + region.height]]
        .map(([x, y]) => new DOMPoint(x, y).matrixTransform(matrix));
      return { left: Math.min(...corners.map(p => p.x)), right: Math.max(...corners.map(p => p.x)),
        top: Math.min(...corners.map(p => p.y)), bottom: Math.max(...corners.map(p => p.y)) };
    }
    const rect = node.getBoundingClientRect();
    // Unlocated retained keyboard facts have no painted box; their visible SVG remains the host boundary.
    return !rect.width && !rect.height && node.ownerSVGElement ? node.ownerSVGElement.getBoundingClientRect() : rect;
  };
  const tooltipShadowContainingBlock = (node, position, source) => {
    if (!(node instanceof HTMLElement) || node.getRootNode() === source.getRootNode()) return false;
    // offsetParent can hide a slotted node's internal containing block. Inspect the accessible flat tree.
    const style = getComputedStyle(node);
    return position === 'absolute' && style.position !== 'static'
      || ['transform', 'translate', 'rotate', 'scale', 'perspective', 'filter', 'backdropFilter'].some(key => style[key] && style[key] !== 'none')
      || /\b(layout|paint|content|strict)\b/.test(style.contain)
      || /\b(transform|translate|rotate|scale|perspective|filter|backdrop-filter)\b/.test(style.willChange)
      || style.contentVisibility === 'auto';
  };
  const tooltipClippingAncestors = (source) => {
    const ancestors = [];
    let positioned = null, containingBlock = null;
    for (let child = source, node = composedParent(child); node; child = node, node = composedParent(node)) {
      if (!positioned && child instanceof HTMLElement && ['absolute', 'fixed'].includes(getComputedStyle(child).position)) {
        positioned = getComputedStyle(child).position; containingBlock = child.offsetParent;
      }
      if (positioned) {
        // Native offsetParent includes transformed/paint-contained blocks. Overflow below it is bypassed.
        if (node !== containingBlock && !tooltipShadowContainingBlock(node, positioned, source)) continue;
        positioned = null;
      }
      if (!(node instanceof HTMLElement) || !node.getClientRects().length) continue;
      const style = getComputedStyle(node);
      // Overflow does not clip a non-replaced inline box. A real zero-sized block still clips.
      if (style.display === 'inline' || style.display === 'contents') continue;
      const paint = /\b(paint|content|strict)\b/.test(style.contain);
      const x = paint || style.overflowX !== 'visible', y = paint || style.overflowY !== 'visible';
      if (x || y) ancestors.push({ node, x, y });
    }
    return ancestors;
  };
  const tooltipClippingBounds = (ancestors) => {
    const viewport = window.visualViewport;
    let left = viewport?.offsetLeft || 0, top = viewport?.offsetTop || 0;
    let right = left + (viewport?.width ?? window.innerWidth), bottom = top + (viewport?.height ?? window.innerHeight);
    for (const { node, x, y } of ancestors) {
      const rect = node.getBoundingClientRect(), scaleX = node.offsetWidth ? rect.width / node.offsetWidth : 1;
      const scaleY = node.offsetHeight ? rect.height / node.offsetHeight : 1;
      if (x) { left = Math.max(left, rect.left + node.clientLeft * scaleX); right = Math.min(right, rect.left + (node.clientLeft + node.clientWidth) * scaleX); }
      if (y) { top = Math.max(top, rect.top + node.clientTop * scaleY); bottom = Math.min(bottom, rect.top + (node.clientTop + node.clientHeight) * scaleY); }
    }
    return { left, top, right, bottom };
  };
  const tooltipSourceInView = (state) => {
    if (state.paintChanged || !state.clippingAncestors) {
      state.clippingAncestors = tooltipClippingAncestors(state.node);
      state.placementClippingAncestors = tooltipClippingAncestors(state.tip);
    }
    // Keep geometry live while caching overflow axes until the existing paint invalidation fires.
    const { left, top, right, bottom } = tooltipClippingBounds(state.clippingAncestors);
    const source = tooltipSourceRect(state.node);
    return state.nativeIntersecting !== false && source && right > left && bottom > top
      && source.right > left && source.left < right && source.bottom > top && source.top < bottom;
  };
  const tooltipPositionAvailable = (state) => state.root.isConnected && state.tip.isConnected && state.node.isConnected
    && state.root.contains(state.tip) && state.root.contains(state.node) && !state.tip.hidden && !state.root.hidden
    && hasFeature(state.root, 'Tooltips') && state.stage.getClientRects().length > 0
    && (!state.paintChanged || tooltipReadoutAvailable(state.node, state.readoutEvent)) && tooltipSourceInView(state);
  const queueTooltipPositionRefresh = (invalidation) => {
    // Native geometry events can change clipping or responsive paint without a DOM attribute mutation.
    if (invalidation) for (const state of activeTooltipPositions) state.paintChanged = true;
    if (tooltipPositionFrame || !activeTooltipPositions.size) return;
    tooltipPositionFrame = requestAnimationFrame(() => {
      tooltipPositionFrame = 0;
      const changes = Array.from(activeTooltipPositions).filter(state => state.paintChanged).map(state =>
        ({ state, ancestry: composedAncestry(state.root) })).filter(({ state, ancestry }) =>
        state.nodes.length !== ancestry.nodes.length || state.nodes.some((node, index) => node !== ancestry.nodes[index]));
      if (changes.length && tooltipPositionObserver) {
        // Preserve queued detach records before rebinding a moved slot's containing block and trees.
        checkTooltipPositionMutations(tooltipPositionObserver.takeRecords());
        if (tooltipPositionObserver) {
          for (const { state, ancestry } of changes) if (activeTooltipPositions.has(state)) Object.assign(state, ancestry);
          tooltipPositionObserver.disconnect(); unwatchTooltipPositionTrees();
          for (const state of activeTooltipPositions) watchTooltipPositionTrees(state);
        }
      }
      for (const state of Array.from(activeTooltipPositions)) {
        if (state.tip.hidden) { forgetTooltipPosition(state.tip); continue; }
        if (!tooltipPositionAvailable(state)) { hideTip(state.root, state.tip, true); continue; }
        state.paintChanged = false;
        placeTooltip(state, true);
      }
    });
  };
  const checkTooltipPositionMutations = (records) => {
    // Placement writes cannot change target paint or layout. External tooltip styles still invalidate geometry.
    records = records.filter(record => {
      const placed = tooltipPositions.get(record.target);
      return !(placed && record.type === 'attributes' && record.attributeName === 'style'
        && placed.style === placed.tip.getAttribute('style'));
    });
    if (!records.length) return;
    for (const current of Array.from(activeTooltipPositions)) {
      // A new delayed target can hide a shown readout. Release geometry without cancelling its new request.
      if (current.tip.hidden) { forgetTooltipPosition(current.tip); continue; }
      const removed = records.some(record => record.type === 'childList'
        && Array.from(record.removedNodes).some(node => node.contains(current.root) || node.contains(current.node) || node.contains(current.tip)
          || current.nodes.some(boundary => node.contains(boundary))));
      if (removed) { hideTip(current.root, current.tip, true); continue; }
      // Other dashboard content may move this chart, but only the chart or its ancestors invalidate paint.
      if (records.some(record => !current.tip.contains(record.target) && (current.root.contains(record.target)
        || record.type === 'attributes' && current.nodes.includes(record.target)))) current.paintChanged = true;
    }
    queueTooltipPositionRefresh();
  };
  const watchTooltipPositionTrees = (state) => {
    for (const tree of state.trees) {
      if (tooltipPositionTrees.has(tree)) continue;
      tooltipPositionObserver.observe(tree, { childList: true, attributes: true,
        attributeFilter: [...paintAvailabilityAttributes, 'data-cfx-interaction-features'], subtree: true });
      // Native geometry events do not cross a shadow boundary. Own them alongside the tree's observer.
      tree.addEventListener('scroll', queueTooltipPositionRefresh, true);
      tree.addEventListener('transitionend', queueTooltipPositionRefresh, true);
      tree.addEventListener('slotchange', queueTooltipPositionRefresh, true);
      tooltipPositionTrees.add(tree);
    }
  };
  const watchTooltipPosition = (state) => {
    if (typeof IntersectionObserver !== 'undefined' && state.intersectionNode !== state.node) {
      state.intersectionObserver?.disconnect(); state.nativeIntersecting = undefined; state.intersectionNode = state.node;
      const box = state.node.getBoundingClientRect();
      if (box.width > 0 && box.height > 0 && !retainedGeometryFact(state.node)) {
        // Native intersection sees opaque closed slots. It supplements the precise source footprint above;
        // an inflated paired hit box still cannot establish exact partial clipping inside an opaque host.
        const observer = new IntersectionObserver(entries => {
          const entry = entries.find(item => item.target === state.node);
          if (!entry || state.intersectionObserver !== observer || !activeTooltipPositions.has(state)) return;
          state.nativeIntersecting = entry.isIntersecting;
          queueTooltipPositionRefresh();
        });
        state.intersectionObserver = observer; observer.observe(state.node);
      }
    }
    if (activeTooltipPositions.has(state)) return;
    activeTooltipPositions.add(state);
    if (!tooltipPositionObserver) {
      // Shared listeners exist only while a readout is visible, including pointer and pinned readouts.
      window.addEventListener('resize', queueTooltipPositionRefresh);
      window.visualViewport?.addEventListener('scroll', queueTooltipPositionRefresh);
      window.visualViewport?.addEventListener('resize', queueTooltipPositionRefresh);
      tooltipPositionObserver = new MutationObserver(checkTooltipPositionMutations);
      if (typeof ResizeObserver !== 'undefined') tooltipPositionResize = new ResizeObserver(queueTooltipPositionRefresh);
    }
    watchTooltipPositionTrees(state);
    if (tooltipPositionResize) tooltipPositionResize.observe(state.stage);
  };
  const refreshTooltipPosition = (root) => {
    const state = tooltipPositions.get(root.querySelector('.cfx-tooltip'));
    if (state && activeTooltipPositions.has(state)) { state.paintChanged = true; queueTooltipPositionRefresh(); }
  };
  const tooltipAnchorRect = (state) => {
    if (state.anchor === 'chart') return state.stage.getBoundingClientRect();
    const rect = state.node.getBoundingClientRect();
    if (state.anchor === 'node') return rect;
    let x = state.event?.clientX, y = state.event?.clientY;
    if (!Number.isFinite(x) || !Number.isFinite(y)) { x = rect.left + rect.width / 2; y = rect.top + rect.height / 2; }
    if (!Number.isFinite(x) || !Number.isFinite(y)) { x = 24; y = 24; }
    return { left: x, right: x, top: y, bottom: y, width: 0, height: 0 };
  };
  const tooltipPositionGeometry = (state, anchor, bounds) => {
    const stage = state.stage.getBoundingClientRect(), tip = state.tip.getBoundingClientRect();
    return [anchor.left, anchor.top, anchor.width, anchor.height, stage.left, stage.top, stage.width, stage.height,
      tip.left, tip.top, tip.width, tip.height, bounds.left, bounds.top, bounds.right, bounds.bottom];
  };
  const placeTooltip = (state, skipUnchanged = false) => {
    const { root, tip } = state, anchor = tooltipAnchorRect(state);
    const bounds = tooltipClippingBounds(state.placementClippingAncestors);
    const geometry = tooltipPositionGeometry(state, anchor, bounds);
    if (skipUnchanged && state.geometry && geometry.every((value, index) => value === state.geometry[index])) return;
    // Measure at a stable origin, then keep the measured width while positioning. This prevents
    // fixed-position shrink-to-fit from changing the readout as it approaches a containing block's edge.
    tip.style.left = '0px'; tip.style.top = '0px';
    tip.style.width = '';
    tip.style.width = getComputedStyle(tip).width;
    const origin = tip.getBoundingClientRect();
    tip.style.left = '1px'; tip.style.top = '1px';
    const unit = tip.getBoundingClientRect();
    // The one-pixel probe converts screen coordinates through ordinary translated/scaled host blocks.
    const scaleX = unit.left - origin.left || 1, scaleY = unit.top - origin.top || 1;
    const gap = Number(root.dataset.cfxTooltipGap ?? 14);
    const offsetX = Number(root.dataset.cfxTooltipOffsetX || 0), offsetY = Number(root.dataset.cfxTooltipOffsetY || 0);
    const placements = (root.dataset.cfxTooltipPlacements || 'bottom-right').split(',');
    const candidates = placements.map(placement => {
      const [dx, dy] = tooltipDirections[placement] || tooltipDirections['bottom-right'];
      return {
        x: (dx > 0 ? anchor.right + gap : dx < 0 ? anchor.left - gap - unit.width : anchor.left + (anchor.width - unit.width) / 2) + offsetX,
        y: (dy > 0 ? anchor.bottom + gap : dy < 0 ? anchor.top - gap - unit.height : anchor.top + (anchor.height - unit.height) / 2) + offsetY
      };
    });
    const position = candidates.find(candidate => candidate.x >= bounds.left + 8 && candidate.y >= bounds.top + 8
      && candidate.x + unit.width <= bounds.right - 8 && candidate.y + unit.height <= bounds.bottom - 8) || candidates[0];
    const x = clamp(position.x, bounds.left + 8, Math.max(bounds.left + 8, bounds.right - unit.width - 8));
    const y = clamp(position.y, bounds.top + 8, Math.max(bounds.top + 8, bounds.bottom - unit.height - 8));
    tip.style.left = (x - origin.left) / scaleX + 'px';
    tip.style.top = (y - origin.top) / scaleY + 'px';
    state.style = tip.getAttribute('style');
    state.geometry = tooltipPositionGeometry(state, anchor, bounds);
  };
  const positionTooltip = (tip, event, node) => {
    if (tip.hidden) return;
    const root = tip.closest('.cfx-interactive-chart'), stage = root && root.querySelector('.cfx-stage');
    if (!root || !stage || !node) return;
    let state = tooltipPositions.get(tip);
    const pinned = state && root.dataset.cfxTooltipPinned === 'true' && state.key === root.dataset.cfxPinnedTarget;
    if (!state) { state = { root, tip, stage, ...composedAncestry(root) }; tooltipPositions.set(tip, state); }
    // A pin retains its acquisition mode: later pointer movement must not invalidate a keyboard-only fact.
    if (!pinned) {
      if (state.node !== node) state.nativeIntersecting = undefined;
      state.node = node; state.key = targetKey(targetIdentity(node)); state.readoutEvent = event;
    }
    state.event = event; state.anchor = root.dataset.cfxTooltipAnchor || 'pointer';
    state.paintChanged = true;
    if (!tooltipPositionAvailable(state)) { hideTip(root, tip, true); return; }
    state.paintChanged = false;
    placeTooltip(state);
    watchTooltipPosition(state);
  };
  // Separately painted captions are pointer surfaces of their declared native target, never new observations.
  const pointLabelTarget = (root, target) => {
    const label = target instanceof Element ? target.closest('[data-cfx-label-for]') : null;
    const point = label && label._cfxLabelTarget;
    return point && root.contains(label) && root.contains(point) ? point : null;
  };
  const pointLabelSurfaces = (root, point) => (point._cfxPointLabels || []).filter(label => root.contains(label));
  // Core exports describe immutable source identity and layout. Browser-only focus and hit areas belong here.
  const prepareChartTargets = (root) => {
    const svg = root.querySelector('.cfx-stage svg');
    if (!svg || !root.dataset.cfxPreparedChart) return;
    let metadata;
    try { metadata = JSON.parse(root.dataset.cfxPreparedChart); } catch (_) { return; }
    const series = metadata.series || [];
    root._cfxDataProjection = metadata.dataProjection || null;
    root._cfxObservationIds = series.map(item => new Map((item.observationIds || []).map(value => [value.index, value.id])));
    const xLabels = new Map((metadata.categoryLabels || metadata.xLabels || []).map((item) => [item.value, item.text]));
    const regions = new Map((metadata.regions || []).map((region) => [region.id, region]));
    // Only the renderer's unambiguous final Cartesian map can bound a guide.
    const plots = (metadata.regions || []).filter(region => region.role === 'cartesian-plot');
    const plot = plots.length === 1 ? plots[0] : null;
    root._cfxCartesianPlot = plot && [plot.x, plot.y, plot.width, plot.height, plot.x + plot.width, plot.y + plot.height].every(Number.isFinite)
      && plot.width > 0 && plot.height > 0 ? plot : null;
    series.forEach((item, index) => {
      svg.setAttribute('data-cfx-series-name-' + index, item.name);
      svg.setAttribute('data-cfx-series-key-' + index, item.key);
      svg.setAttribute('data-cfx-series-scope-' + index, item.local ? 'local' : 'shared');
      svg.setAttribute('data-cfx-series-state-' + index, item.state);
      svg.setAttribute('data-cfx-series-source-indices-' + index, item.indices.join(','));
    });
    const sourceFacts = new Map(Array.from(svg.querySelectorAll('[data-cfx-point][data-cfx-source-points],[data-cfx-point][data-cfx-derived]'))
      .map((node) => [node.dataset.cfxSeries + ':' + node.dataset.cfxPoint,
        { points: node.dataset.cfxSourcePoints, derived: node.dataset.cfxDerived, identity: derivedPointIdentity(node) }]));
    const observationIds = new Map(Array.from(svg.querySelectorAll('[data-cfx-point][data-cfx-observation-id]'))
      .map(node => [node.dataset.cfxSeries + ':' + node.dataset.cfxPoint, node.dataset.cfxObservationId]));
    svg.querySelectorAll('[data-cfx-role="legend-entry"],[data-cfx-role="legend-item"]').forEach((node) => {
      const data = node.dataset;
      const source = data.cfxSourceId || '';
      const match = source.match(/^legend-series-(\d+)(?:-point-(\d+|other))?$/);
      const index = match ? Number(match[1]) : series.findIndex((item) => item.key === data.cfxSeriesKey);
      if (index < 0) return;
      data.cfxRole = 'legend-item'; data.cfxSeries = String(index);
      const region = regions.get(source);
      data.cfxLabel = data.cfxLabel || (region ? region.label : node.getAttribute('aria-label') || '');
      const reference = legendTarget(node);
      if (reference) {
        const mark = referencedTargetNode(svg, { ...reference, seriesKey: data.cfxSeriesKey });
        if (mark) {
          data.cfxLabel = mark.dataset.cfxLabel || data.cfxLabel;
          if (mark.dataset.cfxValue !== undefined) data.cfxValue = mark.dataset.cfxValue;
          if (mark.dataset.cfxFormattedValue !== undefined) data.cfxFormattedValue = mark.dataset.cfxFormattedValue;
          if (mark.dataset.cfxLabelValue !== undefined) data.cfxLabelValue = mark.dataset.cfxLabelValue;
        }
      }
      if (match && match[2] !== undefined) {
        data.cfxPoint = match[2] === 'other' ? '-1' : match[2];
        const collectionKey = index + ':' + data.cfxPoint;
        const observationId = observationIds.get(collectionKey);
        if (observationId !== undefined) data.cfxObservationId = observationId;
        const facts = sourceFacts.get(collectionKey);
        if (facts) {
          if (data.cfxSourcePoints === undefined && facts.points !== undefined) data.cfxSourcePoints = facts.points;
          if (data.cfxDerived === undefined && facts.derived !== undefined) data.cfxDerived = facts.derived;
          if (data.cfxDerivedIdentity === undefined) data.cfxDerivedIdentity = facts.identity;
        }
      }
    });
    svg.querySelectorAll('[data-cfx-point],[data-cfx-series],[data-cfx-role="gauge"],[data-cfx-role="circle-chart"]').forEach((node) => {
      const data = node.dataset;
      if (data.cfxSeries === undefined) {
        const owner = node.parentElement && node.parentElement.closest('[data-cfx-series]');
        data.cfxSeries = owner ? owner.dataset.cfxSeries : '0';
      }
      const item = series[Number(data.cfxSeries)];
      if (!item) return;
      data.cfxSeriesName = item.name; data.cfxSeriesKey = item.key;
      data.cfxSeriesScope = item.local ? 'local' : 'shared';
      data.cfxKind = item.kind;
      if (data.cfxState === undefined) data.cfxState = item.state;
      if (data.cfxRole === 'gauge') data.cfxPoint = '0';
      // A zero-imputed category has no authored observation. Category identity remains stable
      // when a peer has another category or orders its authored observations differently.
      if (data.cfxDerived === 'missing-category-zero') {
        data.cfxRegion = item.key + ':category:' + Number(data.cfxCategory);
        delete data.cfxPoint;
        delete data.cfxSourcePoint;
        delete data.cfxValue;
        delete data.cfxY;
        data.cfxLabel = item.name + ' / Category ' + data.cfxCategory + ' / No observation';
        data.cfxStatus = 'No observation';
      }
      if (data.cfxPoint !== undefined && data.cfxSourcePoint === undefined && !hasDerivedPointIdentity(node)) {
        const point = Number(data.cfxPoint);
        data.cfxSourcePoint = String(item.indices[point] === undefined ? point : item.indices[point]);
      }
      if (data.cfxPoint !== undefined && data.cfxXLabel === undefined) {
        const label = data.cfxCategoryLabel !== undefined ? data.cfxCategoryLabel : xLabels.get(Number(data.cfxX));
        if (label !== undefined) data.cfxXLabel = label;
      }
      if (data.cfxRole === 'legend-item') return;
      const region = regions.get(data.cfxSourceId || '');
      // A scalar series already has a native value and semantic label; it is not a range endpoint observation.
      if (region && data.cfxPoint === undefined && data.cfxValue !== undefined && data.cfxPercent !== undefined
        && !node.hasAttribute('aria-label') && !node.hasAttribute('data-cfx-label'))
        node.setAttribute('aria-label', region.label || item.name);
      if (!region || (data.cfxPoint === undefined && data.cfxRegion === undefined)) return;
      node._cfxNativeRegion = Object.freeze({ x: region.x, y: region.y, width: region.width, height: region.height });
      node._cfxFillInteriors = Object.freeze((region.fillInteriors || []).map(candidate => Object.freeze(candidate.slice())));
      if (pairedExtentObservation(node)) node._cfxPairedRegion = region;
      if (data.cfxVisible === 'false') return;
      const box = node.getBBox();
      if (['line', 'stepline', 'area', 'steparea', 'stackedarea', 'trendline', 'slope'].includes(item.kind)) {
        const marker = document.createElementNS('http://www.w3.org/2000/svg', 'circle');
        marker.setAttribute('cx', region.x + region.width / 2); marker.setAttribute('cy', region.y + region.height / 2);
        marker.setAttribute('r', '4'); marker.setAttribute('class', 'cfx-prepared-point-marker');
        marker.setAttribute('pointer-events', 'none');
        const owner = node.closest('[data-cfx-role="series"]');
        const line = owner && owner.querySelector('[data-cfx-role="line"]');
        marker.setAttribute('fill', line ? getComputedStyle(line).stroke : 'currentColor');
        node.appendChild(marker);
      }
      // Marker-free lines still expose their observations to pointer, keyboard, lasso and crosshair tools.
      // Empty or zero-sized native marks get a minimum eight-unit transparent browser target.
      // Retained numeric facts have keyboard semantics without a pointer surface.
      if (retainedGeometryFact(node)) return;
      // A hollow candle's unpainted interior still belongs to its observation's browser target.
      const hollowCandle = data.cfxRole === 'point' && (data.cfxKind || '').toLowerCase() === 'candlestick'
        && node.querySelector('[data-cfx-role="candlestick-body"][fill="none"]');
      if (box.width > 0 && box.height > 0 && !hollowCandle) return;
      const hit = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
      const width = Math.max(8, region.width); const height = Math.max(8, region.height);
      hit.setAttribute('x', region.x + (region.width - width) / 2);
      hit.setAttribute('y', region.y + (region.height - height) / 2);
      hit.setAttribute('width', width); hit.setAttribute('height', height);
      hit.setAttribute('fill', 'transparent'); hit.setAttribute('pointer-events', 'all');
      hit.setAttribute('data-cfx-browser-hit-area', 'true');
      node.appendChild(hit);
    });
    const marks = new Map(Array.from(svg.querySelectorAll('[data-cfx-source-id]')).map(node => [node.dataset.cfxSourceId, node]));
    svg.querySelectorAll('[data-cfx-label-for]').forEach(label => {
      const mark = marks.get(label.dataset.cfxLabelFor);
      const point = mark && mark.closest('[data-cfx-point],[data-cfx-target-kind]');
      if (!point || point.closest('[data-cfx-role="legend-item"]') || !label.querySelector('text')) return;
      label._cfxLabelTarget = point;
      (point._cfxPointLabels ||= []).push(label);
    });
  };
  // Transient HTML paint is separate from native primitives and their current host CSS.
  // The probe attribute is deliberately outside the scoped availability observer's attribute filter.
  const withNativeHighlightPaint = (node, read) => {
    const root = node && node.closest('.cfx-interactive-chart');
    if (!root || !root._cfxBranchHighlights || root.hasAttribute('data-cfx-highlight-probe')) return read();
    root.setAttribute('data-cfx-highlight-probe', '');
    try { return read(); } finally { root.removeAttribute('data-cfx-highlight-probe'); }
  };
  // Prepared point targets own selection. Every Cartesian legend follows that state;
  // only connected geometry also carries it to its line/fill owner.
  const syncCartesianLinkedPaint = (root, nodes) => {
    const owners = new Map();
    for (const owner of root.querySelectorAll('[data-cfx-role="series"][data-cfx-coordinate-system="cartesian"]')) {
      const connected = ['line', 'stepline', 'area', 'steparea', 'stackedarea', 'trendline', 'slope'].includes((owner.dataset.cfxKind || '').toLowerCase());
      owners.set(owner, { active: false, dim: false, connected, isolated: owner.classList.contains('cfx-series-isolated-out') });
    }
    const observations = new Map();
    for (const node of nodes) {
      const owner = node.closest('[data-cfx-role="series"]');
      const state = owners.get(owner);
      if (!state) continue;
      const active = node.classList.contains('cfx-selected') || node.classList.contains('cfx-highlighted');
      const dim = node.classList.contains('cfx-selection-dim') || node.classList.contains('cfx-linked-dim');
      state.active ||= active;
      state.dim ||= dim;
      if (node.dataset.cfxPoint !== undefined) observations.set(owner.dataset.cfxSeries + ':' + node.dataset.cfxPoint,
        { active, dim, isolated: node.classList.contains('cfx-series-isolated-out') });
    }
    for (const [owner, state] of owners) {
      owner.classList.toggle('cfx-linked-series-active', state.connected && state.active);
      owner.classList.toggle('cfx-linked-series-dim', state.connected && state.dim && !state.active);
    }
    // Legends live outside the series group. Match their local source ordinal, so
    // their swatch and caption retain the same foreground even over a peer's mark.
    const series = new Map(Array.from(owners, ([owner, state]) => [owner.dataset.cfxSeries, state]));
    for (const legend of root.querySelectorAll('[data-cfx-role="legend-item"][data-cfx-series]')) {
      const state = legend.dataset.cfxPoint === undefined ? series.get(legend.dataset.cfxSeries)
        : observations.get(legend.dataset.cfxSeries + ':' + legend.dataset.cfxPoint);
      legend.classList.toggle('cfx-linked-series-active', state?.active === true);
      legend.classList.toggle('cfx-linked-series-dim', state?.dim === true && !state.active);
      if (state) legend.classList.toggle('cfx-series-isolated-out', state.isolated);
    }
  };
  const prepareRootBranchHighlights = (root) => {
    if (root.dataset.cfxHighlightMode !== 'root-branch') return;
    // Prepared targets normalize family names; native metadata also accepts the enum spelling.
    const nodes = Array.from(root.querySelectorAll('[data-cfx-kind="sunburst" i][data-cfx-target-kind="node"]'));
    if (!nodes.length) return;
    root._cfxBranchHighlights = nodes;
    nodes.forEach((node) => {
      node.dataset.cfxHighlightManaged = 'true';
      const paint = document.createElementNS('http://www.w3.org/2000/svg', 'g');
      paint.setAttribute('data-cfx-highlight-paint', '');
      // Keep accessible names on the identity owner. Captions and hatches stay with their native mark.
      Array.from(node.children).filter(child => !child.matches('title,desc')).forEach(child => paint.appendChild(child));
      node.appendChild(paint);
    });
  };
  const highlightStyle = (root, group) => {
    const prefix = 'data-cfx-highlight-' + group + '-';
    const result = {};
    ['stroke', 'stroke-width', 'opacity'].forEach(key => {
      const value = root.getAttribute(prefix + key);
      if (value !== null) result[key] = value;
    });
    return result;
  };
  const highlightOutline = (paint, style) => {
    const primary = paint.querySelector('[data-cfx-role="sunburst-segment-mark"]');
    let outline = paint.querySelector('[data-cfx-highlight-outline]');
    if (!primary || style.stroke === undefined && style['stroke-width'] === undefined) {
      if (outline) outline.remove();
      return;
    }
    // Copy only the primary geometry: no hatch, IDs, targets, tab stops or native paint attributes.
    if (!outline) {
      outline = document.createElementNS('http://www.w3.org/2000/svg', primary.localName);
      outline.setAttribute('data-cfx-role', 'interaction-highlight');
      outline.setAttribute('data-cfx-highlight-outline', '');
      outline.setAttribute('aria-hidden', 'true');
      outline.setAttribute('focusable', 'false');
      paint.insertBefore(outline, paint.firstChild);
    }
    ['d', 'transform', 'fill-rule'].forEach(key => {
      const value = primary.getAttribute(key);
      if (value === null) outline.removeAttribute(key); else outline.setAttribute(key, value);
    });
    const native = getComputedStyle(primary);
    const width = Number(style['stroke-width'] ?? 2.5);
    // Native stroke stays on top. The extra width exposes the requested outline outside it.
    paint.style.setProperty('--cfx-highlight-stroke', style.stroke ?? native.stroke);
    paint.style.setProperty('--cfx-highlight-stroke-width', width > 0 ? String(Math.min(Number.MAX_VALUE, (parseFloat(native.strokeWidth) || 0) + width * 2)) : '0');
  };
  const clearRootBranchHighlight = (root) => {
    (root._cfxBranchHighlights || []).forEach(node => {
      delete node.dataset.cfxHighlightState;
      const paint = node.querySelector('[data-cfx-highlight-paint]');
      if (!paint) return;
      paint.style.removeProperty('--cfx-highlight-opacity');
      paint.style.removeProperty('--cfx-highlight-stroke');
      paint.style.removeProperty('--cfx-highlight-stroke-width');
      const outline = paint.querySelector('[data-cfx-highlight-outline]');
      if (outline) outline.remove();
    });
  };
  const refreshRootBranchHighlightAvailability = (root) => {
    let hiddenActive = false;
    withNativeHighlightPaint(root, () => (root._cfxBranchHighlights || []).forEach(node => {
      const available = !!pointerTargetPaint(node);
      node.toggleAttribute('data-cfx-highlight-unavailable', !available);
      // Zero/collapsed facts deliberately have no pointer paint, but keep keyboard readouts.
      if (!available && node.dataset.cfxHighlightState === 'item' && node.dataset.cfxGeometryStatus === 'visible') hiddenActive = true;
    }));
    // Host paint is local to this chart; losing it must not clear an available peer's hover.
    if (hiddenActive) clearHover(root, true, false);
  };
  const applyRootBranchHighlight = (root, active) => {
    const nodes = root._cfxBranchHighlights;
    if (!nodes || !nodes.includes(active)) return false;
    if (!tooltipReadoutAvailable(active, null)) return false;
    const branch = active.dataset.cfxBranchRoot;
    const graphite = root.dataset.cfxLook === 'graphite';
    const branchStyle = highlightStyle(root, 'branch'), itemStyle = highlightStyle(root, 'item');
    const otherItemStyle = highlightStyle(root, 'other-item'), otherBranchStyle = highlightStyle(root, 'other-branch');
    withNativeHighlightPaint(root, () => nodes.forEach(node => {
      const ownBranch = node.dataset.cfxBranchRoot;
      const highlighted = node === active;
      const related = branch && ownBranch === branch;
      // The central root has no branch. Focusing it leaves the surrounding branches at native strength.
      let style = {}, state = 'neutral';
      if (highlighted) { style = { opacity: '1', ...(branch ? branchStyle : {}), ...itemStyle }; state = 'item'; }
      else if (related) { style = { opacity: graphite ? '1' : '.82', ...branchStyle, ...otherItemStyle }; state = 'branch'; }
      else if (branch && ownBranch) { style = { opacity: graphite ? '.3' : '.38', ...otherBranchStyle }; state = 'other-branch'; }
      node.dataset.cfxHighlightState = state;
      setNodeHovered(node, highlighted, !!related && !highlighted);
      const paint = node.querySelector('[data-cfx-highlight-paint]');
      paint.style.setProperty('--cfx-highlight-opacity', style.opacity ?? '1');
      highlightOutline(paint, style);
    }));
    root.dataset.cfxHovering = 'true';
    root.dataset.cfxHoverMode = 'root-branch';
    root.dataset.cfxHoverUnit = 'branch';
    root.dataset.cfxHoverLabel = text(active);
    refreshRootBranchHighlightAvailability(root);
    return true;
  };
  // One owner releases a chart's browser work. Source data and layout remain native prepared facts.
  const chartLifetimes = new WeakMap();
  const mountedCharts = new Set();
  let chartLifetimeObserver = null;
  const watchChartLifetimes = () => {
    if (!mountedCharts.size) {
      chartLifetimeObserver?.disconnect(); chartLifetimeObserver = null;
      return;
    }
    if (!chartLifetimeObserver) chartLifetimeObserver = new MutationObserver(() => {
      for (const root of Array.from(mountedCharts)) if (!root.isConnected) chartLifetimes.get(root)?.controller.dispose();
      if (Array.from(mountedCharts).some(root => {
        const before = chartLifetimes.get(root).trees, after = tooltipRootTrees(root);
        return before.length !== after.length || before.some((tree, index) => tree !== after[index]);
      })) watchChartLifetimes();
    });
    chartLifetimeObserver.disconnect();
    const trees = new Set();
    for (const root of mountedCharts) {
      const ownedTrees = tooltipRootTrees(root); chartLifetimes.get(root).trees = ownedTrees;
      for (const tree of ownedTrees) trees.add(tree);
    }
    for (const tree of trees) chartLifetimeObserver.observe(tree, { childList: true, subtree: true });
  };
  const ownChartWork = (root, release) => {
    const lifetime = chartLifetimes.get(root);
    if (!lifetime || lifetime.disposed) { release(); return () => {}; }
    let active = true;
    const cleanup = () => {
      if (!active) return;
      active = false; lifetime.cleanups.delete(cleanup); release();
    };
    lifetime.cleanups.add(cleanup);
    return cleanup;
  };
  const listenChart = (root, target, name, listener, options) => {
    target.addEventListener(name, listener, options);
    return ownChartWork(root, () => target.removeEventListener(name, listener, options));
  };
  const clearChartBrowserState = (root) => {
    cancelTooltipRequest(root);
    hideTip(root, root.querySelector('.cfx-tooltip'), true);
    stopScenarioPlayback(root, 'paused', false);
    clearHover(root, false, false);
    hideCrosshair(root, root.querySelector('.cfx-crosshair'));
    clearFocusTrail(root); clearReveals(root);
    applyHostHighlight(root, null);
    clearSelections(root); setSeriesIsolation(root, null, false); resetViewport(root);
    root.querySelectorAll('.cfx-series-muted').forEach(node => node.classList.remove('cfx-series-muted'));
    root.querySelectorAll('[data-cfx-muted]').forEach(node => node.removeAttribute('data-cfx-muted'));
    syncResetControl(root);
    root.querySelectorAll('.cfx-prepared-point-marker,[data-cfx-browser-hit-area],[data-cfx-highlight-outline]').forEach(node => node.remove());
    root.querySelectorAll('[data-cfx-highlight-paint]').forEach(paint => paint.replaceWith(...paint.childNodes));
    root.querySelectorAll('[data-cfx-highlight-managed]').forEach(node => {
      node.removeAttribute('data-cfx-highlight-managed'); node.removeAttribute('data-cfx-highlight-unavailable');
    });
    root.querySelectorAll('[data-cfx-keyboard-component]').forEach(node => {
      node.removeAttribute('data-cfx-keyboard-component'); node.removeAttribute('tabindex');
    });
    root.querySelectorAll('[data-cfx-label-for],[data-cfx-point]').forEach(node => {
      delete node._cfxLabelTarget; delete node._cfxPointLabels;
    });
    for (const key of Object.keys(root)) if (key.startsWith('_cfx')) delete root[key];
    root.removeAttribute('data-cfx-runtime-bound');
  };
  const releaseChartLifetime = (root) => {
    const lifetime = chartLifetimes.get(root);
    if (!lifetime || lifetime.disposed) return;
    cancelXViewportRequest(lifetime);
    lifetime.disposed = true;
    for (const cleanup of Array.from(lifetime.cleanups)) cleanup();
    clearChartBrowserState(root);
    mountedCharts.delete(root); chartLifetimes.delete(root); watchChartLifetimes();
    syncResetControl(root);
  };
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
    if (kind === 'dumbbell') rows.push({ name: rowName(node, 'start', 'Start'), value: numericFactText(data, 'Start') },
      { name: rowName(node, 'end', 'End'), value: numericFactText(data, 'End') }, { name: rowName(node, 'delta', 'Delta'), value: numericFactText(data, 'Delta') });
    else rows.push({ name: rowName(node, 'value', 'Value'), value: numericFactText(data, 'Value') },
      { name: rowName(node, 'lower', 'Lower'), value: numericFactText(data, 'Lower') }, { name: rowName(node, 'upper', 'Upper'), value: numericFactText(data, 'Upper') });
    return rows.filter((row) => row.value !== undefined && row.value !== null);
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
  // Absolute overlays and SVG pan translations use stage CSS pixels, including its scroll origin.
  // Browser hit geometry stays in screen pixels; account for axis-aligned host scaling and the stage border.
  const stageProjection = stage => {
    const rect = stage.getBoundingClientRect();
    return { rect, scaleX: rect.width / stage.offsetWidth || 1, scaleY: rect.height / stage.offsetHeight || 1 };
  };
  const stagePoint = (stage, point, bounded = false) => {
    const { rect, scaleX, scaleY } = stageProjection(stage);
    const x = (point.x - rect.left) / scaleX - stage.clientLeft + stage.scrollLeft;
    const y = (point.y - rect.top) / scaleY - stage.clientTop + stage.scrollTop;
    return bounded ? {
      x: clamp(x, stage.scrollLeft, stage.scrollLeft + stage.clientWidth),
      y: clamp(y, stage.scrollTop, stage.scrollTop + stage.clientHeight)
    } : { x, y };
  };
  const stagePointer = (stage, event, bounded = false) => stagePoint(stage, { x: event.clientX, y: event.clientY }, bounded);
  // The core owns layout. Project its captured plot through the live SVG transform, including pan/zoom.
  // Intersect with the visible scroller so the overlay cannot create new scrollable content.
  const visibleCartesianPlot = (root, stage, plot = root._cfxCartesianPlot) => {
    const svg = stage.querySelector('svg');
    if (!plot || !svg || stage.clientWidth <= 0 || stage.clientHeight <= 0) return null;
    const matrix = svg.getScreenCTM();
    // stagePoint supports positive axis-aligned host scaling, not rotated/skewed/perspective hosts.
    if (!matrix || matrix.is2D === false || matrix.b !== 0 || matrix.c !== 0 || matrix.a <= 0 || matrix.d <= 0) return null;
    const corners = [[plot.x, plot.y], [plot.x + plot.width, plot.y],
      [plot.x, plot.y + plot.height], [plot.x + plot.width, plot.y + plot.height]]
      .map(([x, y]) => stagePoint(stage, new DOMPoint(x, y).matrixTransform(matrix)));
    if (!corners.every(point => Number.isFinite(point.x) && Number.isFinite(point.y))) return null;
    const left = Math.max(stage.scrollLeft, Math.min(...corners.map(point => point.x)));
    const top = Math.max(stage.scrollTop, Math.min(...corners.map(point => point.y)));
    const right = Math.min(stage.scrollLeft + stage.clientWidth, Math.max(...corners.map(point => point.x)));
    const bottom = Math.min(stage.scrollTop + stage.clientHeight, Math.max(...corners.map(point => point.y)));
    const precision = svgCoordinatePrecision(matrix), { scaleX, scaleY } = stageProjection(stage);
    return right > left && bottom > top ? { left, top, width: right - left, height: bottom - top,
      precisionX: precision.x / scaleX, precisionY: precision.y / scaleY } : null;
  };
  // A guide belongs to the current viewport. Discard stale dimensions before they can enlarge a resized scroller.
  // Share the existing resize lifecycle with keyboard availability, including initially hidden fragment hosts.
  const bindStageLayout = (root, stage, crosshair) => {
    if (!stage || !hasFeature(root, 'Crosshair') && !hasFeature(root, 'KeyboardNavigation')) return;
    let width = stage.clientWidth, height = stage.clientHeight;
    let frame = 0;
    let observer;
    const hideGuide = () => hideCrosshair(root, crosshair);
    // A pointer can acquire a guide during a CSS pan/zoom transition; discard that intermediate projection.
    const finishViewport = event => {
      if (event.target === stage.querySelector('svg') && event.propertyName === 'transform') hideGuide();
    };
    const queueRefresh = () => {
      if (chartLifetimes.get(root)?.disposed !== false) return;
      if (frame) cancelAnimationFrame(frame);
      frame = requestAnimationFrame(() => {
        frame = 0;
        if (!root.isConnected) {
          if (observer) observer.disconnect();
          window.removeEventListener('resize', queueRefresh);
          stage.removeEventListener('scroll', hideGuide);
          stage.removeEventListener('transitionend', finishViewport);
          stage.removeEventListener('transitioncancel', finishViewport);
          return;
        }
        if (width !== stage.clientWidth || height !== stage.clientHeight) hideGuide();
        width = stage.clientWidth;
        height = stage.clientHeight;
        refreshKeyboardNavigation(root);
      });
    };
    listenChart(root, stage, 'scroll', hideGuide, { passive: true });
    listenChart(root, stage, 'transitionend', finishViewport);
    listenChart(root, stage, 'transitioncancel', finishViewport);
    listenChart(root, window, 'resize', queueRefresh);
    if (typeof ResizeObserver !== 'undefined') {
      observer = new ResizeObserver(queueRefresh);
      observer.observe(stage);
    }
    ownChartWork(root, () => { if (frame) cancelAnimationFrame(frame); observer?.disconnect(); });
  };
  const clamp = (value, min, max) => Math.max(min, Math.min(max, value));
  const getState = (root) => ({
    zoom: Number(root.dataset.cfxZoom || '1'),
    panX: Number(root.dataset.cfxPanX || '0'),
    panY: Number(root.dataset.cfxPanY || '0')
  });
  const applyViewport = (root, state) => {
    state.zoom = clamp(state.zoom, 0.5, 6);
    state.panX = clamp(state.panX, -4000, 4000);
    state.panY = clamp(state.panY, -4000, 4000);
    root.dataset.cfxZoom = state.zoom.toFixed(3);
    root.dataset.cfxPanX = state.panX.toFixed(1);
    root.dataset.cfxPanY = state.panY.toFixed(1);
    syncResetControl(root);
    const stage = root.querySelector('.cfx-stage');
    if (!stage) return;
    hideCrosshair(root, root.querySelector('.cfx-crosshair'));
    stage.style.setProperty('--cfx-zoom', state.zoom);
    stage.style.setProperty('--cfx-pan-x', state.panX + 'px');
    stage.style.setProperty('--cfx-pan-y', state.panY + 'px');
    refreshTooltipPosition(root);
  };
  // The reset control is contextual: it exists only while the reader has changed the view.
  const viewChanged = (root) => {
    if (connectedXViewport(root) && nativeXWindowChanged(root)) return true;
    const state = getState(root);
    if (Math.abs(state.zoom - 1) > 0.0005 || Math.abs(state.panX) > 0.05 || Math.abs(state.panY) > 0.05) return true;
    if (root.dataset.cfxBrush || root.dataset.cfxIsolatedSeries) return true;
    return root.querySelector('.cfx-selected,.cfx-series-muted,[data-cfx-muted="true"]') !== null;
  };
  const syncResetControl = (root) => {
    const reset = root.querySelector('[data-cfx-reset]');
    if (!reset) return;
    const changed = viewChanged(root);
    if (!changed && document.activeElement === reset) {
      // Keep keyboard focus inside the chart instead of dropping it to the document when the control hides.
      if (!root.hasAttribute('tabindex')) root.setAttribute('tabindex', '-1');
      try { root.focus({ preventScroll: true }); } catch { root.focus(); }
    }
    reset.hidden = !changed;
  };
  const sameGroup = (root, peer) => root !== peer && root.dataset.cfxInteractionGroup && root.dataset.cfxInteractionGroup === peer.dataset.cfxInteractionGroup;
  const emitHostEvent = (root, name, detail) => {
    root.dispatchEvent(new CustomEvent(name, { bubbles: true, composed: true, detail: Object.assign({ chartId: root.dataset.cfxChartId || '' }, detail || {}) }));
  };
  const emitSync = (root, payload) => {
    if (!hasFeature(root, 'SynchronizedCharts') || !root.dataset.cfxInteractionGroup) return;
    // Automatically allocated identities are local handles, never accidental matches in a peer chart.
    const sharedTarget = target => target && target.seriesScope !== 'local';
    if (payload.target?.seriesScope === 'local') {
      if (['hover', 'crosshair', 'navigate'].includes(payload.action) && root._cfxSharedHover) emitSync(root, { action: 'hover-clear' });
      return;
    }
    payload = { ...payload };
    if (Array.isArray(payload.previousTargets)) payload.previousTargets = payload.previousTargets.filter(sharedTarget);
    for (const field of ['targets', 'trail']) if (Array.isArray(payload[field])) {
      const shared = payload[field].filter(sharedTarget);
      const replacedSelection = field === 'targets' && (payload.action === 'compare' || payload.action === 'lasso' && Array.isArray(payload.previousTargets));
      if (payload[field].length && !shared.length && !replacedSelection) return;
      payload[field] = shared;
    }
    if (['compare', 'lasso'].includes(payload.action) && Array.isArray(payload.targets)) {
      // A local-only gesture stays local. Replacing a shared selection with a local one
      // still retires the former shared identities, without exporting the local target.
      if (Array.isArray(payload.previousTargets) && !payload.previousTargets.length && !payload.targets.length) return;
      payload.count = payload.targets.length;
    }
    if (payload.action === 'state' && payload.state) {
      const targets = payload.state.selectedTargets || [], shared = targets.filter(sharedTarget);
      if (targets.length && !shared.length) return;
      payload.state = { ...payload.state, selectedTargets: shared };
    }
    if (payload.action === 'hover-clear') {
      if (!root._cfxSharedHover) return;
      delete root._cfxSharedHover;
    } else if (['hover', 'crosshair', 'navigate'].includes(payload.action)) root._cfxSharedHover = true;
    const detail = Object.assign({ chartId: root.dataset.cfxChartId || '', group: root.dataset.cfxInteractionGroup }, payload);
    root.dispatchEvent(new CustomEvent('cfxsync', { bubbles: true, composed: true, detail }));
    document.querySelectorAll('.cfx-interactive-chart').forEach((peer) => {
      if (chartLifetimes.get(peer)?.disposed === false && sameGroup(root, peer) && hasFeature(peer, 'SynchronizedCharts')) applySync(peer, detail);
    });
  };
  const applyMode = (root, mode) => {
    root.dataset.cfxMode = mode || '';
    root.querySelectorAll('[data-cfx-mode-button]').forEach((button) => {
      button.setAttribute('aria-pressed', button.dataset.cfxModeButton === root.dataset.cfxMode ? 'true' : 'false');
    });
  };
  const setMode = (root, mode) => applyMode(root, root.dataset.cfxMode === mode ? '' : mode);
  // Clearing a host range also cancels its provisional gesture so pointerup cannot recommit it.
  const clearBrush = root => {
    const brush = root.querySelector('.cfx-brush-box');
    const changed = Boolean(root.dataset.cfxBrush || root.dataset.cfxMode === 'brush' || brush && !brush.hidden);
    root._cfxCancelBrushDrag?.();
    root.removeAttribute('data-cfx-brush');
    if (brush) {
      brush.hidden = true;
      for (const name of ['left', 'top', 'width', 'height']) brush.style.removeProperty(name);
    }
    if (root.dataset.cfxMode === 'brush') applyMode(root, '');
    syncResetControl(root);
    return changed;
  };
  const resetViewport = (root) => {
    suspendPointerReadout(root);
    applyViewport(root, { zoom: 1, panX: 0, panY: 0 });
    clearBrush(root);
    applyMode(root, '');
    syncResetControl(root);
  };
  const storeInteractionState = (root, snapshot) => {
    try {
      root.dataset.cfxInteractionSnapshot = JSON.stringify(snapshot);
    } catch {
      root.removeAttribute('data-cfx-interaction-snapshot');
    }
  };
  const captureInteractionState = (root, source) => {
    const snapshot = {
      version: 1,
      chartId: root.dataset.cfxChartId || '',
      source: source || '',
      viewport: getState(root),
      mode: root.dataset.cfxMode || '',
      brush: root.dataset.cfxBrush || '',
      selectedTargets: selectedTargets(root),
      compareCount: Number(root.dataset.cfxCompareCount || '0'),
      scenario: {
        id: root.dataset.cfxActiveScenario || '',
        stepIndex: root.dataset.cfxActiveScenarioStep || '',
        progress: root.dataset.cfxScenarioProgress || '',
        playback: root.dataset.cfxScenarioPlayback || ''
      }
    };
    storeInteractionState(root, snapshot);
    return snapshot;
  };
  const publishInteractionState = (root, source, sync) => {
    if (!hasFeature(root, 'StateBookmarks')) return null;
    const snapshot = captureInteractionState(root, source || 'host');
    emitHostEvent(root, 'cfxstate', { source: source || 'host', snapshot });
    if (sync !== false) emitSync(root, { action: 'state', state: snapshot });
    return snapshot;
  };
  const applyInteractionState = (root, snapshot, emit, sync) => {
    if (!snapshot || !hasFeature(root, 'StateBookmarks')) return;
    const previousSelection = selectedTargets(root).map(targetKey).join('\n');
    if (snapshot.viewport) applyViewport(root, Object.assign({}, snapshot.viewport));
    if (snapshot.mode !== undefined) applyMode(root, snapshot.mode || '');
    if (snapshot.brush !== undefined) root.dataset.cfxBrush = snapshot.brush || '';
    const scenario = snapshot.scenario || {};
    if (scenario.id !== undefined) {
      setScenario(root, scenario.id || '', false, false);
      if (scenario.id && scenario.stepIndex !== undefined && scenario.stepIndex !== '') setScenarioStep(root, scenario.stepIndex, false, false);
      const route = scenarioRoute(root, root.dataset.cfxActiveScenario || '');
      if (scenario.playback === 'playing' && route && route.steps.length) {
        const current = Number(root.dataset.cfxActiveScenarioStep || '-1');
        const next = Number.isFinite(current) ? current + 1 : 0;
        startScenarioPlayback(root, route, next, false, false);
      } else if (scenario.playback) {
        setScenarioPlaybackState(root, scenario.playback, route, false);
      }
    }
    applySelectionSetByTargets(root, snapshot.selectedTargets || [], true);
    renderCompare(root);
    syncResetControl(root);
    storeInteractionState(root, snapshot);
    syncSelectionAppearance(root);
    if (emit !== false) {
      if (previousSelection !== selectedTargets(root).map(targetKey).join('\n')) publishSelectionChange(root, 'command');
      emitHostEvent(root, 'cfxstateapplied', { snapshot });
    }
    if (sync !== false) emitSync(root, { action: 'state', state: snapshot });
  };
  const zoomBy = (root, factor) => {
    if (!hasFeature(root, 'Zoom')) return;
    if (chartLifetimes.get(root)?.xViewportConnection) {
      requestXViewportZoom(root, factor);
      return;
    }
    const state = getState(root);
    state.zoom *= factor;
    applyViewport(root, state);
    emitHostEvent(root, 'cfxviewport', { state: getState(root) });
    emitSync(root, { action: 'viewport', state: getState(root) });
  };
  const serializeSvg = (root) => {
    const svg = root.querySelector('.cfx-stage svg');
    return svg ? { svg, data: new XMLSerializer().serializeToString(svg) } : null;
  };
  const downloadBlob = (root, blob, extension, format) => {
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = (root.dataset.cfxChartId || 'chart') + '.' + extension;
    document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(url);
    emitHostEvent(root, 'cfxexport', { format });
  };
  const svgSize = (svg) => {
    const viewBox = svg.viewBox && svg.viewBox.baseVal ? svg.viewBox.baseVal : null;
    const rect = svg.getBoundingClientRect ? svg.getBoundingClientRect() : { width: 0, height: 0 };
    const width = Math.max(1, Math.round((viewBox && viewBox.width) || (svg.width && svg.width.baseVal ? svg.width.baseVal.value : 0) || rect.width || 800));
    const height = Math.max(1, Math.round((viewBox && viewBox.height) || (svg.height && svg.height.baseVal ? svg.height.baseVal.value : 0) || rect.height || 450));
    return { width, height };
  };
  const exportSvg = (root) => {
    if (!hasFeature(root, 'Export')) return;
    const serialized = serializeSvg(root);
    if (!serialized) return;
    downloadBlob(root, new Blob([serialized.data], { type: 'image/svg+xml;charset=utf-8' }), 'svg', 'svg');
  };
  const exportPng = (root) => {
    if (!hasFeature(root, 'Export')) return;
    const serialized = serializeSvg(root);
    if (!serialized) return;
    const source = new Blob([serialized.data], { type: 'image/svg+xml;charset=utf-8' });
    const sourceUrl = URL.createObjectURL(source);
    const image = new Image();
    let canceled = false;
    const release = ownChartWork(root, () => {
      canceled = true; image.onload = null; image.onerror = null; image.src = ''; URL.revokeObjectURL(sourceUrl);
    });
    image.onload = () => {
      if (canceled) return;
      const size = svgSize(serialized.svg);
      const scale = Math.max(1, Math.ceil(window.devicePixelRatio || 1));
      const canvas = document.createElement('canvas');
      canvas.width = size.width * scale;
      canvas.height = size.height * scale;
      const context = canvas.getContext('2d');
      if (!context) {
        release();
        return;
      }
      context.setTransform(scale, 0, 0, scale, 0, 0);
      context.clearRect(0, 0, size.width, size.height);
      context.drawImage(image, 0, 0, size.width, size.height);
      canvas.toBlob((blob) => {
        if (canceled) return;
        release();
        if (blob && root.isConnected) downloadBlob(root, blob, 'png', 'png');
      }, 'image/png');
    };
    image.onerror = release;
    image.src = sourceUrl;
  };
  // Adapter emphasis changes paint, never membership in the roving component. Host/native
  // class changes still invalidate availability, including a class changed in the same batch.
  const keyboardPaintClasses = new Set(['cfx-hovered', 'cfx-hover-related', 'cfx-hover-column', 'cfx-hover-series',
    'cfx-selected', 'cfx-highlighted', 'cfx-linked-dim', 'cfx-selection-dim', 'cfx-linked-series-active',
    'cfx-linked-series-dim', 'cfx-trail']);
  const keyboardNativeClasses = value => (value || '').split(/\s+/).filter(value => value && !keyboardPaintClasses.has(value)).sort().join(' ');
  const keyboardNativeStyle = value => (value || '').replace(/--cfx-highlight-(?:opacity|stroke|stroke-width)\s*:[^;]*(?:;|$)/g, '').trim();
  const keyboardAvailabilityMutation = (record, stage, ancestry) => {
    if (record.type === 'attributes' && record.attributeName === 'class'
      && keyboardNativeClasses(record.oldValue) === keyboardNativeClasses(record.target.getAttribute('class'))) return false;
    if (record.type === 'attributes' && record.target.closest('[data-cfx-highlight-outline]')) return false;
    if (record.type === 'attributes' && record.attributeName === 'style' && record.target.matches('[data-cfx-highlight-paint]')
      && keyboardNativeStyle(record.oldValue) === keyboardNativeStyle(record.target.getAttribute('style'))) return false;
    // Tooltip, brush, guide and compare UI repaint while the pointer moves. They are not SVG
    // navigation targets; ResizeObserver covers any resulting change to the stage's size.
    const svg = stage.querySelector('svg');
    if (record.type === 'attributes') return record.target === stage || !!svg?.contains(record.target) || ancestry.nodes.includes(record.target);
    const changes = [...record.addedNodes, ...record.removedNodes];
    const adapterPaint = node => node.nodeType === 1 && node.matches('[data-cfx-highlight-outline],.cfx-prepared-point-marker,[data-cfx-browser-hit-area]');
    if (changes.length && changes.every(adapterPaint)) return false;
    return !!svg?.contains(record.target) || changes.some(node => node.contains(stage) || svg && node.contains(svg)
      || ancestry.nodes.some(boundary => node.contains(boundary)));
  };
  // Data and legend are separate roving components. Source identities stay on the actual rendered marks.
  const keyboardTargetAvailable = (node) => {
    if (node.dataset.cfxVisible === 'false') return false;
    if (node.closest('[aria-hidden="true"]') || !paintAncestorsVisible(node, new Map())) return false;
    // Muted data leaves navigation; its legend remains an entry point for restoring the series.
    if (renderedTargetKind(node) !== 'legend' && node.closest('.cfx-series-muted')) return false;
    const style = getComputedStyle(node);
    if (style.display === 'none' || style.visibility === 'hidden' || style.visibility === 'collapse') return false;
    if (pointerTargetPaint(node)) return true;
    // Retained authored facts can have no filled geometry, while still belonging to the data component.
    const data = node.dataset;
    if (!data.cfxTargetKind || !data.cfxTargetId || !retainedGeometryFact(node)) return false;
    const svg = node.ownerSVGElement;
    if (!svg) return false;
    // The viewport detects hidden hosts; CSS-hidden inner groups need their own ancestor check.
    for (let parent = node.parentElement; parent && parent !== svg; parent = parent.parentElement) {
      if (getComputedStyle(parent).display === 'none') return false;
    }
    const viewport = svg.getBoundingClientRect();
    return viewport.width > 0 && viewport.height > 0;
  };
  const keyboardTargets = (root) => withNativeHighlightPaint(root, () => {
    const candidates = interactiveTargets(root).filter(keyboardTargetAvailable);
    const candidateSet = new Set(candidates);
    const containers = new Set();
    candidates.forEach((node) => {
      for (let parent = node.parentElement; parent && parent !== root; parent = parent.parentElement) {
        if (candidateSet.has(parent)) containers.add(parent);
      }
    });
    const seen = new Set();
    return candidates.filter((node) => {
      // A containing series is useful to pointer hover, but it is not an extra keyboard datum.
      if (containers.has(node)) return false;
      const focusNode = targetFocusNode(node);
      if (seen.has(focusNode)) return false;
      seen.add(focusNode);
      return true;
    });
  });
  const keyboardDataGroups = (nodes) => {
    const groups = new Map();
    nodes.forEach((node) => {
      // Cartesian observations and matrix rows share the declared series contract. Other families use target order.
      const owner = node.closest('[data-cfx-role="series"][data-cfx-series]');
      const series = owner ? owner.dataset.cfxSeries : null;
      const key = series === null ? 'targets' : 'series:' + series;
      if (!groups.has(key)) groups.set(key, { series, nodes: [] });
      groups.get(key).nodes.push(node);
    });
    const ordered = Array.from(groups.values()).sort((a, b) => a.series === null ? 1 : b.series === null ? -1 : Number(a.series) - Number(b.series));
    ordered.forEach((group) => {
      if (group.series === null) return;
      group.nodes.sort((a, b) => Number(a.dataset.cfxPoint ?? 0) - Number(b.dataset.cfxPoint ?? 0));
    });
    return ordered;
  };
  const placeKeyboardLegendAfterData = (root, data, legends) => {
    const svg = root.querySelector('.cfx-stage svg');
    if (!svg || !data.length) return;
    const branches = new Set();
    legends.forEach((node) => {
      let branch = node;
      while (branch.parentElement && branch.parentElement !== svg) branch = branch.parentElement;
      // Move the intact legend clip/group, retaining transforms and labels. Never move a branch containing data.
      if (branch.parentElement === svg && !branches.has(branch) && !data.some((mark) => branch.contains(mark))) branches.add(branch);
    });
    branches.forEach((branch) => svg.appendChild(branch));
  };
  const refreshKeyboardNavigation = (root, focused, passive = false) => {
    refreshRootBranchHighlightAvailability(root);
    const state = root._cfxKeyboardNavigation;
    if (!state || !hasFeature(root, 'KeyboardNavigation')) return null;
    const activeElement = root.getRootNode().activeElement;
    const activeOwned = root.contains(activeElement) && state.owned.has(activeElement);
    const targets = keyboardTargets(root);
    state.legends = targets.filter((node) => renderedTargetKind(node) === 'legend');
    state.groups = keyboardDataGroups(targets.filter((node) => renderedTargetKind(node) !== 'legend'));
    state.data = state.groups.flatMap((group) => group.nodes);
    // A hidden host has no available targets. Retain each component's position until layout returns.
    if (state.data.length && !state.data.includes(state.activeData)) state.activeData = state.data[0];
    if (state.legends.length && !state.legends.includes(state.activeLegend)) state.activeLegend = state.legends[0];
    if (state.data.includes(focused)) state.activeData = focused;
    if (state.legends.includes(focused)) state.activeLegend = focused;
    if (!state.legendOrderPrepared && state.data.length && state.legends.length) {
      placeKeyboardLegendAfterData(root, state.data, state.legends);
      state.legendOrderPrepared = true;
    }
    state.owned.forEach((node) => node.setAttribute('tabindex', '-1'));
    targets.forEach((node) => {
      const focusNode = targetFocusNode(node);
      state.owned.add(focusNode);
      focusNode.setAttribute('data-cfx-keyboard-component', renderedTargetKind(node) === 'legend' ? 'legend' : 'data');
      focusNode.setAttribute('tabindex', node === state.activeData || node === state.activeLegend ? '0' : '-1');
    });
    if (activeOwned && !targets.some((node) => targetFocusNode(node) === activeElement)) {
      // Local and synchronized state changes must not strand focus on a datum that just left the component.
      const replacement = state.data.length ? state.activeData : state.legends.length ? state.activeLegend : null;
      if (replacement) {
        // Host paint changes recover a usable focus position without opening a new
        // readout in the same update that dismisses the unavailable source or pin.
        if (passive) { clearHover(root, true, true); hideTip(root, root.querySelector('.cfx-tooltip'), true); }
        focusKeyboardTarget(root, replacement, passive);
      }
    }
    return state;
  };
  const bindKeyboardNavigationAvailability = (root) => {
    const stage = root.querySelector('.cfx-stage');
    if (!stage) return;
    let frame = 0;
    let resizeObserver;
    let paintObserver;
    let ancestry = { nodes: [], trees: [] };
    const unwatchSlots = () => ancestry.trees.forEach(tree => tree.removeEventListener('slotchange', queueRefresh, true));
    const watchAncestry = () => {
      const next = composedAncestry(root);
      if (next.nodes.length === ancestry.nodes.length && next.nodes.every((node, index) => node === ancestry.nodes[index])) return;
      paintObserver.disconnect(); unwatchSlots(); ancestry = next;
      paintObserver.observe(root, { childList: true, subtree: true, attributes: true, attributeOldValue: true, attributeFilter: paintAvailabilityAttributes });
      for (const node of new Set([...ancestry.nodes.slice(1), ...ancestry.trees])) {
        paintObserver.observe(node, { childList: true,
          ...(node instanceof Element ? { attributes: true, attributeOldValue: true, attributeFilter: paintAvailabilityAttributes } : {}) });
      }
      ancestry.trees.forEach(tree => tree.addEventListener('slotchange', queueRefresh, true));
    };
    const queueRefresh = () => {
      if (chartLifetimes.get(root)?.disposed !== false) return;
      if (frame) return;
      frame = requestAnimationFrame(() => {
        frame = 0;
        if (!root.isConnected) {
          if (resizeObserver) resizeObserver.disconnect();
          if (paintObserver) paintObserver.disconnect();
          unwatchSlots();
          window.removeEventListener('resize', queueRefresh);
          return;
        }
        watchAncestry(); refreshKeyboardNavigation(root, undefined, true);
      });
    };
    listenChart(root, window, 'resize', queueRefresh);
    if (typeof ResizeObserver !== 'undefined') {
      // Tabs and other initially hidden hosts acquire layout without a window resize.
      resizeObserver = new ResizeObserver(queueRefresh);
      resizeObserver.observe(stage);
    }
    // Native paint and host styles can change without a resize. Tab and arrow availability share this owner.
    paintObserver = new MutationObserver(records => {
      if (chartLifetimes.get(root)?.disposed !== false) return;
      if (!root.isConnected) { queueRefresh(); return; }
      if (!records.some(record => keyboardAvailabilityMutation(record, stage, ancestry))) return;
      queueRefresh();
    });
    watchAncestry();
    ownChartWork(root, () => {
      if (frame) cancelAnimationFrame(frame);
      resizeObserver?.disconnect(); paintObserver?.disconnect(); unwatchSlots();
    });
  };
  const prepareKeyboardNavigation = (root) => {
    if (!hasFeature(root, 'KeyboardNavigation')) {
      if (root._cfxBranchHighlights) bindKeyboardNavigationAvailability(root);
      return;
    }
    root._cfxKeyboardNavigation = { owned: new Set() };
    // SVG focus listeners can make aggregate groups implicitly tabbable. Only roving leaves enter the tab order.
    interactiveTargets(root).forEach((node) => root._cfxKeyboardNavigation.owned.add(targetFocusNode(node)));
    refreshKeyboardNavigation(root);
    bindKeyboardNavigationAvailability(root);
  };
  const scrollKeyboardTargetIntoView = (root, node) => {
    const stage = root.querySelector('.cfx-stage');
    if (!stage || !stage.contains(node)) return;
    const scrollAxis = (overflow, extent, available, offset, start, end, itemStart, itemEnd) => {
      if (!['auto', 'scroll', 'hidden'].includes(overflow) || extent <= available) return offset;
      const center = (itemStart + itemEnd) / 2;
      if (itemEnd - itemStart > end - start) {
        // Keep the visible portion of a wide mark: pointer focus must not move it between down and up.
        if (itemEnd > start && itemStart < end) return offset;
        return offset + center - (start + end) / 2;
      }
      if (itemStart < start) return offset + itemStart - start;
      if (itemEnd > end) return offset + itemEnd - end;
      return offset;
    };
    let preserveVisiblePortion = false;
    // Scroll inner clips first, then remeasure through each outer clip, including closed shadow hosts.
    for (const { node: clip, x, y } of tooltipClippingAncestors(node)) {
      const style = getComputedStyle(clip), box = node.getBoundingClientRect();
      const start = stagePoint(clip, { x: box.left, y: box.top });
      const end = stagePoint(clip, { x: box.right, y: box.bottom });
      const left = clip.scrollLeft + 8, top = clip.scrollTop + 8;
      preserveVisiblePortion ||= x && end.x - start.x > clip.clientWidth - 16 && end.x > left && start.x < left + clip.clientWidth - 16
        || y && end.y - start.y > clip.clientHeight - 16 && end.y > top && start.y < top + clip.clientHeight - 16;
      clip.scrollTo({ behavior: 'instant',
        left: scrollAxis(style.overflowX, clip.scrollWidth, clip.clientWidth, clip.scrollLeft, left, left + clip.clientWidth - 16, start.x, end.x),
        top: scrollAxis(style.overflowY, clip.scrollHeight, clip.clientHeight, clip.scrollTop, top, top + clip.clientHeight - 16, start.y, end.y) });
    }
    const box = node.getBoundingClientRect(), viewport = stage.getBoundingClientRect();
    // Native scrolling can reveal a small focus mark through an opaque slot's internal scroller.
    // Preserve the precise manual policy for wide marks that already have a visible portion.
    if (!preserveVisiblePortion && box.width <= viewport.width - 16 && box.height <= viewport.height - 16)
      node.scrollIntoView({ behavior: 'instant', block: 'nearest', inline: 'nearest' });
  };
  const focusKeyboardTarget = (root, node, passive = false) => {
    const focusNode = targetFocusNode(node);
    // Native links also recover focus when adapter keyboard navigation is disabled.
    const recovering = root._cfxRecoveringFocus === true;
    if (passive) root._cfxRecoveringFocus = true;
    try {
      scrollKeyboardTargetIntoView(root, focusNode);
      if (focusNode.focus) {
        try { focusNode.focus({ preventScroll: true }); } catch { focusNode.focus(); }
      }
    } finally { if (passive && !recovering) delete root._cfxRecoveringFocus; }
  };
  const focusAdjacentTarget = (root, node, key) => {
    if (!['ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown', 'Home', 'End'].includes(key)) return false;
    resumePointerReadout(root);
    const state = refreshKeyboardNavigation(root, node);
    if (!state) return false;
    const legend = state.legends.includes(node);
    const group = legend ? null : state.groups.find((item) => item.nodes.includes(node));
    const targets = legend ? state.legends : group ? group.nodes : [];
    if (!targets.length) return false;
    const current = targets.indexOf(node);
    let next = node;
    if (key === 'Home') next = targets[0];
    else if (key === 'End') next = targets[targets.length - 1];
    else if (!legend && (group.series !== null || state.groups.length > 1) && (key === 'ArrowUp' || key === 'ArrowDown')) {
      // Ungrouped annotations are part of the same data component as the Cartesian series.
      const groups = state.groups;
      const nextGroup = groups[clamp(groups.indexOf(group) + (key === 'ArrowUp' ? -1 : 1), 0, groups.length - 1)];
      if (nextGroup !== group) {
        const x = node.dataset.cfxX;
        const sourcePoint = sourcePointIndex(node);
        next = nextGroup.nodes.find((item) => x !== undefined && item.dataset.cfxX === x)
          || nextGroup.nodes.find((item) => sourcePoint !== undefined && sourcePointIndex(item) === sourcePoint)
          || nextGroup.nodes[Math.min(current, nextGroup.nodes.length - 1)];
      }
    } else {
      const direction = key === 'ArrowLeft' || key === 'ArrowUp' ? -1 : 1;
      next = targets[clamp(current + direction, 0, targets.length - 1)];
    }
    if (next === node) {
      // An explicit boundary arrow still resumes the readout after passive recovery.
      const tip = root.querySelector('.cfx-tooltip');
      if (tip?.hidden && tooltipReadoutAvailable(node)) { setHover(root, node, true, true); showTip(root, tip, node); }
      return true;
    }
    refreshKeyboardNavigation(root, next);
    focusKeyboardTarget(root, next);
    const target = targetIdentity(next);
    const component = legend ? state.legends : state.data;
    emitHostEvent(root, 'cfxnavigate', { label: text(next), target, index: component.indexOf(next), count: component.length, key });
    emitSync(root, { action: 'navigate', label: text(next), target, index: component.indexOf(next), count: component.length, key });
    return true;
  };
  const seriesTarget = (node) => {
    const data = node.dataset || {};
    const reference = legendTarget(node) || (['node', 'link', 'region'].includes(data.cfxTargetKind)
      ? { targetKind: data.cfxTargetKind, targetId: data.cfxTargetId } : null);
    return { series: data.cfxSeries, point: data.cfxPoint, seriesKey: seriesKey(node), seriesScope: data.cfxSeriesScope,
      observationId: data.cfxObservationId, label: data.cfxLabel || seriesLabel(node),
      targetKind: data.cfxPoint === undefined ? 'series' : 'point',
      targetId: data.cfxPoint === undefined ? seriesKey(node) : pointTargetId(node), ...reference };
  };
  const seriesTargetToken = (target) => !target ? '' : target.targetKind
    ? [target.series ?? '', target.targetKind, target.targetId].join(':') : [target.series ?? '', target.point ?? ''].join(':');
  const matchesLocalSeriesTarget = (node, target) => {
    if (!target) return false;
    const data = node.dataset || {};
    if (data.cfxSeries !== String(target.series)) return false;
    if (target.targetKind && target.targetKind !== 'series' && target.targetId) {
      if (target.targetKind === 'point') return data.cfxPoint !== undefined && pointTargetId(node) === target.targetId;
      const reference = legendTarget(node);
      return reference ? reference.targetKind === target.targetKind && reference.targetId === target.targetId
        : data.cfxTargetKind === target.targetKind && data.cfxTargetId === target.targetId;
    }
    return target.point === undefined || data.cfxPoint === String(target.point);
  };
  const resolveSeriesTarget = (root, target) => {
    if (!target || !target.seriesKey) return null;
    const legends = Array.from(root.querySelectorAll('[data-cfx-role="legend-item"][data-cfx-series]'));
    let matchingSeries = legends.filter((item) => seriesKey(item) === target.seriesKey);
    if (!matchingSeries.length) matchingSeries = Array.from(root.querySelectorAll('[data-cfx-series]')).filter((item) => seriesKey(item) === target.seriesKey);
    if (!matchingSeries.length) return null;
    if (['node', 'link', 'region'].includes(target.targetKind) && target.targetId) {
      const exact = matchingSeries.find((node) => {
        const reference = legendTarget(node);
        return reference && reference.targetKind === target.targetKind && reference.targetId === target.targetId;
      }) || referencedTargetNode(root, target);
      return exact ? seriesTarget(exact) : null;
    }
    if (target.point === undefined) {
      const localSeries = (matchingSeries[0].dataset || {}).cfxSeries;
      return localSeries === undefined ? null : { series: localSeries, seriesKey: target.seriesKey, label: target.label };
    }
    if (target.targetId) {
      const exact = matchingSeries.find((item) => (item.dataset || {}).cfxPoint !== undefined && pointTargetId(item) === target.targetId)
        || Array.from(root.querySelectorAll('[data-cfx-series][data-cfx-point]'))
          .find((item) => seriesKey(item) === target.seriesKey && pointTargetId(item) === target.targetId);
      return exact ? seriesTarget(exact) : null;
    }
    const exactLabel = matchingSeries.find((item) => (item.dataset || {}).cfxLabel === target.label);
    const exactPoint = matchingSeries.find((item) => (item.dataset || {}).cfxPoint === String(target.point));
    return seriesTarget(exactLabel || exactPoint || matchingSeries.find((item) => (item.dataset || {}).cfxPoint === undefined) || matchingSeries[0]);
  };
  const setSeriesMuted = (root, target, muted) => {
    root.querySelectorAll('[data-cfx-series]').forEach((node) => {
      if (!matchesLocalSeriesTarget(node, target)) return;
      const role = node.dataset ? node.dataset.cfxRole || '' : '';
      if (role.indexOf('legend') === 0) {
        node.dataset.cfxMuted = muted ? 'true' : 'false';
        return;
      }
      node.classList.toggle('cfx-series-muted', muted);
      pointLabelSurfaces(root, node).forEach(label => label.classList.toggle('cfx-series-muted', muted));
    });
    refreshKeyboardNavigation(root);
    syncSelectionAppearance(root);
    syncResetControl(root);
  };
  const setSeriesIsolation = (root, target, isolated) => {
    const item = target && target.targetKind ? referencedTargetNode(root, target) : null;
    root.querySelectorAll('[data-cfx-series]').forEach((node) => {
      const data = node.dataset || {};
      const role = data.cfxRole || '';
      const sameSeries = matchesLocalSeriesTarget(node, target);
      if (role.indexOf('legend') === 0) {
        if (isolated && sameSeries) {
          data.cfxIsolated = 'true';
          node.setAttribute('aria-current', 'true');
        } else {
          delete data.cfxIsolated;
          node.removeAttribute('aria-current');
        }
        return;
      }
      node.classList.toggle('cfx-series-isolated-in', isolated && sameSeries);
      // Preserve ancestor containers for a referenced item or an isolated point.
      const context = item && (node.contains(item) || item.contains(node));
      const pointContainer = target && target.point !== undefined && data.cfxSeries === String(target.series)
        && role === 'series' && data.cfxPoint === undefined;
      node.classList.toggle('cfx-series-isolated-out', isolated && !sameSeries && !context && !pointContainer);
      pointLabelSurfaces(root, node).forEach(label => {
        label.classList.toggle('cfx-series-isolated-in', isolated && sameSeries);
        label.classList.toggle('cfx-series-isolated-out', isolated && !sameSeries && !context && !pointContainer);
      });
    });
    if (isolated) root.dataset.cfxIsolatedSeries = seriesTargetToken(target);
    else root.removeAttribute('data-cfx-isolated-series');
    syncSelectionAppearance(root);
    syncResetControl(root);
  };
  const toggleSeriesFocus = (root, item, emit, sync) => {
    if (!hasFeature(root, 'LegendToggles')) return;
    const target = seriesTarget(item);
    if (target.series === undefined) return;
    const isolated = root.dataset.cfxIsolatedSeries !== seriesTargetToken(target);
    setSeriesIsolation(root, target, isolated);
    emitHostEvent(root, 'cfxseriesfocus', { ...target, isolated });
    if (sync !== false) emitSync(root, { action: 'series-focus', target, isolated });
  };
  const toggleSeries = (root, item) => {
    if (!hasFeature(root, 'LegendToggles')) return;
    const target = seriesTarget(item);
    if (target.series === undefined) return;
    if (root.dataset.cfxIsolatedSeries) setSeriesIsolation(root, null, false);
    const muted = item.dataset.cfxMuted !== 'true';
    setSeriesMuted(root, target, muted);
    emitHostEvent(root, 'cfxseries', { ...target, muted });
    emitSync(root, { action: 'series', target, muted });
  };
  const toggleSelection = (root, node, event) => {
    if (!hasFeature(root, 'Selection') || !isDataTarget(node)) return;
    const previousTargets = selectedTargets(root);
    const previous = previousTargets.map(targetKey).join('\n');
    const mode = root.dataset.cfxSelectionMode || 'toggle';
    const toggle = mode === 'toggle' || mode === 'replace' && (event?.ctrlKey || event?.metaKey);
    const selected = toggle ? !node.classList.contains('cfx-selected') : true;
    if (mode === 'single' || mode === 'replace' && !toggle && !event?.shiftKey) clearSelections(root);
    setNodeSelected(node, selected);
    syncResetControl(root);
    const target = targetIdentity(node);
    emitHostEvent(root, 'cfxselect', { label: text(node), selected, target });
    emitSync(root, { action: 'selection', label: text(node), selected, target });
    syncSelectionAppearance(root);
    if (previous !== selectionSignature(root)) publishSelectionChange(root, event instanceof KeyboardEvent ? 'keyboard' : 'click', true, previousTargets);
    publishCompare(root, false);
  };
  const setNodeSelected = (node, selected) => {
    node.classList.toggle('cfx-selected', selected);
    const value = selected ? 'true' : 'false';
    if (node.getAttribute('aria-selected') !== value) node.setAttribute('aria-selected', value);
  };
  const setNodeHovered = (node, hovered, related) => {
    node.classList.toggle('cfx-hovered', hovered);
    node.classList.toggle('cfx-hover-related', related && !hovered);
  };
  const targetRelated = (node, target) => {
    if (!target) return false;
    const data = node.dataset || {};
    if (target.seriesKey && seriesKey(node) === target.seriesKey) return true;
    if (!target.seriesKey && target.series !== undefined && data.cfxSeries === String(target.series)) return true;
    if (target.point !== undefined && data.cfxPoint === String(target.point)) return true;
    if (target.label && (data.cfxLabel === target.label || data.cfxText === target.label || node.getAttribute('aria-label') === target.label)) return true;
    return false;
  };
  const clearHover = (root, emit, sync, preservePaint = false) => {
    clearRootBranchHighlight(root);
    root.removeAttribute('data-cfx-hovering');
    root.removeAttribute('data-cfx-hover-label');
    root.removeAttribute('data-cfx-hover-key');
    root.removeAttribute('data-cfx-hover-mode');
    root.removeAttribute('data-cfx-hover-unit');
    clearReveals(root, 'hover');
    clearReveals(root, 'crosshair');
    clearReveals(root, 'navigate');
    // A following target transition updates each class to its new state. Keep unchanged
    // series paint instead of removing and restoring thousands of related observations.
    if (!preservePaint || root._cfxBranchHighlights)
      root.querySelectorAll('.cfx-hovered,.cfx-hover-related,.cfx-hover-column,.cfx-hover-series').forEach((node) => node.classList.remove('cfx-hovered', 'cfx-hover-related', 'cfx-hover-column', 'cfx-hover-series'));
    // A published hover change supersedes the peer's guide emphasis; the next guide must restore it.
    if (emit !== false || sync !== false) delete root._cfxCrosshairMode;
    if (emit !== false) emitHostEvent(root, 'cfxhoverclear', {});
    if (sync !== false) emitSync(root, { action: 'hover-clear' });
  };
  // Pie-like legends name points, so their emphasis unit is one point rather than the containing series.
  const pointLegendUnits = (root, target) => target.point !== undefined && Array.from(root.querySelectorAll('[data-cfx-role="legend-item"][data-cfx-point]'))
    .some((item) => target.seriesKey ? seriesKey(item) === target.seriesKey : (item.dataset || {}).cfxSeries === String(target.series));
  const legendItemUnit = (root, target) => {
    const reference = target.legendTargetKind ? { targetKind: target.legendTargetKind, targetId: target.legendTargetId }
      : target.targetKind === 'node' ? { targetKind: target.targetKind, targetId: target.targetId } : null;
    if (!reference) return null;
    const hasLegend = Array.from(root.querySelectorAll('[data-cfx-role="legend-item"]')).some((node) => {
      const item = legendTarget(node);
      return item && item.targetKind === reference.targetKind && item.targetId === reference.targetId
        && (target.seriesKey ? seriesKey(node) === target.seriesKey : node.dataset.cfxSeries === String(target.series));
    });
    return hasLegend ? referencedTargetNode(root, { ...reference, seriesKey: target.seriesKey }) : null;
  };
  const inHoverUnit = (node, target, pointUnits, itemUnit) => {
    if (target.series === undefined && !target.seriesKey) return false;
    const data = node.dataset || {};
    const sameSeries = target.seriesKey ? seriesKey(node) === target.seriesKey : data.cfxSeries === String(target.series);
    if (itemUnit) return sameSeries && (node.contains(itemUnit) || itemUnit.contains(node)
      || pointUnits && data.cfxPoint === String(target.point));
    return sameSeries && (!pointUnits || data.cfxPoint === String(target.point) || (data.cfxRole === 'series' && data.cfxPoint === undefined));
  };
  const hoverModeForTarget = (root, node, mode) => mode === 'shared' ? 'shared'
    : root._cfxBranchHighlights?.includes(node) ? 'root-branch' : 'series';
  // 'series' keeps the pointed series at full strength while other series recede;
  // 'shared' (crosshair over the plot background) keeps every series at full strength.
  const applyHoverByTarget = (root, target, mode) => {
    if (!target) { clearHover(root, false, false); return false; }
    const nodes = interactiveTargets(root);
    const localNode = nodes.find((node) => matchesTargetIdentity(node, target));
    if (!localNode && target.targetKind && target.targetId) { clearHover(root, false, false); return false; }
    // A peer's equivalent mark may have a different local point ordinal or series position.
    const localTarget = localNode ? targetIdentity(localNode) : target;
    const hoverMode = hoverModeForTarget(root, localNode, mode);
    if (hoverMode === 'root-branch') return applyRootBranchHighlight(root, localNode);
    const pointUnits = hoverMode === 'series' && pointLegendUnits(root, localTarget);
    const itemUnit = hoverMode === 'series' && (legendItemUnit(root, localTarget)
      || pointUnits && referencedTargetNode(root, { ...localTarget, targetKind: 'point' }));
    let matched = false;
    nodes.forEach((node) => {
      const hovered = matchesTargetIdentity(node, localTarget);
      const related = !hovered && targetRelated(node, localTarget);
      if (hovered || related) matched = true;
      setNodeHovered(node, hovered, related);
      node.classList.toggle('cfx-hover-series', hoverMode === 'series' && inHoverUnit(node, localTarget, pointUnits, itemUnit));
      if (root.dataset.cfxLook === 'graphite') node.classList.toggle('cfx-hover-column', localTarget.point !== undefined && node.dataset.cfxPoint === String(localTarget.point));
    });
    if (matched) {
      root.dataset.cfxHovering = 'true';
      root.dataset.cfxHoverMode = hoverMode;
      root.dataset.cfxHoverUnit = pointUnits ? 'point' : 'series';
      root.dataset.cfxHoverLabel = localTarget.label || localTarget.role || localTarget.id || '';
    }
    return matched;
  };
  const clearFocusTrail = (root) => {
    root._cfxFocusTrail = [];
    root.removeAttribute('data-cfx-trail-count');
    root.querySelectorAll('.cfx-trail').forEach((node) => {
      node.classList.remove('cfx-trail');
      delete node.dataset.cfxTrailIndex;
    });
  };
  const applyFocusTrail = (root, trail) => {
    clearFocusTrail(root);
    const targets = (trail || []).slice(0, 5);
    if (!targets.length || !hasFeature(root, 'FocusTrail')) return [];
    root._cfxFocusTrail = targets;
    root.dataset.cfxTrailCount = String(targets.length);
    targets.forEach((target, index) => {
      interactiveTargets(root).forEach((node) => {
        if (!matchesTargetIdentity(node, target)) return;
        node.classList.add('cfx-trail');
        node.dataset.cfxTrailIndex = String(index + 1);
      });
    });
    return targets;
  };
  const recordFocusTrail = (root, target, emit, sync) => {
    if (!target || !hasFeature(root, 'FocusTrail')) return [];
    const key = targetKey(target);
    if (!key) return [];
    const existing = root._cfxFocusTrail || [];
    const trail = [target, ...existing.filter((item) => targetKey(item) !== key)].slice(0, 5);
    applyFocusTrail(root, trail);
    if (emit !== false) emitHostEvent(root, 'cfxtrail', { target, trail, count: trail.length });
    if (sync !== false) emitSync(root, { action: 'trail', target, trail, count: trail.length });
    return trail;
  };
  const applySelectionByLabel = (root, label, selected) => {
    if (!label) return;
    let nodes = dataTargets(root).filter(node => text(node) === label);
    if (selected && root.dataset.cfxSelectionMode === 'single' && nodes.length) { clearSelections(root); nodes = nodes.slice(0, 1); }
    nodes.forEach(node => setNodeSelected(node, selected));
    syncResetControl(root);
  };
  const matchesTargetIdentity = (node, target) => {
    if (!target) return false;
    const data = node.dataset || {};
    if (target.targetKind && target.targetId) return renderedTargetKind(node) === target.targetKind && renderedTargetId(node, target.targetKind) === target.targetId;
    if (target.id) return node.id === target.id || data.cfxId === target.id;
    if (target.seriesKey) {
      if (seriesKey(node) !== target.seriesKey) return false;
      if (target.point !== undefined) return data.cfxPoint === String(target.point);
      return !target.role || data.cfxRole === target.role;
    }
    if (target.series !== undefined && target.point !== undefined && data.cfxSeries === String(target.series) && data.cfxPoint === String(target.point)) return true;
    if (target.series !== undefined && target.point === undefined && data.cfxSeries === String(target.series) && target.role && data.cfxRole === target.role) return true;
    if (target.role && data.cfxRole === target.role && target.label && (data.cfxLabel === target.label || data.cfxText === target.label || node.getAttribute('aria-label') === target.label)) return true;
    if (target.role && data.cfxRole === target.role && target.value && (data.cfxValue === target.value || data.cfxY === target.value || data.cfxEnd === target.value)) return true;
    return false;
  };
  const applySelectionByTarget = (root, target, selected) => {
    if (!target) return false;
    let nodes = dataTargets(root).filter(node => (!target.seriesKey || seriesKey(node) === target.seriesKey) && matchesTargetIdentity(node, target));
    if (selected && root.dataset.cfxSelectionMode === 'single' && nodes.length) { clearSelections(root); nodes = nodes.slice(0, 1); }
    nodes.forEach(node => setNodeSelected(node, selected));
    syncResetControl(root);
    return nodes.length > 0;
  };
  const clearSelections = (root) => {
    root.querySelectorAll('.cfx-selected').forEach((node) => {
      node.classList.remove('cfx-selected');
      node.removeAttribute('aria-selected');
    });
    syncSelectionAppearance(root);
    renderCompare(root);
    syncResetControl(root);
  };
  const publishResetSelection = (root, previousSelection) => {
    if (previousSelection) publishSelectionChange(root, 'reset');
    publishCompare(root, false);
    // Reset clears the group's data selection even when the compare tray is disabled.
    emitSync(root, { action: 'compare', count: 0, targets: [] });
  };
  const selectionTargetBatch = root => {
    const nodes = dataTargets(root), identities = new Map();
    for (const node of nodes) {
      const kind = renderedTargetKind(node), key = JSON.stringify([kind, renderedTargetId(node, kind)]);
      const matches = identities.get(key) || [];
      matches.push(node); identities.set(key, matches);
    }
    return { nodes, resolve: target => {
      if (!target) return [];
      const candidates = target.targetKind && target.targetId
        ? identities.get(JSON.stringify([target.targetKind, target.targetId])) || [] : nodes.filter(node => matchesTargetIdentity(node, target));
      return target.seriesKey ? candidates.filter(node => seriesKey(node) === target.seriesKey) : candidates;
    } };
  };
  const applySelectionSetByTargets = (root, targets, replace) => {
    const single = root.dataset.cfxSelectionMode === 'single';
    const batch = selectionTargetBatch(root);
    if (replace !== false || single) clearSelections(root);
    let count = 0;
    for (const target of targets || []) {
      const matches = batch.resolve(target);
      if (matches.length) {
        (single ? matches.slice(0, 1) : matches).forEach(node => setNodeSelected(node, true));
        count++;
      }
      if (single && count) break;
    }
    syncResetControl(root);
    return count;
  };
  const reconcileSourceSelection = (root, previous, targets) => {
    // One chart can share raw observations with one peer and derived summaries with another.
    // Reconcile only this gesture's represented identities; an unrelated peer fact stays selected.
    const batch = selectionTargetBatch(root), before = new Set(), after = new Set();
    for (const target of previous) batch.resolve(target).forEach(node => before.add(node));
    for (const target of targets) {
      const matches = batch.resolve(target);
      if (root.dataset.cfxSelectionMode === 'single' && matches.length) {
        clearSelections(root); after.add(matches[0]); break;
      }
      matches.forEach(node => after.add(node));
    }
    for (const node of before) if (!after.has(node) && node.classList.contains('cfx-selected')) setNodeSelected(node, false);
    for (const node of after) setNodeSelected(node, true);
    syncResetControl(root);
  };
  const nodeCenterInRect = (node, rect) => {
    const box = node.getBoundingClientRect();
    if (!box.width && !box.height) return null;
    const x = box.left + box.width / 2;
    const y = box.top + box.height / 2;
    if (x < rect.left || x > rect.right || y < rect.top || y > rect.bottom) return null;
    return { x, y };
  };
  const brushPointInRange = (root, node, rect, selection) => {
    const projection = root._cfxDataProjection;
    if (!projection || !selection?.range) return null;
    const target = targetIdentity(node), data = node.dataset;
    // Scalar source coordinates survive SVG/CSS quantization. Interval facts and
    // non-point marks retain their existing visual-centre selection contract.
    if (target.targetKind !== 'point' || target.x === undefined || target.range) return null;
    if (!keyboardTargetAvailable(node)) return false;
    const box = node.getBoundingClientRect(), x = box.left + box.width / 2, y = box.top + box.height / 2;
    const categoryDirection = projection.horizontal ? 'y' : 'x', valueDirection = projection.horizontal ? 'x' : 'y';
    const inside = direction => direction === 'x' ? x >= rect.left && x <= rect.right : y >= rect.top && y <= rect.bottom;
    const categoryRange = selection.range[categoryDirection];
    if (categoryRange ? !overlapsRange({ x: target.x }, categoryRange)
      : root.dataset.cfxBrushAxis !== 'x' && !inside(categoryDirection)) return false;
    const valueRange = selection.range[valueDirection], valueAxis = projection[valueDirection];
    const scalar = tooltipNumber(data.cfxY) && data.cfxStart === undefined && data.cfxMedian === undefined && data.cfxOpen === undefined;
    if (!valueRange) return root.dataset.cfxBrushAxis === 'x' || inside(valueDirection);
    if (!scalar) return inside(valueDirection);
    const time = valueAxis.scale === 'time' && valueAxis.minimum >= 0;
    const value = time && data.cfxValueInstant !== undefined ? data.cfxValueInstant : dataValue(data.cfxStackEnd ?? data.cfxY, time);
    return overlapsRange({ x: value }, valueRange);
  };
  const selectTargetsInBox = (root, rect, append, selection) => {
    if (!hasFeature(root, 'Selection')) return [];
    const single = root.dataset.cfxSelectionMode === 'single';
    let candidates = dataTargets(root).filter(node => !node.closest('[data-cfx-role="legend-item"]')
      && (brushPointInRange(root, node, rect, selection) ?? !!nodeCenterInRect(node, rect)));
    if (single) candidates = candidates.slice(0, 1);
    if (!append || single && candidates.length) clearSelections(root);
    const targets = [];
    candidates.forEach((node) => {
      setNodeSelected(node, true);
      targets.push(targetIdentity(node));
    });
    syncResetControl(root);
    syncSelectionAppearance(root);
    return targets;
  };
  const applySync = (root, detail) => {
    if (!detail || detail.chartId === root.dataset.cfxChartId) return;
    if (detail.action === 'viewport' && detail.state) applyViewport(root, detail.state);
    else if (detail.action === 'brush') {
      if (detail.clear) clearBrush(root);
      else { root.dataset.cfxBrush = detail.bounds || ''; syncResetControl(root); }
    }
    else if (detail.action === 'highlight') applyHostHighlight(root, detail.state || null);
    else if (detail.action === 'selection') {
      if (!applySelectionByTarget(root, detail.target, detail.selected === true) && !(detail.target && (detail.target.id || detail.target.targetId))) applySelectionByLabel(root, detail.label || '', detail.selected === true);
      renderCompare(root);
      syncSelectionAppearance(root);
    } else if (detail.action === 'lasso') {
      if (Array.isArray(detail.previousTargets)) reconcileSourceSelection(root, detail.previousTargets, detail.targets || []);
      else applySelectionSetByTargets(root, detail.targets || [], detail.replace !== false);
      renderCompare(root);
      syncSelectionAppearance(root);
    } else if (detail.action === 'compare') {
      if (Array.isArray(detail.previousTargets)) reconcileSourceSelection(root, detail.previousTargets, detail.targets || []);
      else applySelectionSetByTargets(root, detail.targets || [], true);
      renderCompare(root);
      syncSelectionAppearance(root);
    }
    else if (detail.action === 'hover') {
      clearHover(root, false, false, true);
      applyHoverByTarget(root, detail.target);
      revealTargets(root, [detail.target], false, false, detail.source || 'hover');
    } else if (detail.action === 'hover-clear') clearHover(root, false, false);
    else if (detail.action === 'crosshair' || detail.action === 'navigate') {
      clearHover(root, false, false, true);
      applyHoverByTarget(root, detail.target, detail.action === 'crosshair' ? detail.mode || 'shared' : 'series');
      revealTargets(root, [detail.target], false, false, detail.action);
    }
    else if (detail.action === 'trail') applyFocusTrail(root, detail.trail || []);
    else if (detail.action === 'reveal') revealTargets(root, detail.targets || [], false, false, detail.source || '');
    else if (detail.action === 'reveal-clear') clearReveals(root, detail.source || '');
    else if (detail.action === 'state') applyInteractionState(root, detail.state, false, false);
    else if (detail.action === 'series') {
      const target = resolveSeriesTarget(root, detail.target);
      if (target) setSeriesMuted(root, target, detail.muted === true);
    }
    else if (detail.action === 'series-focus') {
      const target = resolveSeriesTarget(root, detail.target);
      if (target) setSeriesIsolation(root, target, detail.isolated === true);
    }
    else if (detail.action === 'scenario') setScenario(root, detail.scenarioId || '', false, false);
    else if (detail.action === 'scenario-step') {
      if (detail.scenarioId && root.dataset.cfxActiveScenario !== detail.scenarioId) setScenario(root, detail.scenarioId, false, false);
      setScenarioStep(root, detail.index, false, false);
    }
  };
  // Native links, annotations and scalar series are selectable facts too. A series wrapper
  // has no value of its own; its separately identified children own the observations.
  const isDataTarget = node => isInteractiveTarget(node) && renderedTargetKind(node) !== 'legend'
    && (renderedTargetKind(node) !== 'series' || node.dataset.cfxValue !== undefined);
  const dataTargets = root => interactiveTargets(root).filter(isDataTarget);
  const targetCategory = node => {
    const data = node.dataset || {};
    return data.cfxXLabel ?? data.cfxCategoryLabel ?? data.cfxCategory
      ?? (['pie', 'donut', 'pyramid', 'funnel'].includes((data.cfxKind || '').toLowerCase()) ? data.cfxLabel : undefined);
  };
  const dataValue = (value, time) => {
    if (!tooltipNumber(value)) return undefined;
    const number = Number(value);
    if (!time || number < 0) return number;
    const instant = new Date((number - 25569) * 86400000);
    return Number.isFinite(instant.valueOf()) ? instant.toISOString() : undefined;
  };
  const timeTarget = node => {
    const projection = node.closest('.cfx-interactive-chart')?._cfxDataProjection;
    const axis = projection?.horizontal ? projection.y : projection?.x;
    return axis ? axis.scale === 'time' && axis.minimum >= 0
      : node.dataset.cfxXScale === 'time' || node.dataset.cfxRangeScale === 'time';
  };
  const targetRawX = node => timeTarget(node) ? node.dataset.cfxXInstant ?? dataValue(node.dataset.cfxX, true) : dataValue(node.dataset.cfxX, false);
  const targetDataRange = node => {
    const data = node.dataset;
    const lower = data.cfxRangeStart;
    const upper = data.cfxRangeEnd;
    if (lower === undefined || upper === undefined) return undefined;
    // A range has one unit: native negative OLE coordinates cannot mix with Unix-millisecond ISO instants.
    const time = data.cfxRangeScale === 'time' && timeTarget(node) && Number(lower) >= 0 && Number(upper) >= 0;
    const from = time ? data.cfxRangeStartInstant ?? dataValue(lower, true) : dataValue(lower, false);
    const to = time ? data.cfxRangeInclusiveEndInstant ?? data.cfxRangeEndInstant ?? dataValue(upper, true) : dataValue(upper, false);
    return from === undefined || to === undefined ? undefined : [from, to];
  };
  const contributorObservationIds = node => {
    const sources = sourcePointCollection(node);
    if (!sources) return undefined;
    const ids = node.closest('.cfx-interactive-chart')?._cfxObservationIds?.[Number(node.dataset.cfxSeries)];
    const authored = sources.map(index => ids?.get(index)).filter(id => id !== undefined);
    return authored.length ? authored : undefined;
  };
  const syncSelectionAppearance = root => {
    const nodes = dataTargets(root), selected = nodes.some(node => node.classList.contains('cfx-selected'));
    const selectionForeground = nodes.some(node => node.classList.contains('cfx-selected') && keyboardTargetAvailable(node));
    const highlightForeground = nodes.some(node => node.classList.contains('cfx-highlighted') && keyboardTargetAvailable(node));
    root.toggleAttribute('data-cfx-has-selection', selected);
    if (selected) root.dataset.cfxHasSelection = 'true';
    nodes.forEach(node => {
      // Selection and external highlights are independent foregrounds. Their combined dim policy
      // keeps either active mark at full strength instead of letting the two layers dim each other.
      const active = node.classList.contains('cfx-selected') || node.classList.contains('cfx-highlighted');
      node.classList.toggle('cfx-selection-dim', selectionForeground && root.dataset.cfxDimOthersSelected === 'true' && !active);
      node.classList.toggle('cfx-linked-dim', highlightForeground && root._cfxHostHighlight?.dim === true && !active);
    });
    syncCartesianLinkedPaint(root, nodes);
  };
  const selectionSignature = root => selectedTargets(root).map(targetKey).join('\n');
  const publishSelectionChange = (root, reason, sync = false, previousTargets) => {
    syncSelectionAppearance(root);
    const targets = selectedTargets(root);
    emitHostEvent(root, 'cfxselectionchange', { reason, targets });
    if (sync) emitSync(root, { action: 'compare', count: targets.length, targets, previousTargets });
    return targets;
  };
  const copyHostValue = value => value == null ? value : JSON.parse(JSON.stringify(value));
  const hostMatch = match => {
    if (!match || typeof match !== 'object' || Array.isArray(match)) throw new TypeError('Expected a chart target match.');
    const candidate = copyHostValue(match);
    for (const key of ['targets', 'observationIds', 'categories', 'seriesKeys', 'sourcePoints', 'ranges'])
      if (candidate[key] !== undefined && !Array.isArray(candidate[key])) throw new TypeError('Chart match ' + key + ' must be an array.');
    return candidate;
  };
  const intervalNumber = value => typeof value === 'number' && Number.isFinite(value) ? value
    : typeof value === 'string' && /(?:Z|[+-]\d\d:\d\d)$/.test(value) ? Date.parse(value) : NaN;
  const compareIntervalValues = (left, right) => {
    const a = intervalNumber(left), b = intervalNumber(right);
    if (a !== b) return a < b ? -1 : 1;
    if (typeof left !== 'string') return 0;
    // Date.parse normalizes offsets but truncates fractional milliseconds. Compare that remainder
    // separately so precise instants never round together in a Unix-millisecond floating-point value.
    const remainder = value => (value.match(/\.(\d+)(?:Z|[+-]\d\d:\d\d)$/)?.[1] || '').slice(3);
    const x = remainder(left), y = remainder(right), length = Math.max(x.length, y.length);
    const paddedX = x.padEnd(length, '0'), paddedY = y.padEnd(length, '0');
    return paddedX === paddedY ? 0 : paddedX < paddedY ? -1 : 1;
  };
  const overlapsRange = (target, range) => {
    if (!Array.isArray(range) || range.length !== 2) return false;
    const from = intervalNumber(range[0]), to = intervalNumber(range[1]);
    const values = target.range || (target.x !== undefined ? [target.x, target.x] : null);
    if (!values || !Number.isFinite(from) || !Number.isFinite(to)) return false;
    if (typeof range[0] !== typeof values[0] || typeof range[1] !== typeof values[1] || typeof range[0] !== typeof range[1]) return false;
    const lower = intervalNumber(values[0]), upper = intervalNumber(values[1]);
    if (!Number.isFinite(lower) || !Number.isFinite(upper)) return false;
    const [rangeStart, rangeEnd] = compareIntervalValues(range[0], range[1]) <= 0 ? range : [range[1], range[0]];
    const [targetStart, targetEnd] = compareIntervalValues(values[0], values[1]) <= 0 ? values : [values[1], values[0]];
    return compareIntervalValues(targetStart, rangeEnd) <= 0 && compareIntervalValues(targetEnd, rangeStart) >= 0;
  };
  const matchesHostTarget = (node, match) => {
    const target = targetIdentity(node);
    const scoped = !match.seriesKey || match.seriesKey === target.seriesKey;
    return (match.targets || []).some(item => item && (!item.seriesKey || item.seriesKey === target.seriesKey) && matchesTargetIdentity(node, item))
      || scoped && (match.observationIds || []).some(id => target.observationId === id || target.observationIds?.includes(id))
      || (match.categories || []).includes(target.category)
      || (match.seriesKeys || []).includes(target.seriesKey)
      || scoped && (match.sourcePoints || []).some(item => item && item.seriesKey === target.seriesKey
        && (Number(target.sourcePoint) === item.index || target.sourcePoints?.includes(item.index)))
      || scoped && (match.ranges || []).some(range => overlapsRange(target, range));
  };
  const runSelectionCommand = (root, match, options = {}) => {
    if (!hasFeature(root, 'Selection')) return [];
    const normalized = hostMatch(match), mode = options.mode || 'replace';
    if (!['replace', 'add', 'remove'].includes(mode)) throw new TypeError('Selection mode must be replace, add or remove.');
    const previousTargets = selectedTargets(root);
    const nodes = dataTargets(root), matching = new Set(nodes.filter(node => matchesHostTarget(node, normalized)));
    const single = root.dataset.cfxSelectionMode === 'single', first = matching.values().next().value;
    const changed = [];
    nodes.forEach(node => {
      const before = node.classList.contains('cfx-selected'), matches = matching.has(node);
      const selected = mode === 'replace' ? matches && (!single || node === first)
        : mode === 'add' && single && first ? node === first : matches ? mode === 'add' : before;
      if (selected === before) return;
      setNodeSelected(node, selected); changed.push({ node, selected });
    });
    syncSelectionAppearance(root); renderCompare(root); syncResetControl(root);
    if (changed.length && options.emit === true) {
      changed.forEach(({ node, selected }) => emitHostEvent(root, 'cfxselect', { label: text(node), selected, target: targetIdentity(node) }));
      publishSelectionChange(root, 'command'); publishCompare(root, false);
    }
    const targets = selectedTargets(root);
    if (changed.length && options.sync === true) emitSync(root, { action: 'compare', count: targets.length, targets,
      previousTargets: mode === 'replace' ? undefined : previousTargets });
    return targets;
  };
  const clearBrushCommand = (root, options = {}) => {
    if (!hasFeature(root, 'Brush')) return false;
    const changed = clearBrush(root);
    if (changed && options.emit === true) emitHostEvent(root, 'cfxbrushclear', {});
    if (changed && options.sync === true) emitSync(root, { action: 'brush', bounds: '', clear: true });
    return changed;
  };
  const clearSelectionCommand = (root, options = {}) => {
    clearBrushCommand(root, options);
    return runSelectionCommand(root, { targets: [] }, { ...options, mode: 'replace' });
  };
  const captureHostHighlight = (root, sameSource = false) => {
    const state = copyHostValue(root._cfxHostHighlight || null);
    if (!state || sameSource) return state;
    const match = state.match, nodes = dataTargets(root);
    const sharedKeys = new Set(JSON.parse(root.dataset.cfxPreparedChart).series.filter(series => !series.local).map(series => series.key));
    let removed = false;
    const retain = (key, predicate) => {
      if (!match[key]) return;
      const original = match[key]; match[key] = original.filter(predicate);
      removed ||= match[key].length !== original.length;
    };
    retain('seriesKeys', key => sharedKeys.has(key));
    if (match.targets) {
      const original = match.targets;
      match.targets = original.flatMap(item => nodes.filter(node => item && (!item.seriesKey || item.seriesKey === seriesKey(node))
        && matchesTargetIdentity(node, item)).map(targetIdentity).filter(durableChartTarget)
        .map(target => ({ targetKind: target.targetKind, targetId: target.targetId, seriesKey: target.seriesKey })));
      removed ||= original.some(item => !nodes.some(node => item && (!item.seriesKey || item.seriesKey === seriesKey(node))
        && matchesTargetIdentity(node, item) && durableChartTarget(targetIdentity(node))));
    }
    // Ordinals describe the previous source order; explicit IDs and value filters describe durable intent.
    if (match.sourcePoints?.length) { match.sourcePoints = []; removed = true; }
    if (match.seriesKey && !sharedKeys.has(match.seriesKey)) {
      for (const key of ['observationIds', 'ranges']) if (match[key]?.length) { match[key] = []; removed = true; }
      delete match.seriesKey;
    }
    return removed && !['targets', 'observationIds', 'categories', 'seriesKeys', 'ranges'].some(key => match[key]?.length) ? null : state;
  };
  const applyHostHighlight = (root, state) => {
    root._cfxHostHighlight = state ? copyHostValue(state) : null;
    const nodes = dataTargets(root), highlighted = state ? nodes.filter(node => matchesHostTarget(node, state.match)) : [];
    const active = new Set(highlighted);
    nodes.forEach(node => {
      const match = active.has(node);
      node.classList.toggle('cfx-highlighted', match);
      node.toggleAttribute('data-cfx-highlighted', match);
      if (match) node.dataset.cfxHighlighted = 'true';
    });
    syncSelectionAppearance(root);
    const region = root.querySelector('[data-cfx-live]');
    if (region) region.textContent = state ? highlighted.length + ' marks highlighted' : 'Highlight cleared';
    return highlighted.map(targetIdentity);
  };
  const runHighlightCommand = (root, match, options = {}) => {
    if (!hasFeature(root, 'Selection')) return [];
    const state = { match: hostMatch(match), dim: options.dim === true, source: options.source || '' };
    const targets = applyHostHighlight(root, state);
    if (options.emit === true) emitHostEvent(root, 'cfxhighlight', { count: targets.length, targets, source: state.source });
    if (options.sync === true) emitSync(root, { action: 'highlight', state });
    return targets;
  };
  const clearHighlightCommand = (root, options = {}) => {
    if (!hasFeature(root, 'Selection')) return [];
    applyHostHighlight(root, null);
    if (options.emit === true) emitHostEvent(root, 'cfxhighlightclear', {});
    if (options.sync === true) emitSync(root, { action: 'highlight', state: null });
    return [];
  };
  const bindHostCommands = root => {
    if (!root.querySelector('[data-cfx-live]')) {
      const region = document.createElement('span');
      region.setAttribute('data-cfx-live', 'true'); region.setAttribute('aria-live', 'polite');
      region.className = 'cfx-visually-hidden'; root.appendChild(region);
    }
    listenChart(root, root, 'cfx-unpin-tooltip', () => unpinTooltip(root));
    listenChart(root, root, 'cfx-clear-brush', event => clearBrushCommand(root, event.detail || {}));
    listenChart(root, root, 'cfx-clear-selection', event => clearSelectionCommand(root, event.detail || {}));
    if (!hasFeature(root, 'Selection')) return;
    for (const [name, execute] of [['cfx-select', runSelectionCommand], ['cfx-highlight', runHighlightCommand]])
      listenChart(root, root, name, event => { const detail = event.detail || {}; execute(root, detail.match, detail); });
    listenChart(root, root, 'cfx-clear-highlight', event => clearHighlightCommand(root, event.detail || {}));
  };
  const validDataProjection = projection => {
    const plot = projection?.plotBounds;
    const axis = value => !!value && [value.minimum, value.maximum, value.transformedMinimum, value.transformedMaximum, value.threshold].every(Number.isFinite)
      && value.maximum > value.minimum && value.transformedMaximum > value.transformedMinimum && value.threshold > 0
      && typeof value.reversed === 'boolean' && ['linear', 'time', 'logarithmic', 'symmetriclogarithmic'].includes(value.scale)
      && (value.displayTicks === undefined || Array.isArray(value.displayTicks) && value.displayTicks.every(tick =>
        tick && Number.isFinite(tick.value) && typeof tick.text === 'string' && (tick.instant === undefined || typeof tick.instant === 'string')))
      && (value.displayBounds === undefined || Array.isArray(value.displayBounds) && value.displayBounds.every(tick =>
        tick && Number.isFinite(tick.value) && typeof tick.text === 'string' && (tick.instant === undefined || typeof tick.instant === 'string')));
    return !!plot && [plot.x, plot.y, plot.width, plot.height].every(Number.isFinite) && plot.width > 0 && plot.height > 0
      && typeof projection.horizontal === 'boolean' && axis(projection.x) && (projection.y === undefined || axis(projection.y));
  };
  const brushPointer = (root, stage, event) => {
    const point = stagePointer(stage, event, true), plot = visibleCartesianPlot(root, stage, root._cfxDataProjection?.plotBounds);
    return plot ? { x: clamp(point.x, plot.left, plot.left + plot.width), y: clamp(point.y, plot.top, plot.top + plot.height) } : point;
  };
  const setBrushBox = (root, stage, brush, drag, point) => {
    const plot = root.dataset.cfxBrushAxis === 'x' && visibleCartesianPlot(root, stage, root._cfxDataProjection?.plotBounds);
    brush.style.left = Math.min(drag.left, point.x) + 'px';
    brush.style.top = (plot ? plot.top : Math.min(drag.top, point.y)) + 'px';
    brush.style.width = Math.abs(point.x - drag.left) + 'px';
    brush.style.height = (plot ? plot.height : Math.abs(point.y - drag.top)) + 'px';
  };
  const projectedAxisValue = (axis, ratio) => {
    if (axis.reversed) ratio = 1 - ratio;
    const span = axis.transformedMaximum - axis.transformedMinimum;
    const value = Number.isFinite(span) ? axis.transformedMinimum + ratio * span
      : axis.transformedMinimum * (1 - ratio) + axis.transformedMaximum * ratio;
    const raw = axis.scale === 'logarithmic' ? Math.pow(10, value)
      : axis.scale === 'symmetriclogarithmic' ? Math.sign(value) * axis.threshold * (Math.pow(10, Math.abs(value)) - 1) : value;
    return clamp(raw, axis.minimum, axis.maximum);
  };
  const transformedAxisValue = (axis, value) => axis.scale === 'logarithmic' ? Math.log10(value)
    : axis.scale === 'symmetriclogarithmic' ? Math.sign(value) * Math.log10(1 + Math.abs(value) / axis.threshold) : value;
  const enclosingAxisTicks = (axis, values) => {
    const ticks = [...(axis.displayTicks || []), ...(axis.displayBounds || [])]
      .filter(tick => tick.value >= axis.minimum && tick.value <= axis.maximum);
    const lower = ticks.filter(tick => tick.value <= values[0]).sort((a, b) => b.value - a.value)[0];
    const upper = ticks.filter(tick => tick.value >= values[1]).sort((a, b) => a.value - b.value)[0];
    return lower && upper ? [lower, upper] : undefined;
  };
  const projectedAxisRatio = (axis, value) => {
    const transformed = transformedAxisValue(axis, value), span = axis.transformedMaximum - axis.transformedMinimum;
    const ratio = Number.isFinite(span) ? (transformed - axis.transformedMinimum) / span
      : (transformed / 2 - axis.transformedMinimum / 2) / (axis.transformedMaximum / 2 - axis.transformedMinimum / 2);
    return axis.reversed ? 1 - ratio : ratio;
  };
  const projectBrushSelection = (root, rect) => {
    const projection = root._cfxDataProjection, svg = root.querySelector('.cfx-stage svg');
    const matrix = svg?.getScreenCTM();
    if (!projection || !matrix || matrix.is2D === false || !matrix.a || !matrix.d) return { data: {} };
    let inverse;
    try { inverse = matrix.inverse(); } catch { return { data: {} }; }
    const points = [new DOMPoint(rect.left, rect.top), new DOMPoint(rect.right, rect.bottom)].map(point => point.matrixTransform(inverse));
    if (!points.every(point => Number.isFinite(point.x) && Number.isFinite(point.y))) return { data: {} };
    const plot = projection.plotBounds;
    const x = points.map(point => clamp((point.x - plot.x) / plot.width, 0, 1));
    const y = points.map(point => clamp(1 - (point.y - plot.y) / plot.height, 0, 1));
    let metadata;
    try { metadata = JSON.parse(root.dataset.cfxPreparedChart); } catch { metadata = {}; }
    const categoryDirection = projection.horizontal ? 'y' : 'x';
    const categoryAxis = projection[categoryDirection];
    const categoryLabels = categoryAxis?.scale === 'linear' && Array.isArray(metadata.categoryLabels) ? metadata.categoryLabels : undefined;
    const selectedAxes = [['x', projection.x, x], ...(root.dataset.cfxBrushAxis !== 'x' && projection.y ? [['y', projection.y, y]] : [])];
    const range = {}, displayRange = {}, ratios = { x, y }, captions = {};
    for (const [direction, axis, endpoints] of selectedAxes) {
      const raw = endpoints.map(ratio => projectedAxisValue(axis, ratio)).sort((a, b) => a - b);
      const ticks = direction === categoryDirection && categoryLabels ? undefined : enclosingAxisTicks(axis, raw);
      const values = ticks ? ticks.map(tick => tick.value) : raw;
      const orderedTime = axis.scale === 'time' && axis.minimum >= 0 && values.every(endpoint => endpoint >= 0);
      range[direction] = values.map((value, index) => orderedTime && ticks?.[index].instant !== undefined
        ? ticks[index].instant : dataValue(value, orderedTime));
      if (ticks) {
        displayRange[direction] = range[direction];
        captions[direction] = ticks.every(tick => tick.text === '') ? ''
          : ticks[0].value === ticks[1].value ? ticks[0].text : ticks.map(tick => tick.text).join(' – ');
        ratios[direction] = values.map(value => projectedAxisRatio(axis, value));
      }
    }
    const categoryRange = range[categoryDirection];
    const data = categoryRange ? { range, categoryRange } : { range };
    if (categoryLabels && categoryRange) {
      const included = categoryLabels.filter(item => item.value >= categoryRange[0] && item.value <= categoryRange[1]).sort((a, b) => a.value - b.value);
      data.categories = included.map(item => item.text);
      captions[categoryDirection] = data.categories.join(', ');
      if (included.length) displayRange[categoryDirection] = [included[0].value, included[included.length - 1].value];
    }
    if (Object.keys(displayRange).length) data.displayRange = displayRange;
    const caption = captions[categoryDirection] ?? captions.x;
    if (caption !== undefined) data.label = data.rangeLabel = caption;
    const xs = ratios.x.map(ratio => plot.x + ratio * plot.width);
    const ys = ratios.y.map(ratio => plot.y + (1 - ratio) * plot.height);
    const bounds = [new DOMPoint(Math.min(...xs), Math.min(...ys)), new DOMPoint(Math.max(...xs), Math.max(...ys))]
      .map(point => point.matrixTransform(matrix));
    return { data, bounds };
  };
  const finishBrushSelection = (root, stage, brush) => {
    const selection = projectBrushSelection(root, brush.getBoundingClientRect());
    if (selection.bounds) {
      const corners = selection.bounds.map(point => stagePoint(stage, point));
      brush.style.left = Math.min(...corners.map(point => point.x)) + 'px';
      brush.style.top = Math.min(...corners.map(point => point.y)) + 'px';
      brush.style.width = Math.abs(corners[1].x - corners[0].x) + 'px';
      brush.style.height = Math.abs(corners[1].y - corners[0].y) + 'px';
    }
    return selection.data;
  };
  const revealLayer = (root) => {
    let layer = root.querySelector('[data-cfx-reveal-layer]');
    if (layer) return layer;
    const stage = root.querySelector('.cfx-stage');
    if (!stage) return null;
    layer = document.createElement('div');
    layer.className = 'cfx-reveal-layer';
    layer.dataset.cfxRevealLayer = 'true';
    layer.setAttribute('aria-live', 'polite');
    layer.hidden = true;
    stage.appendChild(layer);
    return layer;
  };
  const clearReveals = (root, source) => {
    if (source && root.dataset.cfxRevealSource && root.dataset.cfxRevealSource !== source) return;
    const layer = root.querySelector('[data-cfx-reveal-layer]');
    if (layer) {
      layer.replaceChildren();
      layer.hidden = true;
    }
    root.removeAttribute('data-cfx-reveal-count');
    root.removeAttribute('data-cfx-reveal-source');
  };
  const revealText = (node, target) => {
    const data = node.dataset || {};
    if (data.cfxLabel || data.cfxText) return data.cfxLabel || data.cfxText;
    if (target && target.label) return target.label;
    if (data.cfxSeries !== undefined && data.cfxPoint === undefined) return seriesLabel(node);
    if (data.cfxPoint !== undefined) return text(node);
    return text(node) || (target ? target.role || target.id : '') || 'Target';
  };
  const revealNodes = (root, nodes, emit, sync, source) => {
    if (!hasFeature(root, 'RevealLabels')) return [];
    const layer = revealLayer(root);
    const stage = root.querySelector('.cfx-stage');
    if (!layer || !stage) return [];
    const stageRect = stage.getBoundingClientRect();
    const shown = [];
    const seen = new Set();
    const placed = [];
    const overlaps = (candidate) => placed.some((item) => candidate.left < item.right + 6 && candidate.right + 6 > item.left && candidate.top < item.bottom + 6 && candidate.bottom + 6 > item.top);
    layer.replaceChildren();
    layer.hidden = false;
    (nodes || []).forEach((node) => {
      if (!node || isPassiveTarget(node) || !stage.contains(node) || node.closest('[data-cfx-role="legend-item"]')) return;
      const target = targetIdentity(node);
      const labelText = revealText(node, target);
      const key = [labelText, target.series ?? '', target.point ?? '', target.value || ''].join('|');
      if (!key || seen.has(key) || shown.length >= 6) return;
      const rect = node.getBoundingClientRect();
      if (!rect.width && !rect.height) return;
      seen.add(key);
      const label = document.createElement('span');
      label.className = 'cfx-reveal-label';
      label.textContent = labelText;
      label.dataset.cfxRevealIndex = String(shown.length + 1);
      label.title = label.textContent;
      label.style.left = (rect.left + rect.width / 2 - stageRect.left) + 'px';
      label.style.top = (rect.top - stageRect.top) + 'px';
      layer.appendChild(label);
      const halfWidth = label.offsetWidth / 2;
      const maxX = Math.max(8 + halfWidth, stageRect.width - halfWidth - 8);
      const maxY = Math.max(0, stageRect.height - label.offsetHeight - 8);
      const centerX = clamp(rect.left + rect.width / 2 - stageRect.left, 8 + halfWidth, maxX);
      const preferredTop = clamp(rect.top - stageRect.top - label.offsetHeight - 8, 8, maxY);
      const belowTop = clamp(rect.bottom - stageRect.top + 8, 8, maxY);
      const candidates = [preferredTop, belowTop];
      for (let offset = 1; offset <= 6; offset++) {
        candidates.push(clamp(preferredTop - offset * (label.offsetHeight + 6), 8, maxY));
        candidates.push(clamp(belowTop + offset * (label.offsetHeight + 6), 8, maxY));
      }
      let top = candidates.find((candidateTop) => !overlaps({ left: centerX - halfWidth, right: centerX + halfWidth, top: candidateTop, bottom: candidateTop + label.offsetHeight }));
      if (top === undefined) {
        label.remove();
        return;
      }
      label.style.left = centerX + 'px';
      label.style.top = top + 'px';
      placed.push({ left: centerX - halfWidth, right: centerX + halfWidth, top, bottom: top + label.offsetHeight });
      shown.push(target);
    });
    if (!shown.length) {
      clearReveals(root);
      return [];
    }
    root.dataset.cfxRevealCount = String(shown.length);
    if (source) root.dataset.cfxRevealSource = source;
    else root.removeAttribute('data-cfx-reveal-source');
    if (emit !== false) emitHostEvent(root, 'cfxreveal', { target: shown[0], targets: shown, count: shown.length, source: source || '' });
    if (sync !== false) emitSync(root, { action: 'reveal', target: shown[0], targets: shown, count: shown.length, source: source || '' });
    return shown;
  };
  const revealTargets = (root, targets, emit, sync, source) => {
    if (!targets || !targets.length) return [];
    const nodes = [];
    const seen = new Set();
    targets.forEach((target) => {
      interactiveTargets(root).forEach((node) => {
        if (!matchesTargetIdentity(node, target)) return;
        const key = targetKey(targetIdentity(node));
        if (!key || seen.has(key)) return;
        seen.add(key);
        nodes.push(node);
      });
    });
    return revealNodes(root, nodes, emit, sync, source);
  };
  const setHover = (root, node, emit, sync, mode) => {
    const target = targetIdentity(node);
    clearHover(root, false, false, true);
    applyHoverByTarget(root, target, mode);
    root.dataset.cfxHoverKey = targetKey(target);
    recordFocusTrail(root, target, emit, sync);
    revealNodes(root, [node], emit, sync, 'hover');
    if (emit !== false || sync !== false) delete root._cfxCrosshairMode;
    if (emit !== false) emitHostEvent(root, 'cfxhover', { label: text(node), target });
    if (sync !== false) emitSync(root, { action: 'hover', label: text(node), target });
  };
  const hideCrosshair = (root, crosshair) => {
    if (crosshair) crosshair.hidden = true;
    root.removeAttribute('data-cfx-crosshair');
    delete root._cfxCrosshairMode;
  };
  // A producer's coordinate contract controls inferred geometry; native painted targets always retain their identity.
  const usesPolarCoordinates = (node) => !!node.closest('[data-cfx-coordinate-system="polar"]');
  const usesCartesianCoordinates = (node) => node.closest('[data-cfx-coordinate-system]')?.dataset.cfxCoordinateSystem === 'cartesian';
  const inferredPlotSurfaceRoles = new Set(['background', 'frame-card', 'frame-card-shadow', 'content-surface',
    'grid-x', 'grid-y', 'axis-x', 'axis-y', 'axis-secondary-y']);
  // Retain native summaries separately: an Exact line hit is not an inferred observation or crosshair.
  const pointerCandidates = (root, event, searchNearest, nativeTarget = event.target, withinSeries = null) => {
    const stage = root.querySelector('.cfx-stage');
    if (!stage) return null;
    const stageRect = stage.getBoundingClientRect();
    if (event.clientX < stageRect.left || event.clientX > stageRect.right || event.clientY < stageRect.top || event.clientY > stageRect.bottom) return null;
    const styles = new Map();
    let hit = pointLabelTarget(root, nativeTarget) || (nativeTarget instanceof Element ? nativeTarget.closest(targetSelector) : null);
    // Native captions can repeat their owner's facts without becoming separate interactive targets.
    // Stop at the nearest independent identity before checking paint; unavailable marks never fall through.
    while (hit && root.contains(hit) && !isInteractiveTarget(hit)) {
      if (hit.dataset.cfxRole === 'legend-entry-omitted') { hit = null; break; }
      hit = hit.parentElement && hit.parentElement.closest(targetSelector);
    }
    let native = null;
    if (hit && root.contains(hit) && pointerTargetPaint(hit, styles)) {
      const box = hit.getBoundingClientRect();
      const summary = renderedTargetKind(hit) === 'series' && !hit.hasAttribute('data-cfx-point') && !hit.hasAttribute('data-cfx-value');
      native = { node: hit, x: usesPolarCoordinates(hit) || summary ? event.clientX : box.left + box.width / 2,
        y: usesPolarCoordinates(hit) || summary ? event.clientY : box.top + box.height / 2, distance: 0, exact: true, summary };
      if (!summary) return { native, observation: hit.hasAttribute('data-cfx-point') && usesCartesianCoordinates(hit) ? native : null };
    }
    // Sparse-plot inference starts on the native stage surface, never on an unrelated host veil.
    // Explicit native targets and mapped captions retain their own acquisition contract.
    const svg = stage.querySelector('svg');
    const plotSurface = nativeTarget === stage || nativeTarget === svg || nativeTarget instanceof SVGElement
      && svg?.contains(nativeTarget) && !nativeTarget.closest('foreignObject')
      && (isPassiveTarget(nativeTarget) || inferredPlotSurfaceRoles.has(nativeTarget.dataset.cfxRole));
    if (!searchNearest || !native && !plotSurface) return { native, observation: null };
    let best = null;
    root.querySelectorAll('[data-cfx-point]').forEach((node) => {
      if (!isInteractiveTarget(node) || !usesCartesianCoordinates(node) || !pointerTargetPaint(node, styles)
        || withinSeries && !matchesLocalSeriesTarget(node, withinSeries)) return;
      const box = node.getBoundingClientRect();
      if (!box.width && !box.height) return;
      const x = box.left + box.width / 2, y = box.top + box.height / 2;
      const distance = Math.hypot(x - event.clientX, y - event.clientY);
      const rank = withinSeries ? Math.abs(root._cfxDataProjection?.horizontal ? y - event.clientY : x - event.clientX) : distance;
      if (!best || rank < best.rank || rank === best.rank && distance < best.distance) best = { node, x, y, distance, rank, exact: false };
    });
    return { native, observation: best };
  };
  const pointerActivationTarget = (root, node, event) => {
    if (isDataTarget(node) || renderedTargetKind(node) !== 'series' || !usesCartesianCoordinates(node)
      || !(event instanceof MouseEvent) || event.detail <= 0) return node;
    // Native fill paint owns the click, including when it covers another series' hit regions.
    // Resolve a real observation at the closest category coordinate within that topmost series.
    const series = seriesTarget(node), candidates = pointerCandidates(root, event, true, event.target, series);
    const fill = event.target instanceof Element ? event.target.closest('[data-cfx-role]') : null;
    return candidates?.native?.summary && matchesLocalSeriesTarget(candidates.native.node, series)
      && candidates.observation && isDataTarget(candidates.observation.node)
      && fill && cartesianFillObservation(fill, candidates.observation.node) ? candidates.observation.node : node;
  };
  const tooltipAcquiresPoint = (root, point) => {
    if (!point || !hasFeature(root, 'Tooltips')) return false;
    if (point.exact) return true;
    const range = root.dataset.cfxTooltipRange || 'distance';
    return range === 'nearest' || range === 'distance' && point.distance <= Number(root.dataset.cfxTooltipDistance ?? 120);
  };
  const acquiredTooltipPoint = (root, candidates) => candidates && (tooltipAcquiresPoint(root, candidates.observation)
    ? candidates.observation : tooltipAcquiresPoint(root, candidates.native) ? candidates.native : null);
  const showCrosshair = (root, crosshair, point, event, nativeTarget = event.target) => {
    if (!crosshair || !point) return;
    const stage = root.querySelector('.cfx-stage');
    if (!stage) return;
    const bounds = visibleCartesianPlot(root, stage), local = stagePoint(stage, point);
    if (!bounds || !Number.isFinite(local.x) || !Number.isFinite(local.y)
      || local.x < bounds.left - bounds.precisionX || local.x > bounds.left + bounds.width + bounds.precisionX
      || local.y < bounds.top - bounds.precisionY || local.y > bounds.top + bounds.height + bounds.precisionY) {
      hideCrosshair(root, crosshair);
      return;
    }
    crosshair.style.left = bounds.left + 'px';
    crosshair.style.top = bounds.top + 'px';
    crosshair.style.width = bounds.width + 'px';
    crosshair.style.height = bounds.height + 'px';
    // Only SVG-sized rounding excursions reach here. Keep their anchors on the actual visible plot.
    const x = clamp(local.x - bounds.left, 0, bounds.width), y = clamp(local.y - bounds.top, 0, bounds.height);
    crosshair.style.setProperty('--cfx-crosshair-x', x + 'px');
    crosshair.style.setProperty('--cfx-crosshair-y', y + 'px');
    crosshair.hidden = false;
    const label = crosshair.querySelector('[data-cfx-crosshair-label]');
    if (label) {
      label.textContent = text(point.node);
      // Measure the constrained label, including its border/padding, before placing it in a narrow plot.
      label.style.left = clamp(x + 10, 0, Math.max(0, bounds.width - label.offsetWidth)) + 'px';
      label.style.top = clamp(y - label.offsetHeight - 6, 0, Math.max(0, bounds.height - label.offsetHeight)) + 'px';
    }
    const target = targetIdentity(point.node);
    const key = targetKey(target), mode = crosshairHoverMode(event, point.node, nativeTarget);
    // A native summary can stay unchanged while its independently inferred guide advances.
    const changed = root.dataset.cfxCrosshair !== key || root._cfxCrosshairMode !== mode;
    root.dataset.cfxCrosshair = key;
    root._cfxCrosshairMode = mode;
    if (changed) {
      emitHostEvent(root, 'cfxcrosshair', { label: text(point.node), target, x: event.clientX, y: event.clientY });
      emitSync(root, { action: 'crosshair', label: text(point.node), target, mode });
    }
  };
  // A pointer resting on a mark of the nearest point's series emphasizes that series; anywhere else on the
  // plot the crosshair is a shared readout and no series recedes.
  const crosshairHoverMode = (event, node, nativeTarget = event && event.target) => {
    const hit = nativeTarget instanceof Element ? nativeTarget.closest('[data-cfx-series]') : null;
    return hit && (hit.dataset || {}).cfxSeries === (node.dataset || {}).cfxSeries ? 'series' : 'shared';
  };
  const updateNearestPoint = (root, crosshair, tip, event, nativeTarget = event.target) => {
    if (event instanceof PointerEvent && pointerReadoutSuspended(root)) return;
    const guideEnabled = hasFeature(root, 'Crosshair');
    if (!guideEnabled && !hasFeature(root, 'Tooltips')) return;
    const legend = nativeTarget instanceof Element && nativeTarget.closest('[data-cfx-role="legend-item"]');
    if (legend && root.contains(legend)) {
      // A reset can suppress native entry before this fresh, coalesced move resumes acquisition.
      // Keep the legend's own live painted summary; never search the plot for a replacement.
      hideCrosshair(root, crosshair);
      const available = hasFeature(root, 'Tooltips') ? currentPointerTooltip(root, event) === legend : pointerTargetPaint(legend);
      if (available) {
        const key = targetKey(targetIdentity(legend));
        if (root.dataset.cfxHoverKey !== key) setHover(root, legend, true, true);
        showTip(root, tip, legend, event);
      } else {
        clearHover(root, true, true);
        hideTip(root, tip, false);
      }
      return;
    }
    const searchNearest = guideEnabled || root.dataset.cfxTooltipRange !== 'exact';
    const candidates = pointerCandidates(root, event, searchNearest, nativeTarget);
    const observation = candidates && candidates.observation;
    // Nearest/bounded acquisition can refine a line summary to a real observation; Exact retains the summary.
    const tooltipPoint = acquiredTooltipPoint(root, candidates);
    const guidePoint = guideEnabled && observation && (observation.exact || observation.distance <= 120) ? observation : null;
    if (!tooltipPoint && !guidePoint) {
      hideCrosshair(root, crosshair);
      clearHover(root, true, true);
      hideTip(root, tip, false);
      return;
    }
    const point = tooltipPoint || guidePoint;
    const key = targetKey(targetIdentity(point.node));
    const mode = hoverModeForTarget(root, point.node, point.node.hasAttribute('data-cfx-point') && usesCartesianCoordinates(point.node)
      ? crosshairHoverMode(event, point.node, nativeTarget) : 'series');
    const changed = root.dataset.cfxHoverKey !== key || root.dataset.cfxHoverMode !== mode;
    if (changed) setHover(root, point.node, true, true, mode);
    if (guidePoint) showCrosshair(root, crosshair, guidePoint, event, nativeTarget);
    else hideCrosshair(root, crosshair);
    // Recheck paint on each event: host CSS may change while the semantic target remains the same.
    if (tooltipPoint) showTip(root, tip, tooltipPoint.node, event);
    else hideTip(root, tip, false);
  };
  const readJson = (value, fallback) => {
    try {
      const parsed = JSON.parse(value || '');
      return parsed === null || parsed === undefined ? fallback : parsed;
    } catch {
      return fallback;
    }
  };
  const scenarioUrlParam = (root, name) => {
    if (root.dataset.cfxDeepLinkState !== 'true') return '';
    try {
      const params = new URL(window.location.href).searchParams;
      return params.get(scenarioUrlKey(root, name)) || params.get(name) || '';
    } catch { return ''; }
  };
  const scenarioUrlKey = (root, name) => {
    const chartId = (root.dataset.cfxChartId || 'chart').replace(/[^a-z0-9_.-]+/gi, '-').replace(/^-+|-+$/g, '') || 'chart';
    return 'cfx-' + chartId + '-' + name;
  };
  const syncScenarioUrl = (root, scenarioId, stepIndex) => {
    if (root.dataset.cfxDeepLinkState !== 'true' || !window.history || !window.history.replaceState) return;
    try {
      const url = new URL(window.location.href);
      const scenarioKey = scenarioUrlKey(root, 'scenario');
      const stepKey = scenarioUrlKey(root, 'scenarioStep');
      scenarioId ? url.searchParams.set(scenarioKey, scenarioId) : url.searchParams.delete(scenarioKey);
      stepIndex === undefined || stepIndex === null || stepIndex === '' ? url.searchParams.delete(stepKey) : url.searchParams.set(stepKey, stepIndex);
      url.searchParams.delete('scenario');
      url.searchParams.delete('scenarioStep');
      window.history.replaceState(window.history.state, '', url);
    } catch { }
  };
  const scenarioButton = (root, scenarioId) => Array.from(root.querySelectorAll('[data-cfx-scenario]')).find((button) => (button.dataset.cfxScenario || '') === scenarioId) || null;
  const scenarioRoute = (root, scenarioId) => {
    const button = scenarioButton(root, scenarioId);
    if (!button || !scenarioId) return null;
    return {
      id: scenarioId,
      label: button.dataset.cfxScenarioLabel || button.textContent.trim(),
      description: button.dataset.cfxScenarioDescription || '',
      color: button.dataset.cfxScenarioColor || '',
      steps: readJson(button.dataset.cfxScenarioSteps || '[]', []),
      metadata: readJson(button.dataset.cfxScenarioMetadata || '{}', {}),
      playbackDelayMilliseconds: Number(button.dataset.cfxScenarioPlaybackDelay || '900'),
      loopPlayback: button.dataset.cfxScenarioLoop === 'true',
      autoPlay: button.dataset.cfxScenarioAutoplay === 'true',
      focusMode: button.dataset.cfxScenarioFocusMode || 'highlight'
    };
  };
  const scenarioTargetMatches = (node, step) => {
    const data = node.dataset || {};
    const kind = step.targetKind || '';
    const target = step.targetId || '';
    if (renderedTargetKind(node) === kind && renderedTargetId(node, kind) === target) return true;
    if (kind === 'series') return seriesKey(node) === target;
    if (kind === 'point' && data.cfxPoint === target) return true;
    if (kind === 'point' && data.cfxSeries !== undefined && data.cfxPoint !== undefined && (data.cfxSeries + ':' + data.cfxPoint === target || data.cfxSeries + '.' + data.cfxPoint === target)) return true;
    if (kind === 'annotation' && (data.cfxRole || '').indexOf('annotation') === 0 && (data.cfxLabel === target || data.cfxKind === target || data.cfxValue === target || data.cfxId === target || node.id === target)) return true;
    if (kind === 'element' && (data.cfxId === target || node.id === target)) return true;
    if (data.cfxRole === kind && (data.cfxLabel === target || data.cfxText === target || data.cfxValue === target || data.cfxY === target)) return true;
    return false;
  };
  const scenarioTargetCandidates = (root, route) => {
    const visualKinds = new Set(['series', 'point', 'annotation', 'region', 'node', 'link', 'legend']);
    const roleKinds = new Set((route ? route.steps : [])
      .map((step) => step.targetKind || '')
      .filter((kind) => kind && kind !== 'element' && !visualKinds.has(kind)));
    const candidates = new Set(Array.from(root.querySelectorAll('.cfx-interactive-region,[data-cfx-target-kind],[data-cfx-label],[data-cfx-role],[data-cfx-series],[data-cfx-point],[data-cfx-id]'))
      .filter((node) => visualKinds.has(renderedTargetKind(node)) || roleKinds.has((node.dataset || {}).cfxRole || '')));
    const elementIds = new Set((route ? route.steps : [])
      .filter((step) => (step.targetKind || '') === 'element' && step.targetId)
      .map((step) => step.targetId));
    if (elementIds.size) {
      root.querySelectorAll('[id]').forEach((node) => {
        if (!elementIds.has(node.id) || node.closest('defs')) return;
        if (node.querySelector('.cfx-interactive-region,[data-cfx-target-kind],[data-cfx-label],[data-cfx-role],[data-cfx-series],[data-cfx-point],[data-cfx-id]')) return;
        candidates.add(node);
      });
    }
    return Array.from(candidates).filter((node) => !isPassiveTarget(node));
  };
  const scenarioTargets = (root, route, stepIndex) => {
    const matches = new Set();
    if (!route) return matches;
    const steps = stepIndex === undefined || stepIndex === null ? route.steps : route.steps.filter((_, index) => index === Number(stepIndex));
    scenarioTargetCandidates(root, route).forEach((node) => {
      if (steps.some((step) => scenarioTargetMatches(node, step))) matches.add(node);
    });
    return matches;
  };
  const scenarioRevealTargets = (route, stepIndex, targets) => {
    const step = route && route.steps ? route.steps[Number(stepIndex)] : null;
    // A series step already highlights its complete visual family. Revealing every
    // matching mark adds duplicate badges that obscure the chart instead of
    // clarifying the route; precise point/annotation steps still reveal labels.
    if (step && (step.targetKind || '') === 'series') return [];
    return Array.from(targets);
  };
  const renderScenarioPanel = (root, route) => {
    const panel = root.querySelector('[data-cfx-scenario-panel]');
    if (!panel) return;
    const title = panel.querySelector('[data-cfx-scenario-panel-title]');
    const meta = panel.querySelector('[data-cfx-scenario-panel-meta]');
    const steps = panel.querySelector('[data-cfx-scenario-panel-steps]');
    if (!route) {
      panel.removeAttribute('data-cfx-panel-active-scenario');
      panel.style.removeProperty('--cfx-scenario-color');
      if (title) title.textContent = 'All';
      if (meta) meta.textContent = 'All chart data visible';
      if (steps) steps.replaceChildren();
      return;
    }
    panel.setAttribute('data-cfx-panel-active-scenario', route.id);
    if (route.color) panel.style.setProperty('--cfx-scenario-color', route.color);
    else panel.style.removeProperty('--cfx-scenario-color');
    if (title) title.textContent = route.label || route.id;
    if (meta) {
      const pairs = Object.entries(route.metadata || {}).slice(0, 2).map(([key, value]) => key + ': ' + value);
      meta.textContent = [route.description, route.steps.length ? route.steps.length + ' steps' : '', ...pairs].filter(Boolean).join(' / ');
    }
    if (steps) {
      steps.replaceChildren();
      route.steps.forEach((step, index) => {
        const item = document.createElement('li');
        item.setAttribute('data-cfx-scenario-step-index', String(index + 1));
        item.setAttribute('data-cfx-scenario-target-kind', step.targetKind || '');
        item.setAttribute('data-cfx-scenario-target-id', step.targetId || '');
        if (step.durationMilliseconds) item.setAttribute('data-cfx-scenario-step-duration', String(step.durationMilliseconds));
        item.setAttribute('role', 'button');
        item.setAttribute('tabindex', '0');
        item.textContent = step.label || step.targetId || '';
        steps.appendChild(item);
      });
    }
  };
  const updateScenarioProgress = (root, route, stepIndex) => {
    const progress = root.querySelector('[data-cfx-scenario-progress]');
    const count = route && route.steps ? route.steps.length : 0;
    const hasStep = stepIndex !== undefined && stepIndex !== null && stepIndex !== '';
    const current = count && hasStep ? clamp(Number(stepIndex) + 1, 0, count) : 0;
    const percent = count ? Math.round((current / count) * 100) : 0;
    const label = 'Step ' + current + ' / ' + count;
    if (progress) {
      progress.hidden = !count;
      progress.style.setProperty('--cfx-scenario-progress', percent + '%');
      progress.setAttribute('aria-valuemax', String(count));
      progress.setAttribute('aria-valuenow', String(current));
      progress.setAttribute('aria-valuetext', label);
      const textNode = progress.querySelector('[data-cfx-scenario-progress-text]');
      if (textNode) textNode.textContent = label;
    }
    const scrubber = root.querySelector('[data-cfx-scenario-scrubber]');
    if (scrubber) {
      scrubber.disabled = !count;
      scrubber.max = String(Math.max(1, count));
      scrubber.value = String(Math.max(1, current));
      scrubber.setAttribute('aria-valuetext', label);
    }
    if (count) root.dataset.cfxScenarioProgress = current + '/' + count;
    else root.removeAttribute('data-cfx-scenario-progress');
    return { current, count, percent, label };
  };
  const clearScenarioStep = (root) => {
    const route = scenarioRoute(root, root.dataset.cfxActiveScenario || '');
    root.removeAttribute('data-cfx-active-scenario-step');
    root.querySelectorAll('.cfx-scenario-step-active,.cfx-scenario-step-muted').forEach((node) => node.classList.remove('cfx-scenario-step-active', 'cfx-scenario-step-muted'));
    root.querySelectorAll('[data-cfx-scenario-step-index]').forEach((node) => node.removeAttribute('aria-current'));
    updateScenarioProgress(root, route, null);
  };
  const setScenarioStep = (root, stepIndex, emit, sync) => {
    const route = scenarioRoute(root, root.dataset.cfxActiveScenario || '');
    if (!route || !route.steps.length) return;
    const index = clamp(Number(stepIndex) || 0, 0, route.steps.length - 1);
    const targets = scenarioTargets(root, route, index);
    root.dataset.cfxActiveScenarioStep = String(index);
    root.querySelectorAll('.cfx-scenario-active').forEach((node) => {
      const active = targets.has(node);
      node.classList.toggle('cfx-scenario-step-active', active);
      node.classList.toggle('cfx-scenario-step-muted', !active && route.focusMode === 'spotlight');
    });
    root.querySelectorAll('[data-cfx-scenario-step-index]').forEach((node) => {
      node.getAttribute('data-cfx-scenario-step-index') === String(index + 1) ? node.setAttribute('aria-current', 'step') : node.removeAttribute('aria-current');
    });
    const progress = updateScenarioProgress(root, route, index);
    const trail = Array.from(targets).map((node) => targetIdentity(node)).slice(0, 5);
    if (trail.length && hasFeature(root, 'FocusTrail')) {
      applyFocusTrail(root, trail);
      if (emit !== false) emitHostEvent(root, 'cfxtrail', { target: trail[0], trail, count: trail.length, source: 'scenario-step' });
      if (sync !== false) emitSync(root, { action: 'trail', target: trail[0], trail, count: trail.length, source: 'scenario-step' });
    }
    revealNodes(root, scenarioRevealTargets(route, index, targets), emit, sync, 'scenario-step');
    syncScenarioUrl(root, route.id, index);
    if (emit !== false) emitHostEvent(root, 'cfxscenariostep', { scenarioId: route.id, index, step: route.steps[index], progress });
    if (sync !== false) emitSync(root, { action: 'scenario-step', scenarioId: route.id, index, progress });
  };
  const setScenario = (root, scenarioId, emit, sync) => {
    const route = scenarioRoute(root, scenarioId);
    const previousPlayback = root.dataset.cfxScenarioPlayback || '';
    stopScenarioPlayback(root, 'idle', emit !== false && previousPlayback && previousPlayback !== 'idle');
    clearScenarioStep(root);
    clearReveals(root);
    root.querySelectorAll('.cfx-scenario-active,.cfx-scenario-muted').forEach((node) => node.classList.remove('cfx-scenario-active', 'cfx-scenario-muted'));
    root.querySelectorAll('[data-cfx-scenario]').forEach((button) => button.setAttribute('aria-pressed', (button.dataset.cfxScenario || '') === (route ? route.id : '') ? 'true' : 'false'));
    if (!route) {
      root.removeAttribute('data-cfx-active-scenario');
      renderScenarioPanel(root, null);
      updateScenarioProgress(root, null, null);
      syncScenarioUrl(root, '', '');
      if (emit !== false) emitHostEvent(root, 'cfxscenarioclear', {});
      if (sync !== false) emitSync(root, { action: 'scenario', scenarioId: '' });
      return;
    }
    root.dataset.cfxActiveScenario = route.id;
    if (route.color) root.style.setProperty('--cfx-scenario-color', route.color);
    else root.style.removeProperty('--cfx-scenario-color');
    const targets = scenarioTargets(root, route, null);
    scenarioTargetCandidates(root, route).forEach((node) => {
      const active = targets.has(node);
      node.classList.toggle('cfx-scenario-active', active);
      node.classList.toggle('cfx-scenario-muted', !active && route.focusMode === 'spotlight');
    });
    renderScenarioPanel(root, route);
    updateScenarioProgress(root, route, null);
    syncScenarioUrl(root, route.id, '');
    if (emit !== false) emitHostEvent(root, 'cfxscenario', { scenarioId: route.id, label: route.label, description: route.description, color: route.color, steps: route.steps, metadata: route.metadata });
    if (sync !== false) emitSync(root, { action: 'scenario', scenarioId: route.id });
  };
  const stepScenario = (root, delta) => {
    const route = scenarioRoute(root, root.dataset.cfxActiveScenario || '');
    if (!route || !route.steps.length) return;
    const current = Number(root.dataset.cfxActiveScenarioStep || (delta > 0 ? '-1' : route.steps.length));
    setScenarioStep(root, current + delta, true, true);
  };
  const scenarioPlaybackDelay = (root, route, stepIndex) => {
    const step = route && route.steps && Number.isFinite(Number(stepIndex)) ? route.steps[Number(stepIndex)] : null;
    const delay = Number((step && step.durationMilliseconds) || (route && route.playbackDelayMilliseconds) || root.dataset.cfxScenarioPlaybackDelay || '900');
    return Number.isFinite(delay) && delay >= 200 ? delay : 900;
  };
  const setScenarioPlaybackState = (root, state, route, emit) => {
    const normalized = state || 'idle';
    root.dataset.cfxScenarioPlayback = normalized;
    root.querySelectorAll('[data-cfx-scenario-step-control="play"]').forEach((button) => {
      const playing = normalized === 'playing';
      button.setAttribute('aria-pressed', playing ? 'true' : 'false');
      button.textContent = playing ? (button.dataset.cfxScenarioPauseLabel || 'Pause') : (button.dataset.cfxScenarioPlayLabel || 'Play');
      button.title = playing ? 'Pause scenario' : 'Play scenario';
    });
    if (emit !== false) {
      emitHostEvent(root, 'cfxscenarioplayback', {
        state: normalized,
        scenarioId: route ? route.id : (root.dataset.cfxActiveScenario || ''),
        stepIndex: root.dataset.cfxActiveScenarioStep || '',
        progress: root.dataset.cfxScenarioProgress || '',
        delay: scenarioPlaybackDelay(root, route, root.dataset.cfxActiveScenarioStep)
      });
    }
  };
  const stopScenarioPlayback = (root, state, emit) => {
    if (root._cfxScenarioPlayback) window.clearTimeout(root._cfxScenarioPlayback);
    root._cfxScenarioPlayback = null;
    setScenarioPlaybackState(root, state || 'idle', scenarioRoute(root, root.dataset.cfxActiveScenario || ''), emit);
  };
  const playScenario = (root, emit) => {
    const route = scenarioRoute(root, root.dataset.cfxActiveScenario || '');
    if (!route || !route.steps.length) return;
    if (root._cfxScenarioPlayback) {
      stopScenarioPlayback(root, 'paused', emit);
      return;
    }
    let index = Number(root.dataset.cfxActiveScenarioStep || '-1') + 1;
    if (!Number.isFinite(index) || index >= route.steps.length) index = 0;
    startScenarioPlayback(root, route, index, emit, true);
  };
  const startScenarioPlayback = (root, route, stepIndex, emit, advanceNow) => {
    if (!route || !route.steps.length) return;
    setScenarioPlaybackState(root, 'playing', route, emit);
    let index = Number(stepIndex);
    if (!Number.isFinite(index) || index < 0) index = 0;
    if (index >= route.steps.length && route.loopPlayback) index = 0;
    const advance = () => {
      if (!root.isConnected || chartLifetimes.get(root)?.disposed !== false) { stopScenarioPlayback(root, 'paused', false); return; }
      if (index >= route.steps.length) {
        if (route.loopPlayback) index = 0;
        else {
          stopScenarioPlayback(root, 'finished', emit);
          return;
        }
      }
      const current = index++;
      setScenarioStep(root, current, true, true);
      root._cfxScenarioPlayback = window.setTimeout(advance, scenarioPlaybackDelay(root, route, current));
    };
    if (advanceNow !== false) advance();
    else root._cfxScenarioPlayback = window.setTimeout(advance, scenarioPlaybackDelay(root, route, Number(root.dataset.cfxActiveScenarioStep || '0')));
  };
  const copyScenarioLink = (root) => {
    syncScenarioUrl(root, root.dataset.cfxActiveScenario || '', root.dataset.cfxActiveScenarioStep || '');
    const url = window.location.href;
    if (navigator.clipboard && navigator.clipboard.writeText) navigator.clipboard.writeText(url).catch(() => {});
    emitHostEvent(root, 'cfxscenariolink', { url });
  };
  const bindChart = (root) => {
    root.dataset.cfxRuntimeBound = 'true';
    const tip = root.querySelector('.cfx-tooltip');
    const stage = root.querySelector('.cfx-stage');
    const brush = root.querySelector('.cfx-brush-box');
    const crosshair = root.querySelector('.cfx-crosshair');
    const compareTray = root.querySelector('[data-cfx-compare-tray]');
    if (!tip) return;
    listenChart(root, root, 'keydown', event => {
      if (event.key === 'Escape') cancelXViewportRequest(chartLifetimes.get(root));
    }, true);
    applyRenderedTargetContract(root);
    bindHostCommands(root);
    prepareRootBranchHighlights(root);
    if (root._cfxBranchHighlights) listenChart(root, root, 'keydown', event => {
      if (event.key !== 'Escape') return;
      event.preventDefault();
      event.stopPropagation();
      clearHover(root, true, true);
      hideCrosshair(root, crosshair);
      hideTip(root, tip, true);
    });
    applyViewport(root, getState(root));
    renderCompare(root);
    syncSelectionAppearance(root);
    listenChart(root, root, 'keydown', event => {
      if (event.key !== 'Escape') return;
      clearBrushCommand(root, { emit: true, sync: true });
      if (!hasFeature(root, 'Selection') || !selectedTargets(root).length) return;
      clearSelections(root);
      publishSelectionChange(root, 'clear', true);
      publishCompare(root, false);
    }, true);
    if (compareTray) listenChart(root, compareTray, 'pointerdown', (event) => event.stopPropagation());
    if (hasFeature(root, 'StateBookmarks')) {
      listenChart(root, root, 'cfx-capture-state', (event) => {
        const detail = event.detail || {};
        publishInteractionState(root, detail.source || 'host', detail.sync);
      });
      listenChart(root, root, 'cfx-apply-state', (event) => {
        const detail = event.detail || {};
        applyInteractionState(root, detail.snapshot || detail.state || detail, true, detail.sync);
      });
    }
    prepareKeyboardNavigation(root);
    bindStageLayout(root, stage, crosshair);
    const targets = interactiveTargets(root);
    targets.forEach((node) => {
      const focusNode = targetFocusNode(node);
      const labels = pointLabelSurfaces(root, node);
      [node, ...labels].forEach(surface => {
        listenChart(root, surface, 'pointerenter', (event) => {
          if (pointerReadoutSuspended(root) || !pointerTargetPaint(node)) return;
          if (!ownsTooltipPointer(root, event)) hideCrosshair(root, crosshair);
          setHover(root, node, true, true);
          showTip(root, tip, node, event);
        });
        listenChart(root, surface, 'pointermove', (event) => {
          if (ownsTooltipPointer(root, event)) moveTip(tip, event, node);
        });
        listenChart(root, surface, 'pointerleave', (event) => {
          if (!ownsTooltipPointer(root, event)) return;
          clearHover(root, true, true);
          if (!retainPointerTip(root, event)) hideTip(root, tip, false);
        });
      });
      // Preserve native link focus; disabled adapter navigation must not create implicit SVG tab stops.
      if (hasFeature(root, 'KeyboardNavigation') || focusNode.matches('a[href]')) {
        let pointerFocus = false;
        // Touch compatibility mouse events can focus the mark after pointerup has already cleared the press.
        ['pointerdown', 'mousedown'].forEach(type => listenChart(root, focusNode, type, (event) => { pointerFocus = event.button === 0; }));
        ['pointerup', 'pointercancel', 'pointerleave', 'mouseup'].forEach(type => listenChart(root, focusNode, type, () => { pointerFocus = false; }));
        listenChart(root, focusNode, 'focus', (event) => {
          refreshKeyboardNavigation(root, node);
          // Revealing a partly visible mark between pointer down and up retargets its native click.
          const keyboardFocus = focusNode.matches(':focus-visible');
          if (hasFeature(root, 'KeyboardNavigation') && !pointerFocus && keyboardFocus) scrollKeyboardTargetIntoView(root, focusNode);
          pointerFocus = false;
          if (!keyboardFocus || root._cfxRecoveringFocus) return;
          resumePointerReadout(root);
          if (!tooltipReadoutAvailable(node, event)) { clearHover(root, true, true); hideTip(root, tip, false); return; }
          setHover(root, node, true, true);
          showTip(root, tip, node, event);
        });
        listenChart(root, focusNode, 'blur', () => {
          pointerFocus = false;
          clearHover(root, true, true);
          hideTip(root, tip, false);
        });
      }
      const activateTarget = (event) => {
        event.stopPropagation();
        if (event instanceof KeyboardEvent) resumePointerReadout(root);
        const activated = pointerActivationTarget(root, node, event);
        if (!tooltipReadoutAvailable(activated, event)) return;
        if ((activated.dataset ? activated.dataset.cfxRole : '') === 'legend-item') {
          if (event.shiftKey) toggleSeriesFocus(root, activated, true, true);
          else toggleSeries(root, activated);
        }
        else {
          toggleSelection(root, activated, event);
          pinTip(root, tip, activated, event);
        }
      };
      listenChart(root, focusNode, 'click', activateTarget);
      labels.forEach(label => listenChart(root, label, 'click', activateTarget));
      if (hasFeature(root, 'KeyboardNavigation')) listenChart(root, focusNode, 'keydown', (event) => {
        if (!hasFeature(root, 'KeyboardNavigation') || event.defaultPrevented || event.target !== focusNode) return;
        if ((node.dataset ? node.dataset.cfxRole : '') === 'legend-item' && event.key.toLowerCase() === 'i') {
          event.preventDefault();
          event.stopPropagation();
          toggleSeriesFocus(root, node, true, true);
          return;
        }
        if (focusAdjacentTarget(root, node, event.key)) {
          event.preventDefault();
          event.stopPropagation();
          return;
        }
        if (event.key !== 'Enter' && event.key !== ' ') return;
        // Enter on a link must retain native navigation. Space selects the cell without following the link.
        if (event.key === 'Enter' && focusNode.matches('a[href]')) return;
        event.preventDefault();
        event.stopPropagation();
        // Keep the keyboard event: a synthetic click's finite (0,0) would replace the focused mark's position.
        activateTarget(event);
      });
    });
    root.querySelectorAll('[data-cfx-zoom]').forEach((button) => {
      listenChart(root, button, 'click', () => zoomBy(root, button.dataset.cfxZoom === 'in' ? 1.25 : 0.8));
    });
    root.querySelectorAll('[data-cfx-mode-button]').forEach((button) => {
      listenChart(root, button, 'click', () => setMode(root, button.dataset.cfxModeButton || ''));
    });
    root.querySelectorAll('[data-cfx-export]').forEach((button) => {
      listenChart(root, button, 'click', () => {
        if (button.dataset.cfxExport === 'png') exportPng(root);
        else exportSvg(root);
      });
    });
    root.querySelectorAll('[data-cfx-scenario]').forEach((button) => {
      listenChart(root, button, 'click', () => setScenario(root, button.dataset.cfxScenario || '', true));
    });
    if (hasFeature(root, 'Scenarios')) {
      listenChart(root, root, 'cfx-set-scenario', (event) => {
        const detail = event.detail || {};
        setScenario(root, detail.scenarioId || '', true, true);
      });
      listenChart(root, root, 'cfx-clear-scenario', () => setScenario(root, '', true, true));
    }
    if (hasFeature(root, 'StepPlayback')) {
      root.querySelectorAll('[data-cfx-scenario-step-control]').forEach((button) => {
        listenChart(root, button, 'click', () => {
          const action = button.dataset.cfxScenarioStepControl || '';
          if (action === 'previous') stepScenario(root, -1);
          else if (action === 'next') stepScenario(root, 1);
          else if (action === 'play') playScenario(root);
          else if (action === 'reset') { stopScenarioPlayback(root, 'paused'); clearScenarioStep(root); clearReveals(root); }
          else if (action === 'link') copyScenarioLink(root);
        });
      });
      listenChart(root, root, 'cfx-set-scenario-step', (event) => {
        const detail = event.detail || {};
        if (detail.scenarioId && root.dataset.cfxActiveScenario !== detail.scenarioId) setScenario(root, detail.scenarioId, true, true);
        const index = detail.index !== undefined ? detail.index : detail.stepIndex;
        setScenarioStep(root, index, true, true);
      });
      listenChart(root, root, 'cfx-clear-scenario-step', () => {
        stopScenarioPlayback(root, 'paused');
        clearScenarioStep(root);
        clearReveals(root);
      });
      listenChart(root, root, 'cfx-play-scenario', (event) => {
        const detail = event.detail || {};
        if (detail.scenarioId && root.dataset.cfxActiveScenario !== detail.scenarioId) setScenario(root, detail.scenarioId, true, true);
        playScenario(root);
      });
      listenChart(root, root, 'cfx-pause-scenario', () => stopScenarioPlayback(root, 'paused'));
      const scrubber = root.querySelector('[data-cfx-scenario-scrubber]');
      if (scrubber) listenChart(root, scrubber, 'input', () => {
        stopScenarioPlayback(root, 'paused');
        setScenarioStep(root, Number(scrubber.value) - 1, true, true);
      });
      const stepsList = root.querySelector('[data-cfx-scenario-panel-steps]');
      if (stepsList) {
        const focusStep = (target) => {
          const item = target instanceof Element ? target.closest('[data-cfx-scenario-step-index]') : null;
          if (item && stepsList.contains(item)) setScenarioStep(root, Number(item.getAttribute('data-cfx-scenario-step-index')) - 1, true, true);
        };
        listenChart(root, stepsList, 'click', (event) => focusStep(event.target));
        listenChart(root, stepsList, 'keydown', (event) => {
          if (event.key !== 'Enter' && event.key !== ' ') return;
          event.preventDefault();
          focusStep(event.target);
        });
      }
    }
    if (hasFeature(root, 'Scenarios')) {
      const initialScenario = scenarioUrlParam(root, 'scenario') || root.dataset.cfxActiveScenario || '';
      const initialScenarioStep = scenarioUrlParam(root, 'scenarioStep');
      setScenario(root, initialScenario, false, false);
      if (hasFeature(root, 'StepPlayback') && initialScenarioStep) setScenarioStep(root, initialScenarioStep, false, false);
      const initialRoute = scenarioRoute(root, root.dataset.cfxActiveScenario || '');
      const reducedMotion = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
      if (hasFeature(root, 'StepPlayback') && initialRoute && initialRoute.autoPlay && !initialScenarioStep && !reducedMotion) startScenarioPlayback(root, initialRoute, 0, false, true);
    }
    if (stage) {
      listenChart(root, stage, 'pointerenter', (event) => {
        // A new contact can acquire sparse plot space before any native mark receives its own entry.
        if (!ownsTooltipPointer(root, event)) updateNearestPoint(root, crosshair, tip, event);
      });
      listenChart(root, stage, 'wheel', (event) => {
        if (!hasFeature(root, 'Zoom')) return;
        if (connectedXViewport(root) && !chartXProjection(root)) return;
        event.preventDefault();
        zoomBy(root, event.deltaY < 0 ? 1.08 : 0.92);
      }, { passive: false });
      let drag = null;
      let pointerFrame = 0, pendingPointerMove = null;
      const cancelPointerMove = () => {
        if (pointerFrame) cancelAnimationFrame(pointerFrame);
        pointerFrame = 0; pendingPointerMove = null;
      };
      // Tooltip cancellation owns queued hover work; an active gesture keeps its
      // provisional movement and still commits its final sample on pointerup.
      root._cfxCancelPointerHover = () => { if (!drag) cancelPointerMove(); };
      const applyPointerMove = (event, nativeTarget = event.target) => {
        if (!drag || drag.id !== event.pointerId) {
          if (!ownsTooltipPointer(root, event)) return;
          updateNearestPoint(root, crosshair, tip, event, nativeTarget);
          return;
        }
        if (drag.mode === 'native-pan') {
          const x = logicalXPointer(root, event);
          if (x !== null) drag.delta = x - drag.x;
        } else if (drag.mode === 'pan') {
          const point = stagePointer(stage, event);
          applyViewport(root, { zoom: getState(root).zoom, panX: drag.panX + point.x - drag.x, panY: drag.panY + point.y - drag.y });
        } else if (drag.mode === 'brush' && brush) {
          setBrushBox(root, stage, brush, drag, brushPointer(root, stage, event));
        }
      };
      // Keep the last sample in each display frame; pointerup commits its final position
      // synchronously, so coalescing cannot omit the last drag segment or selection.
      root._cfxCancelBrushDrag = () => {
        if (drag?.mode !== 'brush') return false;
        cancelPointerMove();
        if (stage.hasPointerCapture(drag.id)) stage.releasePointerCapture(drag.id);
        drag = null; return true;
      };
      ownChartWork(root, () => {
        cancelPointerMove();
        if (drag && stage.hasPointerCapture(drag.id)) stage.releasePointerCapture(drag.id);
        drag = null;
        delete root._cfxCancelBrushDrag;
        delete root._cfxCancelPointerHover;
      });
      listenChart(root, stage, 'pointerdown', (event) => {
        if (event.button !== 0) return;
        resumePointerReadout(root);
        cancelPointerMove();
        if (root.dataset.cfxMode === 'pan' && hasFeature(root, 'Pan')) {
          if (connectedXViewport(root)) {
            const x = logicalXPointer(root, event);
            if (x === null || !chartXProjection(root)) return;
            drag = { mode: 'native-pan', id: event.pointerId, x, delta: 0 };
            event.preventDefault(); stage.setPointerCapture(event.pointerId);
            return;
          }
          const state = getState(root);
          const point = stagePointer(stage, event);
          drag = { mode: 'pan', id: event.pointerId, x: point.x, y: point.y, panX: state.panX, panY: state.panY };
          event.preventDefault();
          stage.setPointerCapture(event.pointerId);
        } else if (root.dataset.cfxMode === 'brush' && hasFeature(root, 'Brush') && brush) {
          const point = brushPointer(root, stage, event);
          const previousBrush = { hidden: brush.hidden, left: brush.style.left, top: brush.style.top,
            width: brush.style.width, height: brush.style.height };
          drag = { mode: 'brush', id: event.pointerId, left: point.x, top: point.y, previousBrush };
          brush.hidden = false;
          brush.style.left = drag.left + 'px';
          setBrushBox(root, stage, brush, drag, point);
          event.preventDefault();
          stage.setPointerCapture(event.pointerId);
        }
      });
      listenChart(root, stage, 'pointermove', (event) => {
        resumePointerReadout(root);
        if (drag ? drag.id !== event.pointerId : !ownsTooltipPointer(root, event)) return;
        // A shadow event is retargeted as dispatch leaves the chart. Capture its
        // native surface now, while retaining the PointerEvent for timing and ownership.
        const nativeTarget = event.composedPath().find(node => node instanceof Element && stage.contains(node)) || event.target;
        pendingPointerMove = { event, nativeTarget };
        if (pointerFrame) return;
        pointerFrame = requestAnimationFrame(() => {
          pointerFrame = 0;
          const pending = pendingPointerMove; pendingPointerMove = null;
          if (pending && root.isConnected && chartLifetimes.get(root)?.disposed === false) applyPointerMove(pending.event, pending.nativeTarget);
        });
      });
      listenChart(root, stage, 'pointerup', (event) => {
        if (!drag || drag.id !== event.pointerId) return;
        cancelPointerMove();
        applyPointerMove(event);
        if (drag.mode === 'native-pan') {
          const plot = chartXProjection(root)?.plotBounds;
          if (plot && Math.abs(drag.delta) > 0.5) requestXViewport(root, 'pan', plot.x - drag.delta, plot.x + plot.width - drag.delta);
        } else if (drag.mode === 'brush' && brush) {
          const dataRange = finishBrushSelection(root, stage, brush);
          root.dataset.cfxBrush = [brush.style.left, brush.style.top, brush.style.width, brush.style.height].join(' ');
          syncResetControl(root);
          const previousTargets = selectedTargets(root), previousSelection = previousTargets.map(targetKey).join('\n');
          const boxTargets = selectTargetsInBox(root, brush.getBoundingClientRect(), event.shiftKey, dataRange);
          const replaceSelection = !event.shiftKey;
          emitHostEvent(root, 'cfxbrush', { bounds: root.dataset.cfxBrush, ...dataRange });
          if (boxTargets.length || replaceSelection) emitHostEvent(root, 'cfxlasso', { bounds: root.dataset.cfxBrush, count: boxTargets.length, targets: boxTargets, ...dataRange });
          if (previousSelection !== selectionSignature(root)) publishSelectionChange(root, 'lasso');
          emitSync(root, { action: 'brush', bounds: root.dataset.cfxBrush });
          if (boxTargets.length || replaceSelection) emitSync(root, { action: 'lasso', bounds: root.dataset.cfxBrush,
            targets: selectedTargets(root), previousTargets, replace: replaceSelection });
          publishCompare(root, false);
        } else if (drag.mode === 'pan') {
          emitHostEvent(root, 'cfxviewport', { state: getState(root) });
          emitSync(root, { action: 'viewport', state: getState(root) });
        }
        stage.releasePointerCapture(event.pointerId);
        drag = null;
      });
      listenChart(root, stage, 'pointerleave', (event) => {
        if (!ownsTooltipPointer(root, event)) return;
        if (!drag) cancelPointerMove();
        hideCrosshair(root, crosshair);
        clearHover(root, true, true);
        hideTip(root, tip, false);
      });
      listenChart(root, stage, 'pointercancel', (event) => {
        if (drag?.id === event.pointerId || !drag && ownsTooltipPointer(root, event)) cancelPointerMove();
        if (drag && drag.id === event.pointerId) {
          // Cancel only the provisional gesture; a previously committed selection and box remain current.
          if (drag.mode === 'brush' && brush) {
            brush.hidden = drag.previousBrush.hidden;
            for (const name of ['left', 'top', 'width', 'height']) brush.style[name] = drag.previousBrush[name];
          }
          drag = null;
        }
        if (!ownsTooltipPointer(root, event)) return;
        hideCrosshair(root, crosshair);
        clearHover(root, true, true);
        hideTip(root, tip, false);
      });
    }
    const reset = root.querySelector('[data-cfx-reset]');
    if (reset) listenChart(root, window, 'resize', () => syncResetControl(root));
    if (reset) listenChart(root, reset, 'click', () => {
      if (connectedXViewport(root) && chartXProjection(root) && (nativeXWindowChanged(root) || chartLifetimes.get(root)?.xViewportPending)) {
        requestXViewport(root, 'reset');
        return;
      }
      resetViewport(root);
      emitHostEvent(root, 'cfxreset', {});
      emitHostEvent(root, 'cfxviewport', { state: getState(root) });
      emitSync(root, { action: 'viewport', state: getState(root) });
      const previousSelection = selectionSignature(root);
      clearSelections(root);
      root.querySelectorAll('.cfx-series-muted').forEach((node) => node.classList.remove('cfx-series-muted'));
      root.querySelectorAll('[data-cfx-muted]').forEach((node) => node.removeAttribute('data-cfx-muted'));
      setSeriesIsolation(root, null, false);
      refreshKeyboardNavigation(root);
      clearFocusTrail(root);
      clearReveals(root);
      hideCrosshair(root, crosshair);
      hideTip(root, tip, true);
      if (root._cfxBranchHighlights) clearHover(root, true, true);
      publishResetSelection(root, previousSelection);
      syncResetControl(root);
    });
  };
  // Generated sections carry the scene and matching semantic metadata as one adapter document.
  const readChartSection = (value, fresh = false) => {
    let section = value;
    if (typeof value === 'string') {
      const template = document.createElement('template'); template.innerHTML = value.trim();
      if (template.content.children.length !== 1) throw new TypeError('Expected one generated chart section without assets.');
      section = template.content.firstElementChild;
    }
    if (!(section instanceof Element) || !section.matches('section.cfx-interactive-chart'))
      throw new TypeError('Expected a generated interactive chart section.');
    if (fresh && section.dataset.cfxRuntimeBound === 'true') throw new TypeError('Update requires a fresh generated section.');
    if (section.querySelector('script') || section.querySelectorAll('.cfx-stage').length !== 1
      || section.querySelectorAll('.cfx-stage svg').length !== 1 || section.querySelectorAll('.cfx-tooltip').length !== 1)
      throw new TypeError('The generated chart section must contain its scene and readout, without scripts.');
    const svg = section.querySelector('.cfx-stage svg');
    const box = svg.getAttribute('viewBox')?.trim().split(/[\s,]+/).map(Number);
    let metadata;
    try { metadata = JSON.parse(section.dataset.cfxPreparedChart); } catch { throw new TypeError('The chart section requires prepared metadata.'); }
    if (!section.dataset.cfxChartId || svg.namespaceURI !== 'http://www.w3.org/2000/svg' || !box || box.length !== 4 || !box.every(Number.isFinite) || box[2] <= 0 || box[3] <= 0
      || !metadata || !Array.isArray(metadata.series) || !Array.isArray(metadata.regions) || !Array.isArray(metadata.xLabels)
      || metadata.series.some(item => !item || typeof item.name !== 'string' || typeof item.key !== 'string' || !item.key
        || typeof item.local !== 'boolean' || typeof item.kind !== 'string' || typeof item.state !== 'string'
        || !Array.isArray(item.indices) || item.indices.some(index => !Number.isSafeInteger(index) || index < 0))
      || new Set(metadata.series.map(item => item.key)).size !== metadata.series.length
      || metadata.xProjection !== undefined && !validXProjection(metadata.xProjection)
      || metadata.dataProjection !== undefined && !validDataProjection(metadata.dataProjection)
      || metadata.xLabels.some(item => !item || !Number.isFinite(item.value) || typeof item.text !== 'string')
      || metadata.categoryLabels !== undefined && (!Array.isArray(metadata.categoryLabels)
        || metadata.categoryLabels.some(item => !item || !Number.isFinite(item.value) || typeof item.text !== 'string'))
      || metadata.regions.some(item => !item || typeof item.id !== 'string' || typeof item.role !== 'string'
        || ![item.x, item.y, item.width, item.height].every(Number.isFinite) || item.width < 0 || item.height < 0
        || item.fillInteriors !== undefined && (!Array.isArray(item.fillInteriors)
          || item.fillInteriors.some(box => !Array.isArray(box) || box.length !== 3 || !box.every(Number.isFinite)))))
      throw new TypeError('The chart section has invalid prepared metadata or scene bounds.');
    if (Array.from(svg.querySelectorAll('[data-cfx-series]')).some(node => {
      const index = Number(node.dataset.cfxSeries);
      return !Number.isInteger(index) || index < 0 || index >= metadata.series.length;
    })) throw new TypeError('The chart scene does not match its prepared series metadata.');
    return section;
  };
  const durableChartTarget = target => !!target && target.seriesScope === 'shared' && !!target.seriesKey
    && (target.targetKind !== 'point' || target.observationId !== undefined)
    && (target.targetKind !== 'legend' || target.point === undefined || target.observationId !== undefined);
  const chartLegendStateTargets = (root, isolated) => {
    // A narrow native legend may omit an entry while its data owner remains painted and muted.
    // Capture that owner's identity too; presentation density must not erase reader state.
    const attribute = isolated ? 'cfxIsolated' : 'cfxMuted';
    const className = isolated ? 'cfx-series-isolated-in' : 'cfx-series-muted';
    const targets = interactiveTargets(root, '[data-cfx-series]')
      .filter(node => node.dataset[attribute] === 'true' || node.classList.contains(className)).map(seriesTarget);
    const wholeSeries = new Set(targets.filter(target => target.targetKind === 'series').map(target => target.seriesKey));
    const captured = new Set();
    return targets.filter(target => {
      if (target.targetKind !== 'series' && wholeSeries.has(target.seriesKey)) return false;
      const key = JSON.stringify([target.seriesKey, target.targetKind, target.targetId]);
      if (captured.has(key)) return false;
      captured.add(key); return true;
    });
  };
  const chartUpdateState = (root, sameSource = false) => {
    const active = root.getRootNode().activeElement;
    const focused = interactiveTargets(root).find(node => targetFocusNode(node) === active);
    return {
      selected: selectedTargets(root).filter(target => sameSource || durableChartTarget(target)),
      muted: chartLegendStateTargets(root, false).filter(target => sameSource || durableChartTarget(target)),
      isolated: chartLegendStateTargets(root, true).filter(target => sameSource || durableChartTarget(target)),
      focused: focused && (sameSource || durableChartTarget(targetIdentity(focused))) ? targetIdentity(focused) : null,
      highlight: captureHostHighlight(root, sameSource),
      ownedFocus: !!active && root.contains(active), viewport: getState(root)
    };
  };
  const installChartSection = (root, next) => {
    // Host attributes and non-adapter CSS properties belong to the embedding application.
    for (const attribute of Array.from(root.attributes)) if (attribute.name.startsWith('data-cfx-')) root.removeAttribute(attribute.name);
    for (const property of Array.from(root.style)) if (property.startsWith('--cfx-')) root.style.removeProperty(property);
    for (const attribute of Array.from(next.attributes)) {
      if (attribute.name.startsWith('data-cfx-') && attribute.name !== 'data-cfx-runtime-bound'
        || attribute.name === 'role' || attribute.name === 'aria-label') root.setAttribute(attribute.name, attribute.value);
    }
    for (const property of Array.from(next.style)) if (property.startsWith('--cfx-'))
      root.style.setProperty(property, next.style.getPropertyValue(property), next.style.getPropertyPriority(property));
    root.replaceChildren(...Array.from(next.childNodes).map(node => node.cloneNode(true)));
  };
  const restoreChartUpdateState = (root, state, nativeCommit = false) => {
    // Explicit identity survives position changes; anonymous ordinal facts never inherit full-replacement state.
    applySelectionSetByTargets(root, state.selected, true);
    for (const target of state.muted) { const resolved = resolveSeriesTarget(root, target); if (resolved) setSeriesMuted(root, resolved, true); }
    for (const target of state.isolated) { const resolved = resolveSeriesTarget(root, target); if (resolved) setSeriesIsolation(root, resolved, true); }
    applyHostHighlight(root, state.highlight);
    syncSelectionAppearance(root);
    applyViewport(root, nativeCommit ? { zoom: 1, panX: 0, panY: 0 } : state.viewport);
    renderCompare(root); refreshKeyboardNavigation(root); syncResetControl(root);
    const focused = state.focused && interactiveTargets(root).find(node => seriesKey(node) === state.focused.seriesKey && matchesTargetIdentity(node, state.focused));
    if (focused && keyboardTargetAvailable(focused)) focusKeyboardTarget(root, focused, pointerReadoutSuspended(root));
    else if (state.ownedFocus) { root.setAttribute('tabindex', '-1'); root.focus({ preventScroll: true }); }
    // Focus recovery must not resurrect a pinned or stale readout from the previous scene.
    hideTip(root, root.querySelector('.cfx-tooltip'), true); clearHover(root, false, false);
  };
  // Connected hosts return a C#-prepared scene. JavaScript owns gesture geometry and reply lifetime only.
  let chartSceneSequence = 0, chartViewportRequestSequence = 0;
  const nextChartSceneRevision = () => 'cfx-scene-' + (++chartSceneSequence);
  const validXProjection = projection => {
    const box = projection?.plotBounds;
    return !!projection && [projection.fullMinimum, projection.fullMaximum, projection.minimum, projection.maximum].every(Number.isFinite)
      && projection.fullMinimum < projection.fullMaximum && projection.minimum < projection.maximum
      && projection.minimum >= projection.fullMinimum && projection.maximum <= projection.fullMaximum
      && typeof projection.reversed === 'boolean' && !!box && [box.x, box.y, box.width, box.height].every(Number.isFinite)
      && box.width > 0 && box.height > 0 && Number.isFinite(box.x + box.width) && Number.isFinite(box.y + box.height);
  };
  const chartXProjection = root => {
    try { return JSON.parse(root.dataset.cfxPreparedChart).xProjection || null; } catch { return null; }
  };
  const nativeXWindowChanged = root => {
    const projection = chartXProjection(root);
    return !!projection && (projection.minimum !== projection.fullMinimum || projection.maximum !== projection.fullMaximum);
  };
  const connectedXViewport = root => !!chartLifetimes.get(root)?.xViewportConnection;
  const syncXViewportControls = root => {
    const unsupported = connectedXViewport(root) && !chartXProjection(root);
    root.querySelectorAll('[data-cfx-zoom],[data-cfx-mode-button="pan"]').forEach(button => { button.disabled = unsupported; });
    if (unsupported && root.dataset.cfxMode === 'pan') applyMode(root, '');
    syncResetControl(root);
  };
  const cancelXViewportRequest = lifetime => {
    const pending = lifetime?.xViewportPending;
    if (!pending) return;
    lifetime.xViewportPending = null;
    pending.abort.abort();
  };
  const logicalXPointer = (root, event) => {
    const matrix = root.querySelector('.cfx-stage svg')?.getScreenCTM();
    if (!matrix) return null;
    const point = new DOMPoint(event.clientX, event.clientY).matrixTransform(matrix.inverse());
    return Number.isFinite(point.x) ? point.x : null;
  };
  const replaceChartSection = (root, lifetime, next, nativeKind = '', adaptiveDrawing = '') => {
    // Validate every alternate before releasing the working scene, preserving atomic host updates.
    const adaptive = adaptiveDrawing ? lifetime.adaptive : readAdaptiveLayout(next);
    const state = chartUpdateState(root, !!adaptiveDrawing);
    const suspended = nativeKind === 'reset' || !!adaptiveDrawing && pointerReadoutSuspended(root);
    // Keep the request current through validation so a rejected alternate reports its failure.
    if (nativeKind) lifetime.xViewportPending = null;
    if (nativeKind === 'reset') { state.selected = []; state.muted = []; state.isolated = []; state.focused = null; }
    releaseChartLifetime(root);
    installChartSection(root, next);
    // The application owns the root and connection; each installed scene owns fresh browser work.
    lifetime.disposed = false; lifetime.sceneRevision = nextChartSceneRevision();
    lifetime.adaptive = adaptive;
    if (adaptiveDrawing) adaptive.current = adaptiveDrawing;
    chartLifetimes.set(root, lifetime); mountedCharts.add(root);
    // Same-source redraws retain explicit reset dismissal before any fresh scene can acquire input.
    if (suspended) suspendPointerReadout(root);
    prepareAdaptiveRoot(root, lifetime);
    bindChart(root); watchChartLifetimes(); restoreChartUpdateState(root, state, !!nativeKind);
    bindAdaptiveLayout(root);
    syncXViewportControls(root);
  };
  const requestXViewport = (root, kind, left, right) => {
    const lifetime = chartLifetimes.get(root), connection = lifetime?.xViewportConnection;
    if (!connection || lifetime.disposed || !root.isConnected || !chartXProjection(root)) return;
    if (kind !== 'reset' && (!Number.isFinite(left) || !Number.isFinite(right) || left >= right)) return;
    if (kind === 'reset') suspendPointerReadout(root);
    cancelXViewportRequest(lifetime);
    const request = Object.freeze({ chartId: root.dataset.cfxChartId, requestId: 'cfx-request-' + (++chartViewportRequestSequence),
      sceneRevision: lifetime.sceneRevision, kind, ...(lifetime.adaptive ? { drawing: lifetime.adaptive.current } : {}),
      ...(kind === 'reset' ? {} : { left, right }) });
    const pending = { request, abort: new AbortController() }; lifetime.xViewportPending = pending;
    const current = () => root.isConnected && chartLifetimes.get(root) === lifetime && !lifetime.disposed
      && lifetime.xViewportConnection === connection && lifetime.xViewportPending === pending
      && lifetime.sceneRevision === request.sceneRevision && !pending.abort.signal.aborted;
    emitHostEvent(root, 'cfxviewportrequest', request);
    Promise.resolve().then(() => {
      if (!current()) return;
      return connection(request, pending.abort.signal);
    }).then(html => {
      if (!current()) return;
      if (typeof html !== 'string') throw new TypeError('The viewport connection must return a fresh generated section HTML string.');
      const next = readChartSection(html, true).cloneNode(true);
      if (next.dataset.cfxChartId !== request.chartId || !chartXProjection(next))
        throw new TypeError('The viewport reply must retain the chart identity and a supported X projection.');
      const previousSelection = kind === 'reset' ? selectionSignature(root) : '';
      replaceChartSection(root, lifetime, next, kind);
      if (kind === 'reset') {
        emitHostEvent(root, 'cfxreset', {});
        publishResetSelection(root, previousSelection);
        emitSync(root, { action: 'viewport', state: getState(root) });
      }
      emitHostEvent(root, 'cfxviewportchange', { requestId: request.requestId, sceneRevision: lifetime.sceneRevision, kind });
    }).catch(error => {
      if (!current()) return;
      lifetime.xViewportPending = null;
      emitHostEvent(root, 'cfxviewporterror', { requestId: request.requestId, sceneRevision: request.sceneRevision, kind,
        message: error instanceof Error ? error.message : String(error) });
    });
  };
  const requestXViewportZoom = (root, factor) => {
    const plot = chartXProjection(root)?.plotBounds;
    if (!plot || !Number.isFinite(factor) || factor <= 0) return;
    const middle = plot.x + plot.width / 2, half = plot.width / (2 * factor);
    requestXViewport(root, 'zoom', middle - half, middle + half);
  };
  // Alternate drawings are native preparations of the same source, not scaled desktop geometry.
  // Keep them inert: one root and controller own the active scene and every browser resource.
  const adaptiveTemplate = (root, name) => Array.from(root.children)
    .find(node => node instanceof HTMLTemplateElement && node.hasAttribute('data-cfx-adaptive-' + name));
  const adaptiveSectionCopy = section => {
    const copy = section.cloneNode(true);
    for (const name of ['wide', 'narrow']) adaptiveTemplate(copy, name)?.remove();
    copy.removeAttribute('data-cfx-runtime-bound');
    return copy;
  };
  const readAdaptiveLayout = root => {
    if (root.dataset.cfxResponsiveLayout !== 'adaptive') return null;
    const breakpoint = Number(root.dataset.cfxAdaptiveBreakpoint), current = root.dataset.cfxAdaptiveDrawing || 'wide';
    const narrowTemplate = adaptiveTemplate(root, 'narrow'), wideTemplate = adaptiveTemplate(root, 'wide');
    if (!Number.isFinite(breakpoint) || breakpoint <= 0 || !['wide', 'narrow'].includes(current)
      || !narrowTemplate || narrowTemplate.content.children.length !== 1
      || wideTemplate && wideTemplate.content.children.length !== 1 || current === 'narrow' && !wideTemplate)
      throw new TypeError('Adaptive charts require a valid breakpoint and matching native drawings.');
    const wide = wideTemplate ? adaptiveSectionCopy(readChartSection(wideTemplate.content.firstElementChild, true)) : adaptiveSectionCopy(root);
    const narrow = adaptiveSectionCopy(readChartSection(narrowTemplate.content.firstElementChild, true));
    const source = JSON.stringify(JSON.parse(wide.dataset.cfxPreparedChart).series);
    for (const section of [wide, narrow]) if (section.dataset.cfxChartId !== root.dataset.cfxChartId
      || section.dataset.cfxResponsiveLayout !== 'adaptive'
      || JSON.stringify(JSON.parse(section.dataset.cfxPreparedChart).series) !== source)
      throw new TypeError('Adaptive drawings must retain the same chart and source identities.');
    return { wide, narrow, breakpoint, current };
  };
  const prepareAdaptiveRoot = (root, lifetime) => {
    const adaptive = lifetime.adaptive;
    if (!adaptive) return;
    root.dataset.cfxAdaptiveDrawing = adaptive.current;
    for (const name of ['wide', 'narrow']) {
      adaptiveTemplate(root, name)?.remove();
      const template = root.ownerDocument.createElement('template');
      template.setAttribute('data-cfx-adaptive-' + name, 'true');
      template.content.appendChild(adaptive[name].cloneNode(true));
      root.appendChild(template);
    }
  };
  const bindAdaptiveLayout = root => {
    const lifetime = chartLifetimes.get(root), adaptive = lifetime?.adaptive;
    if (!adaptive) return;
    let frame = 0;
    const refresh = () => {
      frame = 0;
      if (lifetime.disposed || chartLifetimes.get(root) !== lifetime || !root.isConnected) return;
      const width = root.getBoundingClientRect().width;
      if (width <= 0) return;
      const drawing = width < adaptive.breakpoint ? 'narrow' : 'wide';
      if (drawing === adaptive.current) return;
      const previousSceneRevision = lifetime.sceneRevision;
      replaceChartSection(root, lifetime, adaptive[drawing].cloneNode(true), '', drawing);
      emitHostEvent(root, 'cfxlayoutchange', { drawing, previousSceneRevision, sceneRevision: lifetime.sceneRevision });
    };
    const queue = () => { if (!frame) frame = requestAnimationFrame(refresh); };
    const observer = typeof ResizeObserver === 'undefined' ? null : new ResizeObserver(queue);
    observer?.observe(root);
    if (!observer) listenChart(root, window, 'resize', queue);
    ownChartWork(root, () => { observer?.disconnect(); if (frame) cancelAnimationFrame(frame); });
    queue();
  };
  const chartRoot = target => typeof target === 'string' ? document.querySelector(target) : target;
  const mountChart = target => {
    const root = chartRoot(target);
    const existing = root && chartLifetimes.get(root);
    if (existing && !root.isConnected) { existing.controller.dispose(); throw new TypeError('Mount the chart after inserting its section into the document.'); }
    if (existing && !existing.disposed) return existing.controller;
    readChartSection(root);
    if (!root.isConnected) throw new TypeError('Mount the chart after inserting its section into the document.');
    const lifetime = { cleanups: new Set(), disposed: false, controller: null, xViewportConnection: null,
      xViewportPending: null, sceneRevision: nextChartSceneRevision() };
    lifetime.adaptive = readAdaptiveLayout(root);
    const requireMounted = () => {
      if (lifetime.disposed || !root.isConnected) { controller.dispose(); throw new Error('The chart controller is disposed; mount the connected section again.'); }
    };
    const controller = Object.freeze({
      root,
      get sceneRevision() { return lifetime.sceneRevision; },
      select: (match, options) => { requireMounted(); runSelectionCommand(root, match, options); return controller; },
      clearSelection: options => { requireMounted(); clearSelectionCommand(root, options); return controller; },
      clearBrush: options => { requireMounted(); clearBrushCommand(root, options); return controller; },
      getSelection: () => { requireMounted(); return copyHostValue(selectedTargets(root)); },
      highlight: (match, options) => { requireMounted(); runHighlightCommand(root, match, options); return controller; },
      clearHighlight: options => { requireMounted(); clearHighlightCommand(root, options); return controller; },
      unpinTooltip: () => { requireMounted(); unpinTooltip(root); return controller; },
      update: section => {
        requireMounted();
        const next = readChartSection(section, true).cloneNode(true);
        replaceChartSection(root, lifetime, next);
        emitHostEvent(root, 'cfxupdated', {});
        return controller;
      },
      connectXViewport: connection => {
        requireMounted();
        if (connection !== null && typeof connection !== 'function') throw new TypeError('Expected an X viewport callback or null to disconnect.');
        cancelXViewportRequest(lifetime);
        lifetime.xViewportConnection = connection;
        if (connection) applyViewport(root, { zoom: 1, panX: 0, panY: 0 });
        syncXViewportControls(root);
        return controller;
      },
      resize: () => {
        requireMounted(); hideCrosshair(root, root.querySelector('.cfx-crosshair'));
        refreshKeyboardNavigation(root); refreshTooltipPosition(root); syncResetControl(root);
        return controller;
      },
      dispose: () => { if (!lifetime.disposed) releaseChartLifetime(root); }
    });
    lifetime.controller = controller; chartLifetimes.set(root, lifetime); mountedCharts.add(root);
    try { prepareAdaptiveRoot(root, lifetime); bindChart(root); bindAdaptiveLayout(root); watchChartLifetimes(); } catch (error) { releaseChartLifetime(root); throw error; }
    return controller;
  };
  // Shared assets may be registered by more than one fragment. One installed runtime owns all controllers.
  window.ChartForgeXCharts = Object.freeze({ mount: mountChart,
    get: target => { const root = chartRoot(target), lifetime = root?.isConnected && chartLifetimes.get(root); return lifetime && !lifetime.disposed ? lifetime.controller : null; } });
  document.querySelectorAll('.cfx-interactive-chart').forEach(root => mountChart(root));
})();
