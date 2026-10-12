  // Screen placement belongs to the Html adapter; target acquisition supplies the canonical native node.
  const tooltipPositions = new WeakMap();
  const activeTooltipPositions = new Set();
  const tooltipPositionTrees = new Set();
  let tooltipPositionFrame = 0, tooltipPositionObserver = null, tooltipPositionResize = null;
  const tooltipDirections = {
    top: [0, -1], 'top-right': [1, -1], right: [1, 0], 'bottom-right': [1, 1],
    bottom: [0, 1], 'bottom-left': [-1, 1], left: [-1, 0], 'top-left': [-1, -1]
  };
  const unwatchTooltipPositionTrees = () => {
    for (const tree of tooltipPositionTrees) {
      tree.removeEventListener('scroll', queueTooltipPositionRefresh, true);
      tree.removeEventListener('transitionend', queueTooltipPositionRefresh, true);
    }
    tooltipPositionTrees.clear();
  };
  const forgetTooltipPosition = (tip) => {
    const state = tooltipPositions.get(tip);
    tooltipPositions.delete(tip);
    if (state) activeTooltipPositions.delete(state);
    if (tooltipPositionResize && state) tooltipPositionResize.unobserve(state.stage);
    if (activeTooltipPositions.size) {
      if (tooltipPositionObserver && Array.from(tooltipPositionTrees).some(tree =>
        !Array.from(activeTooltipPositions).some(current => current.trees.includes(tree)))) {
        // Drain surviving hosts' removals before rebuilding registrations to release unused shadow trees.
        checkTooltipPositionMutations(tooltipPositionObserver.takeRecords());
        if (!tooltipPositionObserver) return;
        tooltipPositionObserver.disconnect(); unwatchTooltipPositionTrees();
        for (const current of activeTooltipPositions) watchTooltipPositionTrees(current);
      }
      return;
    }
    if (tooltipPositionObserver) { tooltipPositionObserver.disconnect(); tooltipPositionObserver = null; unwatchTooltipPositionTrees(); }
    if (tooltipPositionResize) { tooltipPositionResize.disconnect(); tooltipPositionResize = null; }
    window.removeEventListener('resize', queueTooltipPositionRefresh);
    if (tooltipPositionFrame) { cancelAnimationFrame(tooltipPositionFrame); tooltipPositionFrame = 0; }
  };
  const tooltipSourceRect = (node) => {
    if (node._cfxPairedRegion) return pairedExtentScreenBox(node);
    const region = node._cfxNativeRegion, matrix = region && node.getScreenCTM();
    if (matrix) {
      const corners = [[region.x, region.y], [region.x + region.width, region.y],
        [region.x, region.y + region.height], [region.x + region.width, region.y + region.height]]
        .map(([x, y]) => new DOMPoint(x, y).matrixTransform(matrix));
      return { left: Math.min(...corners.map(p => p.x)), right: Math.max(...corners.map(p => p.x)),
        top: Math.min(...corners.map(p => p.y)), bottom: Math.max(...corners.map(p => p.y)) };
    }
    const rect = node.getBoundingClientRect();
    // Unlocated retained keyboard facts have no painted box; their visible SVG remains the host boundary.
    return !rect.width && !rect.height && node.ownerSVGElement ? node.ownerSVGElement.getBoundingClientRect() : rect;
  };
  const tooltipSourceInView = (state) => {
    if (state.paintChanged || !state.clippingAncestors) {
      state.clippingAncestors = [];
      for (let node = state.node.parentElement; node; node = node.parentElement || node.getRootNode().host) {
        if (!(node instanceof HTMLElement)) continue;
        const style = getComputedStyle(node), x = style.overflowX !== 'visible', y = style.overflowY !== 'visible';
        if (x || y) state.clippingAncestors.push({ node, x, y });
      }
    }
    // Keep geometry live while caching overflow axes until the existing paint invalidation fires.
    let left = 0, top = 0, right = window.innerWidth, bottom = window.innerHeight;
    for (const { node, x, y } of state.clippingAncestors) {
      const rect = node.getBoundingClientRect(), scaleX = node.offsetWidth ? rect.width / node.offsetWidth : 1;
      const scaleY = node.offsetHeight ? rect.height / node.offsetHeight : 1;
      if (x) { left = Math.max(left, rect.left + node.clientLeft * scaleX); right = Math.min(right, rect.left + (node.clientLeft + node.clientWidth) * scaleX); }
      if (y) { top = Math.max(top, rect.top + node.clientTop * scaleY); bottom = Math.min(bottom, rect.top + (node.clientTop + node.clientHeight) * scaleY); }
    }
    const source = tooltipSourceRect(state.node);
    return source && right > left && bottom > top && source.right > left && source.left < right && source.bottom > top && source.top < bottom;
  };
  const tooltipPositionAvailable = (state) => state.root.isConnected && state.tip.isConnected && state.node.isConnected
    && state.root.contains(state.tip) && state.root.contains(state.node) && !state.tip.hidden && !state.root.hidden
    && hasFeature(state.root, 'Tooltips') && state.stage.getClientRects().length > 0
    && (!state.paintChanged || tooltipReadoutAvailable(state.node, state.readoutEvent)) && tooltipSourceInView(state);
  const queueTooltipPositionRefresh = (invalidation) => {
    // Native geometry events can change clipping or responsive paint without a DOM attribute mutation.
    if (invalidation) for (const state of activeTooltipPositions) state.paintChanged = true;
    if (tooltipPositionFrame || !activeTooltipPositions.size) return;
    tooltipPositionFrame = requestAnimationFrame(() => {
      tooltipPositionFrame = 0;
      for (const state of Array.from(activeTooltipPositions)) {
        if (state.tip.hidden) { forgetTooltipPosition(state.tip); continue; }
        if (!tooltipPositionAvailable(state)) { hideTip(state.root, state.tip, true); continue; }
        state.paintChanged = false;
        placeTooltip(state, true);
      }
    });
  };
  const checkTooltipPositionMutations = (records) => {
    // Placement writes cannot change target paint or layout. External tooltip styles still invalidate geometry.
    records = records.filter(record => {
      const placed = tooltipPositions.get(record.target);
      return !(placed && record.type === 'attributes' && record.attributeName === 'style'
        && placed.style === placed.tip.getAttribute('style'));
    });
    if (!records.length) return;
    for (const current of Array.from(activeTooltipPositions)) {
      // A new delayed target can hide a shown readout. Release geometry without cancelling its new request.
      if (current.tip.hidden) { forgetTooltipPosition(current.tip); continue; }
      const removed = records.some(record => record.type === 'childList'
        && Array.from(record.removedNodes).some(node => node.contains(current.root) || node.contains(current.node) || node.contains(current.tip)
          || current.nodes.some(boundary => node.contains(boundary))));
      if (removed) { hideTip(current.root, current.tip, true); continue; }
      // Other dashboard content may move this chart, but only the chart or its ancestors invalidate paint.
      if (records.some(record => !current.tip.contains(record.target) && (current.root.contains(record.target)
        || record.type === 'attributes' && current.nodes.includes(record.target)))) current.paintChanged = true;
    }
    queueTooltipPositionRefresh();
  };
  const watchTooltipPositionTrees = (state) => {
    for (const tree of state.trees) {
      if (tooltipPositionTrees.has(tree)) continue;
      tooltipPositionObserver.observe(tree, { childList: true, attributes: true,
        attributeFilter: [...paintAvailabilityAttributes, 'data-cfx-interaction-features'], subtree: true });
      // Native geometry events do not cross a shadow boundary. Own them alongside the tree's observer.
      tree.addEventListener('scroll', queueTooltipPositionRefresh, true);
      tree.addEventListener('transitionend', queueTooltipPositionRefresh, true);
      tooltipPositionTrees.add(tree);
    }
  };
  const watchTooltipPosition = (state) => {
    if (activeTooltipPositions.has(state)) return;
    activeTooltipPositions.add(state);
    if (!tooltipPositionObserver) {
      // Shared listeners exist only while a readout is visible, including pointer and pinned readouts.
      window.addEventListener('resize', queueTooltipPositionRefresh);
      tooltipPositionObserver = new MutationObserver(checkTooltipPositionMutations);
      if (typeof ResizeObserver !== 'undefined') tooltipPositionResize = new ResizeObserver(queueTooltipPositionRefresh);
    }
    watchTooltipPositionTrees(state);
    if (tooltipPositionResize) tooltipPositionResize.observe(state.stage);
  };
  const refreshTooltipPosition = (root) => {
    const state = tooltipPositions.get(root.querySelector('.cfx-tooltip'));
    if (state && activeTooltipPositions.has(state)) { state.paintChanged = true; queueTooltipPositionRefresh(); }
  };
  const tooltipAnchorRect = (state) => {
    if (state.anchor === 'chart') return state.stage.getBoundingClientRect();
    const rect = state.node.getBoundingClientRect();
    if (state.anchor === 'node') return rect;
    let x = state.event?.clientX, y = state.event?.clientY;
    if (!Number.isFinite(x) || !Number.isFinite(y)) { x = rect.left + rect.width / 2; y = rect.top + rect.height / 2; }
    if (!Number.isFinite(x) || !Number.isFinite(y)) { x = 24; y = 24; }
    return { left: x, right: x, top: y, bottom: y, width: 0, height: 0 };
  };
  const tooltipPositionGeometry = (state, anchor) => {
    const stage = state.stage.getBoundingClientRect(), tip = state.tip.getBoundingClientRect();
    return [anchor.left, anchor.top, anchor.width, anchor.height, stage.left, stage.top, stage.width, stage.height,
      tip.left, tip.top, tip.width, tip.height, window.innerWidth, window.innerHeight];
  };
  const placeTooltip = (state, skipUnchanged = false) => {
    const { root, tip } = state, anchor = tooltipAnchorRect(state);
    const geometry = tooltipPositionGeometry(state, anchor);
    if (skipUnchanged && state.geometry && geometry.every((value, index) => value === state.geometry[index])) return;
    // Measure at a stable origin, then keep the measured width while positioning. This prevents
    // fixed-position shrink-to-fit from changing the readout as it approaches a containing block's edge.
    tip.style.left = '0px'; tip.style.top = '0px';
    tip.style.width = '';
    tip.style.width = getComputedStyle(tip).width;
    const origin = tip.getBoundingClientRect();
    tip.style.left = '1px'; tip.style.top = '1px';
    const unit = tip.getBoundingClientRect();
    // The one-pixel probe converts screen coordinates through ordinary translated/scaled host blocks.
    const scaleX = unit.left - origin.left || 1, scaleY = unit.top - origin.top || 1;
    const gap = Number(root.dataset.cfxTooltipGap ?? 14);
    const offsetX = Number(root.dataset.cfxTooltipOffsetX || 0), offsetY = Number(root.dataset.cfxTooltipOffsetY || 0);
    const placements = (root.dataset.cfxTooltipPlacements || 'bottom-right').split(',');
    const candidates = placements.map(placement => {
      const [dx, dy] = tooltipDirections[placement] || tooltipDirections['bottom-right'];
      return {
        x: (dx > 0 ? anchor.right + gap : dx < 0 ? anchor.left - gap - unit.width : anchor.left + (anchor.width - unit.width) / 2) + offsetX,
        y: (dy > 0 ? anchor.bottom + gap : dy < 0 ? anchor.top - gap - unit.height : anchor.top + (anchor.height - unit.height) / 2) + offsetY
      };
    });
    const position = candidates.find(candidate => candidate.x >= 8 && candidate.y >= 8
      && candidate.x + unit.width <= window.innerWidth - 8 && candidate.y + unit.height <= window.innerHeight - 8) || candidates[0];
    const x = clamp(position.x, 8, Math.max(8, window.innerWidth - unit.width - 8));
    const y = clamp(position.y, 8, Math.max(8, window.innerHeight - unit.height - 8));
    tip.style.left = (x - origin.left) / scaleX + 'px';
    tip.style.top = (y - origin.top) / scaleY + 'px';
    state.style = tip.getAttribute('style');
    state.geometry = tooltipPositionGeometry(state, anchor);
  };
  const positionTooltip = (tip, event, node) => {
    if (tip.hidden) return;
    const root = tip.closest('.cfx-interactive-chart'), stage = root && root.querySelector('.cfx-stage');
    if (!root || !stage || !node) return;
    let state = tooltipPositions.get(tip);
    const pinned = state && root.dataset.cfxTooltipPinned === 'true' && state.key === root.dataset.cfxPinnedTarget;
    if (!state) { state = { root, tip, stage, trees: tooltipRootTrees(root), nodes: tooltipLifetimeNodes(root) }; tooltipPositions.set(tip, state); }
    // A pin retains its acquisition mode: later pointer movement must not invalidate a keyboard-only fact.
    if (!pinned) { state.node = node; state.key = targetKey(targetIdentity(node)); state.readoutEvent = event; }
    state.event = event; state.anchor = root.dataset.cfxTooltipAnchor || 'pointer';
    state.paintChanged = true;
    if (!tooltipPositionAvailable(state)) { hideTip(root, tip, true); return; }
    state.paintChanged = false;
    placeTooltip(state);
    watchTooltipPosition(state);
  };
