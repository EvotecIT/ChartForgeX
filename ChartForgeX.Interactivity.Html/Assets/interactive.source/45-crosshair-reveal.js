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
      // The live region reads its text in DOM order; separate readouts for screen readers without changing the paint.
      if (shown.length) {
        const separator = document.createElement('span');
        separator.textContent = '; ';
        separator.style.cssText = 'position:absolute;width:1px;height:1px;overflow:hidden;clip:rect(0 0 0 0);white-space:nowrap';
        layer.insertBefore(separator, label);
      }
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
    if (!nodes.length) return [];
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

  const usesPolarCoordinates = (node) => !!node.closest('[data-cfx-coordinate-system="polar"]');
  const usesCartesianCoordinates = (node) => node.closest('[data-cfx-coordinate-system]')?.dataset.cfxCoordinateSystem === 'cartesian';
  const inferredPlotSurfaceRoles = new Set(['background', 'frame-card', 'frame-card-shadow', 'content-surface',
    'grid-x', 'grid-y', 'axis-x', 'axis-y', 'axis-secondary-y']);

  const categoricalPointerKind = node => ['line', 'stepline', 'area', 'steparea', 'stackedarea', 'bar', 'horizontalbar'].includes((node.dataset.cfxKind || '').toLowerCase());
  const eventCoarsePointer = root => root.ownerDocument.defaultView.matchMedia?.('(pointer: coarse)').matches === true;
  const radarPointerPoint = (root, event, styles) => {
    const spoke = root.querySelector('[data-cfx-role="radar-spoke"]'), matrix = spoke && svgScreenMatrix(spoke);
    if (!matrix) return null;
    const center = new DOMPoint(Number(spoke.getAttribute('x1')), Number(spoke.getAttribute('y1'))).matrixTransform(matrix);
    const edge = new DOMPoint(Number(spoke.getAttribute('x2')), Number(spoke.getAttribute('y2'))).matrixTransform(matrix);
    if (Math.hypot(event.clientX - center.x, event.clientY - center.y) > Math.hypot(edge.x - center.x, edge.y - center.y) + 12) return null;
    const angle = Math.atan2(event.clientY - center.y, event.clientX - center.x);
    const tip = root.querySelector('.cfx-tooltip');
    const points = Array.from(root.querySelectorAll('[data-cfx-role="radar-point-source"][data-cfx-angle]'))
      .filter(node => isInteractiveTarget(node) && pointerTargetPaint(node, styles) && tooltipSourceInView({ node, tip: tip || node })).map(node => {
        const box = node.getBoundingClientRect(), x = box.left + box.width / 2, y = box.top + box.height / 2;
        const theta = Math.atan2(y - center.y, x - center.x);
        return { node, x, y, distance: Math.hypot(x - event.clientX, y - event.clientY),
          rank: Math.abs(Math.atan2(Math.sin(theta - angle), Math.cos(theta - angle))), exact: true };
      });
    return points.sort((a, b) => a.rank - b.rank || a.distance - b.distance)[0] || null;
  };

  const pointerCandidates = (root, event, searchNearest, nativeTarget = event.target, withinSeries = null) => {
    const stage = root.querySelector('.cfx-stage');
    if (!stage) return null;
    const stageRect = stage.getBoundingClientRect();
    if (event.clientX < stageRect.left || event.clientX > stageRect.right || event.clientY < stageRect.top || event.clientY > stageRect.bottom) return null;
    const styles = new Map();
    let hit = pointLabelTarget(root, nativeTarget) || (nativeTarget instanceof Element ? nativeTarget.closest(targetSelector) : null);


    while (hit && root.contains(hit) && !isInteractiveTarget(hit)) {
      if (hit.dataset.cfxRole === 'legend-entry-omitted') { hit = null; break; }
      hit = hit.parentElement && hit.parentElement.closest(targetSelector);
    }
    let native = null, enlarged = false;
    const svg = stage.querySelector('svg');
    const plotSurface = nativeTarget === stage || nativeTarget === svg || nativeTarget instanceof SVGElement
      && svg?.contains(nativeTarget) && !nativeTarget.closest('foreignObject')
      && (isPassiveTarget(nativeTarget) || inferredPlotSurfaceRoles.has(nativeTarget.dataset.cfxRole));
    const radar = root.querySelector('[data-cfx-role="radar-spoke"]');
    if (radar && (hit || plotSurface) && !hit?.closest('[data-cfx-role="legend-item"]') && nativeTarget instanceof SVGElement) {
      const point = radarPointerPoint(root, event, styles);
      if (point) return { native: point, observation: point };
    }
    // A painted bar owns its interior even when a line or a generous point hit
    // area sits above it in the SVG paint order.
    const bar = Array.from(root.querySelectorAll('[data-cfx-point]')).find(node => {
      if (!['bar', 'horizontalbar'].includes((node.dataset.cfxKind || '').toLowerCase()) || !pointerTargetPaint(node, styles)) return false;
      const box = node.querySelector('[data-cfx-role="bar"],[data-cfx-role="horizontal-bar"]')?.getBoundingClientRect() || nativeRegionScreenBox(node) || node.getBoundingClientRect();
      return event.clientX >= box.left && event.clientX <= box.right && event.clientY >= box.top && event.clientY <= box.bottom;
    });
    if (bar && (hit || plotSurface) && !hit?.closest('[data-cfx-role="legend-item"]')) {
      const box = bar.querySelector('[data-cfx-role="bar"],[data-cfx-role="horizontal-bar"]')?.getBoundingClientRect() || nativeRegionScreenBox(bar) || bar.getBoundingClientRect();
      native = { node: bar, x: (box.left + box.right) / 2, y: (box.top + box.bottom) / 2, distance: 0, exact: true };
      return { native, observation: native };
    }
    if (hit && root.contains(hit) && pointerTargetPaint(hit, styles)) {
      const box = hit.getBoundingClientRect();
      const summary = renderedTargetKind(hit) === 'series' && !hit.hasAttribute('data-cfx-point') && !hit.hasAttribute('data-cfx-value');
      native = { node: hit, x: usesPolarCoordinates(hit) || summary ? event.clientX : box.left + box.width / 2,
        y: usesPolarCoordinates(hit) || summary ? event.clientY : box.top + box.height / 2, distance: 0, exact: true, summary };
      enlarged = nativeTarget instanceof Element && nativeTarget.closest('[data-cfx-browser-hit-area]') && hit.hasAttribute('data-cfx-point') && usesCartesianCoordinates(hit);
      if (!summary && !enlarged) return { native, observation: hit.hasAttribute('data-cfx-point') && usesCartesianCoordinates(hit) ? native : null };
    }


    if (!searchNearest && !enlarged || !native && !plotSurface) return { native, observation: null };
    const candidates = [];
    root.querySelectorAll('[data-cfx-point]').forEach((node) => {
      if (!isInteractiveTarget(node) || !usesCartesianCoordinates(node)
        || withinSeries && !matchesLocalSeriesTarget(node, withinSeries)) return;
      const box = node.getBoundingClientRect();
      if (!box.width && !box.height) return;
      const area = enlarged && node.querySelector('[data-cfx-browser-hit-area]')?.getBoundingClientRect();
      const exact = area && event.clientX >= area.left && event.clientX <= area.right && event.clientY >= area.top && event.clientY <= area.bottom;
      if (enlarged && !exact) return;
      const x = box.left + box.width / 2, y = box.top + box.height / 2;
      const distance = Math.hypot(x - event.clientX, y - event.clientY);
      const category = !enlarged && root.dataset.cfxTooltipRange === 'auto' && categoricalPointerKind(node);
      const rank = withinSeries || category ? Math.abs(root._cfxDataProjection?.horizontal ? y - event.clientY : x - event.clientX) : distance;
      candidates.push({ node, x, y, distance, rank, category, order: candidates.length, exact: !!exact });
    });

    candidates.sort((first, second) => first.rank - second.rank || first.distance - second.distance || first.order - second.order);
    return { native, observation: candidates.find(candidate => pointerTargetPaint(candidate.node, styles)) || null };
  };
  const pointerActivationTarget = (root, node, event) => {
    if (event instanceof MouseEvent && event.detail > 0 && isDataTarget(node)) {
      const candidates = pointerCandidates(root, event, true, event.target);
      const acquired = candidates?.observation?.exact ? candidates.observation : acquiredTooltipPoint(root, candidates);
      if (acquired && isDataTarget(acquired.node)) return acquired.node;
    }
    if (isDataTarget(node) || renderedTargetKind(node) !== 'series' || !usesCartesianCoordinates(node)
      || !(event instanceof MouseEvent) || event.detail <= 0) return node;


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
    return range === 'nearest' || (range === 'distance' || range === 'auto') && (point.category ? point.rank : point.distance) <= Math.max(
      eventCoarsePointer(root) ? 24 : 0, Number(root.dataset.cfxTooltipDistance ?? 120));
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

    const x = clamp(local.x - bounds.left, 0, bounds.width), y = clamp(local.y - bounds.top, 0, bounds.height);
    crosshair.style.setProperty('--cfx-crosshair-x', x + 'px');
    crosshair.style.setProperty('--cfx-crosshair-y', y + 'px');
    crosshair.hidden = false;
    const label = crosshair.querySelector('[data-cfx-crosshair-label]');
    if (label) label.hidden = true;
    paintCrosshairAxisLabels(root, crosshair, point.node, x, y, bounds);
    const target = targetIdentity(point.node);
    const key = targetKey(target), mode = crosshairHoverMode(event, point.node, nativeTarget);

    const changed = root.dataset.cfxCrosshair !== key || root._cfxCrosshairMode !== mode;
    root.dataset.cfxCrosshair = key;
    root._cfxCrosshairMode = mode;
    if (changed) {
      emitHostEvent(root, 'cfxcrosshair', { label: text(point.node), target, x: event.clientX, y: event.clientY });
      emitSync(root, { action: 'crosshair', label: text(point.node), target, mode });
    }
  };


  // Keyboard focus guides to the focused Cartesian observation with the pointer's axis-edge values.
  const showFocusCrosshair = (root, crosshair, node) => {
    if (!hasFeature(root, 'Crosshair') || !node.hasAttribute('data-cfx-point') || !usesCartesianCoordinates(node)) {
      hideCrosshair(root, crosshair); return;
    }
    const box = node.getBoundingClientRect(), x = box.left + box.width / 2, y = box.top + box.height / 2;
    showCrosshair(root, crosshair, { node, x, y }, { clientX: x, clientY: y, target: node });
  };
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


      hideCrosshair(root, crosshair);
      const available = hasFeature(root, 'Tooltips') ? currentPointerTooltip(root, event) === legend : pointerTargetPaint(legend);
      if (available) {
        const key = targetKey(targetIdentity(legend));
        if (!legendEntryShown(root, legend)) clearHover(root, true, true);
        else if (root.dataset.cfxHoverKey !== key) setHover(root, legend, true, true);
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

    const tooltipPoint = acquiredTooltipPoint(root, candidates);
    const guidePoint = guideEnabled && observation && usesCartesianCoordinates(observation.node)
      && (observation.exact || (observation.category ? observation.rank : observation.distance) <= 120) ? observation : null;
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

    if (tooltipPoint) showTip(root, tip, tooltipPoint.node, event);
    else hideTip(root, tip, false);
  };
