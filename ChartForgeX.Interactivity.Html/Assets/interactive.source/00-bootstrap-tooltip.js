(() => {
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
  const isInteractiveTarget = (node) => (node.dataset ? node.dataset.cfxRole : '') === 'legend-item' || !node.closest('[data-cfx-role="legend-item"]');
  const interactiveTargets = (root) => Array.from(root.querySelectorAll(targetSelector)).filter(isInteractiveTarget);
  const seriesLegend = (node) => {
    const data = node.dataset || {};
    if (data.cfxSeries === undefined) return null;
    const root = node.closest('.cfx-interactive-chart');
    if (root) {
      const legendItems = Array.from(root.querySelectorAll('[data-cfx-role="legend-item"][data-cfx-series]'));
      const sameSeries = (item) => (item.dataset || {}).cfxSeries === data.cfxSeries;
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
  const sourcePointIndex = (node) => {
    const data = node.dataset || {};
    if (data.cfxPoint === undefined || data.cfxSeries === undefined) return data.cfxPoint;
    const svg = node.closest('svg');
    const sourceIndices = svg ? svg.getAttribute('data-cfx-series-source-indices-' + data.cfxSeries) : '';
    if (!sourceIndices) return data.cfxPoint;
    const index = Number(data.cfxPoint);
    const mapped = sourceIndices.split(',')[index];
    return mapped === undefined || mapped === '' ? data.cfxPoint : mapped;
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
    if (kind === 'point') return `${seriesKey(node) || data.cfxSeries || 'series'}:${sourcePointIndex(node) || '0'}`;
    if (kind === 'region') return data.cfxRegion || data.cfxId || data.cfxLabel || '';
    if (kind === 'node') return data.cfxNode || data.cfxId || data.cfxLabel || '';
    if (kind === 'link') return data.cfxId || [data.cfxSource, data.cfxTarget].filter(Boolean).join('->') || data.cfxLabel || '';
    if (kind === 'legend') return data.cfxPoint === undefined ? seriesKey(node) || data.cfxSeries || data.cfxLabel || '' : `${seriesKey(node) || data.cfxSeries || 'series'}:${sourcePointIndex(node)}`;
    if (kind === 'annotation') return data.cfxId || data.cfxLabel || [data.cfxKind, data.cfxValue].filter(Boolean).join(':');
    return data.cfxId || node.id || data.cfxLabel || data.cfxRole || '';
  };
  const applyRenderedTargetContract = (root) => {
    Array.from(root.querySelectorAll(renderedTargetSelector)).filter(isInteractiveTarget).forEach((node) => {
      if (node.closest('defs')) return;
      const kind = renderedTargetKind(node);
      const id = renderedTargetId(node, kind);
      if (!kind || !id) return;
      node.setAttribute('data-cfx-target-kind', kind);
      node.setAttribute('data-cfx-target-id', id);
    });
  };
  const text = (node) => {
    const data = node.dataset || {};
    const aria = node.getAttribute('aria-label');
    if (aria) return aria;
    const label = data.cfxLabel || data.cfxText || '';
    const parts = [];
    if (label) parts.push(label);
    else if (data.cfxRole) parts.push(data.cfxRole.replace(/-/g, ' '));
    if (data.cfxSeries !== undefined && !label) parts.push(seriesLabel(node));
    if (data.cfxPoint !== undefined) parts.push('Point ' + data.cfxPoint);
    const value = data.cfxValue || data.cfxY || data.cfxEnd || data.cfxTarget || '';
    if (value) parts.push('Value ' + value);
    return parts.join(' / ');
  };
  const targetIdentity = (node) => {
    const data = node.dataset || {};
    const targetKind = renderedTargetKind(node);
    return {
      targetKind,
      targetId: renderedTargetId(node, targetKind),
      id: data.cfxId || node.id || '',
      role: data.cfxRole || '',
      label: data.cfxLabel || data.cfxText || node.getAttribute('aria-label') || '',
      series: data.cfxSeries,
      seriesKey: seriesKey(node),
      point: data.cfxPoint,
      sourcePoint: sourcePointIndex(node),
      value: data.cfxValue || data.cfxY || data.cfxEnd || '',
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
  const tooltipRows = (node) => {
    const data = node.dataset || {};
    const rows = [];
    const push = (name, value) => {
      if (value !== undefined && value !== null && value !== '') rows.push({ name, value: String(value) });
    };
    push('Role', data.cfxRole ? data.cfxRole.replace(/-/g, ' ') : '');
    push('Series', seriesLabel(node));
    push('Point', data.cfxPoint);
    push('X', data.cfxX || data.cfxCategory || data.cfxDate || data.cfxStart);
    push('Y', data.cfxY || data.cfxValue);
    push('End', data.cfxEnd);
    push('Target', data.cfxTarget);
    push('Status', data.cfxStatus);
    push('Kind', data.cfxKind);
    push('Percent', data.cfxPercent);
    push('Delta', data.cfxDelta);
    push('Range', data.cfxLower && data.cfxUpper ? data.cfxLower + ' - ' + data.cfxUpper : '');
    metadataRows(node).forEach((row) => push(row.name, row.value));
    return rows;
  };
  const renderTip = (tip, node) => {
    const label = text(node);
    if (!label) return false;
    tip.replaceChildren();
    const title = document.createElement('div');
    title.className = 'cfx-tooltip__title';
    title.textContent = label;
    tip.appendChild(title);
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
    if (!hasFeature(root, 'Tooltips')) return;
    if (root.dataset.cfxTooltipPinned === 'true') return;
    if (!renderTip(tip, node)) return;
    tip.hidden = false;
    moveTip(tip, event, node);
  };
  const moveTip = (tip, event, node) => {
    if (!event || tip.hidden) return;
    let clientX = event.clientX;
    let clientY = event.clientY;
    if ((!Number.isFinite(clientX) || !Number.isFinite(clientY)) && node && node.getBoundingClientRect) {
      const rect = node.getBoundingClientRect();
      clientX = rect.left + rect.width / 2;
      clientY = rect.top + rect.height / 2;
    }
    if (!Number.isFinite(clientX) || !Number.isFinite(clientY)) {
      clientX = 24;
      clientY = 24;
    }
    const tipWidth = tip.offsetWidth || 0;
    const tipHeight = tip.offsetHeight || 0;
    const maxX = Math.max(8, window.innerWidth - tipWidth - 8);
    const maxY = Math.max(8, window.innerHeight - tipHeight - 8);
    const x = Math.max(8, Math.min(maxX, clientX + 14));
    const y = Math.max(8, Math.min(maxY, clientY + 14));
    tip.style.left = x + 'px';
    tip.style.top = y + 'px';
  };
  const targetKey = (target) => target ? [target.targetKind || '', target.targetId || '', target.id || '', target.role || '', target.label || '', target.series ?? '', target.point ?? '', target.value || '', target.kind || ''].join('|') : '';
  const compareItems = (root) => {
    const items = [];
    const seen = new Set();
    root.querySelectorAll('.cfx-selected').forEach((node) => {
      const target = targetIdentity(node);
      const key = targetKey(target);
      if (!key || seen.has(key)) return;
      seen.add(key);
      items.push({ label: text(node) || target.label || target.role || target.id || 'Target', target });
    });
    return items;
  };
  const selectedTargets = (root) => compareItems(root).map((item) => item.target);
  const renderCompare = (root) => {
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
      chip.addEventListener('click', () => {
        clearHover(root, false, false);
        if (applyHoverByTarget(root, item.target)) {
          recordFocusTrail(root, item.target, true, true);
          emitHostEvent(root, 'cfxhover', { label: item.label, target: item.target });
        }
      });
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
    clear.addEventListener('click', () => {
      clearSelections(root);
      publishCompare(root, true);
    });
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
    if (!tip || (!force && root.dataset.cfxTooltipPinned === 'true')) return;
    tip.hidden = true;
    tip.classList.remove('cfx-tooltip--pinned');
    root.removeAttribute('data-cfx-tooltip-pinned');
    root.removeAttribute('data-cfx-pinned-target');
  };
  const pinTip = (root, tip, node, event) => {
    if (!hasFeature(root, 'Tooltips') || !renderTip(tip, node)) return;
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
