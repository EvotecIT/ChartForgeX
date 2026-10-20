  // A fresh index belongs to one paint/bounds pass, so filters, dragging and
  // graph patches cannot reuse stale coordinates or hidden member details.
  const graphEdgeLabelObstacles = (root, state) => {
    const byId = state.byId || new Map(state.nodes.map(node => [node.id, node]));
    const nodes = state.nodes.filter(node => visible(node.el));
    state.clusters.forEach(cluster => {
      const proxy = clusterProxyNode(cluster, byId);
      if (proxy) nodes.push(proxy);
    });
    return buildGraphBoundsTree(nodes.map(node => {
      const extents = attr(node.el, 'data-cfx-role') === 'graph-cluster' ? nodeShapeExtents(node) : nodeLayoutExtents(node, false);
      return { minX: node.x - extents.x, minY: node.y - extents.y, maxX: node.x + extents.x, maxY: node.y + extents.y };
    }));
  };
  const edgeLabelIntersectsObstacles = (point, label, obstacles) => {
    const halfWidth = Math.max(14, graphEstimatedEdgeLabelWidth(label) / 2);
    const query = { minX: point.x - halfWidth - 6, maxX: point.x + halfWidth + 6, minY: point.y - 20, maxY: point.y + 12 };
    return graphBoundsCandidates(obstacles, query).some(obstacle =>
      query.minX < obstacle.maxX && query.maxX > obstacle.minX && query.minY < obstacle.maxY && query.maxY > obstacle.minY);
  };
  const syncGraphEdgeGeometry = (path, label, rendered, control, obstacles) => {
    path.setAttribute('d', edgePathData(rendered, control));
    if (!label) return;
    const point = edgeLabelPoint(rendered, control, obstacles);
    label.setAttribute('x', point.x.toFixed(3));
    label.setAttribute('y', point.y.toFixed(3));
  };
