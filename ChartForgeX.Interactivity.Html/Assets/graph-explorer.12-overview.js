  const overviewCanvas = (root) => root.querySelector('[data-cfx-role="graph-overview"]');
  const overviewScale = (root, state) => {
    const bounds = contentBounds(root, state);
    if (!bounds) return null;
    const width = Math.max(1, bounds.maxX - bounds.minX);
    const height = Math.max(1, bounds.maxY - bounds.minY);
    const pad = Math.max(8, Math.min(14, Math.min(width, height) * 0.03));
    const canvas = overviewCanvas(root);
    if (!canvas) return null;
    const scale = Math.min((canvas.width - pad * 2) / width, (canvas.height - pad * 2) / height);
    return { bounds, scale, pad, width, height };
  };
  const overviewPoint = (metrics, x, y) => ({
    x: metrics.pad + (x - metrics.bounds.minX) * metrics.scale,
    y: metrics.pad + (y - metrics.bounds.minY) * metrics.scale
  });
  const graphPointFromOverview = (metrics, x, y) => ({
    x: metrics.bounds.minX + (x - metrics.pad) / metrics.scale,
    y: metrics.bounds.minY + (y - metrics.pad) / metrics.scale
  });
  const updateOverview = (root, state) => {
    const canvas = overviewCanvas(root);
    if (!canvas || !hasFeature(root, 'Viewport')) return;
    const context = canvas.getContext('2d');
    if (!context) return;
    const currentState = state || graphState(root);
    const metrics = overviewScale(root, currentState);
    context.clearRect(0, 0, canvas.width, canvas.height);
    if (!metrics) {
      root.dataset.cfxGraphOverview = 'empty';
      return;
    }
    root.dataset.cfxGraphOverview = 'ready';
    root.dataset.cfxGraphOverviewScale = metrics.scale.toFixed(4);
    context.fillStyle = 'rgba(248,250,252,.96)';
    context.fillRect(0, 0, canvas.width, canvas.height);
    context.strokeStyle = '#cbd5e1';
    context.lineWidth = 1;
    context.strokeRect(.5, .5, canvas.width - 1, canvas.height - 1);
    const edgeLimit = currentState.edges.length > 1200 ? 1200 : currentState.edges.length;
    context.strokeStyle = 'rgba(148,163,184,.32)';
    context.lineWidth = .75;
    for (let index = 0; index < edgeLimit; index++) {
      const edge = currentState.edges[index];
      if (!visible(edge.el) || !visible(edge.source.el) || !visible(edge.target.el)) continue;
      const source = overviewPoint(metrics, edge.source.x, edge.source.y);
      const target = overviewPoint(metrics, edge.target.x, edge.target.y);
      context.beginPath();
      context.moveTo(source.x, source.y);
      context.lineTo(target.x, target.y);
      context.stroke();
    }
    currentState.nodes.forEach(node => {
      if (!visible(node.el)) return;
      const point = overviewPoint(metrics, node.x, node.y);
      context.fillStyle = node.el.classList.contains('cfx-graph-selected') ? '#f59e0b' : '#2563eb';
      if (node.shape === 'box') context.fillRect(point.x - 2.4, point.y - 1.8, 4.8, 3.6);
      else {
        context.beginPath();
        context.arc(point.x, point.y, node.el.classList.contains('cfx-graph-selected') ? 2.8 : 1.9, 0, Math.PI * 2);
        context.fill();
      }
    });
    const view = viewport(root);
    const size = sceneSize(root);
    const visibleLeft = -view.x / view.scale;
    const visibleTop = -view.y / view.scale;
    const visibleRight = visibleLeft + size.width / view.scale;
    const visibleBottom = visibleTop + size.height / view.scale;
    const a = overviewPoint(metrics, visibleLeft, visibleTop);
    const b = overviewPoint(metrics, visibleRight, visibleBottom);
    const rectX = Math.max(0, Math.min(canvas.width, Math.min(a.x, b.x)));
    const rectY = Math.max(0, Math.min(canvas.height, Math.min(a.y, b.y)));
    const rectW = Math.max(2, Math.min(canvas.width - rectX, Math.abs(b.x - a.x)));
    const rectH = Math.max(2, Math.min(canvas.height - rectY, Math.abs(b.y - a.y)));
    context.strokeStyle = '#0f172a';
    context.lineWidth = 1.4;
    context.strokeRect(rectX, rectY, rectW, rectH);
    root.dataset.cfxGraphOverviewContentWidth = metrics.width.toFixed(3);
    root.dataset.cfxGraphOverviewContentHeight = metrics.height.toFixed(3);
  };
  const bindOverview = (root) => {
    const canvas = overviewCanvas(root);
    if (!canvas) return;
    root.classList.toggle('cfx-graph-has-overview', hasFeature(root, 'Viewport'));
    if (!hasFeature(root, 'Viewport')) return;
    const moveTo = (event) => {
      const metrics = overviewScale(root, graphState(root));
      if (!metrics) return;
      const rect = canvas.getBoundingClientRect();
      const point = graphPointFromOverview(metrics, (event.clientX - rect.left) * canvas.width / Math.max(1, rect.width), (event.clientY - rect.top) * canvas.height / Math.max(1, rect.height));
      const view = viewport(root);
      const size = sceneSize(root);
      root.__cfxGraphViewportTouched = true;
      setViewport(root, { scale: view.scale, x: size.centerX - point.x * view.scale, y: size.centerY - point.y * view.scale });
      emit(root, 'cfxgraphoverview', { graphId: attr(root, 'data-cfx-graph-id'), x: point.x, y: point.y, scale: view.scale });
    };
    canvas.addEventListener('pointerdown', event => {
      event.preventDefault();
      event.stopPropagation();
      canvas.setPointerCapture?.(event.pointerId);
      canvas.__cfxGraphOverviewPointer = event.pointerId;
      moveTo(event);
    });
    canvas.addEventListener('pointermove', event => {
      if (canvas.__cfxGraphOverviewPointer !== event.pointerId) return;
      event.preventDefault();
      moveTo(event);
    });
    const finish = event => {
      if (canvas.__cfxGraphOverviewPointer !== event.pointerId) return;
      canvas.releasePointerCapture?.(event.pointerId);
      canvas.__cfxGraphOverviewPointer = 0;
    };
    canvas.addEventListener('pointerup', finish);
    canvas.addEventListener('pointercancel', finish);
    updateOverview(root, graphState(root));
  };
