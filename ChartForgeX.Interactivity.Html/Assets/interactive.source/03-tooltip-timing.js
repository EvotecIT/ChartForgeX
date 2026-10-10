  // Timing is scoped to one chart and semantic target; hover and guide notifications stay immediate.
  const tooltipRequests = new WeakMap();
  const pendingTooltips = new Set();
  let tooltipObserver = null;
  const tooltipDelay = (root) => {
    const value = Number(root.dataset.cfxTooltipDelay || 0);
    return Number.isInteger(value) && value >= 0 && value <= 2147483647 ? value : 0;
  };
  const removePendingTooltip = (request) => {
    pendingTooltips.delete(request);
    if (!pendingTooltips.size && tooltipObserver) { tooltipObserver.disconnect(); tooltipObserver = null; }
  };
  const cancelTooltipRequest = (root) => {
    const request = tooltipRequests.get(root);
    if (!request) return;
    if (request.timer !== undefined) window.clearTimeout(request.timer);
    removePendingTooltip(request);
    tooltipRequests.delete(root);
  };
  const tooltipRequestAvailable = ({ root, tip, node }) => root.isConnected && tip.isConnected && root.contains(tip)
    && node.isConnected && root.contains(node) && hasFeature(root, 'Tooltips')
    && root.dataset.cfxTooltipPinned !== 'true' && !!pointerTargetPaint(node);
  const currentPointerTooltip = (root, event) => {
    if (!event || !Number.isFinite(event.clientX) || !Number.isFinite(event.clientY) || !hasFeature(root, 'Tooltips')) return null;
    // The event's old hit can outlive a scroll, reflow or host update. Resolve the actual native surface again.
    const hit = document.elementFromPoint(event.clientX, event.clientY);
    if (!hit || !root.contains(hit)) return null;
    const legend = hit && hit.closest('[data-cfx-role="legend-item"]');
    if (legend && root.contains(legend) && pointerTargetPaint(legend)) return legend;
    const candidates = pointerCandidates(root, { target: hit, clientX: event.clientX, clientY: event.clientY }, root.dataset.cfxTooltipRange !== 'exact');
    const point = acquiredTooltipPoint(root, candidates);
    return point && point.node;
  };
  const watchPendingTooltips = () => {
    if (tooltipObserver) return;
    // Observe only while a request is queued, so a detached or briefly replaced host cannot retain a long timer.
    tooltipObserver = new MutationObserver((records) => {
      for (const request of Array.from(pendingTooltips)) {
        const removed = records.some(record => record.type === 'childList'
          && Array.from(record.removedNodes).some(node => node.contains(request.root) || node.contains(request.node) || node.contains(request.tip)));
        if (removed || !tooltipRequestAvailable(request)) cancelTooltipRequest(request.root);
      }
    });
    tooltipObserver.observe(document.documentElement, { childList: true, attributes: true, subtree: true });
  };
  const requestPointerTip = (root, tip, node, event) => {
    const key = targetKey(targetIdentity(node));
    const previous = tooltipRequests.get(root);
    if (previous && previous.key === key) {
      previous.node = node; previous.event = event;
      if (previous.shown) displayTip(tip, node, event);
      return;
    }
    cancelTooltipRequest(root);
    tip.hidden = true;
    const request = { root, tip, node, key, event, shown: false };
    tooltipRequests.set(root, request);
    pendingTooltips.add(request);
    watchPendingTooltips();
    request.timer = window.setTimeout(() => {
      removePendingTooltip(request);
      request.timer = undefined;
      if (tooltipRequests.get(root) !== request) return;
      const acquired = tooltipRequestAvailable(request) && currentPointerTooltip(root, request.event);
      if (!acquired || targetKey(targetIdentity(acquired)) !== request.key) { cancelTooltipRequest(root); return; }
      request.node = acquired;
      request.shown = true;
      displayTip(tip, acquired, request.event);
    }, tooltipDelay(root));
  };
  // An immediate focus readout is already shown for this identity; pointer handoff must not hide it again.
  const rememberImmediateTooltip = (root, tip, node, event) => {
    if (tooltipDelay(root) > 0) tooltipRequests.set(root, { root, tip, node, event, key: targetKey(targetIdentity(node)), shown: true });
  };
  const updateTooltipPointer = (tip, node, event) => {
    if (!(event instanceof PointerEvent)) return;
    const root = tip.closest('.cfx-interactive-chart'), request = root && tooltipRequests.get(root);
    if (request && !request.shown && request.key === targetKey(targetIdentity(node))) request.event = event;
  };
  const retainPointerTip = (root, event) => {
    const request = tooltipRequests.get(root);
    if (!request || !tooltipRequestAvailable(request)) return false;
    const node = currentPointerTooltip(root, event);
    if (!node || targetKey(targetIdentity(node)) !== request.key) return false;
    request.node = node; request.event = event;
    return true;
  };
