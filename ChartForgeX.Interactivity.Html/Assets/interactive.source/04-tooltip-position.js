  // Screen placement belongs to the Html adapter; target acquisition supplies the canonical native node.
  const tooltipPositions = new WeakMap();
  const activeTooltipPositions = new Set();
  let tooltipPositionFrame = 0, tooltipPositionObserver = null, tooltipPositionResize = null;
  const tooltipDirections = {
    top: [0, -1], 'top-right': [1, -1], right: [1, 0], 'bottom-right': [1, 1],
    bottom: [0, 1], 'bottom-left': [-1, 1], left: [-1, 0], 'top-left': [-1, -1]
  };
  const forgetTooltipPosition = (tip) => {
    const state = tooltipPositions.get(tip);
    tooltipPositions.delete(tip);
    if (state) activeTooltipPositions.delete(state);
    if (tooltipPositionResize && state) tooltipPositionResize.unobserve(state.stage);
    if (activeTooltipPositions.size) return;
    if (tooltipPositionObserver) { tooltipPositionObserver.disconnect(); tooltipPositionObserver = null; }
    if (tooltipPositionResize) { tooltipPositionResize.disconnect(); tooltipPositionResize = null; }
    window.removeEventListener('resize', queueTooltipPositionRefresh);
    document.removeEventListener('scroll', queueTooltipPositionRefresh, true);
    document.removeEventListener('transitionend', queueTooltipPositionRefresh, true);
    if (tooltipPositionFrame) { cancelAnimationFrame(tooltipPositionFrame); tooltipPositionFrame = 0; }
  };
  const tooltipPositionAvailable = (state) => state.root.isConnected && state.tip.isConnected && state.node.isConnected
    && state.root.contains(state.tip) && state.root.contains(state.node) && !state.tip.hidden && !state.root.hidden
    && hasFeature(state.root, 'Tooltips') && state.stage.getClientRects().length > 0;
  const queueTooltipPositionRefresh = () => {
    if (tooltipPositionFrame || !activeTooltipPositions.size) return;
    tooltipPositionFrame = requestAnimationFrame(() => {
      tooltipPositionFrame = 0;
      for (const state of Array.from(activeTooltipPositions)) {
        if (state.tip.hidden) { forgetTooltipPosition(state.tip); continue; }
        if (!tooltipPositionAvailable(state)) { hideTip(state.root, state.tip, true); continue; }
        placeTooltip(state);
      }
    });
  };
  const watchTooltipPosition = (state) => {
    if (activeTooltipPositions.has(state)) return;
    activeTooltipPositions.add(state);
    if (!tooltipPositionObserver) {
      // Shared listeners exist only while a node/chart readout is visible, including pinned readouts.
      window.addEventListener('resize', queueTooltipPositionRefresh);
      document.addEventListener('scroll', queueTooltipPositionRefresh, true);
      document.addEventListener('transitionend', queueTooltipPositionRefresh, true);
      tooltipPositionObserver = new MutationObserver((records) => {
        for (const current of Array.from(activeTooltipPositions)) {
          // A new delayed target can hide a shown readout. Release geometry without cancelling its new request.
          if (current.tip.hidden) { forgetTooltipPosition(current.tip); continue; }
          const removed = records.some(record => record.type === 'childList'
            && Array.from(record.removedNodes).some(node => node.contains(current.root) || node.contains(current.node) || node.contains(current.tip)));
          if (removed || !tooltipPositionAvailable(current)) hideTip(current.root, current.tip, true);
        }
        queueTooltipPositionRefresh();
      });
      tooltipPositionObserver.observe(document.documentElement, { childList: true, attributes: true, attributeFilter: ['hidden', 'data-cfx-interaction-features'], subtree: true });
      if (typeof ResizeObserver !== 'undefined') tooltipPositionResize = new ResizeObserver(queueTooltipPositionRefresh);
    }
    if (tooltipPositionResize) tooltipPositionResize.observe(state.stage);
  };
  const refreshTooltipPosition = (root) => {
    const state = tooltipPositions.get(root.querySelector('.cfx-tooltip'));
    if (state && activeTooltipPositions.has(state)) queueTooltipPositionRefresh();
  };
  const tooltipAnchorRect = (state) => {
    if (state.anchor === 'chart') return state.stage.getBoundingClientRect();
    const rect = state.node.getBoundingClientRect();
    if (state.anchor === 'node') return rect;
    let x = state.event.clientX, y = state.event.clientY;
    if (!Number.isFinite(x) || !Number.isFinite(y)) { x = rect.left + rect.width / 2; y = rect.top + rect.height / 2; }
    if (!Number.isFinite(x) || !Number.isFinite(y)) { x = 24; y = 24; }
    return { left: x, right: x, top: y, bottom: y, width: 0, height: 0 };
  };
  const placeTooltip = (state) => {
    const { root, tip } = state, anchor = tooltipAnchorRect(state);
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
  };
  const positionTooltip = (tip, event, node) => {
    if (!event || tip.hidden) return;
    const root = tip.closest('.cfx-interactive-chart'), stage = root && root.querySelector('.cfx-stage');
    if (!root || !stage || !node) return;
    let state = tooltipPositions.get(tip);
    const pinned = state && root.dataset.cfxTooltipPinned === 'true' && state.key === root.dataset.cfxPinnedTarget;
    if (!state) { state = { root, tip, stage }; tooltipPositions.set(tip, state); }
    if (!pinned) { state.node = node; state.key = targetKey(targetIdentity(node)); }
    state.event = event; state.anchor = root.dataset.cfxTooltipAnchor || 'pointer';
    placeTooltip(state);
    if (state.anchor !== 'pointer') watchTooltipPosition(state);
  };
