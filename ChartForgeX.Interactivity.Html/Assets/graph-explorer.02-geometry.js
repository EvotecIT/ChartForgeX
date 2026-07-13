  const nodeShapeExtents = (node) => {
    const size = Math.max(4, node?.size || 8);
    if (node?.shape === 'box') return { x: size * 1.45, y: size * 1.05 };
    if (node?.shape === 'imageRect') return { x: size * 1.3, y: size * .9 };
    if (node?.shape === 'ellipse') return { x: size * 1.55, y: size };
    if (node?.shape === 'square') return { x: size, y: size };
    if (node?.shape === 'diamond') return { x: size * 1.35, y: size * 1.35 };
    if (node?.shape === 'triangle' || node?.shape === 'triangleDown') return { x: size * 1.25, y: size * 1.35 };
    if (node?.shape === 'star') return { x: size * 1.4, y: size * 1.45 };
    if (node?.shape === 'database') return { x: size * 1.25, y: size * .9 };
    if (node?.shape === 'text') return { x: Math.max(size, ((attr(node.el, 'data-node-label') || node.id || '').length * 6) / 2), y: Math.max(size * .75, 8) };
    return { x: size, y: size };
  };
  const nodeUsesRectangularGeometry = (node) => ['box', 'imageRect', 'ellipse', 'square', 'diamond', 'triangle', 'triangleDown', 'star', 'database', 'text'].includes(node?.shape);
  const nodePolygonPoints = (shape, size) => {
    if (shape === 'diamond') return `0,${-size * 1.35} ${size * 1.35},0 0,${size * 1.35} ${-size * 1.35},0`;
    if (shape === 'triangle') return `0,${-size * 1.35} ${size * 1.25},${size * 1.05} ${-size * 1.25},${size * 1.05}`;
    if (shape === 'triangleDown') return `${-size * 1.25},${-size * 1.05} ${size * 1.25},${-size * 1.05} 0,${size * 1.35}`;
    return `0,${-size * 1.45} ${size * .42},${-size * .45} ${size * 1.4},${-size * .45} ${size * .62},${size * .18} ${size * .9},${size * 1.25} 0,${size * .64} ${-size * .9},${size * 1.25} ${-size * .62},${size * .18} ${-size * 1.4},${-size * .45} ${-size * .42},${-size * .45}`;
  };
  const nodeDatabasePath = (size) => {
    const width = size * 1.25, top = -size * .55, bottom = size * .55, radius = size * .38;
    return `M ${-width} ${top} C ${-width} ${top - radius} ${width} ${top - radius} ${width} ${top} L ${width} ${bottom} C ${width} ${bottom + radius} ${-width} ${bottom + radius} ${-width} ${bottom} Z M ${-width} ${top} C ${-width} ${top + radius} ${width} ${top + radius} ${width} ${top}`;
  };
  const nodeHalfWidth = (node) => nodeShapeExtents(node).x;
  const nodeHalfHeight = (node) => nodeShapeExtents(node).y;
  const nodeLayoutExtents = (node) => {
    const mark = nodeShapeExtents(node);
    const label = attr(node.el, 'data-node-label') || node.label || node.id || '';
    const secondary = attr(node.el, 'data-node-secondary-label') || node.secondaryLabel || '';
    const labelHalfWidth = Math.max(24, Math.min(132, label.length * 3.5 + 10), secondary ? Math.min(132, secondary.length * 2.8 + 8) : 0);
    const labelBottom = node.shape === 'text' ? 9 + (secondary ? 15 : 0) : Math.max(4, node.size || 8) + 24 + (secondary ? 15 : 0);
    return { x: Math.max(mark.x + 5, labelHalfWidth), y: Math.max(mark.y + 5, labelBottom) };
  };
  const nodeLayoutRadius = (node) => Math.max(6, Math.max(nodeLayoutExtents(node).x, nodeLayoutExtents(node).y));
  const nodeCollisionRadius = (node, includeLabels) => includeLabels ? nodeLayoutRadius(node) : Math.max(6, Math.max(nodeHalfWidth(node), nodeHalfHeight(node)) + 5);
  const nodeBoundaryInset = (node, unitX, unitY) => {
    const size = Math.max(4, node?.size || 8);
    if (nodeUsesRectangularGeometry(node)) {
      const { x: halfWidth, y: halfHeight } = nodeShapeExtents(node);
      if (Math.abs(unitX) < 0.001 && Math.abs(unitY) < 0.001) return Math.max(6, Math.max(halfWidth, halfHeight) + 7);
      const xInset = Math.abs(unitX) < 0.001 ? Number.POSITIVE_INFINITY : halfWidth / Math.abs(unitX);
      const yInset = Math.abs(unitY) < 0.001 ? Number.POSITIVE_INFINITY : halfHeight / Math.abs(unitY);
      return Math.max(6, Math.min(xInset, yInset) + 7);
    }
    return Math.max(6, size + (node?.shape === 'image' ? 11 : 7));
  };
  const nodeHitDistance = (node, point, tolerance) => {
    const slack = tolerance ?? 10;
    const dx = Math.abs(node.x - point.x);
    const dy = Math.abs(node.y - point.y);
    if (nodeUsesRectangularGeometry(node)) {
      const outsideX = Math.max(0, dx - nodeHalfWidth(node));
      const outsideY = Math.max(0, dy - nodeHalfHeight(node));
      if (dx <= nodeHalfWidth(node) + slack && dy <= nodeHalfHeight(node) + slack) return Math.sqrt(outsideX * outsideX + outsideY * outsideY);
      return Number.POSITIVE_INFINITY;
    }
    const distance = Math.sqrt(dx * dx + dy * dy);
    return distance <= node.size + slack ? distance : Number.POSITIVE_INFINITY;
  };
  const canvasPolygon = (context, node, points) => {
    context.beginPath();
    points.forEach((point, index) => index ? context.lineTo(node.x + point[0] * node.size, node.y + point[1] * node.size) : context.moveTo(node.x + point[0] * node.size, node.y + point[1] * node.size));
    context.closePath();
  };
  const drawNodeShapeMark = (context, node) => {
    const size = node.size;
    if (node.shape === 'ellipse') {
      context.beginPath(); context.ellipse(node.x, node.y, size * 1.55, size, 0, 0, Math.PI * 2); context.fill(); context.stroke();
    } else if (node.shape === 'square') {
      context.beginPath(); context.rect(node.x - size, node.y - size, size * 2, size * 2); context.fill(); context.stroke();
    } else if (node.shape === 'diamond') {
      canvasPolygon(context, node, [[0, -1.35], [1.35, 0], [0, 1.35], [-1.35, 0]]); context.fill(); context.stroke();
    } else if (node.shape === 'triangle') {
      canvasPolygon(context, node, [[0, -1.35], [1.25, 1.05], [-1.25, 1.05]]); context.fill(); context.stroke();
    } else if (node.shape === 'triangleDown') {
      canvasPolygon(context, node, [[-1.25, -1.05], [1.25, -1.05], [0, 1.35]]); context.fill(); context.stroke();
    } else if (node.shape === 'star') {
      canvasPolygon(context, node, [[0, -1.45], [.42, -.45], [1.4, -.45], [.62, .18], [.9, 1.25], [0, .64], [-.9, 1.25], [-.62, .18], [-1.4, -.45], [-.42, -.45]]); context.fill(); context.stroke();
    } else if (node.shape === 'database') {
      const width = size * 1.25, height = size * 1.45;
      context.beginPath(); context.rect(node.x - width, node.y - height * .35, width * 2, height * .7); context.fill(); context.stroke();
      context.beginPath(); context.ellipse(node.x, node.y - height * .35, width, size * .38, 0, 0, Math.PI * 2); context.fill(); context.stroke();
      context.beginPath(); context.ellipse(node.x, node.y + height * .35, width, size * .38, 0, 0, Math.PI); context.stroke();
    } else if (node.shape === 'text') {
      return;
    } else {
      context.beginPath(); context.arc(node.x, node.y, size, 0, Math.PI * 2); context.fill(); context.stroke();
    }
  };
  const edgeControl = (edge) => {
    if (edge.source === edge.target) return null;
    if (edgeHasRoute(edge)) return null;
    if ((edge.shape === 'line' || edge.shape === 'polyline') && Math.abs(edge.curvature) < 0.001) return null;
    const dx = edge.target.x - edge.source.x;
    const dy = edge.target.y - edge.source.y;
    const length = Math.max(1, Math.sqrt(dx * dx + dy * dy));
    const offset = Math.abs(edge.curvature) < 0.001 ? 34 : edge.curvature;
    return { x: (edge.source.x + edge.target.x) / 2 - dy / length * offset, y: (edge.source.y + edge.target.y) / 2 + dx / length * offset };
  };
  const edgeHasRoute = (edge) => Array.isArray(edge.routePoints) && edge.routePoints.length > 1 && !edge.sourceCollapsed && !edge.targetCollapsed;
  const routeMidpoint = (points, yOffset) => {
    let total = 0;
    for (let i = 1; i < points.length; i++) total += Math.hypot(points[i].x - points[i - 1].x, points[i].y - points[i - 1].y);
    if (total <= 0) return { x: points[0].x, y: points[0].y + yOffset };
    let walked = 0;
    for (let i = 1; i < points.length; i++) {
      const length = Math.hypot(points[i].x - points[i - 1].x, points[i].y - points[i - 1].y);
      if (walked + length >= total / 2) {
        const ratio = length <= 0 ? 0 : (total / 2 - walked) / length;
        return { x: points[i - 1].x + (points[i].x - points[i - 1].x) * ratio, y: points[i - 1].y + (points[i].y - points[i - 1].y) * ratio + yOffset };
      }
      walked += length;
    }
    return { x: points[points.length - 1].x, y: points[points.length - 1].y + yOffset };
  };
  const edgeRenderSource = (edge, control) => {
    if (!edge.sourceCollapsed && !edge.sourceArrow) return edge.source;
    const to = control || edge.target;
    const dx = to.x - edge.source.x, dy = to.y - edge.source.y;
    const length = Math.max(1, Math.sqrt(dx * dx + dy * dy));
    const inset = nodeBoundaryInset(edge.source, dx / length, dy / length);
    return { x: edge.source.x + dx / length * inset, y: edge.source.y + dy / length * inset };
  };
  const edgeRenderTarget = (edge, control) => {
    const targetArrow = edge.targetArrow || edge.directed;
    if (!edge.targetCollapsed && !targetArrow) return edge.target;
    const from = control || edge.source;
    const dx = edge.target.x - from.x, dy = edge.target.y - from.y;
    const length = Math.max(1, Math.sqrt(dx * dx + dy * dy));
    const inset = nodeBoundaryInset(edge.target, dx / length, dy / length);
    return { x: edge.target.x - dx / length * inset, y: edge.target.y - dy / length * inset };
  };
  const edgeRenderEndpoints = (edge, control) => ({ source: edgeRenderSource(edge, control), target: edgeRenderTarget(edge, control) });
  const routeEndpointFromNode = (endpoint, node, guide, trim) => {
    const homeX = Number.isFinite(node.homeX) ? node.homeX : node.x, homeY = Number.isFinite(node.homeY) ? node.homeY : node.y;
    if (!trim) return { x: endpoint.x + node.x - homeX, y: endpoint.y + node.y - homeY };
    const dx = guide.x - node.x, dy = guide.y - node.y;
    const length = Math.max(1, Math.sqrt(dx * dx + dy * dy));
    const inset = nodeBoundaryInset(node, dx / length, dy / length);
    return { x: node.x + dx / length * inset, y: node.y + dy / length * inset };
  };
  const routeEndpointToNode = (endpoint, node, guide, trim) => {
    const homeX = Number.isFinite(node.homeX) ? node.homeX : node.x, homeY = Number.isFinite(node.homeY) ? node.homeY : node.y;
    if (!trim) return { x: endpoint.x + node.x - homeX, y: endpoint.y + node.y - homeY };
    const dx = node.x - guide.x, dy = node.y - guide.y;
    const length = Math.max(1, Math.sqrt(dx * dx + dy * dy));
    const inset = nodeBoundaryInset(node, dx / length, dy / length);
    return { x: node.x - dx / length * inset, y: node.y - dy / length * inset };
  };
  const routeRenderPoints = (edge) => {
    if (!edgeHasRoute(edge)) return edge.routePoints || [];
    const points = edge.routePoints.map(point => ({ x: point.x, y: point.y }));
    const targetArrow = edge.targetArrow || edge.directed;
    points[0] = routeEndpointFromNode(points[0], edge.source, points[1] || edge.target, edge.sourceCollapsed || edge.sourceArrow);
    points[points.length - 1] = routeEndpointToNode(points[points.length - 1], edge.target, points[points.length - 2] || edge.source, edge.targetCollapsed || targetArrow);
    return points;
  };
  const edgePathData = (edge, control) => {
    if (edge.source === edge.target) return selfLoopPath(edge.target);
    if (edgeHasRoute(edge)) return routeRenderPoints(edge).map((point, index) => `${index ? 'L' : 'M'} ${point.x.toFixed(3)} ${point.y.toFixed(3)}`).join(' ');
    const endpoints = edgeRenderEndpoints(edge, control);
    return control
      ? `M ${endpoints.source.x.toFixed(3)} ${endpoints.source.y.toFixed(3)} Q ${control.x.toFixed(3)} ${control.y.toFixed(3)} ${endpoints.target.x.toFixed(3)} ${endpoints.target.y.toFixed(3)}`
      : `M ${endpoints.source.x.toFixed(3)} ${endpoints.source.y.toFixed(3)} L ${endpoints.target.x.toFixed(3)} ${endpoints.target.y.toFixed(3)}`;
  };
  const edgeDrawPath = (context, edge, control) => {
    context.beginPath();
    if (edge.source === edge.target) {
      const loop = selfLoopGeometry(edge.source);
      context.moveTo(loop.start.x, loop.start.y);
      context.bezierCurveTo(loop.c1.x, loop.c1.y, loop.c2.x, loop.c2.y, loop.end.x, loop.end.y);
    } else if (edgeHasRoute(edge)) {
      routeRenderPoints(edge).forEach((point, index) => index ? context.lineTo(point.x, point.y) : context.moveTo(point.x, point.y));
    } else {
      const endpoints = edgeRenderEndpoints(edge, control);
      context.moveTo(endpoints.source.x, endpoints.source.y);
      if (control) context.quadraticCurveTo(control.x, control.y, endpoints.target.x, endpoints.target.y);
      else context.lineTo(endpoints.target.x, endpoints.target.y);
    }
  };
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
  const edgeLabelPoint = (edge, control) => {
    if (edge.source === edge.target) return selfLoopGeometry(edge.source).label;
    if (edgeHasRoute(edge)) return routeMidpoint(routeRenderPoints(edge), -7);
    const endpoints = edgeRenderEndpoints(edge, control);
    return control
      ? { x: (endpoints.source.x + 2 * control.x + endpoints.target.x) / 4, y: (endpoints.source.y + 2 * control.y + endpoints.target.y) / 4 - 7 }
      : { x: (endpoints.source.x + endpoints.target.x) / 2, y: (endpoints.source.y + endpoints.target.y) / 2 - 7 };
  };
  const edgeArrowGeometry = (edge, control, side, size = 8) => {
    const loop = edge.source === edge.target ? selfLoopGeometry(edge.source) : null;
    const route = edgeHasRoute(edge) ? routeRenderPoints(edge) : null;
    const sourceSide = side === 'source';
    const from = route ? (sourceSide ? route[1] : route[route.length - 2]) : sourceSide ? (loop?.c1 || control || edge.target) : loop?.c2 || control || edge.source;
    const target = route ? (sourceSide ? route[0] : route[route.length - 1]) : sourceSide ? (loop?.start || edge.source) : loop?.end || edge.target;
    const dx = target.x - from.x;
    const dy = target.y - from.y;
    const length = Math.max(1, Math.sqrt(dx * dx + dy * dy));
    const unitX = dx / length;
    const unitY = dy / length;
    const angle = Math.atan2(unitY, unitX);
    const inset = loop || route ? 0 : nodeBoundaryInset(sourceSide ? edge.source : edge.target, unitX, unitY);
    const x = target.x - unitX * inset;
    const y = target.y - unitY * inset;
    return {
      tip: { x, y },
      left: { x: x - Math.cos(angle - Math.PI / 6) * size, y: y - Math.sin(angle - Math.PI / 6) * size },
      right: { x: x - Math.cos(angle + Math.PI / 6) * size, y: y - Math.sin(angle + Math.PI / 6) * size }
    };
  };
  const drawArrow = (context, edge, control, side, color) => {
    const arrow = edgeArrowGeometry(edge, control, side);
    context.beginPath();
    context.moveTo(arrow.tip.x, arrow.tip.y);
    context.lineTo(arrow.left.x, arrow.left.y);
    context.lineTo(arrow.right.x, arrow.right.y);
    context.closePath();
    context.fillStyle = color || '#64748b';
    context.fill();
  };
