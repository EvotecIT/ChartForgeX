  const drawWebGlUnderlay = (root, state, drawing) => {
    const surface = canvasContext(root, 'graph-underlay');
    if (!surface) return false;
    const { context, bounds } = surface, view = viewport(root);
    context.clearRect(bounds.x, bounds.y, bounds.width, bounds.height);
    context.fillStyle = drawing.palette.paper;
    context.fillRect(bounds.x, bounds.y, bounds.width, bounds.height);
    context.save();
    context.transform(view.scale, 0, 0, view.scale, view.x, view.y);
    drawCanvasClusters(context, root, state, drawing.byId, drawing.palette, drawing.moving, drawing.mesh.clusterMetrics);
    context.restore();
    return true;
  };
  const drawWebGlDetails = (root, state, drawing) => {
    const { byId, palette, compact, dense, moving, mesh } = drawing;
    const fallbackEdges = mesh.fallbackEdges;
    const surface = canvasContext(root);
    if (!surface) return false;
    const { context, bounds } = surface, view = viewport(root);
    context.clearRect(bounds.x, bounds.y, bounds.width, bounds.height);
    context.save();
    context.transform(view.scale, 0, 0, view.scale, view.x, view.y);
    context.lineCap = 'round';
    const fallback = new Set(fallbackEdges.map(item => item.edge));
    fallbackEdges.forEach(item => drawCanvasEdge(context, root, item.edge, item.paint, palette));
    state.edges.forEach(edge => {
      if (!edge.label || !edge.showLabel || fallback.has(edge) || !visible(edge.el) || !edgeHasVisibleEndpoints(edge, byId)) return;
      drawCanvasEdgeLabel(context, root, edge, graphEdgePaint(root, edge, byId, palette, dense, moving), palette);
    });
    drawCanvasNodes(context, root, state.nodes, compact, moving, fallbackEdges.length ? undefined : mesh.nodePoints.nodes);
    context.restore();
    return true;
  };
