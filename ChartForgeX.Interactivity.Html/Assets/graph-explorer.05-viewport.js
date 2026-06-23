  const clusterMetrics = (cluster, byId) => {
    const members = cluster.nodeIds.map(id => byId.get(id)).filter(Boolean);
    if (!members.length) return null;
    const minX = Math.min(...members.map(node => node.x));
    const maxX = Math.max(...members.map(node => node.x));
    const minY = Math.min(...members.map(node => node.y));
    const maxY = Math.max(...members.map(node => node.y));
    const x = members.reduce((sum, node) => sum + node.x, 0) / members.length;
    const y = members.reduce((sum, node) => sum + node.y, 0) / members.length;
    const expanded = cluster.el.classList.contains('cfx-graph-cluster-expanded');
    const spread = Math.max(maxX - minX, maxY - minY);
    const radius = expanded ? Math.max(34, Math.min(96, spread / 2 + 22)) : Math.max(34, Math.min(54, 20 + Math.sqrt(members.length) * 7));
    return { x, y, radius, expanded };
  };
  const contentBounds = (root, state) => {
    const byId = state.byId || new Map(state.nodes.map(node => [node.id, node]));
    const bounds = [];
    state.nodes.forEach(node => {
      if (!visible(node.el)) return;
      const radius = node.size + 26;
      bounds.push({ minX: node.x - radius, minY: node.y - radius, maxX: node.x + radius, maxY: node.y + radius });
    });
    state.clusters.forEach(cluster => {
      if (!visible(cluster.el)) return;
      const metrics = clusterMetrics(cluster, byId);
      if (!metrics) return;
      bounds.push({ minX: metrics.x - metrics.radius - 16, minY: metrics.y - metrics.radius - 16, maxX: metrics.x + metrics.radius + 16, maxY: metrics.y + metrics.radius + 16 });
    });
    if (!bounds.length) return null;
    return {
      minX: Math.min(...bounds.map(item => item.minX)),
      minY: Math.min(...bounds.map(item => item.minY)),
      maxX: Math.max(...bounds.map(item => item.maxX)),
      maxY: Math.max(...bounds.map(item => item.maxY))
    };
  };
  const fitViewport = (root) => {
    const bounds = contentBounds(root, graphState(root));
    const size = sceneSize(root);
    if (!bounds) {
      setViewport(root, { x: 0, y: 0, scale: 1 });
      return;
    }
    const padding = Math.max(34, Math.min(84, Math.min(size.width, size.height) * 0.08));
    const width = Math.max(1, bounds.maxX - bounds.minX);
    const height = Math.max(1, bounds.maxY - bounds.minY);
    const scale = Math.min(1.35, Math.max(0.2, Math.min((size.width - padding * 2) / width, (size.height - padding * 2) / height)));
    const centerX = (bounds.minX + bounds.maxX) / 2;
    const centerY = (bounds.minY + bounds.maxY) / 2;
    root.dataset.cfxGraphFitScale = scale.toFixed(3);
    root.dataset.cfxGraphFitContentWidth = width.toFixed(3);
    root.dataset.cfxGraphFitContentHeight = height.toFixed(3);
    setViewport(root, {
      scale,
      x: size.centerX - centerX * scale,
      y: size.centerY - centerY * scale
    });
  };
