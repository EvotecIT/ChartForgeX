  const roots = () => Array.from(document.querySelectorAll('.cfx-graph-explorer:not([data-cfx-graph-bound])'));
  const attr = (node, name) => node ? node.getAttribute(name) || '' : '';
  const num = (node, name, fallback) => {
    const raw = attr(node, name);
    if (raw === '') return fallback;
    const value = Number(raw);
    return Number.isFinite(value) ? value : fallback;
  };
  const items = (root, selector) => Array.from(root.querySelectorAll(selector));
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
  const profile = (root) => {
    const solver = attr(root, 'data-cfx-graph-physics');
    const base = {
      solver,
      iterations: Math.max(1, num(root, 'data-cfx-graph-stabilization-iterations', 400)),
      minVelocity: Math.max(0.01, num(root, 'data-cfx-graph-min-velocity', 0.1)),
      maxVelocity: Math.max(1, num(root, 'data-cfx-graph-max-velocity', 50)),
      damping: Math.min(0.98, Math.max(0.01, num(root, 'data-cfx-graph-damping', 0.09))),
      linkDistance: Math.max(10, num(root, 'data-cfx-graph-link-distance', 120)),
      repulsion: Math.max(1, num(root, 'data-cfx-graph-repulsion', 4500)),
      centerGravity: Math.max(0, num(root, 'data-cfx-graph-center-gravity', 0.01)),
      timestep: attr(root, 'data-cfx-graph-adaptive-timestep') === 'true' ? 0.8 : 1
    };
    if (solver === 'BarnesHut') return { ...base, repulsion: base.repulsion * 1.35, damping: Math.min(0.98, base.damping * 1.15), timestep: base.timestep * 0.9 };
    if (solver === 'ForceAtlas2') return { ...base, repulsion: base.repulsion * 0.9, linkDistance: base.linkDistance * 0.75, centerGravity: base.centerGravity * 1.8 };
    if (solver === 'HierarchicalRepulsion') return { ...base, repulsion: base.repulsion * 1.15, linkDistance: base.linkDistance * 1.2, centerGravity: base.centerGravity * 1.25 };
    return base;
  };
  const graphState = (root) => {
    const nodes = items(root, '[data-cfx-role="graph-node"]').map((el, index) => ({
      el,
      id: attr(el, 'data-node-id'),
      cluster: attr(el, 'data-node-cluster'),
      groupId: attr(el, 'data-node-group'),
      kind: attr(el, 'data-node-kind'),
      shape: attr(el, 'data-node-shape') || 'circle',
      imageUrl: attr(el, 'data-node-image-url'),
      icon: attr(el, 'data-node-icon'),
      size: Math.max(4, num(el, 'data-node-size', 8)),
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
    const edges = items(root, '[data-cfx-role="graph-edge"]').map(el => ({
      el,
      source: byId.get(attr(el, 'data-source-node-id')),
      target: byId.get(attr(el, 'data-target-node-id')),
      weight: Math.max(0.1, num(el, 'data-edge-weight', 1)),
      length: Math.max(0, num(el, 'data-edge-length', 0)),
      label: attr(el, 'data-edge-label'),
      directed: attr(el, 'data-edge-directed') === 'true',
      shape: attr(el, 'data-edge-shape') || 'line',
      curvature: num(el, 'data-edge-curvature', 0),
      dashed: attr(el, 'data-edge-dashed') === 'true',
      showLabel: attr(el, 'data-edge-show-label') !== 'false'
    })).filter(edge => edge.source && edge.target);
    edges.forEach(edge => {
      edge.source.degree += 1;
      edge.target.degree += 1;
    });
    const clusters = items(root, '[data-cfx-role="graph-cluster"]').map(el => ({
      el,
      id: attr(el, 'data-cluster-id'),
      nodeIds: idList(attr(el, 'data-cluster-node-ids')),
      collapsed: attr(el, 'data-cluster-collapsed') === 'true'
    }));
    return { nodes, edges, clusters, byId };
  };
  const visible = (el) => !el.classList.contains('cfx-graph-hidden') && !el.classList.contains('cfx-graph-cluster-collapsed-member');
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
    drawCanvas(root, graphState(root));
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
    const sx = (event.clientX - rect.left) * size.width / Math.max(1, rect.width);
    const sy = (event.clientY - rect.top) * size.height / Math.max(1, rect.height);
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
    if (!root.classList.contains('cfx-graph-render-canvas') && !options?.force) return;
    const surface = canvasContext(root);
    if (!surface) return;
    const { context } = surface;
    const size = sceneSize(root);
    context.clearRect(0, 0, size.width, size.height);
    context.fillStyle = '#ffffff';
    context.fillRect(0, 0, size.width, size.height);
    const view = viewport(root);
    const byId = state.byId || new Map(state.nodes.map(node => [node.id, node]));
    context.save();
    context.transform(view.scale, 0, 0, view.scale, view.x, view.y);
    context.lineCap = 'round';
    const compact = root.classList.contains('cfx-graph-lod-compact');
    const dense = compact || state.edges.length > 250;
    state.clusters.forEach(cluster => {
      if (!visible(cluster.el)) return;
      const metrics = clusterMetrics(cluster, byId);
      if (!metrics) return;
      const label = attr(cluster.el, 'data-cluster-label') || cluster.id;
      const selected = cluster.el.classList.contains('cfx-graph-selected');
      context.beginPath();
      context.arc(metrics.x, metrics.y, metrics.radius, 0, Math.PI * 2);
      context.globalAlpha = metrics.expanded ? .1 : .86;
      context.fillStyle = metrics.expanded ? 'rgba(224,242,254,0)' : 'rgba(224,242,254,.86)';
      context.strokeStyle = cluster.el.classList.contains('cfx-graph-selected') ? '#f59e0b' : '#0284c7';
      context.lineWidth = selected ? 4 : metrics.expanded ? 1.2 : 2;
      context.setLineDash([6, 4]);
      if (!metrics.expanded) context.fill();
      context.stroke();
      context.setLineDash([]);
      if (!metrics.expanded || selected) {
        context.globalAlpha = metrics.expanded ? .55 : 1;
        context.font = '700 12px Segoe UI, Arial, sans-serif';
        context.textAlign = 'center';
        context.textBaseline = 'middle';
        context.lineWidth = 4;
        context.strokeStyle = '#ffffff';
        context.fillStyle = '#075985';
        context.strokeText(label, metrics.x, metrics.y);
        context.fillText(label, metrics.x, metrics.y);
      }
      context.globalAlpha = 1;
    });
    state.edges.forEach(edge => {
      if (!visible(edge.el) || !visible(edge.source.el) || !visible(edge.target.el)) return;
      const dimmed = edge.el.classList.contains('cfx-graph-neighborhood-dim');
      const related = edge.el.classList.contains('cfx-graph-neighborhood-related');
      const selected = edge.el.classList.contains('cfx-graph-selected');
      context.beginPath();
      context.moveTo(edge.source.x, edge.source.y);
      const control = edgeControl(edge);
      if (control) context.quadraticCurveTo(control.x, control.y, edge.target.x, edge.target.y);
      else context.lineTo(edge.target.x, edge.target.y);
      context.strokeStyle = selected ? '#f59e0b' : related ? '#0f766e' : '#94a3b8';
      context.globalAlpha = dimmed ? .1 : selected ? .95 : related ? .86 : dense ? .28 : .58;
      const baseWidth = dense ? Math.max(.65, Math.min(1.8, edge.weight * .55)) : edge.weight;
      context.lineWidth = Math.max(.65, Math.min(selected ? 6 : related ? 4 : dense ? 1.8 : 4, baseWidth + (selected ? 1.6 : related ? 1.2 : 0)));
      context.setLineDash(edge.dashed ? [8, 6] : []);
      context.stroke();
      context.setLineDash([]);
      context.globalAlpha = 1;
      if (edge.directed) {
        context.globalAlpha = dimmed ? .14 : selected || related ? 1 : dense ? .34 : 1;
        drawArrow(context, edge, control);
        context.globalAlpha = 1;
      }
      if (edge.label && edge.showLabel && !root.classList.contains('cfx-graph-lod-hide-edge-labels')) {
        const label = edgeLabelPoint(edge, control);
        context.font = '11px Segoe UI, Arial, sans-serif';
        context.textAlign = 'center';
        context.textBaseline = 'middle';
        context.lineWidth = 4;
        context.strokeStyle = '#ffffff';
        context.fillStyle = '#475569';
        context.globalAlpha = dimmed ? .16 : 1;
        context.strokeText(edge.label, label.x, label.y);
        context.fillText(edge.label, label.x, label.y);
        context.globalAlpha = 1;
      }
    });
    state.nodes.forEach(node => {
      if (!visible(node.el)) return;
      const dimmed = node.el.classList.contains('cfx-graph-neighborhood-dim');
      const primary = node.el.classList.contains('cfx-graph-neighborhood-primary');
      const selected = node.el.classList.contains('cfx-graph-selected');
      context.save();
      context.globalAlpha = dimmed ? .18 : 1;
      drawNodeMark(context, node, selected, compact, root);
      if (primary) {
        context.beginPath();
        context.arc(node.x, node.y, node.size + 9, 0, Math.PI * 2);
        context.strokeStyle = '#0f766e';
        context.lineWidth = 3;
        context.stroke();
      }
      if (!compact) {
        context.font = '12px Segoe UI, Arial, sans-serif';
        context.textAlign = 'center';
        context.textBaseline = 'top';
        context.lineWidth = 4;
        context.strokeStyle = '#ffffff';
        context.fillStyle = '#334155';
        const label = attr(node.el, 'data-node-label');
        context.strokeText(label, node.x, node.y + node.size + 8);
        context.fillText(label, node.x, node.y + node.size + 8);
      }
      context.restore();
    });
    context.restore();
  };
  const edgeControl = (edge) => {
    if (edge.shape !== 'curve' && Math.abs(edge.curvature) < 0.001) return null;
    const dx = edge.target.x - edge.source.x;
    const dy = edge.target.y - edge.source.y;
    const length = Math.max(1, Math.sqrt(dx * dx + dy * dy));
    const offset = Math.abs(edge.curvature) < 0.001 ? 34 : edge.curvature;
    return { x: (edge.source.x + edge.target.x) / 2 - dy / length * offset, y: (edge.source.y + edge.target.y) / 2 + dx / length * offset };
  };
  const edgeLabelPoint = (edge, control) => control
    ? { x: (edge.source.x + 2 * control.x + edge.target.x) / 4, y: (edge.source.y + 2 * control.y + edge.target.y) / 4 - 7 }
    : { x: (edge.source.x + edge.target.x) / 2, y: (edge.source.y + edge.target.y) / 2 - 7 };
  const drawArrow = (context, edge, control) => {
    const from = control || edge.source;
    const angle = Math.atan2(edge.target.y - from.y, edge.target.x - from.x);
    const size = 8;
    const x = edge.target.x - Math.cos(angle) * (edge.target.size + 5);
    const y = edge.target.y - Math.sin(angle) * (edge.target.size + 5);
    context.beginPath();
    context.moveTo(x, y);
    context.lineTo(x - Math.cos(angle - Math.PI / 6) * size, y - Math.sin(angle - Math.PI / 6) * size);
    context.lineTo(x - Math.cos(angle + Math.PI / 6) * size, y - Math.sin(angle + Math.PI / 6) * size);
    context.closePath();
    context.fillStyle = '#64748b';
    context.fill();
  };
  const drawNodeMark = (context, node, selected, compact, root) => {
    context.fillStyle = '#2563eb';
    context.strokeStyle = selected ? '#f59e0b' : '#eff6ff';
    context.lineWidth = selected ? 5 : compact ? 1.5 : 3;
    if (node.shape === 'box') {
      const width = node.size * 2.9;
      const height = node.size * 2.1;
      context.beginPath();
      if (context.roundRect) context.roundRect(node.x - width / 2, node.y - height / 2, width, height, Math.min(8, node.size * .45));
      else context.rect(node.x - width / 2, node.y - height / 2, width, height);
      context.fill();
      context.stroke();
    } else if (node.shape === 'image' && node.imageUrl) {
      context.beginPath();
      context.arc(node.x, node.y, node.size + 3, 0, Math.PI * 2);
      context.fill();
      context.stroke();
      const image = graphImage(node.imageUrl, () => drawCanvas(root, graphState(root)));
      if (image && image.complete && image.naturalWidth > 0) {
        try {
          context.save();
          context.beginPath();
          context.arc(node.x, node.y, node.size, 0, Math.PI * 2);
          context.clip();
          context.drawImage(image, node.x - node.size, node.y - node.size, node.size * 2, node.size * 2);
          context.restore();
        } catch {
          context.restore();
          // Keep malformed host-supplied images from breaking Canvas interaction.
        }
      }
    } else {
      context.beginPath();
      context.arc(node.x, node.y, node.size, 0, Math.PI * 2);
      context.fill();
      context.stroke();
    }
    if (node.icon) {
      context.font = 'bold 12px Segoe UI, Arial, sans-serif';
      context.textAlign = 'center';
      context.textBaseline = 'middle';
      context.fillStyle = '#ffffff';
      context.fillText(node.icon, node.x, node.y + 1);
    }
  };
