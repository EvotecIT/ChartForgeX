  const imageCache = new Map();
  const imageLoadCallbacks = new Map();
  const graphImage = (url, onload) => {
    if (!url) return null;
    if (imageCache.has(url)) {
      const cached = imageCache.get(url);
      if (onload && cached && !cached.complete) {
        const callbacks = imageLoadCallbacks.get(url) || new Set();
        callbacks.add(onload);
        imageLoadCallbacks.set(url, callbacks);
      }
      return cached;
    }
    const notify = () => {
      const callbacks = imageLoadCallbacks.get(url);
      imageLoadCallbacks.delete(url);
      if (callbacks) callbacks.forEach(callback => callback());
    };
    const callbacks = new Set();
    if (onload) callbacks.add(onload);
    imageLoadCallbacks.set(url, callbacks);
    const image = new Image();
    image.addEventListener('load', notify, { once: true });
    image.addEventListener('error', notify, { once: true });
    image.src = url;
    imageCache.set(url, image);
    return image;
  };
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
    const pushPoint = (point, pad) => {
      if (!point || !Number.isFinite(point.x) || !Number.isFinite(point.y)) return;
      const padding = Number.isFinite(pad) ? pad : 0;
      bounds.push({ minX: point.x - padding, minY: point.y - padding, maxX: point.x + padding, maxY: point.y + padding });
    };
    state.nodes.forEach(node => {
      if (!visible(node.el)) return;
      const pad = 30;
      bounds.push({ minX: node.x - nodeHalfWidth(node) - pad, minY: node.y - nodeHalfHeight(node) - pad, maxX: node.x + nodeHalfWidth(node) + pad, maxY: node.y + nodeHalfHeight(node) + pad });
    });
    state.edges.forEach(edge => {
      if (!visible(edge.el) || !edge.source || !edge.target || !visible(edge.source.el) || !visible(edge.target.el)) return;
      const pad = Math.max(12, Math.min(34, (edge.weight || 1) * 3 + 10));
      if (edge.source === edge.target) {
        const loop = selfLoopGeometry(edge.source);
        pushPoint(loop.start, pad);
        pushPoint(loop.c1, pad);
        pushPoint(loop.c2, pad);
        pushPoint(loop.end, pad);
        pushPoint(loop.label, pad + 10);
        return;
      }

      const control = edgeControl(edge);
      pushPoint(edge.source, pad);
      pushPoint(edge.target, pad);
      pushPoint(control, pad);
      pushPoint(edgeLabelPoint(edge, control), pad + 10);
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
