  const indexHitTesting = (root, state) => {
    const fullState = state.fullState || state;
    const previous = root.__cfxGraphHitGrid;
    const incremental = !!state.fullState && previous?.state === fullState;
    const index = incremental ? previous : { cellSize: 48, grid: new Map(), nodeCells: new Map(), state: fullState };
    // Rebuild membership once after filtering. Later physics frames update only
    // their moving subset, retaining nodes revealed while that session was running.
    const nodes = incremental ? state.nodes : fullState.nodes;
    nodes.forEach(node => {
      for (const key of index.nodeCells.get(node.id) || []) {
        const bucket = index.grid.get(key);
        bucket.delete(node);
        if (!bucket.size) index.grid.delete(key);
      }
      index.nodeCells.delete(node.id);
      if (!visible(node.el)) return;
      const slack = 10, cellSize = index.cellSize;
      const minX = Math.floor((node.x - nodeHalfWidth(node) - slack) / cellSize);
      const maxX = Math.floor((node.x + nodeHalfWidth(node) + slack) / cellSize);
      const minY = Math.floor((node.y - nodeHalfHeight(node) - slack) / cellSize);
      const maxY = Math.floor((node.y + nodeHalfHeight(node) + slack) / cellSize);
      const cells = [];
      for (let x = minX; x <= maxX; x++) for (let y = minY; y <= maxY; y++) {
        const key = `${x}:${y}`;
        const bucket = index.grid.get(key) || new Set();
        bucket.add(node);
        index.grid.set(key, bucket);
        cells.push(key);
      }
      index.nodeCells.set(node.id, cells);
    });
    root.__cfxGraphState = fullState;
    root.__cfxGraphHitGrid = index;
    root.__cfxGraphHitVersion = (root.__cfxGraphHitVersion || 0) + 1;
    root.dataset.cfxGraphHitTest = fullState.nodes.length >= 160 ? 'grid' : 'linear';
  };
  const hitTestNodes = (root, point) => {
    const state = root.__cfxGraphState || graphState(root);
    if (state.nodes.length < 160) { root.dataset.cfxGraphNodeHitCandidates = String(state.nodes.length); return state.nodes; }
    const index = root.__cfxGraphHitGrid?.state === state ? root.__cfxGraphHitGrid : (indexHitTesting(root, state), root.__cfxGraphHitGrid);
    const cx = Math.floor(point.x / index.cellSize);
    const cy = Math.floor(point.y / index.cellSize);
    const candidates = [];
    const seen = new Set();
    for (let x = cx - 1; x <= cx + 1; x++) for (let y = cy - 1; y <= cy + 1; y++) {
      (index.grid.get(`${x}:${y}`) || []).forEach(node => {
        if (seen.has(node.id)) return;
        seen.add(node.id);
        candidates.push(node);
      });
    }
    root.dataset.cfxGraphNodeHitCandidates = String(candidates.length);
    return candidates;
  };
  const hitNodeAt = (root, point) => {
    let best = null;
    let bestDistance = Number.POSITIVE_INFINITY;
    hitTestNodes(root, point).forEach(node => {
      if (!visible(node.el)) return;
      const distance = nodeHitDistance(node, point, 10);
      if (distance < bestDistance) {
        best = node;
        bestDistance = distance;
      }
    });
    return best;
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
  const cubicPoint = (curve, t) => {
    const inv = 1 - t;
    return {
      x: inv ** 3 * curve.start.x + 3 * inv * inv * t * curve.c1.x + 3 * inv * t * t * curve.c2.x + t ** 3 * curve.end.x,
      y: inv ** 3 * curve.start.y + 3 * inv * inv * t * curve.c1.y + 3 * inv * t * t * curve.c2.y + t ** 3 * curve.end.y
    };
  };
  const quadraticPoint = (start, control, end, t) => {
    const inv = 1 - t;
    return {
      x: inv * inv * start.x + 2 * inv * t * control.x + t * t * end.x,
      y: inv * inv * start.y + 2 * inv * t * control.y + t * t * end.y
    };
  };
  const distanceToQuadratic = (point, start, control, end) => {
    let best = Number.POSITIVE_INFINITY;
    let previous = start;
    for (let index = 1; index <= 18; index++) {
      const current = quadraticPoint(start, control, end, index / 18);
      best = Math.min(best, distanceToSegment(point, previous, current));
      previous = current;
    }
    return best;
  };
  const distanceToSelfLoop = (point, node) => {
    const loop = selfLoopGeometry(node);
    let best = Number.POSITIVE_INFINITY;
    let previous = loop.start;
    for (let index = 1; index <= 18; index++) {
      const current = cubicPoint(loop, index / 18);
      best = Math.min(best, distanceToSegment(point, previous, current));
      previous = current;
    }
    return best;
  };
  const distanceToRoute = (point, points) => {
    let best = Number.POSITIVE_INFINITY;
    for (let index = 1; index < points.length; index++) best = Math.min(best, distanceToSegment(point, points[index - 1], points[index]));
    return best;
  };
  const hitEdgeAt = (root, point) => {
    let best = null;
    let bestDistance = Number.POSITIVE_INFINITY;
    const state = root.__cfxGraphState || graphState(root);
    const candidates = edgeHitCandidates(root, state, point);
    root.dataset.cfxGraphEdgeHitCandidates = String(candidates.length);
    candidates.forEach(entry => {
      const { edge, rendered, control, endpoints, route, loop } = entry;
      if (!visible(edge.el) || !endpointVisible(rendered.source) || !endpointVisible(rendered.target)) return;
      const distance = loop
        ? distanceToSelfLoop(point, rendered.source)
        : route
        ? distanceToRoute(point, route)
        : control
        ? distanceToQuadratic(point, endpoints.source, control, endpoints.target)
        : distanceToSegment(point, endpoints.source, endpoints.target);
      if (distance <= entry.tolerance && (distance < bestDistance || distance === bestDistance && entry.order < best.order)) {
        best = entry;
        bestDistance = distance;
      }
    });
    return best?.edge || null;
  };
  const hitClusterAt = (root, point) => {
    let best = null;
    let bestDistance = Number.POSITIVE_INFINITY;
    const state = root.__cfxGraphState || graphState(root);
    state.clusters.forEach(cluster => {
      if (!visible(cluster.el) || cluster.el.classList.contains('cfx-graph-cluster-expanded') || attr(cluster.el, 'data-cluster-collapsed') !== 'true') return;
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
