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
  const pointerCandidates = (root, event, searchNearest, nativeTarget = event.target) => {
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
      if (!isInteractiveTarget(node) || !usesCartesianCoordinates(node) || !pointerTargetPaint(node, styles)) return;
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
    if (nativeTarget instanceof Element && nativeTarget.closest('[data-cfx-role="legend-item"]')) {
      // Legend items own their hover summary; inferred observations must not replace it.
      hideCrosshair(root, crosshair);
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
