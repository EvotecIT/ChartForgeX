  const seriesTarget = (node) => {
    const data = node.dataset || {};
    const reference = legendTarget(node) || (['node', 'link', 'region'].includes(data.cfxTargetKind)
      ? { targetKind: data.cfxTargetKind, targetId: data.cfxTargetId } : null);
    return { series: data.cfxSeries, point: data.cfxPoint, seriesKey: seriesKey(node), label: data.cfxLabel || seriesLabel(node),
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
    });
    refreshKeyboardNavigation(root);
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
    });
    if (isolated) root.dataset.cfxIsolatedSeries = seriesTargetToken(target);
    else root.removeAttribute('data-cfx-isolated-series');
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
  const toggleSelection = (root, node) => {
    if (!hasFeature(root, 'Selection')) return;
    const selected = !node.classList.contains('cfx-selected');
    setNodeSelected(node, selected);
    syncResetControl(root);
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
    root.removeAttribute('data-cfx-hover-mode');
    root.removeAttribute('data-cfx-hover-unit');
    clearReveals(root, 'hover');
    clearReveals(root, 'crosshair');
    clearReveals(root, 'navigate');
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
    if (itemUnit) return sameSeries && (node.contains(itemUnit) || itemUnit.contains(node));
    return sameSeries && (!pointUnits || data.cfxPoint === String(target.point) || (data.cfxRole === 'series' && data.cfxPoint === undefined));
  };
  // 'series' keeps the pointed series at full strength while other series recede;
  // 'shared' (crosshair over the plot background) keeps every series at full strength.
  const applyHoverByTarget = (root, target, mode) => {
    if (!target) return false;
    const nodes = Array.from(root.querySelectorAll(targetSelector));
    const localNode = nodes.find((node) => matchesTargetIdentity(node, target));
    if (!localNode && target.targetKind && target.targetId) return false;
    // A peer's equivalent mark may have a different local point ordinal or series position.
    const localTarget = localNode ? targetIdentity(localNode) : target;
    const hoverMode = mode === 'shared' ? 'shared' : 'series';
    const pointUnits = hoverMode === 'series' && pointLegendUnits(root, localTarget);
    const itemUnit = hoverMode === 'series' && legendItemUnit(root, localTarget);
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
    const placed = [];
    const overlaps = (candidate) => placed.some((item) => candidate.left < item.right + 6 && candidate.right + 6 > item.left && candidate.top < item.bottom + 6 && candidate.bottom + 6 > item.top);
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
  const setHover = (root, node, emit, sync, mode) => {
    const target = targetIdentity(node);
    clearHover(root, false, false);
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
  const pointerCandidates = (root, event, searchNearest) => {
    const stage = root.querySelector('.cfx-stage');
    if (!stage) return null;
    const stageRect = stage.getBoundingClientRect();
    if (event.clientX < stageRect.left || event.clientX > stageRect.right || event.clientY < stageRect.top || event.clientY > stageRect.bottom) return null;
    const styles = new Map();
    const hit = pointLabelTarget(root, event.target) || (event.target instanceof Element ? event.target.closest(targetSelector) : null);
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
    const plotSurface = event.target === stage || event.target === svg || event.target instanceof SVGElement
      && svg?.contains(event.target) && !event.target.closest('foreignObject')
      && inferredPlotSurfaceRoles.has(event.target.dataset.cfxRole);
    if (!searchNearest || !native && !plotSurface) return { native, observation: null };
    let best = null;
    root.querySelectorAll('[data-cfx-point]').forEach((node) => {
      if (!usesCartesianCoordinates(node) || !pointerTargetPaint(node, styles)) return;
      const box = node.getBoundingClientRect();
      if (!box.width && !box.height) return;
      const x = box.left + box.width / 2, y = box.top + box.height / 2;
      const distance = Math.hypot(x - event.clientX, y - event.clientY);
      if (!best || distance < best.distance) best = { node, x, y, distance, exact: false };
    });
    return { native, observation: best };
  };
  const tooltipAcquiresPoint = (root, point) => {
    if (!point || !hasFeature(root, 'Tooltips')) return false;
    if (point.exact) return true;
    const range = root.dataset.cfxTooltipRange || 'distance';
    return range === 'nearest' || range === 'distance' && point.distance <= Number(root.dataset.cfxTooltipDistance ?? 120);
  };
  const showCrosshair = (root, crosshair, point, event) => {
    if (!crosshair || !point) return;
    const stage = root.querySelector('.cfx-stage');
    if (!stage) return;
    const local = stagePoint(stage, point);
    positionStageViewport(stage, crosshair);
    crosshair.hidden = false;
    crosshair.style.setProperty('--cfx-crosshair-x', (local.x - stage.scrollLeft) + 'px');
    crosshair.style.setProperty('--cfx-crosshair-y', (local.y - stage.scrollTop) + 'px');
    const label = crosshair.querySelector('[data-cfx-crosshair-label]');
    if (label) label.textContent = text(point.node);
    const target = targetIdentity(point.node);
    const key = targetKey(target), mode = crosshairHoverMode(event, point.node);
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
  const crosshairHoverMode = (event, node) => {
    const hit = event && event.target instanceof Element ? event.target.closest('[data-cfx-series]') : null;
    return hit && (hit.dataset || {}).cfxSeries === (node.dataset || {}).cfxSeries ? 'series' : 'shared';
  };
  const updateNearestPoint = (root, crosshair, tip, event) => {
    const guideEnabled = hasFeature(root, 'Crosshair');
    if (!guideEnabled && !hasFeature(root, 'Tooltips')) return;
    if (event.target instanceof Element && event.target.closest('[data-cfx-role="legend-item"]')) {
      // Legend items own their hover summary; inferred observations must not replace it.
      hideCrosshair(root, crosshair);
      return;
    }
    const searchNearest = guideEnabled || root.dataset.cfxTooltipRange !== 'exact';
    const candidates = pointerCandidates(root, event, searchNearest);
    const observation = candidates && candidates.observation;
    const native = candidates && candidates.native;
    // Nearest/bounded acquisition can refine a line summary to a real observation; Exact retains the summary.
    const tooltipPoint = tooltipAcquiresPoint(root, observation) ? observation : tooltipAcquiresPoint(root, native) ? native : null;
    const guidePoint = guideEnabled && observation && (observation.exact || observation.distance <= 120) ? observation : null;
    if (!tooltipPoint && !guidePoint) {
      hideCrosshair(root, crosshair);
      clearHover(root, true, true);
      hideTip(root, tip, false);
      return;
    }
    const point = tooltipPoint || guidePoint;
    const key = targetKey(targetIdentity(point.node));
    const mode = point.node.hasAttribute('data-cfx-point') && usesCartesianCoordinates(point.node)
      ? crosshairHoverMode(event, point.node) : 'series';
    const changed = root.dataset.cfxHoverKey !== key || root.dataset.cfxHoverMode !== mode;
    if (changed) setHover(root, point.node, true, true, mode);
    if (guidePoint) showCrosshair(root, crosshair, guidePoint, event);
    else hideCrosshair(root, crosshair);
    // Recheck paint on each event: host CSS may change while the semantic target remains the same.
    if (tooltipPoint) showTip(root, tip, tooltipPoint.node, event);
    else hideTip(root, tip, false);
  };
  const applySelectionByLabel = (root, label, selected) => {
    if (!label) return;
    root.querySelectorAll(targetSelector).forEach((node) => {
      if (text(node) === label) setNodeSelected(node, selected);
    });
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
    let matched = false;
    root.querySelectorAll(targetSelector).forEach((node) => {
      if (!matchesTargetIdentity(node, target)) return;
      matched = true;
      setNodeSelected(node, selected);
    });
    syncResetControl(root);
    return matched;
  };
  const clearSelections = (root) => {
    root.querySelectorAll('.cfx-selected').forEach((node) => {
      node.classList.remove('cfx-selected');
      node.removeAttribute('aria-selected');
    });
    syncResetControl(root);
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
    syncResetControl(root);
    return targets;
  };
  const applySync = (root, detail) => {
    if (!detail || detail.chartId === root.dataset.cfxChartId) return;
    if (detail.action === 'viewport' && detail.state) applyViewport(root, detail.state);
    else if (detail.action === 'brush') {
      root.dataset.cfxBrush = detail.bounds || '';
      syncResetControl(root);
    }
    else if (detail.action === 'selection') {
      if (!applySelectionByTarget(root, detail.target, detail.selected === true) && !(detail.target && (detail.target.id || detail.target.targetId))) applySelectionByLabel(root, detail.label || '', detail.selected === true);
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
