
  const keyboardPaintClasses = new Set(['cfx-hovered', 'cfx-hover-related', 'cfx-hover-column', 'cfx-hover-series',
    'cfx-selected', 'cfx-highlighted', 'cfx-linked-dim', 'cfx-selection-dim', 'cfx-linked-series-active',
    'cfx-linked-series-dim', 'cfx-trail']);
  const keyboardNativeClasses = value => (value || '').split(/\s+/).filter(value => value && !keyboardPaintClasses.has(value)).sort().join(' ');
  const keyboardNativeStyle = value => (value || '').replace(/--cfx-highlight-(?:opacity|stroke|stroke-width)\s*:[^;]*(?:;|$)/g, '').trim();
  const keyboardAvailabilityMutation = (record, stage, ancestry) => {
    // Browser axes and a running glide's transforms and hidden marks are not targets and leave every target where it was.
    if (record.target.closest?.('[data-cfx-browser-axis],[data-cfx-browser-grid]')
      || /^(transform|style)$/.test(record.attributeName) && standaloneGlideNodes.has(record.target)) return false;
    // A rewrite of an unchanged value, or the owned-font swap between two drawings of the same text, moves no target.
    if (record.type === 'attributes' && (record.oldValue === record.target.getAttribute(record.attributeName)
      || record.target.matches('[data-cfx-owned-font-text],[data-cfx-owned-font-native]') && !record.target.matches(targetSelector) && !record.target.querySelector(targetSelector))) return false;
    if (record.type === 'attributes' && record.attributeName === 'class'
      && keyboardNativeClasses(record.oldValue) === keyboardNativeClasses(record.target.getAttribute('class'))) return false;
    if (record.type === 'attributes' && record.target.closest('[data-cfx-highlight-outline],[data-cfx-keyboard-focus-outline],[data-cfx-state-outline]')) return false;
    if (record.type === 'attributes' && record.attributeName === 'style' && record.target.matches('[data-cfx-highlight-paint]')
      && keyboardNativeStyle(record.oldValue) === keyboardNativeStyle(record.target.getAttribute('style'))) return false;

    const svg = stage.querySelector('svg');
    if (record.type === 'attributes') return record.target === stage || !!svg?.contains(record.target) || ancestry.nodes.includes(record.target);
    const changes = [...record.addedNodes, ...record.removedNodes];
    const adapterPaint = node => node.nodeType === 1 && node.matches('[data-cfx-highlight-outline],[data-cfx-keyboard-focus-outline],[data-cfx-state-outline],.cfx-prepared-point-marker,[data-cfx-browser-hit-area],[data-cfx-browser-axis],[data-cfx-browser-grid]');
    if (changes.length && changes.every(adapterPaint)) return false;
    return !!svg?.contains(record.target) || changes.some(node => node.contains(stage) || svg && node.contains(svg)
      || ancestry.nodes.some(boundary => node.contains(boundary)));
  };

  const keyboardTargetAvailable = (node, styles = new Map()) => {
    if (node.dataset.cfxVisible === 'false') return false;
    if (node.closest('[aria-hidden="true"]') || !paintAncestorsVisible(node, styles)) return false;

    if (renderedTargetKind(node) !== 'legend' && node.closest('.cfx-series-muted,.cfx-series-isolated-out')) return false;
    const style = paintStyle(node, styles);
    if (style.display === 'none' || style.visibility === 'hidden' || style.visibility === 'collapse') return false;
    if (pointerTargetPaint(node, styles)) return true;

    const data = node.dataset;
    if (!data.cfxTargetKind || !data.cfxTargetId || !retainedGeometryFact(node)) return false;
    const svg = node.ownerSVGElement;
    if (!svg) return false;

    for (let parent = node.parentElement; parent && parent !== svg; parent = parent.parentElement) {
      if (getComputedStyle(parent).display === 'none') return false;
    }
    const viewport = svg.getBoundingClientRect();
    return viewport.width > 0 && viewport.height > 0;
  };
  // An observation wholly inside the plot keeps its availability while only the window moves: the plot clip, its
  // series paint and its styles stay as they were. Observations at the plot edges are measured again.
  const interiorObservation = (root, node) => {
    const plot = root._cfxDataProjection?.plotBounds, region = node._cfxNativeRegion;
    return !!(plot && region && node.dataset.cfxPoint !== undefined && region.x > plot.x + 1 && region.y > plot.y + 1
      && region.x + region.width < plot.x + plot.width - 1 && region.y + region.height < plot.y + plot.height - 1);
  };
  const keyboardTargets = (root, viewportOnly = false) => withNativeHighlightPaint(root, () => {
    const hierarchy = hasFeature(root, 'HierarchyNavigation') ? capturedHierarchy(root) : null;
    // Discovery only reads, so one pass shares computed styles, series paint layers and shown ancestors across every target.
    const styles = Object.assign(new Map(), { _cfxShown: new Set() });
    // After a repaint that only moved the window, a dense chart re-measures only the observations it cannot reuse.
    const kept = viewportOnly && root._cfxKeyboardAvailability || new Map();
    root._cfxKeyboardAvailability = kept;
    const candidates = chartTargets(root).filter(node => {
      if (node.dataset.cfxVisible === 'false') return false;
      const interior = interiorObservation(root, node);
      if (interior && kept.has(node)) return kept.get(node);
      const available = keyboardTargetAvailable(node, styles);
      if (interior) kept.set(node, available);
      return available;
    });
    const candidateSet = new Set(candidates);
    const containers = new Set();
    candidates.forEach((node) => {
      for (let parent = node.parentElement; parent && parent !== root; parent = parent.parentElement) {
        if (candidateSet.has(parent)) containers.add(parent);
      }
    });
    const seen = new Set();
    return candidates.filter((node) => {
      if (containers.has(node)) {

        const data = node.dataset;
        const hierarchyBranch = hierarchy && ['treemap', 'sunburst'].includes(hierarchy.kind)
          && data.cfxTargetKind === 'node' && data.cfxNode === data.cfxTargetId && data.cfxTargetId
          && (data.cfxKind || '').toLowerCase() === hierarchy.kind && seriesKey(node) === hierarchy.seriesKey
          && data.cfxLeaf === 'false' && data.cfxTargetId !== hierarchy.rootId;
        if (!hierarchyBranch) return false;
      }
      const focusNode = targetFocusNode(node);
      if (seen.has(focusNode)) return false;
      seen.add(focusNode);
      return true;
    });
  });
  const keyboardDataGroups = (nodes) => {
    const groups = new Map();
    nodes.forEach((node) => {

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
  const placeKeyboardLegendBeforeData = (root, data, legends) => {
    const svg = root.querySelector('.cfx-stage svg');
    if (!svg || !data.length) return;
    const branches = new Set();
    legends.forEach((node) => {
      let branch = node;
      while (branch.parentElement && branch.parentElement !== svg) branch = branch.parentElement;

      if (branch.parentElement === svg && !branches.has(branch) && !data.some((mark) => branch.contains(mark))) branches.add(branch);
    });
    const firstData = Array.from(svg.children).find(branch => data.some(mark => branch.contains(mark)));
    if (firstData) branches.forEach(branch => svg.insertBefore(branch, firstData));
  };
  // Roving stops shared by the full runtime and the core reader: one Tab stop per component.
  const assignKeyboardStops = (root, state, targets, focused) => {
    state.targets = targets;
    state.legends = targets.filter((node) => renderedTargetKind(node) === 'legend');
    state.groups = keyboardDataGroups(targets.filter((node) => renderedTargetKind(node) !== 'legend'));
    state.data = state.groups.flatMap((group) => group.nodes);

    if (state.data.length && !state.data.includes(state.activeData)) state.activeData = state.data[0];
    if (state.legends.length && !state.legends.includes(state.activeLegend)) state.activeLegend = state.legends[0];
    if (state.data.includes(focused)) state.activeData = focused;
    if (state.legends.includes(focused)) state.activeLegend = focused;
    // A legend that switches between the drawn and flowing presentation brings new entries; each set is placed once.
    const placed = state.placedLegends ||= new WeakSet();
    if (state.data.length && state.legends.some((node) => !placed.has(node))) {
      placeKeyboardLegendBeforeData(root, state.data, state.legends);
      state.legends.forEach((node) => placed.add(node));
    }
    // The final attributes are settled first and only changed ones are written: a dense chart refreshes its stops often.
    const stops = new Map(Array.from(state.owned, node => [node, { tabindex: '-1' }]));
    targets.forEach((node) => {
      const focusNode = targetFocusNode(node);
      state.owned.add(focusNode);
      stops.set(focusNode, { ...stops.get(focusNode), 'data-cfx-keyboard-component': renderedTargetKind(node) === 'legend' ? 'legend' : 'data',
        tabindex: node === state.activeData || node === state.activeLegend ? '0' : '-1' });
    });
    stops.forEach((values, node) => Object.entries(values).forEach(([name, value]) => node.getAttribute(name) !== value && node.setAttribute(name, value)));
    return state;
  };
  // A viewport repaint passes the changes it made itself; when nothing else changed, its targets reuse earlier measures.
  const refreshKeyboardNavigation = (root, focused, passive = false, moved = null) => {
    // Viewport transition frames are not keyboard state: keep the targets of the final window. A glide that nothing
    // disturbed ends on the drawing those targets were read from, so they stand too.
    const view = root._cfxStandaloneViewport, reused = (view?.motionTarget || view?.glideSettled) && root._cfxKeyboardNavigation?.targets;
    if (reused && view.glide) view.glideTouched = true;
    // A full refresh, or a glide's closing paint whose targets stand, accounts for every paint change made so far,
    // so the availability watch need not queue another.
    const viewportOnly = (!reused || view.glideSettled) && root._cfxSettleAvailability?.(moved) === true;
    refreshRootBranchHighlightAvailability(root);
    refreshSelectionCuePaint(root);
    const state = root._cfxKeyboardNavigation;
    if (!state || !hasFeature(root, 'KeyboardNavigation')) return null;
    const activeElement = root.getRootNode().activeElement;
    const activeOwned = root.contains(activeElement) && state.owned.has(activeElement);
    // Reused targets with no newly focused one keep the stops they were given.
    if (!reused || focused !== undefined) assignKeyboardStops(root, state, reused || keyboardTargets(root, viewportOnly), focused);
    const targets = state.targets;
    if (activeOwned && !targets.some((node) => targetFocusNode(node) === activeElement)) {

      // Focus stays in the component the reader was in, e.g. when the legend switches between drawn and flowing.
      const fromLegend = activeElement.getAttribute('data-cfx-keyboard-component') === 'legend';
      const replacement = fromLegend && state.legends.length ? state.activeLegend
        : state.data.length ? state.activeData : state.legends.length ? state.activeLegend : null;
      if (replacement) {


        if (passive) { clearHover(root, true, true); hideTip(root, root.querySelector('.cfx-tooltip'), true); }
        focusKeyboardTarget(root, replacement, passive);
      }
    }
    refreshKeyboardFocusPaint(root);
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
      observePaint();
      ancestry.trees.forEach(tree => tree.addEventListener('slotchange', queueRefresh, true));
    };
    const observePaint = () => {
      paintObserver.observe(root, { childList: true, subtree: true, attributes: true, attributeOldValue: true, attributeFilter: paintAvailabilityAttributes });
      for (const node of new Set([...ancestry.nodes.slice(1), ...ancestry.trees])) {
        paintObserver.observe(node, { childList: true,
          ...(node instanceof Element ? { attributes: true, attributeOldValue: true, attributeFilter: paintAvailabilityAttributes } : {}) });
      }
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

      // Only a real size change can move targets. The first notification records the laid-out size, so binding reads
      // no layout and the chrome settling around the drawing does not refresh every stop.
      let size;
      resizeObserver = new ResizeObserver(() => { const next = stage.clientWidth + 'x' + stage.clientHeight; if (size !== undefined && size !== next) queueRefresh(); size = next; });
      resizeObserver.observe(stage);
    }

    paintObserver = new MutationObserver(records => {
      if (chartLifetimes.get(root)?.disposed !== false) return;
      if (!root.isConnected) { queueRefresh(); return; }
      if (!records.some(record => keyboardAvailabilityMutation(record, stage, ancestry))) return;
      delete root._cfxKeyboardAvailability; queueRefresh();
    });
    watchAncestry();
    // A window repaint moves every mark and accounts for those writes itself: the watch pauses rather than queue a
    // record per attribute, keeping what it already held for the repaint's own refresh.
    let held = [];
    root._cfxPauseAvailability = () => { held = held.concat(paintObserver.takeRecords()); paintObserver.disconnect(); return observePaint; };
    // Changes outside the chart may move its ancestry, which only the queued refresh re-watches.
    // Reports whether every pending change is either one the caller made (moved) or one that moves no target.
    root._cfxSettleAvailability = moved => {
      const records = held.concat(paintObserver.takeRecords()); held = [];
      if (records.some(record => !root.contains(record.target))) queueRefresh();
      return !!moved && records.every(record => moved(record) || !keyboardAvailabilityMutation(record, stage, ancestry));
    };
    ownChartWork(root, () => {
      if (frame) cancelAnimationFrame(frame);
      resizeObserver?.disconnect(); paintObserver?.disconnect(); unwatchSlots(); delete root._cfxSettleAvailability; delete root._cfxPauseAvailability;
    });
  };
  const prepareKeyboardNavigation = (root) => {
    if (!hasFeature(root, 'KeyboardNavigation')) {
      if (root._cfxBranchHighlights || hasFeature(root, 'Selection')) bindKeyboardNavigationAvailability(root);
      return;
    }
    root._cfxKeyboardNavigation = { owned: new Set() };
    const stage = root.querySelector('.cfx-stage');

    if (stage && !stage.hasAttribute('tabindex')) {
      stage.setAttribute('tabindex', '-1');
      ownChartWork(root, () => { if (stage.getAttribute('tabindex') === '-1') stage.removeAttribute('tabindex'); });
    }

    chartTargets(root).forEach((node) => root._cfxKeyboardNavigation.owned.add(targetFocusNode(node)));
    refreshKeyboardNavigation(root);
    bindKeyboardNavigationAvailability(root);
  };
  const scrollKeyboardTargetIntoView = (root, node) => {
    const stage = root.querySelector('.cfx-stage');
    const tray = root.querySelector('[data-cfx-compare-tray]');
    const owner = stage?.contains(node) ? stage : tray?.contains(node) ? tray : null;
    if (!owner) return;
    const scrollAxis = (overflow, extent, available, offset, start, end, itemStart, itemEnd) => {
      if (!['auto', 'scroll', 'hidden'].includes(overflow) || extent <= available) return offset;
      const center = (itemStart + itemEnd) / 2;
      if (itemEnd - itemStart > end - start) {

        if (itemEnd > start && itemStart < end) return offset;
        return offset + center - (start + end) / 2;
      }
      if (itemStart < start) return offset + itemStart - start;
      if (itemEnd > end) return offset + itemEnd - end;
      return offset;
    };
    let preserveVisiblePortion = false;
    const inset = 16; // Keep focused paint clear of native overlay scrollbars.

    const revealClips = () => {
      for (const { node: clip, x, y } of tooltipClippingAncestors(node)) {
        const style = getComputedStyle(clip), box = node.getBoundingClientRect();
        const start = stagePoint(clip, { x: box.left, y: box.top });
        const end = stagePoint(clip, { x: box.right, y: box.bottom });
        const left = clip.scrollLeft + inset, top = clip.scrollTop + inset;
        preserveVisiblePortion ||= x && end.x - start.x > clip.clientWidth - inset * 2 && end.x > left && start.x < left + clip.clientWidth - inset * 2
          || y && end.y - start.y > clip.clientHeight - inset * 2 && end.y > top && start.y < top + clip.clientHeight - inset * 2;
        clip.scrollTo({ behavior: 'instant',
          left: scrollAxis(style.overflowX, clip.scrollWidth, clip.clientWidth, clip.scrollLeft, left, left + clip.clientWidth - inset * 2, start.x, end.x),
          top: scrollAxis(style.overflowY, clip.scrollHeight, clip.clientHeight, clip.scrollTop, top, top + clip.clientHeight - inset * 2, start.y, end.y) });
      }
    };
    revealClips();
    const box = node.getBoundingClientRect(), viewport = owner.getBoundingClientRect();

    if (!preserveVisiblePortion && box.width <= viewport.width - inset * 2 && box.height <= viewport.height - inset * 2) {


      let nativeTarget = node;
      if (node instanceof SVGElement) {
        const start = stagePoint(owner, { x: box.left, y: box.top });
        const end = stagePoint(owner, { x: box.right, y: box.bottom });
        nativeTarget = document.createElement('span');
        nativeTarget.setAttribute('aria-hidden', 'true');
        nativeTarget.style.cssText = 'all:initial;position:absolute;display:block;pointer-events:none;opacity:0;margin:0;padding:0;border:0;'
          + `scroll-margin:${inset}px;left:${start.x}px;top:${start.y}px;width:${end.x - start.x}px;height:${end.y - start.y}px`;
        owner.appendChild(nativeTarget);
      }
      try { nativeTarget.scrollIntoView({ behavior: 'instant', block: 'nearest', inline: 'nearest' }); }
      finally { if (nativeTarget !== node) nativeTarget.remove(); }

      revealClips();
    }
  };
  const focusKeyboardTarget = (root, node, passive = false) => {
    const focusNode = targetFocusNode(node);

    const recovering = root._cfxRecoveringFocus === true;
    if (passive) root._cfxRecoveringFocus = true;
    try {
      scrollKeyboardTargetIntoView(root, focusNode);
      if (focusNode.focus) {
        try { focusNode.focus({ preventScroll: true }); } catch { focusNode.focus(); }
      }
    } finally { if (passive && !recovering) delete root._cfxRecoveringFocus; }
  };
  const keyboardNavigationKeys = ['ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown', 'PageUp', 'PageDown', 'Home', 'End'];
  // The target a navigation key reaches from node within the assigned stops, or null when the key does not navigate.
  // Horizontal charts run their categories down the value axis, so Up/Down step along a series there
  // and Left/Right cross series; otherwise Left/Right step and Up/Down cross series.
  const adjacentKeyboardTarget = (root, state, node, key) => {
    const legend = state.legends.includes(node);
    const group = legend ? null : state.groups.find((item) => item.nodes.includes(node));
    const targets = legend ? state.legends : group ? group.nodes : [];
    if (!targets.length) return null;
    const current = targets.indexOf(node);
    const horizontal = !legend && root._cfxDataProjection?.horizontal === true;
    const across = horizontal ? key === 'ArrowLeft' || key === 'ArrowRight' : key === 'ArrowUp' || key === 'ArrowDown';
    if (key === 'Home') return targets[0];
    if (key === 'End') return targets[targets.length - 1];
    if (key === 'PageUp' || key === 'PageDown') return targets[clamp(current + (key === 'PageUp' ? -10 : 10), 0, targets.length - 1)];
    if (!legend && across && (group.series !== null || state.groups.length > 1)) {
      const groups = state.groups;
      const nextGroup = groups[clamp(groups.indexOf(group) + (key === 'ArrowUp' || key === 'ArrowLeft' ? -1 : 1), 0, groups.length - 1)];
      if (nextGroup === group) return node;
      const x = node.dataset.cfxX;
      const sourcePoint = sourcePointIndex(node);
      return nextGroup.nodes.find((item) => x !== undefined && item.dataset.cfxX === x)
        || nextGroup.nodes.find((item) => sourcePoint !== undefined && sourcePointIndex(item) === sourcePoint)
        || nextGroup.nodes[Math.min(current, nextGroup.nodes.length - 1)];
    }
    return targets[clamp(current + (key === 'ArrowLeft' || key === 'ArrowUp' ? -1 : 1), 0, targets.length - 1)];
  };
  const focusAdjacentTarget = (root, node, key) => {
    if (!keyboardNavigationKeys.includes(key)) return false;
    resumePointerReadout(root);
    const state = refreshKeyboardNavigation(root, node);
    if (!state) return false;
    const next = adjacentKeyboardTarget(root, state, node, key);
    if (!next) return false;
    const legend = state.legends.includes(node);
    if (next === node) {

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
