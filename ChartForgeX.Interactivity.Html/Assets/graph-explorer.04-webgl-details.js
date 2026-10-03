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
    drawCanvasClusters(context, root, state, byId, palette, moving, mesh.clusterMetrics);
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
