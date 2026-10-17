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
        : legendItems.find((item) => sameSeries(item) && data.cfxGroupIndex !== undefined && item.dataset.cfxGroupIndex === data.cfxGroupIndex)
          || legendItems.find((item) => sameSeries(item) && (item.dataset || {}).cfxPoint === data.cfxPoint)
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
    if (data.cfxDerived === 'five-number-summary' && data.cfxGroupId !== undefined)
      return JSON.stringify([seriesKey(node), 'five-number-summary', data.cfxGroupId]);
    if (data.cfxObservationId !== undefined) return JSON.stringify([seriesKey(node), data.cfxObservationId]);
    const derived = hasDerivedPointIdentity(node);
    const point = derived ? 'derived:' + derivedPointIdentity(node) : sourcePointIndex(node) ?? '0';
    return `${seriesKey(node) || data.cfxSeries || 'series'}:${point}`;
  };
  // Group legends own a distribution; the individual outlier retains its sample target.
  const boxPlotGroup = (node) => {
    const data = node.dataset || {};
    return data.cfxGroupIndex !== undefined && (data.cfxDerived === 'five-number-summary'
      || ['boxplot', 'boxplotoutlier'].includes((data.cfxKind || '').toLowerCase()));
  };
  const boxPlotGroupTargetId = (node) => {
    const data = node.dataset || {};
    return data.cfxGroupId !== undefined ? JSON.stringify([seriesKey(node), 'five-number-summary', data.cfxGroupId])
      : `${seriesKey(node) || data.cfxSeries || 'series'}:derived:five-number-summary:${data.cfxGroupIndex}`;
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
