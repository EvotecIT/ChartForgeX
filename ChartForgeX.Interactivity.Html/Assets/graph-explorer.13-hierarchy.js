  const hierarchyIndex = (root) => {
    const state = root.__cfxGraphState || graphState(root);
    const children = new Map();
    state.nodes.forEach(node => {
      if (!node.parentId) return;
      const siblings = children.get(node.parentId) || [];
      siblings.push(node.id);
      children.set(node.parentId, siblings);
    });
    children.forEach(ids => ids.sort((left, right) => left.localeCompare(right)));
    return { state, children };
  };
  const hierarchyAncestors = (state, nodeId) => {
    const result = [];
    const visited = new Set();
    let current = state.byId.get(nodeId);
    while (current?.parentId && !visited.has(current.parentId)) {
      visited.add(current.parentId);
      current = state.byId.get(current.parentId);
      if (current) result.unshift(current);
    }
    return result;
  };
  const hierarchyVisibleIds = (rootId, depth, children) => {
    if (!rootId) return null;
    const visibleIds = new Set([rootId]);
    let frontier = [rootId];
    for (let level = 0; level < depth && frontier.length; level += 1) {
      const next = [];
      frontier.forEach(parentId => (children.get(parentId) || []).forEach(childId => {
        if (visibleIds.add(childId)) next.push(childId);
      }));
      frontier = next;
    }
    return visibleIds;
  };
  const updateHierarchyBreadcrumb = (root, state, rootId) => {
    const path = root.querySelector('[data-cfx-role="graph-breadcrumb-path"]');
    const up = root.querySelector('[data-cfx-graph-action="hierarchy-up"]');
    if (!path) return;
    const current = state.byId.get(rootId);
    const ancestors = current && attr(root, 'data-cfx-graph-hierarchy-breadcrumbs') !== 'false' ? hierarchyAncestors(state, rootId) : [];
    const nodes = current ? [...ancestors, current] : [];
    const labels = nodes.length ? nodes.map(node => node.label || node.id) : ['Overview'];
    path.replaceChildren();
    if (!nodes.length) path.textContent = 'Overview';
    nodes.forEach((node, index) => {
      if (index) {
        const separator = root.ownerDocument.createElement('span');
        separator.className = 'cfx-graph-breadcrumb-separator';
        separator.setAttribute('aria-hidden', 'true');
        separator.textContent = '›';
        path.appendChild(separator);
      }
      const button = root.ownerDocument.createElement('button');
      button.type = 'button';
      button.className = 'cfx-graph-breadcrumb-link';
      button.textContent = node.label || node.id;
      button.setAttribute('data-cfx-hierarchy-node', node.id);
      button.addEventListener('click', () => applyHierarchyView(root, node.id, Number(root.dataset.cfxGraphHierarchyDepth || num(root, 'data-cfx-graph-hierarchy-depth', 2))));
      path.appendChild(button);
    });
    path.title = labels.join(' / ');
    if (up) up.disabled = !current;
  };
  const applyHierarchyView = (root, requestedRootId, requestedDepth, options) => {
    if (!hasFeature(root, 'HierarchyNavigation')) return false;
    const { state, children } = hierarchyIndex(root);
    const rootId = requestedRootId || '';
    if (rootId && !state.byId.has(rootId)) return false;
    const depth = Math.max(0, Number.isFinite(Number(requestedDepth)) ? Number(requestedDepth) : num(root, 'data-cfx-graph-hierarchy-depth', 2));
    const visibleIds = hierarchyVisibleIds(rootId, depth, children);
    state.nodes.forEach(node => node.el.classList.toggle('cfx-graph-hierarchy-hidden', !!visibleIds && !visibleIds.has(node.id)));
    const hiddenEdges = new Set();
    state.edges.forEach(edge => {
      const hidden = !!visibleIds && (!visibleIds.has(edge.source.id) || !visibleIds.has(edge.target.id));
      edge.el.classList.toggle('cfx-graph-hierarchy-hidden', hidden);
      if (hidden) hiddenEdges.add(edge.id);
    });
    items(root, '[data-cfx-role="graph-edge-label"]').forEach(label => label.classList.toggle('cfx-graph-hierarchy-hidden', hiddenEdges.has(attr(label, 'data-edge-label-for'))));
    state.clusters.forEach(cluster => {
      const visibleMemberCount = visibleIds ? cluster.nodeIds.filter(id => visibleIds.has(id)).length : cluster.nodeIds.length;
      const collapsed = attr(cluster.el, 'data-cluster-collapsed') === 'true';
      const hidden = collapsed ? visibleMemberCount === 0 : visibleMemberCount < 2;
      cluster.el.classList.toggle('cfx-graph-hierarchy-hidden', hidden);
    });
    updateClusters(state.clusters, state.byId);
    root.dataset.cfxGraphHierarchyRoot = rootId;
    root.dataset.cfxGraphHierarchyDepth = String(depth);
    root.dataset.cfxGraphHierarchyVisibleNodes = String(visibleIds?.size || state.nodes.length);
    updateHierarchyBreadcrumb(root, state, rootId);
    clearHiddenSelections(root);
    applyFilters(root);
    if (hasFeature(root, 'LevelOfDetail')) applyLod(root);
    applyLayout(root, state);
    const shouldFit = options?.fit !== false && attr(root, 'data-cfx-graph-hierarchy-auto-fit') !== 'false';
    if (shouldFit && hasFeature(root, 'Viewport')) {
      root.__cfxGraphViewportTouched = false;
      fitViewport(root);
    }
    if (options?.restartPhysics !== false && attr(root, 'data-cfx-graph-reheat-hierarchy') !== 'false' && hasFeature(root, 'RuntimePhysics')) reheatPhysics(root, 'hierarchy-change', { rebuild: true, fit: false });
    emit(root, 'cfxgraphnavigate', { graphId: attr(root, 'data-cfx-graph-id'), rootNodeId: rootId, depth, visibleNodeCount: visibleIds?.size || state.nodes.length, breadcrumbNodeIds: rootId ? [...hierarchyAncestors(state, rootId).map(node => node.id), rootId] : [] });
    return true;
  };
  const drillHierarchyNode = (root, nodeId) => {
    const { children } = hierarchyIndex(root);
    if (!(children.get(nodeId) || []).length) return false;
    return applyHierarchyView(root, nodeId, Number(root.dataset.cfxGraphHierarchyDepth || num(root, 'data-cfx-graph-hierarchy-depth', 2)));
  };
  const navigateHierarchyUp = (root) => {
    const currentId = root.dataset.cfxGraphHierarchyRoot || '';
    if (!currentId) return applyHierarchyView(root, '', undefined);
    const state = root.__cfxGraphState || graphState(root);
    return applyHierarchyView(root, state.byId.get(currentId)?.parentId || '', Number(root.dataset.cfxGraphHierarchyDepth || num(root, 'data-cfx-graph-hierarchy-depth', 2)));
  };
  const toggleGraphCluster = (root, clusterId) => {
    const cluster = items(root, '[data-cfx-role="graph-cluster"]').find(item => attr(item, 'data-cluster-id') === clusterId);
    if (!cluster) return false;
    applyClusterState(root, attr(cluster, 'data-cluster-collapsed') !== 'true', clusterId);
    return true;
  };
  const applySemanticZoom = (root, scale) => {
    if (!hasFeature(root, 'LevelOfDetail')) return;
    const overview = scale <= num(root, 'data-cfx-lod-overview-scale', .58);
    const detail = scale >= num(root, 'data-cfx-lod-detail-scale', 1.08);
    root.classList.toggle('cfx-graph-semantic-overview', overview);
    root.classList.toggle('cfx-graph-semantic-compact', !overview && !detail);
    root.classList.toggle('cfx-graph-semantic-detail', detail);
    root.dataset.cfxGraphSemanticZoom = overview ? 'overview' : detail ? 'detail' : 'compact';
  };
  const centerGraphNode = (root, nodeId, minimumScale) => {
    const state = root.__cfxGraphState || graphState(root);
    const node = state.byId.get(nodeId);
    if (!node || !visible(node.el)) return false;
    const size = sceneSize(root), current = viewport(root), scale = Math.max(current.scale, minimumScale || 1.15);
    root.__cfxGraphViewportTouched = true;
    setViewport(root, { x: size.centerX - node.x * scale, y: size.centerY - node.y * scale, scale });
    return true;
  };
  const bindHierarchyInteractions = (root) => {
    if (!hasFeature(root, 'HierarchyNavigation') && !hasFeature(root, 'Clustering')) return;
    const stage = root.querySelector('.cfx-graph-stage');
    if (!stage) return;
    const hierarchy = hierarchyIndex(root);
    hierarchy.state.nodes.forEach(node => node.el.setAttribute('data-node-has-children', (hierarchy.children.get(node.id) || []).length ? 'true' : 'false'));
    stage.addEventListener('dblclick', event => {
      if (event.target.closest?.('.cfx-graph-command-rail,.cfx-graph-breadcrumbs')) return;
      let item = event.target.closest?.('[data-cfx-role="graph-node"],[data-cfx-role="graph-cluster"]');
      const recentPointerNode = Date.now() - (root.__cfxGraphLastPointerHitTick || 0) < 700 ? root.dataset.cfxGraphLastPointerHit : '';
      if (!item && recentPointerNode) item = (root.__cfxGraphState || graphState(root)).byId.get(recentPointerNode)?.el;
      if (!item) item = hitGraphItemAt(root, scenePoint(root, event))?.el;
      if (!item) {
        if (root.dataset.cfxGraphHierarchyRoot && navigateHierarchyUp(root)) event.preventDefault();
        return;
      }
      const role = attr(item, 'data-cfx-role');
      if (role === 'graph-node' && attr(root, 'data-cfx-graph-hierarchy-drill') !== 'false' && drillHierarchyNode(root, attr(item, 'data-node-id'))) { event.preventDefault(); event.stopPropagation(); }
      if (role === 'graph-cluster' && toggleGraphCluster(root, attr(item, 'data-cluster-id'))) { event.preventDefault(); event.stopPropagation(); }
    });
    stage.addEventListener('keydown', event => {
      if (event.target.closest?.('.cfx-graph-command-rail,.cfx-graph-breadcrumbs')) return;
      const item = event.target.closest?.('[data-cfx-role="graph-node"],[data-cfx-role="graph-cluster"]');
      if (event.key === 'ArrowRight' && item && attr(item, 'data-cfx-role') === 'graph-node' && drillHierarchyNode(root, attr(item, 'data-node-id'))) event.preventDefault();
      if ((event.key === 'ArrowLeft' || event.key === 'Escape' || event.key === 'Backspace') && navigateHierarchyUp(root)) event.preventDefault();
    });
  };
