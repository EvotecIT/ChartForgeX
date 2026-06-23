  const setNodePosition = (node) => {
    node.el.setAttribute('transform', `translate(${node.x.toFixed(3)} ${node.y.toFixed(3)})`);
    node.el.setAttribute('data-node-x', node.x.toFixed(3));
    node.el.setAttribute('data-node-y', node.y.toFixed(3));
  };
  const edgeRenderTarget = (edge, control) => {
    if (!edge.directed) return edge.target;
    const from = control || edge.source;
    const dx = edge.target.x - from.x, dy = edge.target.y - from.y;
    const length = Math.max(1, Math.sqrt(dx * dx + dy * dy));
    const inset = nodeBoundaryInset(edge.target, dx / length, dy / length);
    return { x: edge.target.x - dx / length * inset, y: edge.target.y - dy / length * inset };
  };
  const updateEdges = (root, edges) => {
    const labels = new Map(items(root, '[data-cfx-role="graph-edge-label"]').map(label => [attr(label, 'data-edge-label-for'), label]));
    const state = root.__cfxGraphState || graphState(root);
    const byId = state.byId || new Map(state.nodes.map(node => [node.id, node]));
    edges.forEach(edge => {
      const rendered = visualEdge(edge, byId);
      const control = edgeControl(rendered);
      const target = edgeRenderTarget(rendered, control);
      const d = rendered.source === rendered.target
        ? selfLoopPath(rendered.target)
        : control
        ? `M ${rendered.source.x.toFixed(3)} ${rendered.source.y.toFixed(3)} Q ${control.x.toFixed(3)} ${control.y.toFixed(3)} ${target.x.toFixed(3)} ${target.y.toFixed(3)}`
        : `M ${rendered.source.x.toFixed(3)} ${rendered.source.y.toFixed(3)} L ${target.x.toFixed(3)} ${target.y.toFixed(3)}`;
      edge.el.setAttribute('d', d);
      const label = labels.get(attr(edge.el, 'data-edge-id'));
      if (label) {
        const point = edgeLabelPoint(rendered, control);
        label.setAttribute('x', point.x.toFixed(3));
        label.setAttribute('y', point.y.toFixed(3));
      }
    });
  };
  const updateClusters = (clusters, byId) => clusters.forEach(cluster => {
    const members = cluster.nodeIds.map(id => byId.get(id)).filter(Boolean);
    if (!members.length) return;
    const x = members.reduce((sum, node) => sum + node.x, 0) / members.length;
    const y = members.reduce((sum, node) => sum + node.y, 0) / members.length;
    cluster.el.setAttribute('transform', `translate(${x.toFixed(3)} ${y.toFixed(3)})`);
  });
  const applyLayout = (root, state) => {
    const byId = state.byId || new Map(state.nodes.map(node => [node.id, node]));
    state.nodes.forEach(setNodePosition);
    updateEdges(root, state.edges);
    updateClusters(state.clusters, byId);
    indexHitTesting(root, state);
    drawCanvas(root, state);
    if (typeof updateOverview === 'function') updateOverview(root, state);
  };
  const applyLod = (root) => {
    const nodes = Number(attr(root, 'data-cfx-graph-node-count'));
    const edges = Number(attr(root, 'data-cfx-graph-edge-count'));
    const compact = nodes >= num(root, 'data-cfx-lod-compact-node-threshold', 500);
    const hideEdgeLabels = edges >= num(root, 'data-cfx-lod-hide-edge-labels-threshold', 250);
    const preferCanvas = nodes >= num(root, 'data-cfx-lod-canvas-threshold', 1200) || (edges > num(root, 'data-cfx-performance-max-svg-edges', 3000) && edges <= num(root, 'data-cfx-performance-max-canvas-edges', 12000));
    const configured = attr(root, 'data-cfx-graph-renderer');
    const normalizedRenderer = configured === 'webgl' ? 'canvas' : configured;
    const useCanvas = normalizedRenderer === 'canvas' || (normalizedRenderer === 'svg' && preferCanvas && attr(root, 'data-cfx-graph-canvas-fallback') !== 'false');
    root.classList.toggle('cfx-graph-lod-compact', compact);
    root.classList.toggle('cfx-graph-lod-hide-edge-labels', hideEdgeLabels);
    root.classList.toggle('cfx-graph-render-canvas', useCanvas);
    root.classList.toggle('cfx-graph-render-svg', !useCanvas);
    root.dataset.cfxGraphLod = preferCanvas ? 'canvas-preferred' : compact ? 'compact' : hideEdgeLabels ? 'edge-labels-hidden' : 'full';
    root.dataset.cfxGraphClusterLod = nodes >= num(root, 'data-cfx-lod-cluster-threshold', Number.POSITIVE_INFINITY) ? 'threshold' : 'none';
    root.dataset.cfxGraphRendererActive = useCanvas ? 'canvas' : 'svg';
    syncGraphItemTabStops(root);
    emit(root, 'cfxgraphlod', { graphId: attr(root, 'data-cfx-graph-id'), mode: root.dataset.cfxGraphLod, renderer: root.dataset.cfxGraphRendererActive, nodes, edges });
  };
  const applyClusterState = (root, collapsed) => {
    const state = graphState(root);
    const hiddenNodeIds = new Set();
    state.clusters.forEach(cluster => {
      const isCollapsed = collapsed === undefined ? cluster.collapsed : collapsed;
      cluster.collapsed = isCollapsed;
      cluster.el.classList.toggle('cfx-graph-cluster-expanded', !isCollapsed);
      cluster.el.setAttribute('data-cluster-collapsed', isCollapsed ? 'true' : 'false');
      cluster.el.setAttribute('tabindex', isCollapsed ? '0' : '-1');
      cluster.el.setAttribute('aria-hidden', isCollapsed ? 'false' : 'true');
      cluster.nodeIds.forEach(id => { if (isCollapsed) hiddenNodeIds.add(id); });
    });
    state.nodes.forEach(node => node.el.classList.toggle('cfx-graph-cluster-collapsed-member', hiddenNodeIds.has(node.id)));
    const collapsedEdgeIds = new Set();
    state.edges.forEach(edge => {
      const sourceHidden = hiddenNodeIds.has(edge.source.id);
      const targetHidden = hiddenNodeIds.has(edge.target.id);
      const sameCollapsedCluster = sourceHidden && targetHidden && edge.sourceCluster && edge.sourceCluster === edge.targetCluster;
      edge.el.classList.toggle('cfx-graph-cluster-collapsed-member', sameCollapsedCluster);
      if (sameCollapsedCluster) collapsedEdgeIds.add(attr(edge.el, 'data-edge-id'));
    });
    items(root, '[data-cfx-role="graph-edge-label"]').forEach(label => label.classList.toggle('cfx-graph-cluster-collapsed-member', collapsedEdgeIds.has(attr(label, 'data-edge-label-for'))));
    root.dataset.cfxGraphClusters = hiddenNodeIds.size ? 'collapsed' : 'expanded';
    syncGraphItemTabStops(root); clearHiddenSelections(root); if (typeof syncClusterControls === 'function') syncClusterControls(root);
    emit(root, 'cfxgraphcluster', { graphId: attr(root, 'data-cfx-graph-id'), collapsed: hiddenNodeIds.size > 0, hiddenNodeCount: hiddenNodeIds.size });
    applyFilters(root); updateEdges(root, state.edges); indexHitTesting(root, state); drawCanvas(root, state); if (typeof updateOverview === 'function') updateOverview(root, state);
  };
  const publishPerformance = (root, detail) => {
    if (!hasFeature(root, 'PerformanceTelemetry')) return;
    const summary = root.__cfxGraphPerformanceSummary || {
      samples: 0,
      budgetMisses: 0,
      maxSampleMs: 0,
      maxVelocity: 0,
      overlapPressureEvents: 0,
      communityPackingEvents: 0,
      lastTick: 0,
      frameBudget: num(root, 'data-cfx-performance-frame-budget', 16)
    };
    const sampleMs = Number.isFinite(detail.sampleMs) ? detail.sampleMs : 0;
    const sampleTicks = Math.max(1, Number(detail.sampleTicks) || 1);
    const sampleBudgetMs = summary.frameBudget * sampleTicks;
    summary.samples += detail.mode === 'physics' ? 1 : 0;
    summary.lastTick = Number.isFinite(detail.tick) ? detail.tick : summary.lastTick;
    summary.maxVelocity = Math.max(summary.maxVelocity, Number.isFinite(detail.maxVelocity) ? detail.maxVelocity : 0);
    summary.overlapPressureEvents += Math.max(0, Number.isFinite(detail.overlaps) ? detail.overlaps : 0);
    summary.communityPackingEvents += Math.max(0, Number.isFinite(detail.communityPushes) ? detail.communityPushes : 0);
    summary.maxSampleMs = Math.max(summary.maxSampleMs, sampleMs);
    summary.lastSampleMs = sampleMs;
    summary.lastSampleTicks = sampleTicks;
    summary.lastSampleBudgetMs = sampleBudgetMs;
    summary.thread = detail.thread || summary.thread || '';
    summary.acceleration = detail.acceleration || summary.acceleration || '';
    summary.renderer = root.dataset.cfxGraphRendererActive || attr(root, 'data-cfx-graph-renderer');
    summary.budgetMisses += sampleMs > sampleBudgetMs ? 1 : 0;
    root.__cfxGraphPerformanceSummary = summary;
    root.dataset.cfxGraphPerformanceSamples = String(summary.samples);
    root.dataset.cfxGraphPerformanceLastTick = String(summary.lastTick);
    root.dataset.cfxGraphPerformanceLastSampleMs = sampleMs.toFixed(3);
    root.dataset.cfxGraphPerformanceMaxSampleMs = summary.maxSampleMs.toFixed(3);
    root.dataset.cfxGraphPerformanceOverlapPressureEvents = String(summary.overlapPressureEvents);
    root.dataset.cfxGraphPerformanceCommunityPackingEvents = String(summary.communityPackingEvents);
    root.dataset.cfxGraphPerformanceSampleTicks = String(sampleTicks);
    root.dataset.cfxGraphPerformanceSampleBudgetMs = sampleBudgetMs.toFixed(3);
    root.dataset.cfxGraphPerformanceBudgetMisses = String(summary.budgetMisses);
    root.dataset.cfxGraphPerformanceThread = summary.thread;
    root.dataset.cfxGraphPerformanceAcceleration = summary.acceleration;
    root.dataset.cfxGraphPerformanceBudget = summary.budgetMisses ? 'over-budget' : 'within-budget';
    emit(root, 'cfxgraphperformance', { ...detail, summary: { ...summary } });
  };
  const performanceGate = (root) => {
    const nodeCount = Number(attr(root, 'data-cfx-graph-node-count'));
    const edgeCount = Number(attr(root, 'data-cfx-graph-edge-count'));
    const canvas = root.dataset.cfxGraphRendererActive === 'canvas';
    const nodeLimit = canvas ? num(root, 'data-cfx-performance-max-canvas-nodes', 5000) : num(root, 'data-cfx-performance-max-svg-nodes', 1200);
    const edgeLimit = canvas ? num(root, 'data-cfx-performance-max-canvas-edges', 12000) : num(root, 'data-cfx-performance-max-svg-edges', 3000);
    const gated = nodeCount > nodeLimit || edgeCount > edgeLimit;
    root.classList.toggle('cfx-graph-performance-gated', gated);
    root.dataset.cfxGraphPerformance = gated ? 'gated' : 'interactive';
    if (gated) publishPerformance(root, { graphId: attr(root, 'data-cfx-graph-id'), mode: 'gated', renderer: root.dataset.cfxGraphRendererActive, nodeCount, edgeCount, nodeLimit, edgeLimit });
    return gated;
  };
  const metadataDetail = (node) => {
    const raw = attr(node, 'data-cfx-metadata');
    if (!raw) return {};
    try {
      const parsed = JSON.parse(raw);
      return parsed && typeof parsed === 'object' && !Array.isArray(parsed) ? parsed : {};
    } catch {
      return {};
    }
  };
  const selectionDetail = (root, node) => ({
      graphId: attr(root, 'data-cfx-graph-id'),
      id: attr(node, 'data-node-id') || attr(node, 'data-edge-id') || attr(node, 'data-cluster-id'),
      label: attr(node, 'data-node-label') || attr(node, 'data-edge-label') || attr(node, 'data-cluster-label'),
      kind: attr(node, 'data-node-kind') || attr(node, 'data-edge-kind') || attr(node, 'data-cluster-kind'),
      status: attr(node, 'data-cfx-status'),
      role: attr(node, 'data-cfx-role'),
      metadata: metadataDetail(node)
    });
  const selectedItems = (root) => items(root, '.cfx-graph-selected').map(node => selectionDetail(root, node));
  const updateSelectionState = (root) => {
    const details = selectedItems(root);
    const primaryNode = details.find(item => item.role === 'graph-node');
    root.dataset.cfxGraphSelectionCount = String(details.length);
    root.dataset.cfxGraphSelectionIds = details.map(item => item.id).join(',');
    root.dataset.cfxGraphSelectionRoles = details.map(item => item.role).join(',');
    root.dataset.cfxGraphSelectionPrimary = primaryNode ? primaryNode.id : details[0]?.id || '';
    emit(root, 'cfxgraphselection', { graphId: attr(root, 'data-cfx-graph-id'), count: details.length, items: details });
    return details;
  };
  const clearSelection = (root) => {
    items(root, '.cfx-graph-selected').forEach(item => item.classList.remove('cfx-graph-selected'));
    updateSelectionState(root);
    const tip = root.querySelector('.cfx-graph-tooltip');
    if (tip) tip.hidden = true;
    if (root.dataset.cfxGraphFocus === 'active') clearNeighborhoodFocus(root);
    else {
      const state = graphState(root);
      drawCanvas(root, state);
      if (typeof updateOverview === 'function') updateOverview(root, state);
    }
  };
  const select = (root, node, options) => {
    if (!hasFeature(root, 'Selection')) return;
    const additive = hasFeature(root, 'MultiSelection') && !!options?.additive;
    const toggle = additive && !!options?.toggle;
    const selected = node.classList.contains('cfx-graph-selected');
    if (!additive) items(root, '.cfx-graph-selected').forEach(item => item.classList.remove('cfx-graph-selected'));
    if (toggle && selected) node.classList.remove('cfx-graph-selected');
    else node.classList.add('cfx-graph-selected');
    const detail = selectionDetail(root, node);
    const details = updateSelectionState(root);
    syncSelectionTooltip(root, details, detail);
    if (hasFeature(root, 'NeighborhoodFocus') && root.dataset.cfxGraphFocus === 'active') {
      const primary = details.find(item => item.role === 'graph-node');
      if (primary) applyNeighborhoodFocus(root, primary.id);
      else clearNeighborhoodFocus(root);
    }
    const state = graphState(root);
    drawCanvas(root, state);
    if (typeof updateOverview === 'function') updateOverview(root, state);
    emit(root, 'cfxgraphselect', { ...detail, selected: node.classList.contains('cfx-graph-selected'), selectionCount: details.length });
  };
  const selectedGraphNodeId = (root) => {
    const selected = selectedItems(root).find(item => item.role === 'graph-node');
    return selected ? selected.id : '';
  };
  const clearNeighborhoodFocus = (root) => {
    root.classList.remove('cfx-graph-neighborhood-active');
    root.dataset.cfxGraphFocus = 'none';
    root.dataset.cfxGraphFocusNode = '';
    items(root, '.cfx-graph-neighborhood-dim,.cfx-graph-neighborhood-related,.cfx-graph-neighborhood-primary').forEach(item => {
      item.classList.remove('cfx-graph-neighborhood-dim', 'cfx-graph-neighborhood-related', 'cfx-graph-neighborhood-primary');
    });
    const state = graphState(root);
    drawCanvas(root, state);
    if (typeof updateOverview === 'function') updateOverview(root, state);
    if (typeof syncFocusControls === 'function') syncFocusControls(root);
    emit(root, 'cfxgraphfocus', { graphId: attr(root, 'data-cfx-graph-id'), active: false, nodeId: '', neighborNodeCount: 0, edgeCount: 0 });
  };
  const applyNeighborhoodFocus = (root, nodeId) => {
    const state = graphState(root);
    const relatedNodes = new Set([nodeId]);
    const relatedEdges = new Set();
    state.edges.forEach(edge => {
      if (edge.source.id !== nodeId && edge.target.id !== nodeId) return;
      relatedNodes.add(edge.source.id);
      relatedNodes.add(edge.target.id);
      relatedEdges.add(attr(edge.el, 'data-edge-id'));
    });
    state.nodes.forEach(node => {
      const related = relatedNodes.has(node.id);
      node.el.classList.toggle('cfx-graph-neighborhood-primary', node.id === nodeId);
      node.el.classList.toggle('cfx-graph-neighborhood-related', related && node.id !== nodeId);
      node.el.classList.toggle('cfx-graph-neighborhood-dim', !related);
    });
    state.edges.forEach(edge => {
      const related = relatedEdges.has(attr(edge.el, 'data-edge-id'));
      edge.el.classList.toggle('cfx-graph-neighborhood-related', related);
      edge.el.classList.toggle('cfx-graph-neighborhood-dim', !related);
    });
    items(root, '[data-cfx-role="graph-edge-label"]').forEach(label => {
      const related = relatedEdges.has(attr(label, 'data-edge-label-for'));
      label.classList.toggle('cfx-graph-neighborhood-related', related);
      label.classList.toggle('cfx-graph-neighborhood-dim', !related);
    });
    state.clusters.forEach(cluster => {
      const related = cluster.nodeIds.some(id => relatedNodes.has(id));
      cluster.el.classList.toggle('cfx-graph-neighborhood-related', related);
      cluster.el.classList.toggle('cfx-graph-neighborhood-dim', !related);
    });
    root.classList.add('cfx-graph-neighborhood-active');
    root.dataset.cfxGraphFocus = 'active';
    root.dataset.cfxGraphFocusNode = nodeId;
    drawCanvas(root, state);
    if (typeof updateOverview === 'function') updateOverview(root, state);
    if (typeof syncFocusControls === 'function') syncFocusControls(root);
    emit(root, 'cfxgraphfocus', { graphId: attr(root, 'data-cfx-graph-id'), active: true, nodeId, neighborNodeCount: relatedNodes.size - 1, edgeCount: relatedEdges.size });
  };
  const toggleNeighborhoodFocus = (root) => {
    if (!hasFeature(root, 'NeighborhoodFocus')) return;
    const nodeId = selectedGraphNodeId(root);
    if (!nodeId || root.dataset.cfxGraphFocusNode === nodeId) {
      clearNeighborhoodFocus(root);
      return;
    }
    applyNeighborhoodFocus(root, nodeId);
  };
  const applyFilters = (root) => {
    const query = (root.querySelector('[data-cfx-graph-search]')?.value || '').trim().toLowerCase();
    const filters = {};
    items(root, '[data-cfx-graph-filter]').forEach(filter => { filters[attr(filter, 'data-cfx-graph-filter')] = filter.value || ''; });
    const visibleNodes = new Set();
    const hiddenMemberHits = new Set();
    const nodeMatches = new Map();
    items(root, '[data-cfx-role="graph-node"]').forEach(node => {
      const queryOk = !query || searchable(node).includes(query);
      const statusOk = !filters.status || attr(node, 'data-cfx-status') === filters.status;
      const kindOk = !filters.kind || attr(node, 'data-node-kind') === filters.kind;
      const id = attr(node, 'data-node-id');
      const collapsedMember = node.classList.contains('cfx-graph-cluster-collapsed-member');
      const matches = queryOk && statusOk && kindOk && !collapsedMember;
      if (queryOk && statusOk && kindOk && collapsedMember) hiddenMemberHits.add(id);
      nodeMatches.set(id, { node, matches });
      if (matches) visibleNodes.add(id);
    });
    const edgeMatches = items(root, '[data-cfx-role="graph-edge"]').map(edge => {
      const edgeQueryOk = !query || searchable(edge).includes(query);
      const edgeStatusOk = !filters.status || attr(edge, 'data-cfx-status') === filters.status;
      const edgeKindOk = !filters.kind || attr(edge, 'data-edge-kind') === filters.kind;
      const collapsedMember = edge.classList.contains('cfx-graph-cluster-collapsed-member');
      const edgeMatchesFacet = edgeQueryOk && edgeStatusOk && edgeKindOk;
      const matches = edgeMatchesFacet && !collapsedMember;
      if (edgeMatchesFacet && collapsedMember) {
        hiddenMemberHits.add(attr(edge, 'data-source-node-id'));
        hiddenMemberHits.add(attr(edge, 'data-target-node-id'));
      }
      if (matches) {
        visibleNodes.add(attr(edge, 'data-source-node-id'));
        visibleNodes.add(attr(edge, 'data-target-node-id'));
      }
      return { edge, matches };
    });
    items(root, '[data-cfx-role="graph-cluster"]').forEach(cluster => {
      const queryOk = !query || searchable(cluster).includes(query);
      const statusOk = !filters.status || attr(cluster, 'data-cfx-status') === filters.status;
      const kindOk = !filters.kind || attr(cluster, 'data-cluster-kind') === filters.kind;
      const expanded = attr(cluster, 'data-cluster-collapsed') !== 'true';
      if (expanded && queryOk && statusOk && kindOk) idList(attr(cluster, 'data-cluster-node-ids')).forEach(id => visibleNodes.add(id));
    });
    nodeMatches.forEach((entry, id) => {
      const visible = visibleNodes.has(id);
      entry.node.classList.toggle('cfx-graph-hidden', !visible);
    });
    const labels = new Map(items(root, '[data-cfx-role="graph-edge-label"]').map(label => [attr(label, 'data-edge-label-for'), label]));
    edgeMatches.forEach(({ edge, matches }) => {
      const endpointsVisible = visibleNodes.has(attr(edge, 'data-source-node-id')) && visibleNodes.has(attr(edge, 'data-target-node-id'));
      const edgeVisible = matches && endpointsVisible;
      edge.classList.toggle('cfx-graph-hidden', !edgeVisible);
      const label = labels.get(attr(edge, 'data-edge-id'));
      if (label) label.classList.toggle('cfx-graph-hidden', !edgeVisible);
    });
    items(root, '[data-cfx-role="graph-cluster"]').forEach(cluster => {
      const queryOk = !query || searchable(cluster).includes(query);
      const statusOk = !filters.status || attr(cluster, 'data-cfx-status') === filters.status;
      const kindOk = !filters.kind || attr(cluster, 'data-cluster-kind') === filters.kind;
      const memberIds = idList(attr(cluster, 'data-cluster-node-ids'));
      const memberVisible = memberIds.some(id => visibleNodes.has(id));
      const hiddenMemberHit = memberIds.some(id => hiddenMemberHits.has(id));
      cluster.classList.toggle('cfx-graph-hidden', !(queryOk && statusOk && kindOk) && !memberVisible && !hiddenMemberHit);
    });
    clearHiddenSelections(root);
    const state = graphState(root);
    drawCanvas(root, state);
    if (typeof updateOverview === 'function') updateOverview(root, state);
    emit(root, 'cfxgraphfilter', { graphId: attr(root, 'data-cfx-graph-id'), query, filters, visibleNodeCount: visibleNodes.size });
  };

