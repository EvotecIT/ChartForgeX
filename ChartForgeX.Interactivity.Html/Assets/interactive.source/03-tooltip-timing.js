
  const tooltipRequests = new WeakMap();
  const pendingTooltips = new Set();
  let tooltipObserver = null;
  const tooltipObservedNodes = new Map();
  const pointerReadoutSuspended = root => root._cfxPointerReadoutSuspended === true;
  const resumePointerReadout = root => { delete root._cfxPointerReadoutSuspended; };

  const keyboardReadoutOwnsPointer = root => {
    const node = root._cfxKeyboardReadout;
    return !!node && node.isConnected && root.contains(node)
      && root.getRootNode().activeElement === targetFocusNode(node);
  };
  const resumePointerHover = root => {
    delete root._cfxKeyboardReadout;
    resumePointerReadout(root);
  };
  const suspendPointerReadout = root => {
    // Reset resumes on fresh input only.
    root._cfxPointerReadoutSuspended = true;
    hideTip(root, root.querySelector('.cfx-tooltip'), true);
  };

  const tooltipRootTrees = (root) => {
    const trees = [];
    for (let tree = root.getRootNode(); tree; tree = tree.host && tree.host.getRootNode()) trees.push(tree);
    return trees;
  };
  const tooltipLifetimeNodes = (root) => {
    const nodes = [];
    for (let node = root; node; node = node.parentNode || node.host) nodes.push(node);
    return nodes;
  };
  const tooltipDelay = (root) => {
    const value = Number(root.dataset.cfxTooltipDelay || 0);
    return Number.isInteger(value) && value >= 0 && value <= 2147483647 ? value : 0;
  };
  const removePendingTooltip = (request) => {
    if (!pendingTooltips.delete(request)) return;
    if (!pendingTooltips.size && tooltipObserver) {
      tooltipObserver.disconnect(); tooltipObserver = null; tooltipObservedNodes.clear();
    } else if (tooltipObserver) {

      checkPendingTooltipMutations(tooltipObserver.takeRecords());
      if (!tooltipObserver) return;
      tooltipObserver.disconnect(); tooltipObservedNodes.clear();
      for (const pending of pendingTooltips) watchPendingTooltips(pending);
    }
  };
  const cancelTooltipRequest = (root) => {
    // Restored paint never restarts canceled timers.
    root._cfxCancelPointerHover?.();
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

    let hit = root.ownerDocument.elementFromPoint(event.clientX, event.clientY);
    const trees = tooltipRootTrees(root);

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
    // Ignore probes to avoid observer loops.
    records = records.filter(record => record.type !== 'attributes' || record.attributeName !== 'data-cfx-highlight-probe');
    if (!records.length) return;
    for (const pending of Array.from(pendingTooltips)) {
      const removed = records.some(record => record.type === 'childList'
        && Array.from(record.removedNodes).some(node => node.contains(pending.root) || node.contains(pending.node) || node.contains(pending.tip)
          || pending.nodes.some(boundary => node.contains(boundary))));

      const paintChanged = records.some(record => pending.root.contains(record.target)
        || record.type === 'attributes' && pending.nodes.includes(record.target));
      if (removed || paintChanged && !tooltipRequestAvailable(pending)) cancelTooltipRequest(pending.root);
    }
  };
  const watchPendingTooltips = (request) => {

    if (!tooltipObserver) tooltipObserver = new MutationObserver(checkPendingTooltipMutations);
    for (const node of request.nodes) {
      const subtree = node === request.root;
      if (tooltipObservedNodes.has(node) && (tooltipObservedNodes.get(node) || !subtree)) continue;

      tooltipObserver.observe(node, { childList: true, attributes: node instanceof Element, subtree });
      tooltipObservedNodes.set(node, subtree);
    }
  };
  const requestPointerTip = (root, tip, node, event) => {
    if (pointerReadoutSuspended(root)) return;
    const key = targetKey(targetIdentity(node));
    const previous = tooltipRequests.get(root);
    if (previous && previous.key === key && (previous.pointerId === undefined || previous.pointerId === event.pointerId)) {
      previous.node = node; previous.event = event; previous.pointerId = event.pointerId;
      if (previous.shown) displayTip(tip, node, event);
      return;
    }
    cancelTooltipRequest(root);
    tip.hidden = true;
    const request = { root, tip, node, key, event, pointerId: event.pointerId, shown: false, nodes: tooltipLifetimeNodes(root) };
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

  const rememberImmediateTooltip = (root, tip, node, event) => {
    if (tooltipDelay(root) > 0) tooltipRequests.set(root, { root, tip, node, event, key: targetKey(targetIdentity(node)), shown: true });
  };
  const ownsTooltipPointer = (root, event) => {
    const request = tooltipRequests.get(root);
    return !(event instanceof PointerEvent) || !request || request.pointerId === undefined || request.pointerId === event.pointerId;
  };
  const updateTooltipPointer = (tip, node, event) => {
    if (!(event instanceof PointerEvent)) return;
    const root = tip.closest('.cfx-interactive-chart'), request = root && tooltipRequests.get(root);
    if (request && !request.shown && ownsTooltipPointer(root, event) && request.key === targetKey(targetIdentity(node))) request.event = event;
  };
  const retainPointerTip = (root, event) => {
    const request = tooltipRequests.get(root);
    if (!request || !tooltipRequestAvailable(request)) return false;
    const node = currentPointerTooltip(root, event);
    if (!node || targetKey(targetIdentity(node)) !== request.key) return false;
    request.node = node; request.event = event;
    return true;
  };
