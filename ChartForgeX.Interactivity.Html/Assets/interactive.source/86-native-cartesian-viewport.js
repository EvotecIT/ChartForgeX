  // Explicit primary Y/XY policy uses the same captured transforms and native preparation channel as X.
  const chartYProjection = root => {
    try { return JSON.parse(root.dataset.cfxPreparedChart).yProjection || null; } catch { return null; }
  };
  const nativeYWindow = root => {
    const projection = chartYProjection(root);
    return projection ? { minimum: projection.minimum, maximum: projection.maximum, scale: projection.scale } : null;
  };
  const nativeYWindowChanged = root => {
    const projection = chartYProjection(root);
    return !!projection && (projection.minimum !== projection.fullMinimum || projection.maximum !== projection.fullMaximum);
  };
  const nativeViewportChanged = root => nativeXWindowChanged(root) || nativeYWindowChanged(root);
  const cartesianViewportConnected = root => nativePreparationBinding(root, 'viewport')?.legacy === false;
  const nativeViewportWindows = root => ({ x: nativeXWindow(root), y: nativeYWindow(root) });
  const logicalChartPointer = (root, event) => {
    const matrix = svgScreenMatrix(root.querySelector('.cfx-stage svg'));
    if (!matrix) return null;
    const point = new DOMPoint(event.clientX, event.clientY).matrixTransform(matrix.inverse());
    return Number.isFinite(point.x) && Number.isFinite(point.y) ? { x: point.x, y: point.y } : null;
  };
  const validSourceAxisWindow = (window, projection, axis) => {
    if (!window || !projection || !axis || !Number.isFinite(window.minimum) || !Number.isFinite(window.maximum)
      || window.minimum >= window.maximum || window.scale && window.scale !== projection.scale
      || axis.scale === 'logarithmic' && window.minimum <= 0) return false;
    const first = projectedAxisRatio(axis, window.minimum), last = projectedAxisRatio(axis, window.maximum);
    return Number.isFinite(first) && Number.isFinite(last) && first !== last;
  };
  const requestCartesianViewport = (root, payload, sync = true, accepted) => {
    if (!cartesianViewportConnected(root) || !chartXProjection(root)
      || payload.axes?.includes('y') && !chartYProjection(root)) return false;
    if (payload.kind === 'reset') suspendPointerReadout(root);
    return requestNativePreparation(root, 'viewport', payload, {
      eventPrefix: 'cfxviewport', resetView: payload.kind === 'reset', accepted,
      validate: next => {
        if (!chartXProjection(next) || (payload.axes?.includes('y') || chartYProjection(root)) && !chartYProjection(next))
          throw new TypeError('The viewport reply must retain its supported native axis projections.');
      },
      beforeCommit: () => payload.kind === 'reset' ? selectionSignature(root) : '',
      committed: (_, previousSelection) => {
        if (payload.kind === 'reset') { emitHostEvent(root, 'cfxreset', {}); publishResetSelection(root, previousSelection); }
        if (sync) emitSync(root, { action: 'viewport', state: getState(root), xWindow: nativeXWindow(root), yWindow: nativeYWindow(root) });
      }
    });
  };
  const requestNativeCartesianWindow = (root, viewport, sync = true, accepted) => {
    if (!cartesianViewportConnected(root) || !viewport || (!viewport.x && !viewport.y)) return false;
    const x = chartXProjection(root), y = chartYProjection(root), data = root._cfxDataProjection;
    if (viewport.x && !validSourceAxisWindow(viewport.x, x, data?.x)
      || viewport.y && !validSourceAxisWindow(viewport.y, y, data?.y)) return false;
    const sourceViewport = Object.freeze({
      ...(viewport.x ? { x: Object.freeze({ minimum: viewport.x.minimum, maximum: viewport.x.maximum }) } : {}),
      ...(viewport.y ? { y: Object.freeze({ minimum: viewport.y.minimum, maximum: viewport.y.maximum }) } : {})
    });
    return requestCartesianViewport(root, { kind: 'window', axes: (viewport.x ? 'x' : '') + (viewport.y ? 'y' : ''), sourceViewport }, sync, accepted);
  };
  const requestViewportReset = root => {
    if (!cartesianViewportConnected(root)) return requestXViewport(root, 'reset');
    // Reset restores the native view, rather than replaying a bookmark's complete
    // window. A replacement can lose Y eligibility while retaining a zoomed X.
    const axes = nativePreparationBinding(root, 'viewport').axes.replace('y', chartYProjection(root) ? 'y' : '');
    return requestCartesianViewport(root, { kind: 'reset', axes: axes || 'x' });
  };
  const requestCartesianViewportZoom = (root, factor, logicalX, logicalY) => {
    const projection = chartXProjection(root), binding = nativePreparationBinding(root, 'viewport'), plot = projection?.plotBounds;
    if (!plot || !Number.isFinite(factor) || factor <= 0) return;
    const x = Number.isFinite(logicalX) ? clamp(logicalX, plot.x, plot.x + plot.width) : plot.x + plot.width / 2;
    const y = Number.isFinite(logicalY) ? clamp(logicalY, plot.y, plot.y + plot.height) : plot.y + plot.height / 2;
    const axes = binding.axes;
    requestCartesianViewport(root, { kind: 'zoom', axes,
      ...(axes.includes('x') ? { left: x - (x - plot.x) / factor, right: x + (plot.x + plot.width - x) / factor } : {}),
      ...(axes.includes('y') ? { top: y - (y - plot.y) / factor, bottom: y + (plot.y + plot.height - y) / factor } : {})
    });
  };
  const requestNativeViewportPan = (root, deltaX, deltaY) => {
    const plot = chartXProjection(root)?.plotBounds, binding = nativePreparationBinding(root, 'viewport');
    if (!plot || !binding) return;
    if (binding.legacy) {
      if (Math.abs(deltaX) > .5) requestXViewport(root, 'pan', plot.x - deltaX, plot.x + plot.width - deltaX);
      return;
    }
    const axes = binding.axes;
    if (!(axes.includes('x') && Math.abs(deltaX) > .5 || axes.includes('y') && Math.abs(deltaY) > .5)) return;
    requestCartesianViewport(root, { kind: 'pan', axes,
      ...(axes.includes('x') ? { left: plot.x - deltaX, right: plot.x + plot.width - deltaX } : {}),
      ...(axes.includes('y') ? { top: plot.y - deltaY, bottom: plot.y + plot.height - deltaY } : {})
    });
  };
