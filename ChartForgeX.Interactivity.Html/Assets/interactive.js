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
  const text = (node) => {
    const data = node.dataset || {};
    const aria = node.getAttribute('aria-label');
    if (aria) return aria;
    const label = data.cfxLabel || data.cfxText || '';
    const parts = [];
    if (label) parts.push(label);
    else if (data.cfxRole) parts.push(data.cfxRole.replace(/-/g, ' '));
    if (data.cfxSeries !== undefined && !label) parts.push('Series ' + data.cfxSeries);
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
    push('Series', data.cfxSeries);
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
    const stage = root.querySelector('.cfx-stage');
    if (!stage) return;
    stage.style.setProperty('--cfx-zoom', state.zoom);
    stage.style.setProperty('--cfx-pan-x', state.panX + 'px');
    stage.style.setProperty('--cfx-pan-y', state.panY + 'px');
  };
  const sameGroup = (root, peer) => root !== peer && root.dataset.cfxInteractionGroup && root.dataset.cfxInteractionGroup === peer.dataset.cfxInteractionGroup;
  const emitHostEvent = (root, name, detail) => {
    root.dispatchEvent(new CustomEvent(name, { detail: Object.assign({ chartId: root.dataset.cfxChartId || '' }, detail || {}) }));
  };
  const emitSync = (root, payload) => {
    if (!hasFeature(root, 'SynchronizedCharts') || !root.dataset.cfxInteractionGroup) return;
    const detail = Object.assign({ chartId: root.dataset.cfxChartId || '', group: root.dataset.cfxInteractionGroup }, payload);
    root.dispatchEvent(new CustomEvent('cfxsync', { detail }));
    document.querySelectorAll('.cfx-interactive-chart').forEach((peer) => {
      if (sameGroup(root, peer) && hasFeature(peer, 'SynchronizedCharts')) applySync(peer, detail);
    });
  };
  const applyMode = (root, mode) => {
    root.dataset.cfxMode = mode || '';
    root.querySelectorAll('[data-cfx-mode-button]').forEach((button) => {
      button.setAttribute('aria-pressed', button.dataset.cfxModeButton === root.dataset.cfxMode ? 'true' : 'false');
    });
  };
  const setMode = (root, mode) => applyMode(root, root.dataset.cfxMode === mode ? '' : mode);
  const resetViewport = (root) => {
    applyViewport(root, { zoom: 1, panX: 0, panY: 0 });
    root.dataset.cfxBrush = '';
    root.dataset.cfxMode = '';
    root.querySelectorAll('[data-cfx-mode-button]').forEach((button) => button.setAttribute('aria-pressed', 'false'));
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
        const next = Number.isFinite(current) && current + 1 < route.steps.length ? current + 1 : 0;
        startScenarioPlayback(root, route, next, false, false);
      } else if (scenario.playback) {
        setScenarioPlaybackState(root, scenario.playback, route, false);
      }
    }
    applySelectionSetByTargets(root, snapshot.selectedTargets || [], true);
    renderCompare(root);
    storeInteractionState(root, snapshot);
    if (emit !== false) emitHostEvent(root, 'cfxstateapplied', { snapshot });
    if (sync !== false) emitSync(root, { action: 'state', state: snapshot });
  };
  const zoomBy = (root, factor) => {
    if (!hasFeature(root, 'Zoom')) return;
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
    image.onload = () => {
      const size = svgSize(serialized.svg);
      const scale = Math.max(1, Math.ceil(window.devicePixelRatio || 1));
      const canvas = document.createElement('canvas');
      canvas.width = size.width * scale;
      canvas.height = size.height * scale;
      const context = canvas.getContext('2d');
      if (!context) {
        URL.revokeObjectURL(sourceUrl);
        return;
      }
      context.setTransform(scale, 0, 0, scale, 0, 0);
      context.clearRect(0, 0, size.width, size.height);
      context.drawImage(image, 0, 0, size.width, size.height);
      canvas.toBlob((blob) => {
        URL.revokeObjectURL(sourceUrl);
        if (blob) downloadBlob(root, blob, 'png', 'png');
      }, 'image/png');
    };
    image.onerror = () => URL.revokeObjectURL(sourceUrl);
    image.src = sourceUrl;
  };
  const setSeriesMuted = (root, series, muted) => {
    root.querySelectorAll('[data-cfx-series]').forEach((node) => {
      if ((node.dataset ? node.dataset.cfxSeries : undefined) !== series) return;
      const role = node.dataset ? node.dataset.cfxRole || '' : '';
      if (role.indexOf('legend') === 0) {
        node.dataset.cfxMuted = muted ? 'true' : 'false';
        return;
      }
      node.classList.toggle('cfx-series-muted', muted);
    });
  };
  const setSeriesIsolation = (root, series, isolated) => {
    root.querySelectorAll('[data-cfx-series]').forEach((node) => {
      const data = node.dataset || {};
      const role = data.cfxRole || '';
      const sameSeries = data.cfxSeries === series;
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
      node.classList.toggle('cfx-series-isolated-out', isolated && !sameSeries);
    });
    if (isolated) root.dataset.cfxIsolatedSeries = series;
    else root.removeAttribute('data-cfx-isolated-series');
  };
  const toggleSeriesFocus = (root, item, emit, sync) => {
    if (!hasFeature(root, 'LegendToggles')) return;
    const series = item.dataset.cfxSeries;
    if (series === undefined) return;
    const isolated = root.dataset.cfxIsolatedSeries !== series;
    setSeriesIsolation(root, series, isolated);
    emitHostEvent(root, 'cfxseriesfocus', { series, isolated });
    if (sync !== false) emitSync(root, { action: 'series-focus', series, isolated });
  };
  const toggleSeries = (root, item) => {
    if (!hasFeature(root, 'LegendToggles')) return;
    const series = item.dataset.cfxSeries;
    if (series === undefined) return;
    if (root.dataset.cfxIsolatedSeries) setSeriesIsolation(root, series, false);
    const muted = item.dataset.cfxMuted !== 'true';
    setSeriesMuted(root, series, muted);
    emitHostEvent(root, 'cfxseries', { series, muted });
    emitSync(root, { action: 'series', series, muted });
  };
  const toggleSelection = (root, node) => {
    if (!hasFeature(root, 'Selection')) return;
    const selected = !node.classList.contains('cfx-selected');
    setNodeSelected(node, selected);
    const target = targetIdentity(node);
    emitHostEvent(root, 'cfxselect', { label: text(node), selected, target });
    emitSync(root, { action: 'selection', label: text(node), selected, target });
    publishCompare(root, true);
  };
  const setNodeSelected = (node, selected) => {
    node.classList.toggle('cfx-selected', selected);
    node.setAttribute('aria-selected', selected ? 'true' : 'false');
  };
  const setNodeHovered = (node, hovered, related) => {
    node.classList.toggle('cfx-hovered', hovered);
    node.classList.toggle('cfx-hover-related', related && !hovered);
  };
  const targetRelated = (node, target) => {
    if (!target) return false;
    const data = node.dataset || {};
    if (target.series !== undefined && data.cfxSeries === String(target.series)) return true;
    if (target.point !== undefined && data.cfxPoint === String(target.point)) return true;
    if (target.label && (data.cfxLabel === target.label || data.cfxText === target.label || node.getAttribute('aria-label') === target.label)) return true;
    return false;
  };
  const clearHover = (root, emit, sync) => {
    root.removeAttribute('data-cfx-hovering');
    root.removeAttribute('data-cfx-hover-label');
    root.removeAttribute('data-cfx-hover-key');
    clearReveals(root, 'hover');
    clearReveals(root, 'crosshair');
    clearReveals(root, 'navigate');
    root.querySelectorAll('.cfx-hovered,.cfx-hover-related').forEach((node) => node.classList.remove('cfx-hovered', 'cfx-hover-related'));
    if (emit !== false) emitHostEvent(root, 'cfxhoverclear', {});
    if (sync !== false) emitSync(root, { action: 'hover-clear' });
  };
  const applyHoverByTarget = (root, target) => {
    if (!target) return false;
    let matched = false;
    root.querySelectorAll(targetSelector).forEach((node) => {
      const hovered = matchesTargetIdentity(node, target);
      const related = !hovered && targetRelated(node, target);
      if (hovered || related) matched = true;
      setNodeHovered(node, hovered, related);
    });
    if (matched) {
      root.dataset.cfxHovering = 'true';
      root.dataset.cfxHoverLabel = target.label || target.role || target.id || '';
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
      root.querySelectorAll(targetSelector).forEach((node) => {
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
    if (data.cfxSeries !== undefined && data.cfxPoint === undefined) return 'Series ' + data.cfxSeries;
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
    layer.replaceChildren();
    layer.hidden = false;
    (nodes || []).forEach((node) => {
      if (!node || !stage.contains(node) || node.closest('[data-cfx-role="legend-item"]')) return;
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
      label.style.left = clamp(rect.left + rect.width / 2 - stageRect.left, 8 + halfWidth, maxX) + 'px';
      label.style.top = clamp(rect.top - stageRect.top - label.offsetHeight - 8, 8, maxY) + 'px';
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
      root.querySelectorAll(targetSelector).forEach((node) => {
        if (!matchesTargetIdentity(node, target)) return;
        const key = targetKey(targetIdentity(node));
        if (!key || seen.has(key)) return;
        seen.add(key);
        nodes.push(node);
      });
    });
    return revealNodes(root, nodes, emit, sync, source);
  };
  const setHover = (root, node, emit, sync) => {
    const target = targetIdentity(node);
    clearHover(root, false, false);
    applyHoverByTarget(root, target);
    root.dataset.cfxHoverKey = targetKey(target);
    recordFocusTrail(root, target, emit, sync);
    revealNodes(root, [node], emit, sync, 'hover');
    if (emit !== false) emitHostEvent(root, 'cfxhover', { label: text(node), target });
    if (sync !== false) emitSync(root, { action: 'hover', label: text(node), target });
  };
  const hideCrosshair = (root, crosshair) => {
    if (crosshair) crosshair.hidden = true;
    root.removeAttribute('data-cfx-crosshair');
  };
  const nearestPoint = (root, event) => {
    const stage = root.querySelector('.cfx-stage');
    if (!stage) return null;
    const stageRect = stage.getBoundingClientRect();
    if (event.clientX < stageRect.left || event.clientX > stageRect.right || event.clientY < stageRect.top || event.clientY > stageRect.bottom) return null;
    let best = null;
    root.querySelectorAll('[data-cfx-point]').forEach((node) => {
      if (node.closest('[data-cfx-role="legend-item"]') || node.classList.contains('cfx-series-muted')) return;
      const box = node.getBoundingClientRect();
      if (!box.width && !box.height) return;
      const x = box.left + box.width / 2;
      const y = box.top + box.height / 2;
      const dx = x - event.clientX;
      const dy = y - event.clientY;
      const distance = Math.sqrt(dx * dx + dy * dy);
      if (!best || distance < best.distance) best = { node, x, y, distance };
    });
    return best && best.distance <= 120 ? best : null;
  };
  const showCrosshair = (root, crosshair, point, event, emit) => {
    if (!crosshair || !point) return;
    const stage = root.querySelector('.cfx-stage');
    if (!stage) return;
    const rect = stage.getBoundingClientRect();
    crosshair.hidden = false;
    crosshair.style.setProperty('--cfx-crosshair-x', (point.x - rect.left) + 'px');
    crosshair.style.setProperty('--cfx-crosshair-y', (point.y - rect.top) + 'px');
    const label = crosshair.querySelector('[data-cfx-crosshair-label]');
    if (label) label.textContent = text(point.node);
    const target = targetIdentity(point.node);
    root.dataset.cfxCrosshair = targetKey(target);
    if (emit !== false) {
      emitHostEvent(root, 'cfxcrosshair', { label: text(point.node), target, x: event.clientX, y: event.clientY });
      emitSync(root, { action: 'crosshair', label: text(point.node), target });
    }
  };
  const updateNearestPoint = (root, crosshair, tip, event) => {
    if (!hasFeature(root, 'Crosshair')) return;
    const point = nearestPoint(root, event);
    if (!point) {
      hideCrosshair(root, crosshair);
      clearHover(root, true, true);
      return;
    }
    const target = targetIdentity(point.node);
    const key = targetKey(target);
    if (root.dataset.cfxHoverKey !== key) {
      setHover(root, point.node, true, true);
      showCrosshair(root, crosshair, point, event, true);
      showTip(root, tip, point.node, event);
    } else {
      showCrosshair(root, crosshair, point, event, false);
      moveTip(tip, event, point.node);
    }
  };
  const focusAdjacentTarget = (root, node, key) => {
    const targets = interactiveTargets(root);
    if (!targets.length) return false;
    const current = Math.max(0, targets.indexOf(node));
    let next = current;
    if (key === 'Home') next = 0;
    else if (key === 'End') next = targets.length - 1;
    else if (key === 'ArrowLeft' || key === 'ArrowUp') next = current <= 0 ? targets.length - 1 : current - 1;
    else if (key === 'ArrowRight' || key === 'ArrowDown') next = current >= targets.length - 1 ? 0 : current + 1;
    else return false;
    const targetNode = targets[next];
    if (!targetNode) return false;
    if (targetNode.focus) {
      try { targetNode.focus({ preventScroll: true }); } catch { targetNode.focus(); }
    }
    const target = targetIdentity(targetNode);
    emitHostEvent(root, 'cfxnavigate', { label: text(targetNode), target, index: next, count: targets.length, key });
    emitSync(root, { action: 'navigate', label: text(targetNode), target, index: next, count: targets.length, key });
    return true;
  };
  const applySelectionByLabel = (root, label, selected) => {
    if (!label) return;
    root.querySelectorAll(targetSelector).forEach((node) => {
      if (text(node) === label) setNodeSelected(node, selected);
    });
  };
  const matchesTargetIdentity = (node, target) => {
    if (!target) return false;
    const data = node.dataset || {};
    if (target.id && (node.id === target.id || data.cfxId === target.id)) return true;
    if (target.series !== undefined && target.point !== undefined && data.cfxSeries === String(target.series) && data.cfxPoint === String(target.point)) return true;
    if (target.series !== undefined && target.point === undefined && data.cfxSeries === String(target.series) && target.role && data.cfxRole === target.role) return true;
    if (target.role && data.cfxRole === target.role && target.label && (data.cfxLabel === target.label || data.cfxText === target.label || node.getAttribute('aria-label') === target.label)) return true;
    if (target.role && data.cfxRole === target.role && target.value && (data.cfxValue === target.value || data.cfxY === target.value || data.cfxEnd === target.value)) return true;
    return false;
  };
  const applySelectionByTarget = (root, target, selected) => {
    if (!target) return false;
    let matched = false;
    root.querySelectorAll(targetSelector).forEach((node) => {
      if (!matchesTargetIdentity(node, target)) return;
      matched = true;
      setNodeSelected(node, selected);
    });
    return matched;
  };
  const clearSelections = (root) => {
    root.querySelectorAll('.cfx-selected').forEach((node) => {
      node.classList.remove('cfx-selected');
      node.removeAttribute('aria-selected');
    });
  };
  const applySelectionSetByTargets = (root, targets, replace) => {
    if (replace !== false) clearSelections(root);
    let count = 0;
    (targets || []).forEach((target) => {
      if (applySelectionByTarget(root, target, true)) count++;
    });
    return count;
  };
  const nodeCenterInRect = (node, rect) => {
    const box = node.getBoundingClientRect();
    if (!box.width && !box.height) return null;
    const x = box.left + box.width / 2;
    const y = box.top + box.height / 2;
    if (x < rect.left || x > rect.right || y < rect.top || y > rect.bottom) return null;
    return { x, y };
  };
  const selectTargetsInBox = (root, rect, append) => {
    if (!hasFeature(root, 'Selection')) return [];
    if (!append) clearSelections(root);
    const targets = [];
    root.querySelectorAll(lassoSelector).forEach((node) => {
      if (node.closest('[data-cfx-role="legend-item"]')) return;
      if (!nodeCenterInRect(node, rect)) return;
      setNodeSelected(node, true);
      targets.push(targetIdentity(node));
    });
    return targets;
  };
  const applySync = (root, detail) => {
    if (!detail || detail.chartId === root.dataset.cfxChartId) return;
    if (detail.action === 'viewport' && detail.state) applyViewport(root, detail.state);
    else if (detail.action === 'brush') root.dataset.cfxBrush = detail.bounds || '';
    else if (detail.action === 'selection') {
      if (!applySelectionByTarget(root, detail.target, detail.selected === true)) applySelectionByLabel(root, detail.label || '', detail.selected === true);
      renderCompare(root);
    } else if (detail.action === 'lasso') {
      applySelectionSetByTargets(root, detail.targets || [], detail.replace !== false);
      renderCompare(root);
    } else if (detail.action === 'compare') {
      applySelectionSetByTargets(root, detail.targets || [], true);
      renderCompare(root);
    }
    else if (detail.action === 'hover') {
      clearHover(root, false, false);
      applyHoverByTarget(root, detail.target);
      revealTargets(root, [detail.target], false, false, detail.source || 'hover');
    } else if (detail.action === 'hover-clear') clearHover(root, false, false);
    else if (detail.action === 'crosshair' || detail.action === 'navigate') {
      clearHover(root, false, false);
      applyHoverByTarget(root, detail.target);
      revealTargets(root, [detail.target], false, false, detail.action);
    }
    else if (detail.action === 'trail') applyFocusTrail(root, detail.trail || []);
    else if (detail.action === 'reveal') revealTargets(root, detail.targets || [], false, false, detail.source || '');
    else if (detail.action === 'reveal-clear') clearReveals(root, detail.source || '');
    else if (detail.action === 'state') applyInteractionState(root, detail.state, false, false);
    else if (detail.action === 'series' && detail.series !== undefined) setSeriesMuted(root, detail.series, detail.muted === true);
    else if (detail.action === 'series-focus') setSeriesIsolation(root, detail.series || '', detail.isolated === true);
    else if (detail.action === 'scenario') setScenario(root, detail.scenarioId || '', false, false);
    else if (detail.action === 'scenario-step') {
      if (detail.scenarioId && root.dataset.cfxActiveScenario !== detail.scenarioId) setScenario(root, detail.scenarioId, false, false);
      setScenarioStep(root, detail.index, false, false);
    }
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
      metadata: readJson(button.dataset.cfxScenarioMetadata || '{}', {})
    };
  };
  const scenarioTargetMatches = (node, step) => {
    const data = node.dataset || {};
    const kind = step.targetKind || '';
    const target = step.targetId || '';
    if (kind === 'series' && data.cfxSeries === target) return true;
    if (kind === 'point' && data.cfxPoint === target) return true;
    if (kind === 'point' && data.cfxSeries !== undefined && data.cfxPoint !== undefined && (data.cfxSeries + ':' + data.cfxPoint === target || data.cfxSeries + '.' + data.cfxPoint === target)) return true;
    if (kind === 'annotation' && (data.cfxRole || '').indexOf('annotation') === 0 && (data.cfxLabel === target || data.cfxKind === target || data.cfxValue === target || data.cfxId === target || node.id === target)) return true;
    if (kind === 'element' && (data.cfxId === target || node.id === target)) return true;
    if (data.cfxRole === kind && (data.cfxLabel === target || data.cfxText === target || data.cfxValue === target || data.cfxY === target)) return true;
    return false;
  };
  const scenarioTargetCandidates = (root, route) => {
    const candidates = new Set(root.querySelectorAll('.cfx-interactive-region,[data-cfx-label],[data-cfx-role],[data-cfx-series],[data-cfx-point],[data-cfx-id]'));
    const elementIds = new Set((route ? route.steps : [])
      .filter((step) => (step.targetKind || '') === 'element' && step.targetId)
      .map((step) => step.targetId));
    if (elementIds.size) {
      root.querySelectorAll('[id]').forEach((node) => {
        if (!elementIds.has(node.id) || node.closest('defs')) return;
        if (node.querySelector('.cfx-interactive-region,[data-cfx-label],[data-cfx-role],[data-cfx-series],[data-cfx-point],[data-cfx-id]')) return;
        candidates.add(node);
      });
    }
    return Array.from(candidates);
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
      node.classList.toggle('cfx-scenario-step-muted', !active);
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
    revealNodes(root, Array.from(targets), emit, sync, 'scenario-step');
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
      node.classList.toggle('cfx-scenario-muted', !active);
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
  const scenarioPlaybackDelay = (root) => {
    const delay = Number(root.dataset.cfxScenarioPlaybackDelay || '900');
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
        delay: scenarioPlaybackDelay(root)
      });
    }
  };
  const stopScenarioPlayback = (root, state, emit) => {
    if (root._cfxScenarioPlayback) window.clearInterval(root._cfxScenarioPlayback);
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
    if (!Number.isFinite(index) || index >= route.steps.length) index = 0;
    root._cfxScenarioPlayback = window.setInterval(() => {
      if (index >= route.steps.length) {
        stopScenarioPlayback(root, 'finished', emit);
        return;
      }
      setScenarioStep(root, index++, true, true);
    }, scenarioPlaybackDelay(root));
    if (advanceNow !== false) setScenarioStep(root, index++, true, true);
  };
  const copyScenarioLink = (root) => {
    syncScenarioUrl(root, root.dataset.cfxActiveScenario || '', root.dataset.cfxActiveScenarioStep || '');
    const url = window.location.href;
    if (navigator.clipboard && navigator.clipboard.writeText) navigator.clipboard.writeText(url).catch(() => {});
    emitHostEvent(root, 'cfxscenariolink', { url });
  };
  document.querySelectorAll('.cfx-interactive-chart').forEach((root) => {
    if (root.dataset.cfxRuntimeBound === 'true') return;
    root.dataset.cfxRuntimeBound = 'true';
    const tip = root.querySelector('.cfx-tooltip');
    const stage = root.querySelector('.cfx-stage');
    const brush = root.querySelector('.cfx-brush-box');
    const crosshair = root.querySelector('.cfx-crosshair');
    const compareTray = root.querySelector('[data-cfx-compare-tray]');
    if (!tip) return;
    applyViewport(root, getState(root));
    renderCompare(root);
    if (compareTray) compareTray.addEventListener('pointerdown', (event) => event.stopPropagation());
    if (hasFeature(root, 'StateBookmarks')) {
      root.addEventListener('cfx-capture-state', (event) => {
        const detail = event.detail || {};
        publishInteractionState(root, detail.source || 'host', detail.sync);
      });
      root.addEventListener('cfx-apply-state', (event) => {
        const detail = event.detail || {};
        applyInteractionState(root, detail.snapshot || detail.state || detail, true, detail.sync);
      });
    }
    const targets = interactiveTargets(root);
    targets.forEach((node) => {
      if (!node.hasAttribute('tabindex')) node.setAttribute('tabindex', '0');
      node.addEventListener('pointerenter', (event) => {
        setHover(root, node, true, true);
        showTip(root, tip, node, event);
      });
      node.addEventListener('pointermove', (event) => moveTip(tip, event, node));
      node.addEventListener('pointerleave', () => {
        clearHover(root, true, true);
        hideTip(root, tip, false);
      });
      node.addEventListener('focus', (event) => {
        setHover(root, node, true, true);
        showTip(root, tip, node, event);
      });
      node.addEventListener('blur', () => {
        clearHover(root, true, true);
        hideTip(root, tip, false);
      });
      node.addEventListener('click', (event) => {
        if ((node.dataset ? node.dataset.cfxRole : '') === 'legend-item') {
          if (event.shiftKey) toggleSeriesFocus(root, node, true, true);
          else toggleSeries(root, node);
        }
        else {
          toggleSelection(root, node);
          pinTip(root, tip, node, event);
        }
      });
      node.addEventListener('keydown', (event) => {
        if (!hasFeature(root, 'KeyboardNavigation')) return;
        if ((node.dataset ? node.dataset.cfxRole : '') === 'legend-item' && event.key.toLowerCase() === 'i') {
          event.preventDefault();
          toggleSeriesFocus(root, node, true, true);
          return;
        }
        if (focusAdjacentTarget(root, node, event.key)) {
          event.preventDefault();
          return;
        }
        if (event.key !== 'Enter' && event.key !== ' ') return;
        event.preventDefault();
        if ((node.dataset ? node.dataset.cfxRole : '') === 'legend-item' && event.shiftKey) toggleSeriesFocus(root, node, true, true);
        else node.dispatchEvent(new MouseEvent('click', { bubbles: true }));
      });
    });
    root.querySelectorAll('[data-cfx-zoom]').forEach((button) => {
      button.addEventListener('click', () => zoomBy(root, button.dataset.cfxZoom === 'in' ? 1.25 : 0.8));
    });
    root.querySelectorAll('[data-cfx-mode-button]').forEach((button) => {
      button.addEventListener('click', () => setMode(root, button.dataset.cfxModeButton || ''));
    });
    root.querySelectorAll('[data-cfx-export]').forEach((button) => {
      button.addEventListener('click', () => {
        if (button.dataset.cfxExport === 'png') exportPng(root);
        else exportSvg(root);
      });
    });
    root.querySelectorAll('[data-cfx-scenario]').forEach((button) => {
      button.addEventListener('click', () => setScenario(root, button.dataset.cfxScenario || '', true));
    });
    if (hasFeature(root, 'Scenarios')) {
      root.addEventListener('cfx-set-scenario', (event) => {
        const detail = event.detail || {};
        setScenario(root, detail.scenarioId || '', true, true);
      });
      root.addEventListener('cfx-clear-scenario', () => setScenario(root, '', true, true));
    }
    if (hasFeature(root, 'StepPlayback')) {
      root.querySelectorAll('[data-cfx-scenario-step-control]').forEach((button) => {
        button.addEventListener('click', () => {
          const action = button.dataset.cfxScenarioStepControl || '';
          if (action === 'previous') stepScenario(root, -1);
          else if (action === 'next') stepScenario(root, 1);
          else if (action === 'play') playScenario(root);
          else if (action === 'reset') { stopScenarioPlayback(root, 'paused'); clearScenarioStep(root); clearReveals(root); }
          else if (action === 'link') copyScenarioLink(root);
        });
      });
      root.addEventListener('cfx-set-scenario-step', (event) => {
        const detail = event.detail || {};
        if (detail.scenarioId && root.dataset.cfxActiveScenario !== detail.scenarioId) setScenario(root, detail.scenarioId, true, true);
        const index = detail.index !== undefined ? detail.index : detail.stepIndex;
        setScenarioStep(root, index, true, true);
      });
      root.addEventListener('cfx-clear-scenario-step', () => {
        stopScenarioPlayback(root, 'paused');
        clearScenarioStep(root);
        clearReveals(root);
      });
      root.addEventListener('cfx-play-scenario', (event) => {
        const detail = event.detail || {};
        if (detail.scenarioId && root.dataset.cfxActiveScenario !== detail.scenarioId) setScenario(root, detail.scenarioId, true, true);
        playScenario(root);
      });
      root.addEventListener('cfx-pause-scenario', () => stopScenarioPlayback(root, 'paused'));
      const stepsList = root.querySelector('[data-cfx-scenario-panel-steps]');
      if (stepsList) {
        const focusStep = (target) => {
          const item = target instanceof Element ? target.closest('[data-cfx-scenario-step-index]') : null;
          if (item && stepsList.contains(item)) setScenarioStep(root, Number(item.getAttribute('data-cfx-scenario-step-index')) - 1, true, true);
        };
        stepsList.addEventListener('click', (event) => focusStep(event.target));
        stepsList.addEventListener('keydown', (event) => {
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
    }
    if (stage) {
      stage.addEventListener('wheel', (event) => {
        if (!hasFeature(root, 'Zoom')) return;
        event.preventDefault();
        zoomBy(root, event.deltaY < 0 ? 1.08 : 0.92);
      }, { passive: false });
      let drag = null;
      stage.addEventListener('pointerdown', (event) => {
        if (event.button !== 0) return;
        if (root.dataset.cfxMode === 'pan' && hasFeature(root, 'Pan')) {
          const state = getState(root);
          drag = { mode: 'pan', id: event.pointerId, x: event.clientX, y: event.clientY, panX: state.panX, panY: state.panY };
          stage.setPointerCapture(event.pointerId);
        } else if (root.dataset.cfxMode === 'brush' && hasFeature(root, 'Brush') && brush) {
          const rect = stage.getBoundingClientRect();
          drag = { mode: 'brush', id: event.pointerId, left: event.clientX - rect.left, top: event.clientY - rect.top };
          brush.hidden = false;
          brush.style.left = drag.left + 'px';
          brush.style.top = drag.top + 'px';
          brush.style.width = '0px';
          brush.style.height = '0px';
          stage.setPointerCapture(event.pointerId);
        }
      });
      stage.addEventListener('pointermove', (event) => {
        if (!drag || drag.id !== event.pointerId) {
          updateNearestPoint(root, crosshair, tip, event);
          return;
        }
        if (drag.mode === 'pan') {
          applyViewport(root, { zoom: getState(root).zoom, panX: drag.panX + event.clientX - drag.x, panY: drag.panY + event.clientY - drag.y });
        } else if (drag.mode === 'brush' && brush) {
          const rect = stage.getBoundingClientRect();
          const x = clamp(event.clientX - rect.left, 0, rect.width);
          const y = clamp(event.clientY - rect.top, 0, rect.height);
          const left = Math.min(drag.left, x);
          const top = Math.min(drag.top, y);
          brush.style.left = left + 'px';
          brush.style.top = top + 'px';
          brush.style.width = Math.abs(x - drag.left) + 'px';
          brush.style.height = Math.abs(y - drag.top) + 'px';
        }
      });
      stage.addEventListener('pointerup', (event) => {
        if (!drag || drag.id !== event.pointerId) return;
        if (drag.mode === 'brush' && brush) {
          root.dataset.cfxBrush = [brush.style.left, brush.style.top, brush.style.width, brush.style.height].join(' ');
          const selectedTargets = selectTargetsInBox(root, brush.getBoundingClientRect(), event.shiftKey);
          const replaceSelection = !event.shiftKey;
          emitHostEvent(root, 'cfxbrush', { bounds: root.dataset.cfxBrush });
          if (selectedTargets.length || replaceSelection) emitHostEvent(root, 'cfxlasso', { bounds: root.dataset.cfxBrush, count: selectedTargets.length, targets: selectedTargets });
          emitSync(root, { action: 'brush', bounds: root.dataset.cfxBrush });
          if (selectedTargets.length || replaceSelection) emitSync(root, { action: 'lasso', bounds: root.dataset.cfxBrush, targets: selectedTargets, replace: replaceSelection });
          publishCompare(root, true);
        } else if (drag.mode === 'pan') {
          emitHostEvent(root, 'cfxviewport', { state: getState(root) });
          emitSync(root, { action: 'viewport', state: getState(root) });
        }
        stage.releasePointerCapture(event.pointerId);
        drag = null;
      });
      stage.addEventListener('pointerleave', () => {
        hideCrosshair(root, crosshair);
        clearHover(root, true, true);
        hideTip(root, tip, false);
      });
      stage.addEventListener('pointercancel', () => {
        drag = null;
        hideCrosshair(root, crosshair);
        clearHover(root, true, true);
        hideTip(root, tip, false);
      });
    }
    const reset = root.querySelector('[data-cfx-reset]');
    if (reset) reset.addEventListener('click', () => {
      resetViewport(root);
      emitHostEvent(root, 'cfxreset', {});
      emitHostEvent(root, 'cfxviewport', { state: getState(root) });
      emitSync(root, { action: 'viewport', state: getState(root) });
      clearSelections(root);
      root.querySelectorAll('.cfx-series-muted').forEach((node) => node.classList.remove('cfx-series-muted'));
      root.querySelectorAll('[data-cfx-muted]').forEach((node) => node.removeAttribute('data-cfx-muted'));
      setSeriesIsolation(root, root.dataset.cfxIsolatedSeries || '', false);
      clearFocusTrail(root);
      clearReveals(root);
      if (brush) brush.hidden = true;
      hideCrosshair(root, crosshair);
      hideTip(root, tip, true);
      publishCompare(root, true);
    });
  });
})();
