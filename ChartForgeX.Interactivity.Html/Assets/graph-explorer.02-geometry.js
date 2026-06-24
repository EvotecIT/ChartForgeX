  const nodeHalfWidth = (node) => node.shape === 'box' ? node.size * 1.45 : node.size;
  const nodeHalfHeight = (node) => node.shape === 'box' ? node.size * 1.05 : node.size;
  const nodeBoundaryInset = (node, unitX, unitY) => {
    const size = Math.max(4, node?.size || 8);
    if (node?.shape === 'box') {
      if (Math.abs(unitX) < 0.001 && Math.abs(unitY) < 0.001) return Math.max(6, Math.max(size * 1.45, size * 1.05) + 7);
      const xInset = Math.abs(unitX) < 0.001 ? Number.POSITIVE_INFINITY : size * 1.45 / Math.abs(unitX);
      const yInset = Math.abs(unitY) < 0.001 ? Number.POSITIVE_INFINITY : size * 1.05 / Math.abs(unitY);
      return Math.max(6, Math.min(xInset, yInset) + 7);
    }
    return Math.max(6, size + (node?.shape === 'image' ? 11 : 7));
  };
  const nodeHitDistance = (node, point, tolerance) => {
    const slack = tolerance ?? 10;
    const dx = Math.abs(node.x - point.x);
    const dy = Math.abs(node.y - point.y);
    if (node.shape === 'box') {
      const outsideX = Math.max(0, dx - nodeHalfWidth(node));
      const outsideY = Math.max(0, dy - nodeHalfHeight(node));
      if (dx <= nodeHalfWidth(node) + slack && dy <= nodeHalfHeight(node) + slack) return Math.sqrt(outsideX * outsideX + outsideY * outsideY);
      return Number.POSITIVE_INFINITY;
    }
    const distance = Math.sqrt(dx * dx + dy * dy);
    return distance <= node.size + slack ? distance : Number.POSITIVE_INFINITY;
  };
  const edgeControl = (edge) => {
    if (edge.source === edge.target) return null;
    if (edge.shape !== 'curve' && Math.abs(edge.curvature) < 0.001) return null;
    const dx = edge.target.x - edge.source.x;
    const dy = edge.target.y - edge.source.y;
    const length = Math.max(1, Math.sqrt(dx * dx + dy * dy));
    const offset = Math.abs(edge.curvature) < 0.001 ? 34 : edge.curvature;
    return { x: (edge.source.x + edge.target.x) / 2 - dy / length * offset, y: (edge.source.y + edge.target.y) / 2 + dx / length * offset };
  };
  const edgeRenderSource = (edge, control) => {
    if (!edge.sourceCollapsed) return edge.source;
    const to = control || edge.target;
    const dx = to.x - edge.source.x, dy = to.y - edge.source.y;
    const length = Math.max(1, Math.sqrt(dx * dx + dy * dy));
    const inset = nodeBoundaryInset(edge.source, dx / length, dy / length);
    return { x: edge.source.x + dx / length * inset, y: edge.source.y + dy / length * inset };
  };
  const edgeRenderTarget = (edge, control) => {
    if (!edge.directed && !edge.targetCollapsed) return edge.target;
    const from = control || edge.source;
    const dx = edge.target.x - from.x, dy = edge.target.y - from.y;
    const length = Math.max(1, Math.sqrt(dx * dx + dy * dy));
    const inset = nodeBoundaryInset(edge.target, dx / length, dy / length);
    return { x: edge.target.x - dx / length * inset, y: edge.target.y - dy / length * inset };
  };
  const edgeRenderEndpoints = (edge, control) => ({ source: edgeRenderSource(edge, control), target: edgeRenderTarget(edge, control) });
  const selfLoopGeometry = (node) => {
    const right = nodeBoundaryInset(node, 1, 0) + 5;
    const left = nodeBoundaryInset(node, -1, 0) + 5;
    const top = nodeBoundaryInset(node, 0, -1) + 42;
    return {
      start: { x: node.x + right, y: node.y },
      c1: { x: node.x + right + 44, y: node.y - top },
      c2: { x: node.x - left - 44, y: node.y - top },
      end: { x: node.x - left, y: node.y },
      label: { x: node.x, y: node.y - top - 7 }
    };
  };
  const selfLoopPath = (node) => {
    const loop = selfLoopGeometry(node);
    return `M ${loop.start.x.toFixed(3)} ${loop.start.y.toFixed(3)} C ${loop.c1.x.toFixed(3)} ${loop.c1.y.toFixed(3)} ${loop.c2.x.toFixed(3)} ${loop.c2.y.toFixed(3)} ${loop.end.x.toFixed(3)} ${loop.end.y.toFixed(3)}`;
  };
  const edgeLabelPoint = (edge, control) => control
    ? { x: (edge.source.x + 2 * control.x + edge.target.x) / 4, y: (edge.source.y + 2 * control.y + edge.target.y) / 4 - 7 }
    : edge.source === edge.target
      ? selfLoopGeometry(edge.source).label
      : { x: (edge.source.x + edge.target.x) / 2, y: (edge.source.y + edge.target.y) / 2 - 7 };
  const drawArrow = (context, edge, control) => {
    const loop = edge.source === edge.target ? selfLoopGeometry(edge.source) : null;
    const from = loop?.c2 || control || edge.source;
    const target = loop?.end || edge.target;
    const dx = target.x - from.x;
    const dy = target.y - from.y;
    const length = Math.max(1, Math.sqrt(dx * dx + dy * dy));
    const unitX = dx / length;
    const unitY = dy / length;
    const angle = Math.atan2(unitY, unitX);
    const size = 8;
    const inset = loop ? 0 : nodeBoundaryInset(edge.target, unitX, unitY);
    const x = target.x - unitX * inset;
    const y = target.y - unitY * inset;
    context.beginPath();
    context.moveTo(x, y);
    context.lineTo(x - Math.cos(angle - Math.PI / 6) * size, y - Math.sin(angle - Math.PI / 6) * size);
    context.lineTo(x - Math.cos(angle + Math.PI / 6) * size, y - Math.sin(angle + Math.PI / 6) * size);
    context.closePath();
    context.fillStyle = '#64748b';
    context.fill();
  };
