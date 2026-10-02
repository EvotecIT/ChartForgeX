  // Canvas, WebGL and raster export share the same visible edge style and LOD policy.
  const graphEdgePaint = (root, edge, byId, palette, dense, moving) => {
    const rendered = visualEdge(edge, byId), control = edgeControl(rendered);
    const dimmed = edge.el.classList.contains('cfx-graph-neighborhood-dim');
    const related = edge.el.classList.contains('cfx-graph-neighborhood-related');
    const selected = edge.el.classList.contains('cfx-graph-selected');
    const baseWidth = dense ? Math.max(.65, Math.min(1.8, edge.weight * .55)) : edge.weight;
    const emphasis = selected ? 1.6 : related ? 1.2 : 0;
    const width = edge.strokeWidth > 0 ? Math.max(.65, edge.strokeWidth + emphasis)
      : Math.max(.65, Math.min(selected ? 6 : related ? 4 : dense ? 1.8 : 4, baseWidth + emphasis));
    return { rendered, control, width, dimmed,
      color: selected ? palette.selected : related ? '#14b8a6' : edge.strokeColor || palette.edge,
      alpha: dimmed ? .1 : selected ? .95 : related ? .86 : dense ? .28 : .58,
      arrowAlpha: dimmed ? .14 : selected || related ? 1 : dense ? .34 : 1,
      arrows: !moving || selected || related,
      labels: (!moving || selected || related) && edge.label && edge.showLabel &&
        (!root.classList.contains('cfx-graph-lod-hide-edge-labels') || selected || related) &&
        (!root.classList.contains('cfx-graph-priority-overview') || selected || related) };
  };
  const drawCanvasEdgeLabel = (context, root, edge, paint, palette) => {
    if (!paint.labels) return;
    const label = edgeLabelPoint(paint.rendered, paint.control);
    context.font = '11px Segoe UI, Arial, sans-serif';
    context.textAlign = 'center';
    context.textBaseline = 'middle';
    context.lineWidth = 4;
    context.strokeStyle = palette.halo;
    context.fillStyle = graphAdaptiveTextColor(root, edge.labelColor, palette.edgeLabel);
    context.globalAlpha = paint.dimmed ? .16 : 1;
    context.strokeText(edge.label, label.x, label.y);
    context.fillText(edge.label, label.x, label.y);
    context.globalAlpha = 1;
  };
  const drawCanvasEdge = (context, root, edge, paint, palette) => {
    edgeDrawPath(context, paint.rendered, paint.control);
    context.strokeStyle = paint.color;
    context.globalAlpha = paint.alpha;
    context.lineWidth = paint.width;
    context.setLineDash(edge.dashed ? edge.dashPattern : []);
    context.stroke();
    context.setLineDash([]);
    context.globalAlpha = 1;
    if (paint.arrows) {
      context.globalAlpha = paint.arrowAlpha;
      if (edge.sourceArrow) drawArrow(context, paint.rendered, paint.control, 'source', paint.color);
      if (edge.targetArrow || edge.directed) drawArrow(context, paint.rendered, paint.control, 'target', paint.color);
      context.globalAlpha = 1;
    }
    drawCanvasEdgeLabel(context, root, edge, paint, palette);
  };
