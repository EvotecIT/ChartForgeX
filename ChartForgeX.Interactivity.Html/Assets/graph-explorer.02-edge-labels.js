  // Uses the clipped painted path, retaining the same baseline lift as midpoint captions.
  const graphEdgeCaptionPathPoint = (edge, control, fraction, offset) => {
    const ends = edgeRenderEndpoints(edge, control), t = fraction, u = 1 - t;
    const x = control ? u * u * ends.source.x + 2 * u * t * control.x + t * t * ends.target.x : ends.source.x + (ends.target.x - ends.source.x) * t;
    const y = control ? u * u * ends.source.y + 2 * u * t * control.y + t * t * ends.target.y : ends.source.y + (ends.target.y - ends.source.y) * t;
    const dx = control ? 2 * u * (control.x - ends.source.x) + 2 * t * (ends.target.x - control.x) : ends.target.x - ends.source.x;
    const dy = control ? 2 * u * (control.y - ends.source.y) + 2 * t * (ends.target.y - control.y) : ends.target.y - ends.source.y;
    const length = Math.max(.001, Math.hypot(dx, dy));
    return { x: x - dy / length * offset, y: y + dx / length * offset - 7 };
  };
  const graphEstimatedEdgeLabelWidth = value => {
    let width = 8;
    Array.from(String(value || '')).forEach(character => {
      width += character === ' ' ? 3.2
        : '.,:;!|'.includes(character) ? 3.6
          : 'MW@%&'.includes(character) ? 9
            : /[A-Z]/.test(character) ? 7.2
              : /[0-9]/.test(character) ? 6.2
                : 5.8;
    });
    return width;
  };
  const avoidEdgeLabelNodeCollisions = (edge, candidate, obstacles, control) => {
    const label = graphLabelText(edge.label) || graphLabelText(attr(edge.el, 'data-edge-label'));
    const intersects = point => edgeLabelIntersectsObstacles(point, label, obstacles);
    if (!label || !intersects(candidate)) return candidate;
    if (edgeHasRoute(edge)) {
      const points = routeRenderPoints(edge);
      for (const fraction of [.45, .55, .35, .65, .25, .75]) {
        const point = routePointAt(points, fraction, -7);
        if (!intersects(point)) return point;
      }
      return candidate;
    }
    // Search the painted line/curve before moving a caption away from it.
    // A clear midpoint normal can otherwise be hundreds of units from its edge.
    for (const offset of [0, -12, 12, -24, 24, -36, 36, -48, 48, -60, 60]) {
      for (const fraction of [.45, .55, .35, .65, .25, .75, .15, .85]) {
        const point = graphEdgeCaptionPathPoint(edge, control, fraction, offset);
        if (!intersects(point)) return point;
      }
    }
    const dx = edge.target.x - edge.source.x;
    const dy = edge.target.y - edge.source.y;
    const distance = Math.sqrt(dx * dx + dy * dy);
    const normal = distance < .001 ? { x: 0, y: -1 } : { x: -dy / distance, y: dx / distance };
    const preferredSign = normal.y > 0 ? -1 : 1;
    const offsets = [20, 36, 54, 76, 102, 136, 176, 220, 280];
    for (const offset of offsets) {
      const preferred = { x: candidate.x + normal.x * offset * preferredSign, y: candidate.y + normal.y * offset * preferredSign };
      if (!intersects(preferred)) return preferred;
      const alternate = { x: candidate.x - normal.x * offset * preferredSign, y: candidate.y - normal.y * offset * preferredSign };
      if (!intersects(alternate)) return alternate;
    }
    const offset = offsets[offsets.length - 1];
    return { x: candidate.x + normal.x * offset * preferredSign, y: candidate.y + normal.y * offset * preferredSign };
  };
  const edgeLabelPoint = (edge, control, obstacles) => {
    if (edge.source === edge.target) return selfLoopGeometry(edge.source).label;
    if (edgeHasRoute(edge)) return avoidEdgeLabelNodeCollisions(edge, routeMidpoint(routeRenderPoints(edge), -7), obstacles);
    const endpoints = edgeRenderEndpoints(edge, control);
    const point = control
      ? { x: (endpoints.source.x + 2 * control.x + endpoints.target.x) / 4, y: (endpoints.source.y + 2 * control.y + endpoints.target.y) / 4 - 7 }
      : { x: (endpoints.source.x + endpoints.target.x) / 2, y: (endpoints.source.y + endpoints.target.y) / 2 - 7 };
    return avoidEdgeLabelNodeCollisions(edge, point, obstacles, control);
  };
