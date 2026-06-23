  const indexHitTesting = (root, state) => {
    const cellSize = 48;
    const grid = new Map();
    state.nodes.forEach(node => {
      const key = `${Math.floor(node.x / cellSize)}:${Math.floor(node.y / cellSize)}`;
      const bucket = grid.get(key) || [];
      bucket.push(node);
      grid.set(key, bucket);
    });
    root.__cfxGraphState = state;
    root.__cfxGraphHitGrid = { cellSize, grid };
    root.dataset.cfxGraphHitTest = state.nodes.length >= 160 ? 'grid' : 'linear';
  };
  const hitTestNodes = (root, point) => {
    const state = root.__cfxGraphState || graphState(root);
    if (state.nodes.length < 160) return state.nodes;
    const index = root.__cfxGraphHitGrid || (indexHitTesting(root, state), root.__cfxGraphHitGrid);
    const cx = Math.floor(point.x / index.cellSize);
    const cy = Math.floor(point.y / index.cellSize);
    const candidates = [];
    for (let x = cx - 1; x <= cx + 1; x++) for (let y = cy - 1; y <= cy + 1; y++) candidates.push(...index.grid.get(`${x}:${y}`) || []);
    return candidates.length ? candidates : state.nodes;
  };
  const domHitNodeAt = (root, point) => {
    let best = null;
    let bestDistance = Number.POSITIVE_INFINITY;
    items(root, '[data-cfx-role="graph-node"]').forEach(el => {
      if (!visible(el)) return;
      const size = Math.max(4, num(el, 'data-node-size', 8));
      const x = num(el, 'data-node-x', 0);
      const y = num(el, 'data-node-y', 0);
      const dx = x - point.x;
      const dy = y - point.y;
      const distance = Math.sqrt(dx * dx + dy * dy);
      if (distance <= size + 10 && distance < bestDistance) {
        best = { el, id: attr(el, 'data-node-id'), x, y, size };
        bestDistance = distance;
      }
    });
    if (!best) return null;
    return root.__cfxGraphState?.byId?.get(best.id) || best;
  };
  const hitNodeAt = (root, point) => {
    let best = null;
    let bestDistance = Number.POSITIVE_INFINITY;
    hitTestNodes(root, point).forEach(node => {
      if (!visible(node.el)) return;
      const dx = node.x - point.x;
      const dy = node.y - point.y;
      const distance = Math.sqrt(dx * dx + dy * dy);
      if (distance <= node.size + 10 && distance < bestDistance) {
        best = node;
        bestDistance = distance;
      }
    });
    return best || domHitNodeAt(root, point);
  };
  const distanceToSegment = (point, a, b) => {
    const dx = b.x - a.x;
    const dy = b.y - a.y;
    const length = dx * dx + dy * dy;
    if (length <= 0.001) return Math.sqrt((point.x - a.x) ** 2 + (point.y - a.y) ** 2);
    const t = Math.max(0, Math.min(1, ((point.x - a.x) * dx + (point.y - a.y) * dy) / length));
    const x = a.x + t * dx;
    const y = a.y + t * dy;
    return Math.sqrt((point.x - x) ** 2 + (point.y - y) ** 2);
  };
  const hitEdgeAt = (root, point) => {
    let best = null;
    let bestDistance = Number.POSITIVE_INFINITY;
    const state = root.__cfxGraphState || graphState(root);
    state.edges.forEach(edge => {
      if (!visible(edge.el) || !visible(edge.source.el) || !visible(edge.target.el)) return;
      const control = edgeControl(edge);
      const distance = control
        ? Math.min(distanceToSegment(point, edge.source, control), distanceToSegment(point, control, edge.target))
        : distanceToSegment(point, edge.source, edge.target);
      if (distance <= Math.max(8, edge.weight + 6) && distance < bestDistance) {
        best = edge;
        bestDistance = distance;
      }
    });
    return best;
  };
  const hitClusterAt = (root, point) => {
    let best = null;
    let bestDistance = Number.POSITIVE_INFINITY;
    const state = root.__cfxGraphState || graphState(root);
    state.clusters.forEach(cluster => {
      if (!visible(cluster.el)) return;
      const metrics = clusterMetrics(cluster, state.byId);
      if (!metrics) return;
      const distance = Math.sqrt((point.x - metrics.x) ** 2 + (point.y - metrics.y) ** 2);
      if (distance <= metrics.radius + 6 && distance < bestDistance) {
        best = { ...cluster, x: metrics.x, y: metrics.y, size: metrics.radius };
        bestDistance = distance;
      }
    });
    return best;
  };
  const hitGraphItemAt = (root, point) => hitNodeAt(root, point) || hitClusterAt(root, point) || hitEdgeAt(root, point);
