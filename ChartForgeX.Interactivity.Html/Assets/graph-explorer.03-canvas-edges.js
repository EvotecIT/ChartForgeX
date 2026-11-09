  // Canvas, WebGL and raster export share the same visible edge style and LOD policy.
  const graphEdgePaint = (root, edge, byId, palette, dense, moving, labelObstacles) => {
    const rendered = visualEdge(edge, byId), control = edgeControl(rendered);
    const dimmed = edge.el.classList.contains('cfx-graph-neighborhood-dim');
    const related = edge.el.classList.contains('cfx-graph-neighborhood-related');
    const selected = edge.el.classList.contains('cfx-graph-selected') || edge.el.classList.contains('cfx-graph-highlighted');
    const baseWidth = dense ? Math.max(.65, Math.min(1.8, edge.weight * .55)) : edge.weight;
    const emphasis = selected ? 1.6 : related ? 1.2 : 0;
    const width = selected && edge.selectionWidth || edge.el.classList.contains('cfx-graph-hovered') && edge.hoverWidth ||
      (edge.strokeWidth > 0 ? Math.max(.65, edge.strokeWidth + emphasis) : Math.max(.65, Math.min(selected ? 6 : related ? 4 : dense ? 1.8 : 4, baseWidth + emphasis)));
    return { rendered, control, width, dimmed, labelObstacles, paper: palette.paper,
      color: selected ? palette.selected : related ? '#14b8a6' : edge.strokeColor || palette.edge,
      alpha: (dimmed ? .1 : selected ? .95 : related ? .86 : dense ? .28 : .58) * graphLinkOpacity(edge.el),
      arrowAlpha: (dimmed ? .14 : selected || related ? 1 : dense ? .34 : 1) * graphLinkOpacity(edge.el),
      arrows: !moving || selected || related,
      labels: (!moving || selected || related) && edge.label && edge.showLabel &&
        (!root.classList.contains('cfx-graph-lod-hide-edge-labels') || selected || related) &&
        (!root.classList.contains('cfx-graph-priority-overview') || selected || related) };
  };
  const drawCanvasEdgeLabel = (context, root, edge, paint, palette) => {
    if (!paint.labels) return;
    const label = edgeLabelPoint(paint.rendered, paint.control, paint.labelObstacles);
    context.font = `11px ${graphNodeFontFamily(root)}`;
    context.textAlign = 'center';
    context.textBaseline = 'middle';
    context.lineWidth = 4;
    context.strokeStyle = palette.halo;
    context.fillStyle = graphAdaptiveTextColor(root, edge.labelColor, palette.edgeLabel);
    context.globalAlpha = (paint.dimmed ? .16 : 1) * graphLinkOpacity(edge.el);
    context.strokeText(edge.label, label.x, label.y);
    context.fillText(edge.label, label.x, label.y);
    context.globalAlpha = 1;
  };
  const drawCanvasEdge = (context, root, edge, paint, palette) => {
    edgeDrawPath(context, paint.rendered, paint.control);
    context.strokeStyle = paint.color;
    context.globalAlpha = paint.alpha;
    context.lineWidth = paint.width;
    const pattern = edge.dashed ? edge.dashPattern : [];
    const matrix = pattern.length && context.getTransform();
    const pixelScale = matrix ? Math.max(Math.hypot(matrix.a, matrix.b), Math.hypot(matrix.c, matrix.d)) : 1;
    // Round caps overlap subpixel gaps. Avoid asking the native stroker to
    // construct millions of indistinguishable dash fragments at this zoom.
    const covered = context.lineCap === 'round' && pattern.length > 0 &&
      pattern.reduce((sum, part) => sum + part, 0) * (pattern.length % 2 ? 2 : 1) * pixelScale < .5 &&
      pattern.every(part => part <= paint.width);
    context.setLineDash(covered ? [] : pattern);
    // SVG and dependency-free PNG use the SVG default miter limit of four.
    // Restore the context policy before drawing node marks and labels.
    const miterLimit = context.miterLimit;
    context.miterLimit = 4;
    context.stroke();
    context.miterLimit = miterLimit;
    context.setLineDash([]);
    context.globalAlpha = 1;
    if (paint.arrows) {
      context.globalAlpha = paint.arrowAlpha;
      edgeArrowMarks(edge, paint).forEach(mark => {
        context.beginPath();
        mark.points.forEach((point, index) => index ? context.lineTo(point.x, point.y) : context.moveTo(point.x, point.y));
        context.closePath();
        context.fillStyle = mark.paper ? paint.paper : paint.color;
        context.fill();
      });
      context.globalAlpha = 1;
    }
    drawCanvasEdgeLabel(context, root, edge, paint, palette);
  };
  // (back, across) pairs in arrow sizes from the tip; later polygons are paper. EdgeArrows.cs mirrors this table.
  const graphArrowShapes = { arrow: [[0, 0, .8660254, .5, .8660254, -.5]], vee: [[0, 0, 1.1, .6, .65, 0, 1.1, -.6]], triangle: [[0, 0, 1.2, .6, 1.2, -.6], [.4, 0, 1.02, .31, 1.02, -.31]],
    bar: [[0, .7, .3, .7, .3, -.7, 0, -.7]], diamond: [[0, 0, .7, .5, 1.4, 0, .7, -.5]],
    circle: [Array.from({ length: 16 }, (_, i) => [.65 - .65 * Math.cos(i * Math.PI / 8), .65 * Math.sin(i * Math.PI / 8)]).flat()] };
  // Every renderer and export paints these marks in source, middle, target order, sized from the authored width.
  const edgeArrowMarks = (edge, { rendered, control }) => {
    const [source = 'arrow', target = 'arrow', middle, scale] = edge.arrowStyle || [], size = (+scale || 1) * (6 + 1.7 * (edge.strokeWidth || 1.5)), marks = [];
    [['source', edge.sourceArrow, source], ['middle', middle, middle], ['target', edge.targetArrow || edge.directed, target]].forEach(([side, on, shape]) => {
      if (!on) return;
      const end = side !== 'middle', polygons = graphArrowShapes[shape] || graphArrowShapes.arrow;
      const f = end ? edgeArrowFrame(rendered, control, side) : edgeMiddleFrame(rendered, control);
      const shift = end ? 0 : Math.max(...polygons[0].filter((_, i) => i % 2 === 0)) / 2;
      polygons.forEach((p, index) => {
        const points = [];
        for (let i = 0; i < p.length; i += 2) points.push({ x: f.x + (f.ux * (shift - p[i]) - f.uy * p[i + 1]) * size, y: f.y + (f.uy * (shift - p[i]) + f.ux * p[i + 1]) * size });
        marks.push({ side, points, paper: index > 0 });
      });
    });
    return marks;
  };