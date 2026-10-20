  const imageCache = new Map();
  const imageLoadCallbacks = new Map();
  const imageConsumers = new Map();
  const graphImageUrls = nodes => new Set(nodes.filter(node => (node.shape === 'image' || node.shape === 'imageRect') && node.imageUrl).map(node => node.imageUrl));
  const retainGraphImageUrl = url => imageConsumers.set(url, (imageConsumers.get(url) || 0) + 1);
  const releaseGraphImageUrl = url => {
    const count = imageConsumers.get(url) || 0;
    if (count > 1) imageConsumers.set(url, count - 1);
    else {
      imageConsumers.delete(url); imageCache.delete(url); imageLoadCallbacks.delete(url);
    }
  };
  const releaseGraphImageRedraw = (root, url) => {
    const callbacks = imageLoadCallbacks.get(url);
    callbacks?.delete(root.__cfxGraphImageRedraw);
    if (callbacks && !callbacks.size) imageLoadCallbacks.delete(url);
  };
  // Scene ownership changes only when canonical nodes are reconstructed. Paints
  // reuse their images, and a peer scene can keep the same URL alive independently.
  const syncGraphImageOwnership = (root, nodes) => {
    const lifetime = graphLifetime(root);
    if (!lifetime?.active) return;
    let owned = root.__cfxGraphImageUrls;
    if (!owned) {
      owned = root.__cfxGraphImageUrls = new Set();
      lifetime.own(() => {
        for (const url of owned) { releaseGraphImageRedraw(root, url); releaseGraphImageUrl(url); }
        delete root.__cfxGraphImageUrls;
      });
    }
    const next = graphImageUrls(nodes);
    for (const url of owned) if (!next.has(url)) {
      releaseGraphImageRedraw(root, url); releaseGraphImageUrl(url); owned.delete(url);
    }
    for (const url of next) if (!owned.has(url)) { retainGraphImageUrl(url); owned.add(url); }
  };
  // An asynchronous PNG export can still need its captured scene after a patch
  // replaces that scene. Its lease ends at export completion or root disposal.
  const leaseGraphImages = (root, state) => {
    const lifetime = graphLifetime(root);
    if (!lifetime?.active) return () => {};
    const urls = graphImageUrls(state.nodes);
    urls.forEach(retainGraphImageUrl);
    return lifetime.own(() => urls.forEach(releaseGraphImageUrl));
  };
  const graphImageRedraw = (root) => {
    if (root.__cfxGraphImageRedraw) return root.__cfxGraphImageRedraw;
    const redraw = () => { if (graphLifetime(root)?.active && root.isConnected) drawCanvas(root, root.__cfxGraphState || graphState(root)); };
    root.__cfxGraphImageRedraw = redraw;
    graphLifetime(root)?.own(() => {
      for (const [url, callbacks] of imageLoadCallbacks) { callbacks.delete(redraw); if (!callbacks.size) imageLoadCallbacks.delete(url); }
      delete root.__cfxGraphImageRedraw;
    });
    return redraw;
  };
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
    const image = new Image();
    const notify = () => {
      // An abandoned load can finish after this URL has acquired a new image.
      if (imageCache.get(url) !== image) return;
      const callbacks = imageLoadCallbacks.get(url);
      imageLoadCallbacks.delete(url);
      if (callbacks) callbacks.forEach(callback => callback());
    };
    const callbacks = new Set();
    if (onload) callbacks.add(onload);
    imageLoadCallbacks.set(url, callbacks);
    image.addEventListener('load', notify, { once: true });
    image.addEventListener('error', notify, { once: true });
    image.src = url;
    imageCache.set(url, image);
    return image;
  };
  const clusterMetrics = (cluster, byId) => {
    const allMembers = cluster.nodeIds.map(id => byId.get(id)).filter(Boolean);
    const expanded = cluster.el.classList.contains('cfx-graph-cluster-expanded');
    const members = expanded ? allMembers.filter(node => visible(node.el)) : allMembers;
    if (!members.length) return null;
    const minX = Math.min(...members.map(node => node.x));
    const maxX = Math.max(...members.map(node => node.x));
    const minY = Math.min(...members.map(node => node.y));
    const maxY = Math.max(...members.map(node => node.y));
    const x = members.reduce((sum, node) => sum + node.x, 0) / members.length;
    const y = members.reduce((sum, node) => sum + node.y, 0) / members.length;
    const spread = Math.max(maxX - minX, maxY - minY);
    const radius = expanded ? Math.max(34, Math.min(96, spread / 2 + 22)) : Math.max(34, Math.min(54, 20 + Math.sqrt(members.length) * 7));
    return { x, y, radius, expanded };
  };
  const clusterProxyNode = (cluster, byId) => {
    if (!cluster || !visible(cluster.el) || cluster.el.classList.contains('cfx-graph-cluster-expanded')) return null;
    const metrics = clusterMetrics(cluster, byId);
    return metrics ? { el: cluster.el, id: cluster.id, x: metrics.x, y: metrics.y, size: metrics.radius, shape: 'circle' } : null;
  };
  const edgeVisualNode = (edge, side, byId) => {
    const node = side === 'source' ? edge.source : edge.target;
    if (visible(node.el)) return node;
    return clusterProxyNode(side === 'source' ? edge.sourceCluster : edge.targetCluster, byId) || node;
  };
  const endpointVisible = (node) => node && (visible(node.el) || attr(node.el, 'data-node-hidden') === 'true');
  const visualEdge = (edge, byId) => { const source = edgeVisualNode(edge, 'source', byId), target = edgeVisualNode(edge, 'target', byId); return { ...edge, source, target, sourceCollapsed: source !== edge.source, targetCollapsed: target !== edge.target }; };
  const edgeHasVisibleEndpoints = (edge, byId) => endpointVisible(edgeVisualNode(edge, 'source', byId)) && endpointVisible(edgeVisualNode(edge, 'target', byId));
  const contentBounds = (root, state) => {
    const byId = state.byId || new Map(state.nodes.map(node => [node.id, node]));
    const labelObstacles = graphEdgeLabelObstacles(root, state);
    const bounds = [];
    const pushPoint = (point, pad) => {
      if (!point || !Number.isFinite(point.x) || !Number.isFinite(point.y)) return;
      const padding = Number.isFinite(pad) ? pad : 0;
      bounds.push({ minX: point.x - padding, minY: point.y - padding, maxX: point.x + padding, maxY: point.y + padding });
    };
    const pushEdgeLabel = (edge, point, pad) => {
      if (!point || !edge.showLabel || !edge.label) return;
      const halfWidth = Math.max(14, graphEstimatedEdgeLabelWidth(edge.label) / 2);
      bounds.push({ minX: point.x - halfWidth - pad, minY: point.y - 16 - pad, maxX: point.x + halfWidth + pad, maxY: point.y + 8 + pad });
    };
    state.nodes.forEach(node => {
      if (!visible(node.el)) return;
      const pad = 30;
      bounds.push({ minX: node.x - nodeHalfWidth(node) - pad, minY: node.y - nodeHalfHeight(node) - pad, maxX: node.x + nodeHalfWidth(node) + pad, maxY: node.y + nodeHalfHeight(node) + pad });
    });
    state.edges.forEach(edge => {
      if (!visible(edge.el) || !edge.source || !edge.target || !edgeHasVisibleEndpoints(edge, byId)) return;
      const rendered = visualEdge(edge, byId);
      const pad = Math.max(12, Math.min(34, (edge.weight || 1) * 3 + 10));
      if (rendered.source === rendered.target) {
        const loop = selfLoopGeometry(rendered.source);
        pushPoint(loop.start, pad);
        pushPoint(loop.c1, pad);
        pushPoint(loop.c2, pad);
        pushPoint(loop.end, pad);
        pushEdgeLabel(rendered, loop.label, pad);
        return;
      }

      const control = edgeControl(rendered);
      pushPoint(rendered.source, pad);
      pushPoint(rendered.target, pad);
      if (edgeHasRoute(rendered)) routeRenderPoints(rendered).forEach(point => pushPoint(point, pad));
      pushPoint(control, pad);
      pushEdgeLabel(rendered, edgeLabelPoint(rendered, control, labelObstacles), pad);
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
    const bounds = contentBounds(root, root.__cfxGraphState || graphState(root));
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

  const bindGraphSurfaceResize = root => {
    const stage = root.querySelector('.cfx-graph-stage');
    if (!stage || typeof ResizeObserver === 'undefined') return;
    let frame = 0;
    const observer = new ResizeObserver(() => {
      if (frame) graphLifetime(root)?.cancelFrame(frame);
      frame = graphLifetime(root)?.frame(() => {
        frame = 0;
        if (root.isConnected === false) { observer.disconnect(); return; }
        if (!graphStageHasLayout(root)) return;
        if (hasFeature(root, 'Viewport') && !root.__cfxGraphViewportTouched) fitViewport(root);
        else drawCanvas(root, root.__cfxGraphState || graphState(root));
      });
    });
    observer.observe(stage);
    root.__cfxGraphResizeObserver = observer;
    graphLifetime(root)?.own(() => { observer.disconnect(); if (frame) graphLifetime(root)?.cancelFrame(frame); delete root.__cfxGraphResizeObserver; });
  };
