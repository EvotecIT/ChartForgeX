  const polarGestureProjection = root => {
    const binding = nativePreparationBinding(root, 'viewport');
    return binding?.coordinateSystem === 'polar' && binding.gestures === true ? capturedPolarProjection(root) : null;
  };
  const polarPointer = (root, point, hit = true) => {
    const p = polarGestureProjection(root);
    if (!p || !point) return null;
    const axes = nativePreparationBinding(root, 'viewport').axes;
    const x = point.x - p.centerX, y = point.y - p.centerY, radius = Math.hypot(x, y), bearing = Math.atan2(y, x);
    if (!Number.isFinite(radius) || !radius) return null;
    if (hit) {
      const turn = Math.PI * 2, span = p.angle.maximum - p.angle.minimum;
      const raw = (p.angleReversed ? bearing - p.rotationRadians : p.rotationRadians - bearing) - p.angle.minimum;
      const offset = (raw % turn + turn) % turn;
      if (radius > p.outerRadius || span < turn && (offset > span || offset === span && p.angle.maximumInclusive === false)
        || axes.some(name => name !== 'angle' && radius < p[name].innerRadius)) return null;
    }
    return { bearing, radius };
  };
  const requestPolarViewportZoom = (root, factor, x, y) => {
    if (!polarGestureProjection(root) || !Number.isFinite(factor) || factor <= 0) return false;
    const axes = nativePreparationBinding(root, 'viewport').axes;
    const anchor = Number.isFinite(x) && Number.isFinite(y) ? Object.freeze({ x, y }) : null;
    if (anchor && !polarPointer(root, anchor)) return false;
    return requestPolarViewport(root, { kind: 'zoom', axes, factor, ...(anchor ? { anchor } : {}) });
  };

  const requestPolarViewportPan = (root, drag) => {
    const p = polarGestureProjection(root);
    if (!p || drag.invalid) return false;
    const axes = nativePreparationBinding(root, 'viewport').axes;
    if (!axes.some(name => (name === 'angle' ? Math.abs(drag.angle) * p.outerRadius : Math.abs(drag.radial)) > .5)) return false;
    return requestPolarViewport(root, { kind: 'pan', axes, clockwiseAngleDelta: drag.angle, outwardRadiusDelta: drag.radial });
  };
