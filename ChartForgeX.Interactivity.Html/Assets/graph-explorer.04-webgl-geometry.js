  // Sample the same trimmed routes and curves used by Canvas and SVG. Curves have
  // a bounded tessellation cost and tighten with display scale, including high DPI.
  const webGlEdgePoints = (edge, control, scale) => {
    if (edgeHasRoute(edge)) return routeRenderPoints(edge);
    const loop = edge.source === edge.target ? selfLoopGeometry(edge.source) : null;
    const ends = loop ? { source: loop.start, target: loop.end } : edgeRenderEndpoints(edge, control);
    if (!loop && !control) return [ends.source, ends.target];
    const bend = loop ? Math.hypot(loop.c1.x - loop.c2.x, loop.c1.y - loop.c2.y)
      : Math.hypot(ends.source.x - 2 * control.x + ends.target.x, ends.source.y - 2 * control.y + ends.target.y);
    const steps = Math.max(4, Math.min(64, Math.ceil(Math.sqrt(Math.max(1, bend * scale) * 2))));
    const points = [];
    for (let index = 0; index <= steps; index++) {
      const t = index / steps, u = 1 - t;
      points.push(loop ? {
        x: u * u * u * ends.source.x + 3 * u * u * t * loop.c1.x + 3 * u * t * t * loop.c2.x + t * t * t * ends.target.x,
        y: u * u * u * ends.source.y + 3 * u * u * t * loop.c1.y + 3 * u * t * t * loop.c2.y + t * t * t * ends.target.y
      } : {
        x: u * u * ends.source.x + 2 * u * t * control.x + t * t * ends.target.x,
        y: u * u * ends.source.y + 2 * u * t * control.y + t * t * ends.target.y
      });
    }
    return points;
  };
  const webGlDashedPaths = (points, pattern) => {
    const dash = (pattern || []).filter(value => Number.isFinite(value) && value >= 0);
    if (!dash.length || !dash.some(value => value > 0)) return [points];
    if (dash.length % 2) dash.push(...dash);
    const total = points.slice(1).reduce((sum, point, index) => sum + Math.hypot(point.x - points[index].x, point.y - points[index].y), 0);
    // Extremely fine patterns stay in the native Canvas stroker rather than
    // producing unbounded JavaScript geometry. Normal routes remain on the GPU.
    if (total / dash.reduce((sum, value) => sum + value, 0) * dash.length > 4096) return null;
    const paths = [];
    let phase = 0, remaining = dash[0], current = null;
    const advance = () => {
      do { phase = (phase + 1) % dash.length; remaining = dash[phase]; } while (remaining === 0);
      if (phase % 2) current = null;
    };
    if (remaining === 0) advance();
    for (let index = 1; index < points.length; index++) {
      const a = points[index - 1], b = points[index], length = Math.hypot(b.x - a.x, b.y - a.y);
      if (length <= 1e-8) continue;
      let walked = 0;
      while (walked < length - 1e-8) {
        const step = Math.min(remaining, length - walked);
        const start = { x: a.x + (b.x - a.x) * walked / length, y: a.y + (b.y - a.y) * walked / length };
        walked += step;
        const end = { x: a.x + (b.x - a.x) * walked / length, y: a.y + (b.y - a.y) * walked / length };
        if (phase % 2 === 0) {
          if (!current) { current = [start]; paths.push(current); }
          current.push(end);
        }
        remaining -= step;
        if (remaining <= 1e-8) advance();
      }
    }
    return paths;
  };
  const webGlTriangle = (mesh, a, b, c, color) => {
    mesh.positions.push(a.x, a.y, b.x, b.y, c.x, c.y);
    mesh.colors.push(...color, ...color, ...color);
  };
  const webGlStrokePath = (mesh, input, width, color, caps = true) => {
    const points = input.filter((point, index) => !index || Math.hypot(point.x - input[index - 1].x, point.y - input[index - 1].y) > 1e-8);
    if (points.length < 2) return;
    const half = width / 2, normals = [];
    for (let index = 1; index < points.length; index++) {
      const a = points[index - 1], b = points[index], length = Math.hypot(b.x - a.x, b.y - a.y);
      normals.push({ x: -(b.y - a.y) / length, y: (b.x - a.x) / length });
    }
    const sides = points.map((point, index) => {
      const previous = normals[Math.max(0, index - 1)], next = normals[Math.min(index, normals.length - 1)];
      const dx = previous.x + next.x, dy = previous.y + next.y, length = Math.hypot(dx, dy);
      const unit = length < 1e-8 ? next : { x: dx / length, y: dy / length };
      const factor = half / Math.max(.5, unit.x * next.x + unit.y * next.y);
      return { left: { x: point.x + unit.x * factor, y: point.y + unit.y * factor }, right: { x: point.x - unit.x * factor, y: point.y - unit.y * factor } };
    });
    for (let index = 1; index < sides.length; index++) {
      const a = sides[index - 1], b = sides[index];
      webGlTriangle(mesh, a.left, a.right, b.left, color);
      webGlTriangle(mesh, b.left, a.right, b.right, color);
    }
    // During dense motion, caps smaller than a device pixel do not need extra triangles.
    if (!caps) return;
    // Round caps meet the ribbon without overlapping translucent interiors.
    for (const index of [0, points.length - 1]) {
      const center = points[index], normal = normals[index === 0 ? 0 : normals.length - 1];
      const angle = Math.atan2(normal.y, normal.x) + (index === 0 ? 0 : Math.PI);
      const steps = width < 2 ? 2 : 6;
      for (let step = 0; step < steps; step++) {
        const a = angle + step * Math.PI / steps, b = angle + (step + 1) * Math.PI / steps;
        webGlTriangle(mesh, center, { x: center.x + Math.cos(a) * half, y: center.y + Math.sin(a) * half }, { x: center.x + Math.cos(b) * half, y: center.y + Math.sin(b) * half }, color);
      }
    }
  };
