
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
      tree.removeEventListener('slotchange', queueTooltipPositionRefresh, true);
    }
    tooltipPositionTrees.clear();
  };
  const forgetTooltipPosition = (tip) => {
    const state = tooltipPositions.get(tip);
    tooltipPositions.delete(tip);
    if (state?.intersectionObserver) state.intersectionObserver.disconnect();
    if (state) activeTooltipPositions.delete(state);
    if (tooltipPositionResize && state) tooltipPositionResize.unobserve(state.stage);
    if (activeTooltipPositions.size) {
      if (tooltipPositionObserver && Array.from(tooltipPositionTrees).some(tree =>
        !Array.from(activeTooltipPositions).some(current => current.trees.includes(tree)))) {
        // Drain detaches before rebind.
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
    window.visualViewport?.removeEventListener('scroll', queueTooltipPositionRefresh);
    window.visualViewport?.removeEventListener('resize', queueTooltipPositionRefresh);
    if (tooltipPositionFrame) { cancelAnimationFrame(tooltipPositionFrame); tooltipPositionFrame = 0; }
  };
  const tooltipSourceRect = (node) => {
    if (node._cfxPairedRegion) return pairedExtentScreenBox(node);
    const source = nativeRegionScreenBox(node);
    if (source) return source;
    const rect = node.getBoundingClientRect();

    return !rect.width && !rect.height && node.ownerSVGElement ? node.ownerSVGElement.getBoundingClientRect() : rect;
  };
  const tooltipShadowContainingBlock = (node, position, source) => {
    if (!(node instanceof HTMLElement) || node.getRootNode() === source.getRootNode()) return false;
    // Containing blocks bypass clips; inline never clips.
    const style = getComputedStyle(node);
    return position === 'absolute' && style.position !== 'static'
      || ['transform', 'translate', 'rotate', 'scale', 'perspective', 'filter', 'backdropFilter'].some(key => style[key] && style[key] !== 'none')
      || /\b(layout|paint|content|strict)\b/.test(style.contain)
      || /\b(transform|translate|rotate|scale|perspective|filter|backdrop-filter)\b/.test(style.willChange)
      || style.contentVisibility === 'auto';
  };
  const tooltipClippingAncestors = (source) => {
    const ancestors = [];
    let positioned = null, containingBlock = null;
    for (let child = source, node = composedParent(child); node; child = node, node = composedParent(node)) {
      if (!positioned && child instanceof HTMLElement && ['absolute', 'fixed'].includes(getComputedStyle(child).position)) {
        positioned = getComputedStyle(child).position; containingBlock = child.offsetParent;
      }
      if (positioned) {

        if (node !== containingBlock && !tooltipShadowContainingBlock(node, positioned, source)) continue;
        positioned = null;
      }
      if (!(node instanceof HTMLElement) || !node.getClientRects().length) continue;
      const style = getComputedStyle(node);

      if (style.display === 'inline' || style.display === 'contents') continue;
      const paint = /\b(paint|content|strict)\b/.test(style.contain);
      const x = paint || style.overflowX !== 'visible', y = paint || style.overflowY !== 'visible';
      if (x || y) ancestors.push({ node, x, y });
    }
    return ancestors;
  };
  const tooltipClippingBounds = (ancestors) => {
    const viewport = window.visualViewport;
    let left = viewport?.offsetLeft || 0, top = viewport?.offsetTop || 0;
    let right = left + (viewport?.width ?? window.innerWidth), bottom = top + (viewport?.height ?? window.innerHeight);
    for (const { node, x, y } of ancestors) {
      const rect = node.getBoundingClientRect(), scaleX = node.offsetWidth ? rect.width / node.offsetWidth : 1;
      const scaleY = node.offsetHeight ? rect.height / node.offsetHeight : 1;
      if (x) { left = Math.max(left, rect.left + node.clientLeft * scaleX); right = Math.min(right, rect.left + (node.clientLeft + node.clientWidth) * scaleX); }
      if (y) { top = Math.max(top, rect.top + node.clientTop * scaleY); bottom = Math.min(bottom, rect.top + (node.clientTop + node.clientHeight) * scaleY); }
    }
    return { left, top, right, bottom };
  };
  const tooltipSourceInView = (state) => {
    if (state.paintChanged || !state.clippingAncestors) {
      state.clippingAncestors = tooltipClippingAncestors(state.node);
      state.placementClippingAncestors = tooltipClippingAncestors(state.tip);
    }

    const { left, top, right, bottom } = tooltipClippingBounds(state.clippingAncestors);
    const source = tooltipSourceRect(state.node);
    return state.nativeIntersecting !== false && source && right > left && bottom > top
      && source.right > left && source.left < right && source.bottom > top && source.top < bottom;
  };
  const tooltipPositionAvailable = (state) => state.root.isConnected && state.tip.isConnected && state.node.isConnected
    && state.root.contains(state.tip) && state.root.contains(state.node) && !state.tip.hidden && !state.root.hidden
    && hasFeature(state.root, 'Tooltips') && state.stage.getClientRects().length > 0
    && (!state.paintChanged || tooltipReadoutAvailable(state.node, state.readoutEvent)) && tooltipSourceInView(state);
  const queueTooltipPositionRefresh = (invalidation) => {

    if (invalidation) for (const state of activeTooltipPositions) state.paintChanged = true;
    if (tooltipPositionFrame || !activeTooltipPositions.size) return;
    tooltipPositionFrame = requestAnimationFrame(() => {
      tooltipPositionFrame = 0;
      const changes = Array.from(activeTooltipPositions).filter(state => state.paintChanged).map(state =>
        ({ state, ancestry: composedAncestry(state.root) })).filter(({ state, ancestry }) =>
        state.nodes.length !== ancestry.nodes.length || state.nodes.some((node, index) => node !== ancestry.nodes[index]));
      if (changes.length && tooltipPositionObserver) {

        checkTooltipPositionMutations(tooltipPositionObserver.takeRecords());
        if (tooltipPositionObserver) {
          for (const { state, ancestry } of changes) if (activeTooltipPositions.has(state)) Object.assign(state, ancestry);
          tooltipPositionObserver.disconnect(); unwatchTooltipPositionTrees();
          for (const state of activeTooltipPositions) watchTooltipPositionTrees(state);
        }
      }
      for (const state of Array.from(activeTooltipPositions)) {
        if (state.tip.hidden) { forgetTooltipPosition(state.tip); continue; }
        if (!tooltipPositionAvailable(state)) { hideTip(state.root, state.tip, true); continue; }
        state.paintChanged = false;
        placeTooltip(state, true);
      }
    });
  };
  const checkTooltipPositionMutations = (records) => {

    records = records.filter(record => {
      const placed = tooltipPositions.get(record.target);
      return !(placed && record.type === 'attributes' && record.attributeName === 'style'
        && placed.style === placed.tip.getAttribute('style'));
    });
    if (!records.length) return;
    for (const current of Array.from(activeTooltipPositions)) {
      // Release geometry, not requests.
      if (current.tip.hidden) { forgetTooltipPosition(current.tip); continue; }
      const removed = records.some(record => record.type === 'childList'
        && Array.from(record.removedNodes).some(node => node.contains(current.root) || node.contains(current.node) || node.contains(current.tip)
          || current.nodes.some(boundary => node.contains(boundary))));
      if (removed) { hideTip(current.root, current.tip, true); continue; }

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
      // Keep shadow-local events.
      tree.addEventListener('scroll', queueTooltipPositionRefresh, true);
      tree.addEventListener('transitionend', queueTooltipPositionRefresh, true);
      tree.addEventListener('slotchange', queueTooltipPositionRefresh, true);
      tooltipPositionTrees.add(tree);
    }
  };
  const watchTooltipPosition = (state) => {
    if (typeof IntersectionObserver !== 'undefined' && state.intersectionNode !== state.node) {
      state.intersectionObserver?.disconnect(); state.nativeIntersecting = undefined; state.intersectionNode = state.node;
      const box = state.node.getBoundingClientRect();
      if (box.width > 0 && box.height > 0 && !retainedGeometryFact(state.node)) {
        // Intersect opaque slots with ink, not hit boxes.

        const observer = new IntersectionObserver(entries => {
          const entry = entries.find(item => item.target === state.node);
          if (!entry || state.intersectionObserver !== observer || !activeTooltipPositions.has(state)) return;
          state.nativeIntersecting = entry.isIntersecting;
          queueTooltipPositionRefresh();
        });
        state.intersectionObserver = observer; observer.observe(state.node);
      }
    }
    if (activeTooltipPositions.has(state)) return;
    activeTooltipPositions.add(state);
    if (!tooltipPositionObserver) {

      window.addEventListener('resize', queueTooltipPositionRefresh);
      window.visualViewport?.addEventListener('scroll', queueTooltipPositionRefresh);
      window.visualViewport?.addEventListener('resize', queueTooltipPositionRefresh);
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
  const tooltipPositionGeometry = (state, anchor, bounds) => {
    const stage = state.stage.getBoundingClientRect(), tip = state.tip.getBoundingClientRect();
    return [anchor.left, anchor.top, anchor.width, anchor.height, stage.left, stage.top, stage.width, stage.height,
      tip.left, tip.top, tip.width, tip.height, bounds.left, bounds.top, bounds.right, bounds.bottom];
  };
  const placeTooltip = (state, skipUnchanged = false) => {
    const { root, tip } = state, anchor = tooltipAnchorRect(state);
    const bounds = tooltipClippingBounds(state.placementClippingAncestors);
    const dock = root.querySelector('[data-cfx-compare-tray]');
    if (dock && !dock.hidden) {
      const box = dock.getBoundingClientRect();
      if (box.right > bounds.left && box.left < bounds.right && box.top < bounds.bottom
        && box.top >= bounds.top + tip.getBoundingClientRect().height + 16) bounds.bottom = box.top;
    }
    const geometry = tooltipPositionGeometry(state, anchor, bounds);
    if (skipUnchanged && state.geometry && geometry.every((value, index) => value === state.geometry[index])) return;
    // Fix measured shrink-to-fit width.
    tip.style.left = '0px'; tip.style.top = '0px';
    tip.style.width = '';
    tip.style.width = getComputedStyle(tip).width;
    const origin = tip.getBoundingClientRect();
    tip.style.left = '1px'; tip.style.top = '1px';
    const unit = tip.getBoundingClientRect();

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
    const position = candidates.find(candidate => candidate.x >= bounds.left + 8 && candidate.y >= bounds.top + 8
      && candidate.x + unit.width <= bounds.right - 8 && candidate.y + unit.height <= bounds.bottom - 8) || candidates[0];
    const x = clamp(position.x, bounds.left + 8, Math.max(bounds.left + 8, bounds.right - unit.width - 8));
    const y = clamp(position.y, bounds.top + 8, Math.max(bounds.top + 8, bounds.bottom - unit.height - 8));
    tip.style.left = (x - origin.left) / scaleX + 'px';
    tip.style.top = (y - origin.top) / scaleY + 'px';
    state.style = tip.getAttribute('style');
    state.geometry = tooltipPositionGeometry(state, anchor, bounds);
  };
  const positionTooltip = (tip, event, node) => {
    if (tip.hidden) return;
    const root = tip.closest('.cfx-interactive-chart'), stage = root && root.querySelector('.cfx-stage');
    if (!root || !stage || !node) return;
    let state = tooltipPositions.get(tip);
    const pinned = state && root.dataset.cfxTooltipPinned === 'true' && state.key === root.dataset.cfxPinnedTarget;
    if (!state) { state = { root, tip, stage, ...composedAncestry(root) }; tooltipPositions.set(tip, state); }

    if (!pinned) {
      if (state.node !== node) state.nativeIntersecting = undefined;
      state.node = node; state.key = targetKey(targetIdentity(node)); state.readoutEvent = event;
    }
    state.event = event; state.anchor = root.dataset.cfxTooltipAnchor || 'pointer';
    state.paintChanged = true;
    if (!tooltipPositionAvailable(state)) { hideTip(root, tip, true); return; }
    state.paintChanged = false;
    placeTooltip(state);
    watchTooltipPosition(state);
  };
