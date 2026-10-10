  // Semantic groups do not paint. Resolve the real mark before using a series or legend fallback.
  const paintShapes = 'rect,circle,ellipse,line,polyline,path,polygon';
  // Cache only for this render: host CSS can change between successive focus and pointer events.
  const paintStyle = (node, styles) => {
    if (styles.has(node)) return styles.get(node);
    const style = getComputedStyle(node); styles.set(node, style); return style;
  };
  const paintAncestorsVisible = (node, styles) => {
    if (!node || node.closest('defs,[hidden]')) return false;
    for (let ancestor = node; ancestor; ancestor = ancestor.parentElement) {
      const style = paintStyle(ancestor, styles);
      if (style.display === 'none' || Number(style.opacity) === 0) return false;
    }
    // aria-hidden changes accessibility exposure, not whether SVG marks are painted.
    return true;
  };
  const paintNodeVisible = (node, styles) => {
    if (!paintAncestorsVisible(node, styles)) return false;
    // Visibility is inherited, and a painted descendant can explicitly restore it.
    const style = paintStyle(node, styles);
    return style.visibility !== 'hidden' && style.visibility !== 'collapse';
  };
  const solidPaintColour = (value, opacity) => value && value !== 'none' && value !== 'transparent'
    && !/^url\(/i.test(value) && !/^rgba\(.*[,]\s*0(?:\.0+)?\s*\)$|\/\s*0(?:\.0+)?%?\s*\)$/i.test(value)
    && Number(opacity) > 0 ? value : '';
  const paintValue = (node, value, opacity, styles) => {
    const colour = solidPaintColour(value, opacity);
    if (colour) return { colour };
    if (Number(opacity) <= 0) return null;
    const reference = /^url\(\s*["']?([^"')]+)["']?\s*\)$/i.exec(value || '');
    if (!reference || !node.ownerSVGElement) return null;
    // Read only paint servers in this SVG. One visible stop represents a gradient; never follow linked servers.
    let server;
    try {
      const url = new URL(reference[1], node.ownerDocument.baseURI);
      if (url.href.split('#')[0] !== node.ownerDocument.URL.split('#')[0]) return null;
      server = node.ownerSVGElement.getElementById(decodeURIComponent(url.hash.slice(1)));
    } catch (_) { return null; }
    // A pattern is a painted surface even though it has no single representative swatch colour.
    if (server && server.matches('pattern')) return { colour: '' };
    if (!server || !server.matches('linearGradient,radialGradient')) return null;
    const stops = server.querySelectorAll('stop');
    for (let index = 0; index < Math.min(stops.length, 32); index++) {
      const stop = paintStyle(stops[index], styles);
      const paint = solidPaintColour(stop.stopColor, stop.stopOpacity);
      if (paint) return { colour: paint };
    }
    return null;
  };
  const shapePaint = (node, styles) => {
    if (!node || !node.matches(paintShapes) || node.closest('[data-cfx-browser-hit-area]') || node.classList.contains('cfx-prepared-point-marker')) return null;
    if (/-(highlight|pattern|halo|shadow(?:-soft)?)$/.test((node.dataset || {}).cfxRole || '') || !paintNodeVisible(node, styles)) return null;
    const paint = paintStyle(node, styles);
    const stroke = parseFloat(paint.strokeWidth) > 0 ? paintValue(node, paint.stroke, paint.strokeOpacity, styles) : null;
    // Open line marks never paint their inherited default black fill.
    if (/^(line|polyline)$/i.test(node.tagName)) return stroke;
    return paintValue(node, paint.fill, paint.fillOpacity, styles) || stroke;
  };
  const childPaint = (node, decoration, styles) => {
    if (!node) return null;
    const shapes = node.matches(paintShapes) ? [node] : Array.from(node.querySelectorAll(paintShapes));
    const primary = shapes.filter((shape) => shape.matches('[data-cfx-role^="circle-value"],[data-cfx-role^="gauge-value"],[data-cfx-role="gauge-needle"],[data-cfx-role="bullet-value"]'));
    for (const shape of primary.concat(shapes)) {
      if (!decoration && shape.closest('[data-cfx-label-decoration]')) continue;
      const paint = shapePaint(shape, styles);
      if (paint) return paint;
    }
    return null;
  };
  const seriesPaint = (node, styles) => {
    const owner = node.closest('[data-cfx-role="series"],[data-cfx-role="radar-series"],[data-cfx-role="polar-series"]');
    if (!owner) return null;
    for (const layer of owner.querySelectorAll('[data-cfx-role="line"],[data-cfx-role="trend-line"],[data-cfx-role="slope-line"],[data-cfx-role="area"],[data-cfx-role="range-area"],[data-cfx-role="range-band"],[data-cfx-role="radar-outline"],[data-cfx-role="radar-area"],[data-cfx-role="polar-line"]')) {
      const paint = childPaint(layer, false, styles);
      if (paint) return paint;
    }
    return null;
  };
  const observationPaint = (node, styles) => paintAncestorsVisible(node, styles)
    ? childPaint(node, false, styles) || seriesPaint(node, styles) : null;
  // Geometry alone is not evidence of paint: transparent browser hit areas and retained facts have boxes too.
  // Legend summaries remain usable when their data is muted; data targets obey ancestor muting.
  const pointerTargetPaint = (node, styles = new Map()) => {
    if (!node || !isInteractiveTarget(node)) return null;
    const legend = (node.dataset || {}).cfxRole === 'legend-item';
    if (legend) return childPaint(node, true, styles);
    if (node.closest('.cfx-series-muted') || ['zero', 'precision-collapse'].includes(node.dataset.cfxGeometryStatus)) return null;
    const paint = observationPaint(node, styles);
    if (paint) return paint;
    // Text annotations are painted text rather than data geometry, and retain their native readout.
    if ((node.dataset.cfxRole || '').startsWith('annotation') && paintNodeVisible(node, styles)) {
      const box = node.getBoundingClientRect();
      if (box.width || box.height) return { colour: '' };
    }
    return null;
  };
  const tooltipReadoutAvailable = (node, event, styles = new Map()) => {
    if (pointerTargetPaint(node, styles)) return true;
    // Authored zero/precision-collapse facts remain a keyboard readout, without becoming pointer targets.
    const pointer = event instanceof PointerEvent || event instanceof MouseEvent && event.detail > 0;
    return !pointer && ['zero', 'precision-collapse'].includes((node.dataset || {}).cfxGeometryStatus)
      && paintAncestorsVisible(node, styles) && keyboardTargetAvailable(node);
  };
  const paintColour = (node, styles = new Map()) => {
    if (!node) return '';
    const legend = (node.dataset || {}).cfxRole === 'legend-item';
    const paint = legend ? childPaint(node, true, styles) : observationPaint(node, styles);
    if (paint && paint.colour) return paint.colour;
    const fallback = !legend && childPaint(seriesLegend(node), true, styles);
    return fallback && fallback.colour || '';
  };
