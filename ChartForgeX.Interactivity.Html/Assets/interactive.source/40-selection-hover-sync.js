  const seriesTarget = (node) => {
    const data = node.dataset || {};
    return { series: data.cfxSeries, point: data.cfxPoint, seriesKey: seriesKey(node), label: data.cfxLabel || seriesLabel(node) };
  };
  const seriesTargetToken = (target) => target ? [target.series ?? '', target.point ?? ''].join(':') : '';
  const matchesLocalSeriesTarget = (node, target) => {
    if (!target) return false;
    const data = node.dataset || {};
    return data.cfxSeries === String(target.series) && (target.point === undefined || data.cfxPoint === String(target.point));
  };
  const resolveSeriesTarget = (root, target) => {
    if (!target || !target.seriesKey) return null;
    const legends = Array.from(root.querySelectorAll('[data-cfx-role="legend-item"][data-cfx-series]'));
    let matchingSeries = legends.filter((item) => seriesKey(item) === target.seriesKey);
    if (!matchingSeries.length) matchingSeries = Array.from(root.querySelectorAll('[data-cfx-series]')).filter((item) => seriesKey(item) === target.seriesKey);
    if (!matchingSeries.length) return null;
    if (target.point === undefined) {
      const localSeries = (matchingSeries[0].dataset || {}).cfxSeries;
      return localSeries === undefined ? null : { series: localSeries, seriesKey: target.seriesKey, label: target.label };
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
    });
  };
  const setSeriesIsolation = (root, target, isolated) => {
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
      node.classList.toggle('cfx-series-isolated-out', isolated && !sameSeries);
    });
    if (isolated) root.dataset.cfxIsolatedSeries = seriesTargetToken(target);
    else root.removeAttribute('data-cfx-isolated-series');
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
    if (target.seriesKey && seriesKey(node) === target.seriesKey) return true;
    if (!target.seriesKey && target.series !== undefined && data.cfxSeries === String(target.series)) return true;
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
