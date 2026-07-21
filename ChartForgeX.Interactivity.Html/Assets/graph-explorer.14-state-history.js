  const graphStateStorage = root => attr(root, 'data-cfx-graph-state-key') || `cfx-graph-state:${attr(root, 'data-cfx-graph-id') || 'graph'}`;
  const graphHistory = root => root.__cfxGraphHistory || (root.__cfxGraphHistory = { undo: [], redo: [], applying: false });
  const graphSnapshotDocument = root => {
    const document = exportGraphJson(root);
    return { nodes: document.nodes, edges: document.edges, clusters: document.clusters };
  };
  const captureGraphInteractionState = (root, source) => ({
    version: 1,
    graphId: attr(root, 'data-cfx-graph-id'),
    source: source || 'api',
    capturedAt: new Date().toISOString(),
    viewport: viewport(root),
    selection: (root.dataset.cfxGraphSelectionIds || '').split(',').filter(Boolean),
    focus: { active: root.dataset.cfxGraphFocus === 'active', nodeId: root.dataset.cfxGraphFocusNode || '' },
    hierarchy: { rootNodeId: root.dataset.cfxGraphHierarchyRoot || '', depth: Number(root.dataset.cfxGraphHierarchyDepth || attr(root, 'data-cfx-graph-hierarchy-depth') || 0) },
    clusters: items(root, '[data-cfx-role="graph-cluster"]').map(cluster => ({ id: attr(cluster, 'data-cluster-id'), collapsed: attr(cluster, 'data-cluster-collapsed') === 'true' })),
    positions: (root.__cfxGraphState || graphState(root)).nodes.map(node => ({ id: node.id, x: Number(node.x.toFixed(3)), y: Number(node.y.toFixed(3)), fixed: attr(node.el, 'data-node-fixed') === 'true' })),
    document: hasFeature(root, 'IncrementalUpdates') ? graphSnapshotDocument(root) : null
  });
  const syncGraphHistoryControls = root => {
    const history = graphHistory(root);
    items(root, "[data-cfx-graph-action='undo']").forEach(button => { button.disabled = history.undo.length === 0; button.setAttribute('aria-disabled', button.disabled ? 'true' : 'false'); });
    items(root, "[data-cfx-graph-action='redo']").forEach(button => { button.disabled = history.redo.length === 0; button.setAttribute('aria-disabled', button.disabled ? 'true' : 'false'); });
    root.dataset.cfxGraphUndoCount = String(history.undo.length);
    root.dataset.cfxGraphRedoCount = String(history.redo.length);
  };
  const persistGraphInteractionState = (root, source) => {
    const state = captureGraphInteractionState(root, source || 'persistence');
    if (attr(root, 'data-cfx-graph-state-persist') === 'true') {
      try { localStorage.setItem(graphStateStorage(root), JSON.stringify(state)); root.dataset.cfxGraphStatePersisted = 'true'; }
      catch (error) { root.dataset.cfxGraphStatePersisted = 'false'; root.dataset.cfxGraphStateError = error?.name || 'storage-error'; }
    }
    emit(root, 'cfxgraphstate', { graphId: attr(root, 'data-cfx-graph-id'), source: source || 'persistence', state });
    return state;
  };
  const restoreGraphClusterStates = (root, snapshots) => {
    (snapshots || []).forEach(snapshot => applyClusterState(root, !!snapshot.collapsed, String(snapshot.id || ''), { reheat: false }));
  };
  const restoreGraphSelection = (root, ids) => {
    const selected = new Set((ids || []).map(String));
    items(root, '[data-cfx-role="graph-node"],[data-cfx-role="graph-edge"],[data-cfx-role="graph-cluster"]').forEach(item => {
      const id = attr(item, 'data-node-id') || attr(item, 'data-edge-id') || attr(item, 'data-cluster-id');
      item.classList.toggle('cfx-graph-selected', selected.has(id));
    });
    const details = updateSelectionState(root);
    syncSelectionTooltip(root, details);
  };
  const graphSnapshotPatch = (root, snapshot) => {
    const current = graphSnapshotDocument(root), document = snapshot.document;
    const retained = (values, upserts) => values.filter(id => !(upserts || []).some(item => String(item.id) === String(id)));
    return {
      removeNodeIds: retained(current.nodes.map(node => node.id), document.nodes),
      removeEdgeIds: retained(current.edges.map(edge => edge.id), document.edges),
      removeClusterIds: retained(current.clusters.map(cluster => cluster.id), document.clusters),
      upsertNodes: document.nodes,
      upsertEdges: document.edges,
      upsertClusters: document.clusters,
      removeIncidentReferences: true,
      fit: false
    };
  };
  const applyGraphInteractionState = (root, snapshot, options) => {
    if (!root || !snapshot || Number(snapshot.version) !== 1) return false;
    const history = graphHistory(root); history.applying = true;
    try {
      if (snapshot.document && hasFeature(root, 'IncrementalUpdates')) applyGraphRuntimePatch(root, graphSnapshotPatch(root, snapshot));
      const byId = new Map((root.__cfxGraphState || graphState(root)).nodes.map(node => [node.id, node]));
      (snapshot.positions || []).forEach(position => {
        const node = byId.get(String(position.id));
        if (!node || !Number.isFinite(Number(position.x)) || !Number.isFinite(Number(position.y))) return;
        node.x = Number(position.x); node.y = Number(position.y); node.vx = 0; node.vy = 0; node.fixed = !!position.fixed;
        node.el.setAttribute('data-node-x', node.x.toFixed(3)); node.el.setAttribute('data-node-y', node.y.toFixed(3)); node.el.setAttribute('data-node-fixed', node.fixed ? 'true' : 'false');
      });
      const state = root.__cfxGraphState || graphState(root); applyLayout(root, state);
      restoreGraphClusterStates(root, snapshot.clusters);
      if (snapshot.hierarchy && hasFeature(root, 'HierarchyNavigation')) applyHierarchyView(root, snapshot.hierarchy.rootNodeId || '', snapshot.hierarchy.depth, { fit: false, restartPhysics: false });
      if (snapshot.viewport && hasFeature(root, 'Viewport')) { root.__cfxGraphViewportTouched = true; setViewport(root, snapshot.viewport); }
      restoreGraphSelection(root, snapshot.selection);
      if (snapshot.focus?.active && snapshot.focus.nodeId) applyNeighborhoodFocus(root, snapshot.focus.nodeId); else if (root.dataset.cfxGraphFocus === 'active') clearNeighborhoodFocus(root);
      drawCanvas(root, state); if (typeof updateOverview === 'function') updateOverview(root, state);
      emit(root, 'cfxgraphstateapplied', { graphId: attr(root, 'data-cfx-graph-id'), source: options?.source || snapshot.source || 'api', state: snapshot });
      if (options?.persist !== false) persistGraphInteractionState(root, options?.source || 'state-apply');
      return true;
    } finally { history.applying = false; syncGraphHistoryControls(root); }
  };
  const checkpointGraphState = (root, label, snapshot) => {
    if (!hasFeature(root, 'History')) return;
    const history = graphHistory(root), limit = Math.max(1, Number(attr(root, 'data-cfx-graph-history-limit')) || 40);
    history.undo.push({ label: label || 'Graph change', state: snapshot || captureGraphInteractionState(root, 'history') });
    if (history.undo.length > limit) history.undo.splice(0, history.undo.length - limit);
    history.redo.length = 0; syncGraphHistoryControls(root);
  };
  const traverseGraphHistory = (root, direction) => {
    if (!hasFeature(root, 'History')) return false;
    const history = graphHistory(root), source = direction === 'redo' ? history.redo : history.undo, destination = direction === 'redo' ? history.undo : history.redo;
    const entry = source.pop(); if (!entry) return false;
    destination.push({ label: entry.label, state: captureGraphInteractionState(root, 'history') });
    const applied = applyGraphInteractionState(root, entry.state, { source: direction, persist: true });
    syncGraphHistoryControls(root);
    emit(root, 'cfxgraphhistory', { graphId: attr(root, 'data-cfx-graph-id'), action: direction, label: entry.label, undoCount: history.undo.length, redoCount: history.redo.length });
    return applied;
  };
  const initializeGraphInteractionState = root => {
    syncGraphHistoryControls(root);
    if (!hasFeature(root, 'StateSnapshots') || attr(root, 'data-cfx-graph-state-persist') !== 'true') return false;
    try {
      const value = localStorage.getItem(graphStateStorage(root));
      if (!value) return false;
      return applyGraphInteractionState(root, JSON.parse(value), { source: 'storage', persist: false });
    } catch (error) { root.dataset.cfxGraphStateError = error?.name || 'storage-error'; return false; }
  };
  const bindGraphStatePersistence = root => {
    if (!hasFeature(root, 'StateSnapshots') || attr(root, 'data-cfx-graph-state-persist') !== 'true') return;
    let timer = 0;
    const schedule = event => {
      if (graphHistory(root).applying || event.detail?.source === 'storage') return;
      if (timer) clearTimeout(timer);
      timer = setTimeout(() => { timer = 0; persistGraphInteractionState(root, event.type); }, 80);
    };
    ['cfxgraphselection', 'cfxgraphviewport', 'cfxgraphcluster', 'cfxgraphfocus', 'cfxgraphhierarchy'].forEach(name => root.addEventListener(name, schedule));
  };
