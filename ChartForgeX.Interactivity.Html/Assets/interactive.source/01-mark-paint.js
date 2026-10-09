  // Semantic groups do not paint. Resolve the real mark before using a series or legend fallback.
  const paintShapes = 'rect,circle,ellipse,line,polyline,path,polygon';
  const shapePaintColour = (node) => {
    if (!node || !node.matches(paintShapes) || node.closest('[data-cfx-browser-hit-area]') || node.classList.contains('cfx-prepared-point-marker')) return '';
    const paint = getComputedStyle(node);
    if (paint.display === 'none' || paint.visibility === 'hidden' || paint.visibility === 'collapse' || Number(paint.opacity) === 0) return '';
    const colour = (value, opacity) => value && value !== 'none' && value !== 'transparent'
      && !/^url\(/i.test(value) && !/^rgba\(.*[,]\s*0(?:\.0+)?\s*\)$|\/\s*0(?:\.0+)?%?\s*\)$/i.test(value)
      && Number(opacity) > 0 ? value : '';
    const stroke = parseFloat(paint.strokeWidth) > 0 ? colour(paint.stroke, paint.strokeOpacity) : '';
    // Open line marks never paint their inherited default black fill.
    if (/^(line|polyline)$/i.test(node.tagName)) return stroke;
    return colour(paint.fill, paint.fillOpacity) || stroke;
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
