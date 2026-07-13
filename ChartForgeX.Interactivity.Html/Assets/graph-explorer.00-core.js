  const roots = () => Array.from(document.querySelectorAll('.cfx-graph-explorer:not([data-cfx-graph-bound])'));
  const attr = (node, name) => node ? node.getAttribute(name) || '' : '';
  const num = (node, name, fallback) => {
    const raw = attr(node, name);
    if (raw === '') return fallback;
    const value = Number(raw);
    return Number.isFinite(value) ? value : fallback;
  };
  const items = (root, selector) => {
    const physical = Array.from(root.querySelectorAll(selector)).filter(item => attr(item, 'data-cfx-runtime-overlay') !== 'true');
    const virtual = (root.__cfxGraphVirtualItems || []).filter(item => !item.__cfxRemoved && graphVirtualMatches(item, selector));
    return physical.concat(virtual);
  };
  const emit = (root, name, detail, options) => root.dispatchEvent(new CustomEvent(name, { bubbles: true, cancelable: !!options?.cancelable, detail }));
  const featureGroups = {
    Explorer: ['Selection', 'MultiSelection', 'Search', 'Filtering', 'Viewport', 'NeighborhoodFocus', 'Clustering', 'LevelOfDetail']
  };
  const features = (root) => {
    const names = new Set();
    (attr(root, 'data-cfx-graph-features') || '').split(',').map(item => item.trim()).filter(Boolean).forEach(name => {
      names.add(name);
      (featureGroups[name] || []).forEach(groupFeature => names.add(groupFeature));
    });
    return names;
  };
  const hasFeature = (root, feature) => features(root).has(feature);
  const idList = (value) => value.split(',').map(item => item.trim()).filter(Boolean);
  const dashPattern = (value, fallback) => { const parts = (value || '').split(/[,\s]+/).map(Number).filter(item => Number.isFinite(item) && item > 0); return parts.length ? parts : fallback; };
  const routePoints = (value) => (value || '').split(';').map(item => item.split(',').map(Number)).filter(pair => pair.length === 2 && pair.every(Number.isFinite)).map(pair => ({ x: pair[0], y: pair[1] }));
  const sceneSize = (root) => {
    const svg = root.querySelector('[data-cfx-role="graph-scene"]');
    const parts = attr(svg, 'viewBox').split(/\s+/).map(Number);
    const width = Number.isFinite(parts[2]) && parts[2] > 0 ? parts[2] : 960, height = Number.isFinite(parts[3]) && parts[3] > 0 ? parts[3] : 560;
    return { width, height, centerX: width / 2, centerY: height / 2 };
  };
  const searchable = (node) => [
    attr(node, 'data-node-id'),
    attr(node, 'data-node-label'),
    attr(node, 'data-node-kind'),
    attr(node, 'data-node-group'),
    attr(node, 'data-node-cluster'),
    attr(node, 'data-node-icon'),
    attr(node, 'data-edge-id'),
    attr(node, 'data-edge-label'),
    attr(node, 'data-edge-kind'),
    attr(node, 'data-cluster-id'),
    attr(node, 'data-cluster-label'),
    attr(node, 'data-cluster-kind'),
    attr(node, 'data-cfx-search'),
    attr(node, 'data-cfx-status')
  ].join(' ').toLowerCase();
  const graphState = (root) => {
    ensureGraphDocument(root);
    const detailGroups = new Map(items(root, '[data-cfx-role="graph-node-details"]').map(el => [attr(el, 'data-node-details-for'), el]));
    const nodes = items(root, '[data-cfx-role="graph-node"]').map((el, index) => ({
      el,
      detailsEl: detailGroups.get(attr(el, 'data-node-id')) || null,
      id: attr(el, 'data-node-id'),
      label: attr(el, 'data-node-label'),
      secondaryLabel: attr(el, 'data-node-secondary-label'),
      badge: attr(el, 'data-node-badge'),
      parentId: attr(el, 'data-node-parent'),
      cluster: attr(el, 'data-node-cluster'),
      groupId: attr(el, 'data-node-group'),
      kind: attr(el, 'data-node-kind'),
      shape: attr(el, 'data-node-shape') || 'circle',
      imageUrl: attr(el, 'data-node-image-url'),
      icon: attr(el, 'data-node-icon'),
      backgroundColor: attr(el, 'data-node-background-color'),
      borderColor: attr(el, 'data-node-border-color'),
      labelColor: attr(el, 'data-node-label-color'),
      labelBackgroundColor: attr(el, 'data-node-label-background-color'),
      shadow: attr(el, 'data-node-shadow') === 'true',
      size: Math.max(4, num(el, 'data-node-size', 8)),
      level: attr(el, 'data-node-level') === '' ? null : num(el, 'data-node-level', 0),
      degree: 0,
      fixed: attr(el, 'data-node-fixed') === 'true',
      x: num(el, 'data-node-x', 160 + index * 22),
      y: num(el, 'data-node-y', 160 + index * 17),
      homeX: num(el, 'data-node-x', 160 + index * 22),
      homeY: num(el, 'data-node-y', 160 + index * 17),
      vx: 0,
      vy: 0
    }));
    const byId = new Map(nodes.map(node => [node.id, node]));
    const clusters = items(root, '[data-cfx-role="graph-cluster"]').map(el => ({
      el,
      id: attr(el, 'data-cluster-id'),
      label: attr(el, 'data-cluster-label'),
      parentId: attr(el, 'data-cluster-parent'),
      nodeIds: idList(attr(el, 'data-cluster-node-ids')),
      collapsed: attr(el, 'data-cluster-collapsed') === 'true'
    }));
    const clusterById = new Map(clusters.map(cluster => [cluster.id, cluster]));
    const edges = items(root, '[data-cfx-role="graph-edge"]').map(el => {
      const source = byId.get(attr(el, 'data-source-node-id'));
      const target = byId.get(attr(el, 'data-target-node-id'));
      return {
        el,
        id: attr(el, 'data-edge-id'),
        source,
        target,
        sourceCluster: clusterById.get(attr(el, 'data-source-cluster-id') || source?.cluster || ''),
        targetCluster: clusterById.get(attr(el, 'data-target-cluster-id') || target?.cluster || ''),
        weight: Math.max(0.1, num(el, 'data-edge-weight', 1)),
        length: Math.max(0, num(el, 'data-edge-length', 0)),
        label: attr(el, 'data-edge-label'),
        directed: attr(el, 'data-edge-directed') === 'true',
        sourceArrow: attr(el, 'data-edge-source-arrow') === 'true',
        targetArrow: attr(el, 'data-edge-target-arrow') === 'true',
        shape: attr(el, 'data-edge-shape') || 'line',
        routePoints: routePoints(attr(el, 'data-edge-route-points')),
        curvature: num(el, 'data-edge-curvature', 0),
        dashed: attr(el, 'data-edge-dashed') === 'true',
        dashPattern: dashPattern(attr(el, 'data-edge-dash-pattern'), [8, 6]),
        showLabel: attr(el, 'data-edge-show-label') !== 'false',
        strokeColor: attr(el, 'data-edge-color'),
        labelColor: attr(el, 'data-edge-label-color'),
        strokeWidth: num(el, 'data-edge-width', 0),
        physics: attr(el, 'data-edge-physics') !== 'false'
      };
    }).filter(edge => edge.source && edge.target);
    edges.filter(edge => edge.physics !== false).forEach(edge => {
      edge.source.degree += 1;
      edge.target.degree += 1;
    });
    return { nodes, edges, clusters, byId, clusterById };
  };
  const visible = (el) => !el.classList.contains('cfx-graph-hidden') && !el.classList.contains('cfx-graph-cluster-collapsed-member') && !el.classList.contains('cfx-graph-hierarchy-hidden');
  const viewport = (root) => ({
    x: num(root, 'data-cfx-viewport-x', 0),
    y: num(root, 'data-cfx-viewport-y', 0),
    scale: Math.min(4, Math.max(0.2, num(root, 'data-cfx-viewport-scale', 1)))
  });
  const setViewport = (root, next) => {
    const state = {
      x: Number.isFinite(next.x) ? next.x : 0,
      y: Number.isFinite(next.y) ? next.y : 0,
      scale: Math.min(4, Math.max(0.2, Number.isFinite(next.scale) ? next.scale : 1))
    };
    root.setAttribute('data-cfx-viewport-x', state.x.toFixed(3));
    root.setAttribute('data-cfx-viewport-y', state.y.toFixed(3));
    root.setAttribute('data-cfx-viewport-scale', state.scale.toFixed(3));
    const group = root.querySelector('[data-cfx-role="graph-viewport"]');
    if (group) group.setAttribute('transform', `translate(${state.x.toFixed(3)} ${state.y.toFixed(3)}) scale(${state.scale.toFixed(3)})`);
    const renderer = root.dataset.cfxGraphRendererActive || attr(root, 'data-cfx-graph-renderer');
    const currentState = root.__cfxGraphState || (renderer === 'canvas' || renderer === 'webgl' || typeof updateOverview === 'function' ? graphState(root) : null);
    if (typeof applySemanticZoom === 'function') applySemanticZoom(root, state.scale);
    if ((renderer === 'canvas' || renderer === 'webgl') && currentState) drawCanvas(root, currentState);
    if (typeof updateOverview === 'function') updateOverview(root, currentState);
    emit(root, 'cfxgraphviewport', { graphId: attr(root, 'data-cfx-graph-id'), ...state });
  };
  const zoomViewport = (root, factor, anchor) => {
    const current = viewport(root);
    const scale = Math.min(4, Math.max(0.2, current.scale * factor));
    const size = sceneSize(root);
    const point = anchor || { x: size.centerX, y: size.centerY };
    setViewport(root, {
      scale,
      x: point.x - (point.x - current.x) * scale / current.scale,
      y: point.y - (point.y - current.y) * scale / current.scale
    });
  };
  const scenePoint = (root, event) => {
    const stage = root.querySelector('.cfx-graph-stage');
    const rect = (stage || root).getBoundingClientRect();
    const size = sceneSize(root);
    const renderer = root.dataset.cfxGraphRendererActive || attr(root, 'data-cfx-graph-renderer');
    const uniformScale = renderer === 'svg' ? Math.min(rect.width / size.width, rect.height / size.height) : 0;
    const contentWidth = uniformScale > 0 ? size.width * uniformScale : rect.width;
    const contentHeight = uniformScale > 0 ? size.height * uniformScale : rect.height;
    const offsetX = (rect.width - contentWidth) / 2;
    const offsetY = (rect.height - contentHeight) / 2;
    const sx = (event.clientX - rect.left - offsetX) * size.width / Math.max(1, contentWidth);
    const sy = (event.clientY - rect.top - offsetY) * size.height / Math.max(1, contentHeight);
    const current = viewport(root);
    return { x: (sx - current.x) / current.scale, y: (sy - current.y) / current.scale, screenX: sx, screenY: sy };
  };
  const canvasContext = (root) => {
    const canvas = root.querySelector('[data-cfx-role="graph-canvas"]');
    if (!canvas) return null;
    const rect = canvas.getBoundingClientRect();
    const ratio = Math.max(1, window.devicePixelRatio || 1);
    const size = sceneSize(root);
    const hasLayoutBox = rect.width > 0 && rect.height > 0;
    const width = Math.max(1, Math.round(hasLayoutBox ? rect.width : size.width));
    const height = Math.max(1, Math.round(hasLayoutBox ? rect.height : size.height));
    if (canvas.width !== Math.round(width * ratio) || canvas.height !== Math.round(height * ratio)) {
      canvas.width = Math.round(width * ratio);
      canvas.height = Math.round(height * ratio);
    }
    const context = canvas.getContext('2d');
    if (!context) return null;
    context.setTransform(ratio * width / size.width, 0, 0, ratio * height / size.height, 0, 0);
    return { canvas, context };
  };
  const drawCanvas = (root, state, options) => {
    if (typeof syncNodeDetailLayers === 'function') syncNodeDetailLayers(state);
    if ((root.dataset.cfxGraphRendererActive || attr(root, 'data-cfx-graph-renderer')) === 'svg' && attr(root, 'data-cfx-graph-accelerated-markup') === 'true' && !options?.force) {
      if (typeof drawAcceleratedSvgRuntime === 'function' && drawAcceleratedSvgRuntime(root, state)) return;
    }
    if ((root.dataset.cfxGraphRendererActive || attr(root, 'data-cfx-graph-renderer')) === 'webgl' && !options?.force) {
      if (typeof drawWebGl === 'function' && drawWebGl(root, state)) return;
    }
    if (!root.classList.contains('cfx-graph-render-canvas') && !options?.force) return;
    const surface = canvasContext(root);
    if (!surface) return;
    const { context } = surface;
    const size = sceneSize(root);
    const palette = graphThemePalette(root);
    context.clearRect(0, 0, size.width, size.height);
    context.fillStyle = palette.paper;
    context.fillRect(0, 0, size.width, size.height);
    const view = viewport(root);
    const byId = state.byId || new Map(state.nodes.map(node => [node.id, node]));
    context.save();
    context.transform(view.scale, 0, 0, view.scale, view.x, view.y);
    context.lineCap = 'round';
    const compact = root.classList.contains('cfx-graph-lod-compact') || root.classList.contains('cfx-graph-semantic-overview');
    const dense = compact || state.edges.length > 250;
    const moving = root.dataset.cfxGraphPhysicsState === 'running' && !options?.force;
    state.clusters.forEach(cluster => {
      if (!visible(cluster.el)) return;
      const metrics = clusterMetrics(cluster, byId);
      if (!metrics) return;
      const label = attr(cluster.el, 'data-cluster-label') || cluster.id;
      const selected = cluster.el.classList.contains('cfx-graph-selected');
      context.beginPath();
      context.arc(metrics.x, metrics.y, metrics.radius, 0, Math.PI * 2);
      context.globalAlpha = metrics.expanded ? .1 : .86;
      context.fillStyle = metrics.expanded ? 'rgba(224,242,254,0)' : palette.clusterFill;
      context.strokeStyle = cluster.el.classList.contains('cfx-graph-selected') ? palette.selected : palette.clusterStroke;
      context.lineWidth = selected ? 4 : metrics.expanded ? 1.2 : 2;
      context.setLineDash([6, 4]);
      if (!metrics.expanded) context.fill();
      context.stroke();
      context.setLineDash([]);
      if ((!moving && !metrics.expanded) || selected) {
        context.globalAlpha = metrics.expanded ? .55 : 1;
        context.font = '700 12px Segoe UI, Arial, sans-serif';
        context.textAlign = 'center';
        context.textBaseline = 'middle';
        context.lineWidth = 4;
        context.strokeStyle = palette.halo;
        context.fillStyle = palette.clusterText;
        context.strokeText(label, metrics.x, metrics.y);
        context.fillText(label, metrics.x, metrics.y);
      }
      context.globalAlpha = 1;
    });
    state.edges.forEach(edge => {
      if (!visible(edge.el) || !edgeHasVisibleEndpoints(edge, byId)) return;
      const rendered = visualEdge(edge, byId);
      const control = edgeControl(rendered);
      const dimmed = edge.el.classList.contains('cfx-graph-neighborhood-dim');
      const related = edge.el.classList.contains('cfx-graph-neighborhood-related');
      const selected = edge.el.classList.contains('cfx-graph-selected');
      edgeDrawPath(context, rendered, control);
      const edgeColor = selected ? palette.selected : related ? '#14b8a6' : edge.strokeColor || palette.edge;
      context.strokeStyle = edgeColor;
      context.globalAlpha = dimmed ? .1 : selected ? .95 : related ? .86 : dense ? .28 : .58;
      const baseWidth = dense ? Math.max(.65, Math.min(1.8, edge.weight * .55)) : edge.weight;
      const styledWidth = edge.strokeWidth > 0 ? edge.strokeWidth : baseWidth;
      context.lineWidth = edge.strokeWidth > 0
        ? Math.max(.65, edge.strokeWidth + (selected ? 1.6 : related ? 1.2 : 0))
        : Math.max(.65, Math.min(selected ? 6 : related ? 4 : dense ? 1.8 : 4, styledWidth + (selected ? 1.6 : related ? 1.2 : 0)));
      context.setLineDash(edge.dashed ? edge.dashPattern : []);
      context.stroke();
      context.setLineDash([]);
      context.globalAlpha = 1;
      if ((!moving || selected || related) && (edge.sourceArrow || edge.targetArrow || edge.directed)) {
        context.globalAlpha = dimmed ? .14 : selected || related ? 1 : dense ? .34 : 1;
        if (edge.sourceArrow) drawArrow(context, rendered, control, 'source', edgeColor);
        if (edge.targetArrow || edge.directed) drawArrow(context, rendered, control, 'target', edgeColor);
        context.globalAlpha = 1;
      }
      if ((!moving || selected || related) && edge.label && edge.showLabel && (!root.classList.contains('cfx-graph-lod-hide-edge-labels') || selected || related)) {
        const label = edgeLabelPoint(rendered, control);
        context.font = '11px Segoe UI, Arial, sans-serif';
        context.textAlign = 'center';
        context.textBaseline = 'middle';
        context.lineWidth = 4;
        context.strokeStyle = palette.halo;
        context.fillStyle = graphAdaptiveTextColor(root, edge.labelColor, palette.edgeLabel);
        context.globalAlpha = dimmed ? .16 : 1;
        context.strokeText(edge.label, label.x, label.y);
        context.fillText(edge.label, label.x, label.y);
        context.globalAlpha = 1;
      }
    });
    drawCanvasNodes(context, root, state.nodes, compact, moving);
    context.restore();
  };
