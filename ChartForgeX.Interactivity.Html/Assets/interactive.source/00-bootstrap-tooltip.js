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
  const targetSelector = '.cfx-interactive-region,[data-cfx-label],[data-cfx-point],[data-cfx-series],[data-cfx-role="legend-item"]';
  const lassoSelector = '.cfx-interactive-region,[data-cfx-label],[data-cfx-point]';
  const isInteractiveTarget = (node) => (node.dataset ? node.dataset.cfxRole : '') === 'legend-item' || !node.closest('[data-cfx-role="legend-item"]');
  const interactiveTargets = (root) => Array.from(root.querySelectorAll(targetSelector)).filter(isInteractiveTarget);
  const seriesLabel = (node) => {
    const data = node.dataset || {};
    if (data.cfxSeriesName) return data.cfxSeriesName;
    if (data.cfxSeries === undefined) return '';
    const root = node.closest('.cfx-interactive-chart');
    if (root) {
      const legend = Array.from(root.querySelectorAll('[data-cfx-role="legend-item"][data-cfx-series]'))
        .find((item) => (item.dataset || {}).cfxSeries === data.cfxSeries);
      if (legend) return (legend.dataset || {}).cfxLabel || legend.getAttribute('aria-label') || legend.textContent || '';
    }
    return 'Series ' + data.cfxSeries;
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
    return {
      id: data.cfxId || node.id || '',
      role: data.cfxRole || '',
      label: data.cfxLabel || data.cfxText || node.getAttribute('aria-label') || '',
      series: data.cfxSeries,
      point: data.cfxPoint,
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
  const targetKey = (target) => target ? [target.id || '', target.role || '', target.label || '', target.series ?? '', target.point ?? '', target.value || '', target.kind || ''].join('|') : '';
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
