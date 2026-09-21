  const graphNeighborhoodConfiguration = root => ({
    hops: num(root, 'data-cfx-neighborhood-hops', 1), maximumNodes: num(root, 'data-cfx-neighborhood-max-nodes', 13),
    maximumEdges: num(root, 'data-cfx-neighborhood-max-edges', 24), neighborOffset: num(root, 'data-cfx-neighborhood-offset', 0)
  });
  const graphFocusSnapshot = root => ({ active: root.dataset.cfxGraphFocus === 'active',
    nodeId: root.dataset.cfxGraphFocusNode || '', ...graphNeighborhoodConfiguration(root), ...(root.__cfxGraphNeighborhood || {}),
    overview: root.__cfxGraphNeighborhoodOverview ? JSON.parse(JSON.stringify(root.__cfxGraphNeighborhoodOverview)) : null,
    history: JSON.parse(JSON.stringify(root.__cfxGraphNeighborhoodHistory || [])) });
  const neighborhoodBaseVisible = element => !['cfx-graph-hidden', 'cfx-graph-cluster-collapsed-member', 'cfx-graph-bundle-member', 'cfx-graph-overview-member', 'cfx-graph-hierarchy-hidden'].some(name => element.classList.contains(name));
  const refreshNeighborhoodPresentation = root => {
    root.__cfxGraphHitGrid = null; root.__cfxGraphHitVersion = (root.__cfxGraphHitVersion || 0) + 1;
    const state = root.__cfxGraphState || graphState(root);
    syncNodeDetailLayers(state);
    drawCanvas(root, state);
    if (typeof updateOverview === 'function') updateOverview(root, state);
    if (typeof syncFocusControls === 'function') syncFocusControls(root);
    syncGraphItemTabStops(root);
  };
  const clearInvalidatedNeighborhoodFocus = (root, state) => {
    if (root.dataset.cfxGraphFocus !== 'active') return false;
    const focusNode = state?.byId?.get(root.dataset.cfxGraphFocusNode || '');
    if (focusNode && visible(focusNode.el)) return false;
    return clearNeighborhoodFocus(root, { restoreSelection: false, restoreViewport: false });
  };
  const clearNeighborhoodFocus = (root, options) => {
    const overview = root.__cfxGraphNeighborhoodOverview;
    root.classList.remove('cfx-graph-neighborhood-active');
    root.dataset.cfxGraphFocus = 'none'; root.dataset.cfxGraphFocusNode = '';
    root.__cfxGraphNeighborhood = null; root.__cfxGraphNeighborhoodView = null; root.__cfxGraphNeighborhoodHistory = []; root.__cfxGraphNeighborhoodOverview = null;
    items(root, '.cfx-graph-neighborhood-hidden,.cfx-graph-neighborhood-dim,.cfx-graph-neighborhood-related,.cfx-graph-neighborhood-primary').forEach(item => {
      item.classList.remove('cfx-graph-neighborhood-hidden', 'cfx-graph-neighborhood-dim', 'cfx-graph-neighborhood-related', 'cfx-graph-neighborhood-primary');
    });
    const navigation = root.querySelector('[data-cfx-role="graph-neighborhood-navigation"]');
    if (navigation) navigation.hidden = true;
    if (overview && options?.restoreSelection !== false) { restoreGraphSelection(root, overview.selection); clearHiddenSelections(root); }
    if (overview && options?.restoreViewport !== false && hasFeature(root, 'Viewport')) {
      setViewport(root, overview.viewport);
      root.__cfxGraphViewportTouched = overview.viewportTouched === true;
    }
    if (hasFeature(root, 'LevelOfDetail')) applyLod(root);
    refreshNeighborhoodPresentation(root);
    emit(root, 'cfxgraphfocus', { graphId: attr(root, 'data-cfx-graph-id'), active: false, nodeId: '', neighborNodeCount: 0, edgeCount: 0 });
    return true;
  };
  const syncNeighborhoodNavigation = (root, view) => {
    const navigation = root.querySelector('[data-cfx-role="graph-neighborhood-navigation"]');
    if (!navigation) return;
    navigation.hidden = false;
    const history = root.__cfxGraphNeighborhoodHistory || [];
    for (const [action, enabled] of [['focus-back', history.length > 0], ['focus-previous', view.hasPrevious], ['focus-next', view.hasNext]]) {
      const button = navigation.querySelector(`[data-cfx-graph-action="${action}"]`);
      if (button) { button.disabled = !enabled; button.setAttribute('aria-disabled', String(!enabled)); }
    }
    const summary = navigation.querySelector('[data-cfx-role="graph-neighborhood-summary"]');
    if (summary) {
      const state = graphState(root), label = id => state.byId.get(id)?.label || id;
      const path = [...history.slice(-3).map(entry => label(entry.nodeId)), label(view.rootNodeId)].join(' / ');
      summary.textContent = `${path}: ${view.nodeIds.size} of ${view.scopeNodeCount} neighborhood nodes · ${view.edgeIds.size} relationships · ${view.hiddenNodeCount} nodes and ${view.hiddenEdgeCount} relationships omitted`;
    }
  };
  const applyNeighborhoodFocus = (root, nodeId, configuration, navigation) => {
    if (!hasFeature(root, 'NeighborhoodFocus')) return false;
    const cachedState = root.__cfxGraphState;
    const state = cachedState || graphState(root), options = neighborhoodOptions({ ...graphNeighborhoodConfiguration(root), ...configuration });
    const plannedNodes = state.nodes.map(node => ({ id: node.id, visible: neighborhoodBaseVisible(node.el) && attr(node.el, 'data-node-hidden') !== 'true' }));
    const plannedEdges = state.edges.map(edge => ({ id: edge.id || attr(edge.el, 'data-edge-id'), sourceId: edge.source.id, targetId: edge.target.id,
        visible: neighborhoodBaseVisible(edge.el) && attr(edge.el, 'data-edge-hidden') !== 'true' }));
    let view = planGraphNeighborhood(plannedNodes, plannedEdges, nodeId, options);
    if (view && navigation?.refresh && options.neighborOffset > 0 && options.neighborOffset >= view.scopeNodeCount - 1) {
      options.neighborOffset = 0; view = planGraphNeighborhood(plannedNodes, plannedEdges, nodeId, options);
    }
    if (!view) return false;
    if (typeof pausePhysics === 'function' && hasFeature(root, 'RuntimePhysics')) pausePhysics(root);
    if (cachedState && typeof syncSvgLayout === 'function') syncSvgLayout(root, cachedState);
    if (!navigation?.refresh) {
      if (!root.__cfxGraphNeighborhoodOverview) root.__cfxGraphNeighborhoodOverview = {
        viewport: { ...viewport(root) },
        viewportTouched: root.__cfxGraphViewportTouched === true,
        selection: selectedItems(root).map(item => ({ id: item.id, role: item.role }))
      };
      if (root.dataset.cfxGraphFocus === 'active' && root.dataset.cfxGraphFocusNode !== nodeId) {
        const history = root.__cfxGraphNeighborhoodHistory || (root.__cfxGraphNeighborhoodHistory = []);
        history.push({ nodeId: root.dataset.cfxGraphFocusNode, options: root.__cfxGraphNeighborhood, viewport: { ...viewport(root) } });
        if (history.length > 50) history.shift();
      }
    }
    state.nodes.forEach(node => {
      node.el.classList.toggle('cfx-graph-neighborhood-hidden', !view.nodeIds.has(node.id));
      node.el.classList.toggle('cfx-graph-neighborhood-primary', node.id === nodeId);
      node.el.classList.toggle('cfx-graph-neighborhood-related', view.nodeIds.has(node.id) && node.id !== nodeId);
    });
    state.edges.forEach(edge => {
      const related = view.edgeIds.has(edge.id || attr(edge.el, 'data-edge-id'));
      edge.el.classList.toggle('cfx-graph-neighborhood-hidden', !related);
      edge.el.classList.toggle('cfx-graph-neighborhood-related', related);
    });
    items(root, '[data-cfx-role="graph-edge-label"]').forEach(label => {
      const related = view.edgeIds.has(attr(label, 'data-edge-label-for'));
      label.classList.toggle('cfx-graph-neighborhood-hidden', !related); label.classList.toggle('cfx-graph-neighborhood-related', related);
    });
    state.clusters.forEach(cluster => cluster.el.classList.add('cfx-graph-neighborhood-hidden'));
    root.classList.add('cfx-graph-neighborhood-active'); root.dataset.cfxGraphFocus = 'active'; root.dataset.cfxGraphFocusNode = nodeId;
    root.__cfxGraphNeighborhood = { hops: options.hops, maximumNodes: options.maximumNodes, maximumEdges: options.maximumEdges, neighborOffset: options.neighborOffset };
    root.__cfxGraphNeighborhoodView = view;
    clearHiddenSelections(root);
    if (hasFeature(root, 'LevelOfDetail')) applyLod(root);
    syncNeighborhoodNavigation(root, view); refreshNeighborhoodPresentation(root);
    if (navigation?.fit !== false && hasFeature(root, 'Viewport')) { root.__cfxGraphViewportTouched = true; fitViewport(root); }
    emit(root, 'cfxgraphfocus', { graphId: attr(root, 'data-cfx-graph-id'), active: true, nodeId, ...root.__cfxGraphNeighborhood,
      neighborNodeCount: view.nodeIds.size - 1, edgeCount: view.edgeIds.size, scopeNodeCount: view.scopeNodeCount,
      hiddenNodeCount: view.hiddenNodeCount, hiddenEdgeCount: view.hiddenEdgeCount, boundaryEdgeCount: view.boundaryEdgeCount });
    return true;
  };
  const pageGraphNeighborhood = (root, direction) => {
    if (root.dataset.cfxGraphFocus !== 'active') return false;
    const options = root.__cfxGraphNeighborhood;
    if (!options || (direction < 0 ? !root.__cfxGraphNeighborhoodView?.hasPrevious : !root.__cfxGraphNeighborhoodView?.hasNext)) return false;
    return applyNeighborhoodFocus(root, root.dataset.cfxGraphFocusNode, { ...options, neighborOffset: Math.max(0, options.neighborOffset + direction * (options.maximumNodes - 1)) });
  };
  const backGraphNeighborhood = root => {
    const history = root.__cfxGraphNeighborhoodHistory || [], previous = history.pop();
    if (!previous) return clearNeighborhoodFocus(root);
    const applied = applyNeighborhoodFocus(root, previous.nodeId, previous.options, { refresh: true, fit: false });
    if (!applied) return clearNeighborhoodFocus(root);
    if (hasFeature(root, 'Viewport')) setViewport(root, previous.viewport);
    if (hasFeature(root, 'Selection')) restoreGraphSelection(root, [{ id: previous.nodeId, role: 'graph-node' }]);
    refreshNeighborhoodPresentation(root);
    return true;
  };
  const toggleNeighborhoodFocus = root => {
    if (!hasFeature(root, 'NeighborhoodFocus')) return;
    const nodeId = selectedGraphNodeId(root);
    if (!nodeId || root.dataset.cfxGraphFocusNode === nodeId) clearNeighborhoodFocus(root);
    else applyNeighborhoodFocus(root, nodeId);
  };
