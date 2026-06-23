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
      const members = cluster.nodeIds.map(id => state.byId.get(id)).filter(Boolean);
      if (!members.length) return;
      const x = members.reduce((sum, node) => sum + node.x, 0) / members.length;
      const y = members.reduce((sum, node) => sum + node.y, 0) / members.length;
      const radius = Math.max(22, 12 + members.length * 3);
      const distance = Math.sqrt((point.x - x) ** 2 + (point.y - y) ** 2);
      if (distance <= radius + 6 && distance < bestDistance) {
        best = { ...cluster, x, y, size: radius };
        bestDistance = distance;
      }
    });
    return best;
  };
  const hitGraphItemAt = (root, point) => hitNodeAt(root, point) || hitClusterAt(root, point) || hitEdgeAt(root, point);
