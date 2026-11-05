  // Polar readers trace an observation with a ring at its radius and a spoke from the centre,
  // reading its angle beyond the outer edge and its radius on the ring, inside the card and clear of the mark.
  const polarGuideTarget = node => !!node?.matches?.('[data-cfx-role="radar-point-source"],[data-cfx-role="polar-point-source"]');
  const showPolarCrosshair = (root, crosshair, node, event, nativeTarget) => {
    const stage = root.querySelector('.cfx-stage'), svg = stage?.querySelector('svg');
    const spoke = root.querySelector('[data-cfx-role="radar-spoke"],[data-cfx-role="polar-spoke"]'), p = capturedCartesianMetadata(root).polarProjection;
    const matrix = (spoke || svg) && svgScreenMatrix(spoke || svg), box = node.getBoundingClientRect();
    const ends = spoke ? ['x1', 'y1', 'x2', 'y2'].map(name => +spoke.getAttribute(name)) : p && [p.centerX, p.centerY, p.centerX + p.outerRadius, p.centerY];
    const card = (root.querySelector('[data-cfx-role="frame-card"]') || svg)?.getBoundingClientRect();
    if (!matrix || !card || !ends?.every(Number.isFinite) || !box.width && !box.height) { hideCrosshair(root, crosshair); return; }
    const origin = stagePoint(stage, { x: card.left, y: card.top }), corner = stagePoint(stage, { x: card.right, y: card.bottom });
    const width = corner.x - origin.x, height = corner.y - origin.y;
    const at = point => { const local = stagePoint(stage, point); return { x: local.x - origin.x, y: local.y - origin.y }; };
    const svgAt = (x, y) => at(new DOMPoint(x, y).matrixTransform(matrix));
    const c = svgAt(ends[0], ends[1]), edge = svgAt(ends[2], ends[3]), first = at({ x: box.left, y: box.top }), last = at({ x: box.right, y: box.bottom });
    const x = (first.x + last.x) / 2 - c.x, y = (first.y + last.y) / 2 - c.y, r = Math.hypot(x, y), theta = r > .5 ? Math.atan2(y, x) : -Math.PI / 2;
    const outer = Math.max(r, Math.hypot(edge.x - c.x, edge.y - c.y)), px = value => value + 'px';
    Object.assign(crosshair.style, { left: px(origin.x), top: px(origin.y), width: px(width), height: px(height) });
    crosshair.dataset.cfxCrosshairShape = 'polar';
    crosshair.hidden = false;
    const part = name => crosshair.querySelector('.cfx-crosshair__' + name)
      || crosshair.appendChild(Object.assign(document.createElement('span'), { className: 'cfx-crosshair__' + name }));
    Object.assign(part('ring').style, { left: px(c.x - r), top: px(c.y - r), width: px(2 * r), height: px(2 * r) });
    Object.assign(part('spoke').style, { left: px(c.x), top: px(c.y), width: px(r), transform: 'rotate(' + theta + 'rad)' });
    const blocked = [{ left: first.x - 4, top: first.y - 4, right: last.x + 4, bottom: last.y + 4 }];
    const place = (axis, value, anchors) => {
      const label = crosshairAxisLabel(crosshair, axis, value), w = label.offsetWidth, h = label.offsetHeight;
      if (label.hidden) return;
      const gap = Math.abs(Math.cos(theta)) * w / 2 + Math.abs(Math.sin(theta)) * h / 2 + 8;
      const rects = anchors(gap).map(([angle, distance]) => {
        const left = clamp(c.x + Math.cos(angle) * distance - w / 2, 2, Math.max(2, width - w - 2));
        const top = clamp(c.y + Math.sin(angle) * distance - h / 2, 2, Math.max(2, height - h - 2));
        return { left, top, right: left + w, bottom: top + h };
      });
      const rect = rects.find(item => !blocked.some(other => item.left < other.right && item.right > other.left
        && item.top < other.bottom && item.bottom > other.top)) || rects[0];
      label.style.left = px(rect.left); label.style.top = px(rect.top);
      blocked.push(rect);
    };
    place('angle', crosshairXText(node), gap => [[theta, outer + gap], [theta, r + gap], [theta, r - gap]]);
    // A quarter turn from the observation keeps the radius between category spokes, away from their marks.
    place('radius', tooltipValueText(node), gap => [theta - Math.PI / 2, theta + Math.PI / 2, theta + Math.PI]
      .map(angle => [angle, r]).concat([[theta, r + gap]]));
    announceCrosshair(root, node, event, crosshairHoverMode(event, node, nativeTarget));
  };
