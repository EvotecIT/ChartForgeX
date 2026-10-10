  // Timing is scoped to one chart and semantic target; hover and guide notifications stay immediate.
  const tooltipRequests = new WeakMap();
  const pendingTooltips = new Set();
  let tooltipObserver = null;
  const tooltipObservedTrees = new Set();
  // A closed shadow tree is still reachable from its chart. Keep every enclosing tree in the lifetime boundary.
  const tooltipRootTrees = (root) => {
    const trees = [];
    for (let tree = root.getRootNode(); tree; tree = tree.host && tree.host.getRootNode()) trees.push(tree);
    return trees;
  };
  const tooltipDelay = (root) => {
    const value = Number(root.dataset.cfxTooltipDelay || 0);
    return Number.isInteger(value) && value >= 0 && value <= 2147483647 ? value : 0;
  };
  const removePendingTooltip = (request) => {
    pendingTooltips.delete(request);
    if (!pendingTooltips.size && tooltipObserver) {
      tooltipObserver.disconnect(); tooltipObserver = null; tooltipObservedTrees.clear();
    } else if (tooltipObserver && Array.from(tooltipObservedTrees).some(tree =>
      !Array.from(pendingTooltips).some(pending => pending.trees.includes(tree)))) {
      // A shared observer cannot unobserve one tree. Rebuild through the same watcher to release canceled hosts.
      // Preserve queued removals for surviving requests before disconnect discards the native record queue.
      checkPendingTooltipMutations(tooltipObserver.takeRecords());
      if (!tooltipObserver) return;
      tooltipObserver.disconnect(); tooltipObservedTrees.clear();
      for (const pending of pendingTooltips) watchPendingTooltips(pending);
    }
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
    let hit = root.ownerDocument.elementFromPoint(event.clientX, event.clientY);
    const trees = tooltipRootTrees(root);
    // Descend only through the actual enclosing host hit: outer dialogs must continue to obscure the chart.
    for (let index = trees.length - 2; index >= 0; index--) {
      if (hit !== trees[index].host) return null;
      hit = trees[index].elementFromPoint(event.clientX, event.clientY);
    }
    if (!hit || !root.contains(hit)) return null;
    const legend = hit && hit.closest('[data-cfx-role="legend-item"]');
    if (legend && root.contains(legend) && pointerTargetPaint(legend)) return legend;
    const candidates = pointerCandidates(root, { target: hit, clientX: event.clientX, clientY: event.clientY }, root.dataset.cfxTooltipRange !== 'exact');
    const point = acquiredTooltipPoint(root, candidates);
    return point && point.node;
  };
  const checkPendingTooltipMutations = (records) => {
    for (const pending of Array.from(pendingTooltips)) {
      const removed = records.some(record => record.type === 'childList'
        && Array.from(record.removedNodes).some(node => node.contains(pending.root) || node.contains(pending.node) || node.contains(pending.tip)
          || pending.trees.some(tree => tree.host && node.contains(tree.host))));
      if (removed || !tooltipRequestAvailable(pending)) cancelTooltipRequest(pending.root);
    }
  };
  const watchPendingTooltips = (request) => {
    // Observe only while a request is queued, so a detached or briefly replaced host cannot retain a long timer.
    if (!tooltipObserver) tooltipObserver = new MutationObserver(checkPendingTooltipMutations);
    for (const tree of request.trees) {
      if (tooltipObservedTrees.has(tree)) continue;
      tooltipObserver.observe(tree, { childList: true, attributes: true, subtree: true });
      tooltipObservedTrees.add(tree);
    }
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
    const request = { root, tip, node, key, event, shown: false, trees: tooltipRootTrees(root) };
    tooltipRequests.set(root, request);
    pendingTooltips.add(request);
    watchPendingTooltips(request);
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
