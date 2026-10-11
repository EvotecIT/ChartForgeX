  // Absolute overlays and SVG pan translations use stage CSS pixels, including its scroll origin.
  // Browser hit geometry stays in screen pixels; account for axis-aligned host scaling and the stage border.
  const stagePoint = (stage, point, bounded = false) => {
    const rect = stage.getBoundingClientRect();
    const scaleX = rect.width / stage.offsetWidth || 1;
    const scaleY = rect.height / stage.offsetHeight || 1;
    const x = (point.x - rect.left) / scaleX - stage.clientLeft + stage.scrollLeft;
    const y = (point.y - rect.top) / scaleY - stage.clientTop + stage.scrollTop;
    return bounded ? {
      x: clamp(x, stage.scrollLeft, stage.scrollLeft + stage.clientWidth),
      y: clamp(y, stage.scrollTop, stage.scrollTop + stage.clientHeight)
    } : { x, y };
  };
  const stagePointer = (stage, event, bounded = false) => stagePoint(stage, { x: event.clientX, y: event.clientY }, bounded);
  // The core owns layout. Project its captured plot through the live SVG transform, including pan/zoom.
  // Intersect with the visible scroller so the overlay cannot create new scrollable content.
  const visibleCartesianPlot = (root, stage) => {
    const plot = root._cfxCartesianPlot, svg = stage.querySelector('svg');
    if (!plot || !svg || stage.clientWidth <= 0 || stage.clientHeight <= 0) return null;
    const matrix = svg.getScreenCTM();
    // stagePoint supports positive axis-aligned host scaling, not rotated/skewed/perspective hosts.
    if (!matrix || matrix.is2D === false || matrix.b !== 0 || matrix.c !== 0 || matrix.a <= 0 || matrix.d <= 0) return null;
    const corners = [[plot.x, plot.y], [plot.x + plot.width, plot.y],
      [plot.x, plot.y + plot.height], [plot.x + plot.width, plot.y + plot.height]]
      .map(([x, y]) => stagePoint(stage, new DOMPoint(x, y).matrixTransform(matrix)));
    if (!corners.every(point => Number.isFinite(point.x) && Number.isFinite(point.y))) return null;
    const left = Math.max(stage.scrollLeft, Math.min(...corners.map(point => point.x)));
    const top = Math.max(stage.scrollTop, Math.min(...corners.map(point => point.y)));
    const right = Math.min(stage.scrollLeft + stage.clientWidth, Math.max(...corners.map(point => point.x)));
    const bottom = Math.min(stage.scrollTop + stage.clientHeight, Math.max(...corners.map(point => point.y)));
    return right > left && bottom > top ? { left, top, width: right - left, height: bottom - top } : null;
  };
  // A guide belongs to the current viewport. Discard stale dimensions before they can enlarge a resized scroller.
  // Share the existing resize lifecycle with keyboard availability, including initially hidden fragment hosts.
  const bindStageLayout = (root, stage, crosshair) => {
    if (!stage || !hasFeature(root, 'Crosshair') && !hasFeature(root, 'KeyboardNavigation')) return;
    let width = stage.clientWidth, height = stage.clientHeight;
    let frame = 0;
    let observer;
    const hideGuide = () => hideCrosshair(root, crosshair);
    // A pointer can acquire a guide during a CSS pan/zoom transition; discard that intermediate projection.
    const finishViewport = event => {
      if (event.target === stage.querySelector('svg') && event.propertyName === 'transform') hideGuide();
    };
    const queueRefresh = () => {
      if (frame) cancelAnimationFrame(frame);
      frame = requestAnimationFrame(() => {
        frame = 0;
        if (!root.isConnected) {
          if (observer) observer.disconnect();
          window.removeEventListener('resize', queueRefresh);
          stage.removeEventListener('scroll', hideGuide);
          stage.removeEventListener('transitionend', finishViewport);
          stage.removeEventListener('transitioncancel', finishViewport);
          return;
        }
        if (width !== stage.clientWidth || height !== stage.clientHeight) hideGuide();
        width = stage.clientWidth;
        height = stage.clientHeight;
        refreshKeyboardNavigation(root);
      });
    };
    stage.addEventListener('scroll', hideGuide, { passive: true });
    stage.addEventListener('transitionend', finishViewport);
    stage.addEventListener('transitioncancel', finishViewport);
    window.addEventListener('resize', queueRefresh);
    if (typeof ResizeObserver !== 'undefined') {
      observer = new ResizeObserver(queueRefresh);
      observer.observe(stage);
    }
  };
