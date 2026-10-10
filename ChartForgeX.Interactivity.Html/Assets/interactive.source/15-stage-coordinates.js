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
  // The guide spans only the visible viewport, so a Readable overlay cannot add scrollable content.
  const positionStageViewport = (stage, overlay) => {
    overlay.style.left = stage.scrollLeft + 'px';
    overlay.style.top = stage.scrollTop + 'px';
    overlay.style.width = stage.clientWidth + 'px';
    overlay.style.height = stage.clientHeight + 'px';
  };
  // A guide belongs to the current viewport. Discard stale dimensions before they can enlarge a resized scroller.
  // Share the existing resize lifecycle with keyboard availability, including initially hidden fragment hosts.
  const bindStageLayout = (root, stage, crosshair) => {
    if (!stage || !hasFeature(root, 'Crosshair') && !hasFeature(root, 'KeyboardNavigation')) return;
    let width = stage.clientWidth, height = stage.clientHeight;
    let frame = 0;
    let observer;
    const hideGuide = () => hideCrosshair(root, crosshair);
    const queueRefresh = () => {
      if (frame) cancelAnimationFrame(frame);
      frame = requestAnimationFrame(() => {
        frame = 0;
        if (!root.isConnected) {
          if (observer) observer.disconnect();
          window.removeEventListener('resize', queueRefresh);
          stage.removeEventListener('scroll', hideGuide);
          return;
        }
        if (width !== stage.clientWidth || height !== stage.clientHeight) hideGuide();
        width = stage.clientWidth;
        height = stage.clientHeight;
        refreshKeyboardNavigation(root);
      });
    };
    stage.addEventListener('scroll', hideGuide, { passive: true });
    window.addEventListener('resize', queueRefresh);
    if (typeof ResizeObserver !== 'undefined') {
      observer = new ResizeObserver(queueRefresh);
      observer.observe(stage);
    }
  };
