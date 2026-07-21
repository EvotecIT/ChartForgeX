  const graphSelectionBox = root => root.querySelector('[data-cfx-role="graph-selection-box"]');
  const setGraphBoxSelectionMode = (root, enabled) => {
    const active = !!enabled && hasFeature(root, 'BoxSelection') && hasFeature(root, 'Selection');
    root.dataset.cfxGraphPointerMode = active ? 'box-select' : 'navigate';
    root.classList.toggle('cfx-graph-box-selecting', active);
    items(root, "[data-cfx-graph-action='box-select']").forEach(button => button.setAttribute('aria-pressed', active ? 'true' : 'false'));
    if (!active) { const box = graphSelectionBox(root); if (box) box.hidden = true; }
    return active;
  };
  const updateGraphSelectionBox = (root, start, current) => {
    const box = graphSelectionBox(root); if (!box) return;
    box.hidden = false;
    box.style.left = `${Math.min(start.x, current.x)}px`; box.style.top = `${Math.min(start.y, current.y)}px`;
    box.style.width = `${Math.abs(current.x - start.x)}px`; box.style.height = `${Math.abs(current.y - start.y)}px`;
  };
  const graphItemScenePoint = (root, state, item) => {
    const role = attr(item, 'data-cfx-role');
    if (role === 'graph-node') { const node = state.nodes.find(candidate => candidate.el === item); return node ? { x: node.x, y: node.y } : null; }
    if (role === 'graph-edge') { const edge = state.edges.find(candidate => candidate.el === item); return edge ? { x: (edge.source.x + edge.target.x) / 2, y: (edge.source.y + edge.target.y) / 2 } : null; }
    if (role === 'graph-cluster') { const cluster = state.clusters.find(candidate => candidate.el === item); const metrics = cluster ? clusterMetrics(cluster, state.byId) : null; return metrics ? { x: metrics.x, y: metrics.y } : null; }
    return null;
  };
  const selectGraphItemsInBox = (root, start, end, additive) => {
    if (!hasFeature(root, 'Selection')) return [];
    const left = Math.min(start.x, end.x), right = Math.max(start.x, end.x), top = Math.min(start.y, end.y), bottom = Math.max(start.y, end.y);
    const state = root.__cfxGraphState || graphState(root);
    const candidates = items(root, '[data-cfx-role="graph-node"],[data-cfx-role="graph-edge"],[data-cfx-role="graph-cluster"]').filter(item => visible(item));
    if (!additive) candidates.forEach(item => item.classList.remove('cfx-graph-selected'));
    const selected = [];
    candidates.forEach(item => {
      const point = graphItemScenePoint(root, state, item);
      if (!point || point.x < left || point.x > right || point.y < top || point.y > bottom) return;
      item.classList.add('cfx-graph-selected'); selected.push(selectionDetail(root, item));
    });
    const details = updateSelectionState(root); syncSelectionTooltip(root, details);
    drawCanvas(root, state); if (typeof updateOverview === 'function') updateOverview(root, state);
    emit(root, 'cfxgraphboxselect', { graphId: attr(root, 'data-cfx-graph-id'), additive: !!additive, bounds: { left, top, right, bottom }, items: selected, selectionCount: details.length });
    return selected;
  };
  const bindGraphBoxSelection = root => {
    root.addEventListener('keydown', event => { if (event.key === 'Escape' && root.dataset.cfxGraphPointerMode === 'box-select') setGraphBoxSelectionMode(root, false); });
  };
