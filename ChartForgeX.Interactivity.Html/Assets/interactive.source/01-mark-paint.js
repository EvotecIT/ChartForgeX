  // Semantic groups do not paint. Resolve the real mark before using a series or legend fallback.
  const paintShapes = 'rect,circle,ellipse,line,polyline,path,polygon';
  const solidPaintColour = (value, opacity) => value && value !== 'none' && value !== 'transparent'
    && !/^url\(/i.test(value) && !/^rgba\(.*[,]\s*0(?:\.0+)?\s*\)$|\/\s*0(?:\.0+)?%?\s*\)$/i.test(value)
    && Number(opacity) > 0 ? value : '';
  const paintValueColour = (node, value, opacity) => {
    const colour = solidPaintColour(value, opacity);
    if (colour || Number(opacity) <= 0) return colour;
    const reference = /^url\(\s*["']?([^"')]+)["']?\s*\)$/i.exec(value || '');
    if (!reference || !node.ownerSVGElement) return '';
    // Read only paint servers in this SVG. One visible stop represents a gradient; never follow linked servers.
    let server;
    try {
      const url = new URL(reference[1], node.ownerDocument.baseURI);
      if (url.href.split('#')[0] !== node.ownerDocument.URL.split('#')[0]) return '';
      server = node.ownerSVGElement.getElementById(decodeURIComponent(url.hash.slice(1)));
    } catch (_) { return ''; }
    if (!server || !server.matches('linearGradient,radialGradient')) return '';
    const stops = server.querySelectorAll('stop');
    for (let index = 0; index < Math.min(stops.length, 32); index++) {
      const stop = getComputedStyle(stops[index]);
      const paint = solidPaintColour(stop.stopColor, stop.stopOpacity);
      if (paint) return paint;
    }
    return '';
  };
  const shapePaintColour = (node) => {
    if (!node || !node.matches(paintShapes) || node.closest('[data-cfx-browser-hit-area]') || node.classList.contains('cfx-prepared-point-marker')) return '';
    if (/-(highlight|pattern|halo|shadow(?:-soft)?)$/.test((node.dataset || {}).cfxRole || '')) return '';
    const paint = getComputedStyle(node);
    if (paint.display === 'none' || paint.visibility === 'hidden' || paint.visibility === 'collapse' || Number(paint.opacity) === 0) return '';
    const stroke = parseFloat(paint.strokeWidth) > 0 ? paintValueColour(node, paint.stroke, paint.strokeOpacity) : '';
    // Open line marks never paint their inherited default black fill.
    if (/^(line|polyline)$/i.test(node.tagName)) return stroke;
    return paintValueColour(node, paint.fill, paint.fillOpacity) || stroke;
  };
  const childPaintColour = (node, decoration) => {
    if (!node) return '';
    const shapes = node.matches(paintShapes) ? [node] : Array.from(node.querySelectorAll(paintShapes));
    for (const shape of shapes) {
      if (!decoration && shape.closest('[data-cfx-label-decoration]')) continue;
      const colour = shapePaintColour(shape);
      if (colour) return colour;
    }
    return '';
  };
  const paintColour = (node) => {
    if (!node) return '';
    const legend = (node.dataset || {}).cfxRole === 'legend-item';
    const colour = childPaintColour(node, legend);
    if (colour || legend) return colour;
    const owner = node.closest('[data-cfx-role="series"]');
    const layer = owner && owner.querySelector('[data-cfx-role="line"],[data-cfx-role="area"],[data-cfx-role="range-area"],[data-cfx-role="range-band"]');
    return childPaintColour(layer, false) || childPaintColour(seriesLegend(node), true);
  };
