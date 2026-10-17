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
    if (sameSeries && pointUnits && target.groupIndex !== undefined && boxPlotGroup(node))
      return target.groupId !== undefined ? data.cfxGroupId === target.groupId : data.cfxGroupIndex === String(target.groupIndex);
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
