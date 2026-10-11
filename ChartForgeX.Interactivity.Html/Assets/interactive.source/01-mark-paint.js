  // Semantic groups do not paint. Resolve the real mark before using a series or legend fallback.
  const paintShapes = 'rect,circle,ellipse,line,polyline,path,polygon';
  // Both readout lifetime and keyboard availability follow the same native paint inputs.
  // Adapter state and paint-probe attributes are excluded so observing paint cannot observe itself.
  const paintAvailabilityAttributes = ['class', 'style', 'hidden', 'aria-hidden', 'display', 'visibility',
    'fill', 'fill-opacity', 'stroke', 'stroke-width', 'stroke-opacity', 'opacity', 'clip-path', 'clipPathUnits',
    'x', 'y', 'x1', 'x2', 'y1', 'y2', 'width', 'height', 'cx', 'cy', 'r', 'rx', 'ry', 'd', 'points', 'transform',
    'stop-color', 'stop-opacity', 'font-size'];
  // Cache only for this render: host CSS can change between successive focus and pointer events.
  const paintStyle = (node, styles) => {
    if (styles.has(node)) return styles.get(node);
    const style = getComputedStyle(node); styles.set(node, style); return style;
  };
  const paintAncestorsVisible = (node, styles) => {
    if (!node || node.closest('defs,[hidden]')) return false;
    for (let ancestor = node; ancestor; ancestor = ancestor.parentElement || ancestor.getRootNode().host) {
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
  // These Cartesian observations describe both boundaries, rather than one source Y location.
  const pairedExtentObservation = (node) => node.hasAttribute('data-cfx-point')
    && ['differencearea', 'rangearea', 'rangeband'].includes((node.dataset.cfxKind || '').toLowerCase());
  const pairedConnectedSpan = (node, owner) => {
    const index = Number(node.dataset.cfxPoint);
    const followsPrevious = (point) => point
      && point.getAttribute('data-cfx-source-0-break') !== 'true'
      && point.getAttribute('data-cfx-source-1-break') !== 'true';
    const previous = owner.querySelector('[data-cfx-role="point"][data-cfx-point="' + (index - 1) + '"]');
    const next = owner.querySelector('[data-cfx-role="point"][data-cfx-point="' + (index + 1) + '"]');
    // Native paired fills need a connected span; isolated observations can still paint their own markers or caps.
    return !!(previous && followsPrevious(node) || followsPrevious(next));
  };
  const pairedExtentScreenBox = (node) => {
    const region = node._cfxPairedRegion, matrix = node.getScreenCTM();
    if (!region || !matrix) return null;
    // Use the captured native extent, excluding the minimum-size transparent browser hit area.
    const corners = [[region.x, region.y], [region.x + region.width, region.y],
      [region.x, region.y + region.height], [region.x + region.width, region.y + region.height]]
      .map(([x, y]) => new DOMPoint(x, y).matrixTransform(matrix));
    return { left: Math.min(...corners.map(p => p.x)), right: Math.max(...corners.map(p => p.x)),
      top: Math.min(...corners.map(p => p.y)), bottom: Math.max(...corners.map(p => p.y)) };
  };
  // Native producers use rectangular user-space clips. A retained fact or path box can lie outside them.
  const paintWithinNativeClips = (node, subject = node) => {
    const paired = subject !== node && pairedExtentObservation(subject);
    let box;
    for (let parent = node; parent && parent.ownerSVGElement; parent = parent.parentElement) {
      const reference = /^url\(#([^)]*)\)$/.exec(parent.getAttribute('clip-path') || '');
      if (!reference) continue;
      const clip = parent.ownerSVGElement.getElementById(reference[1]);
      if (!clip || clip.getAttribute('clipPathUnits') !== 'userSpaceOnUse' || clip.children.length !== 1
        || !clip.firstElementChild.matches('rect')) continue;
      const matrix = parent.getScreenCTM();
      if (!matrix) return false;
      const rect = clip.firstElementChild.getBBox();
      if (!(rect.width > 0 && rect.height > 0)) return false;
      const corners = [[rect.x, rect.y], [rect.x + rect.width, rect.y], [rect.x, rect.y + rect.height], [rect.x + rect.width, rect.y + rect.height]]
        .map(([x, y]) => new DOMPoint(x, y).matrixTransform(matrix));
      const left = Math.min(...corners.map(p => p.x)), right = Math.max(...corners.map(p => p.x));
      const top = Math.min(...corners.map(p => p.y)), bottom = Math.max(...corners.map(p => p.y));
      // Native SVG coordinates use three decimals; prepared fact locations retain full precision.
      const precision = .001 * Math.max(Math.abs(matrix.a) + Math.abs(matrix.c), Math.abs(matrix.b) + Math.abs(matrix.d)) + .0001;
      box ||= paired ? pairedExtentScreenBox(subject) : subject.getBoundingClientRect();
      if (!box) return false;
      if (subject !== node) {
        // Keep the source X location; a paired observation remains usable when its own Y extent is visible.
        // Single-point line/radar observations still require their source location inside the native clip.
        const x = (box.left + box.right) / 2, y = (box.top + box.bottom) / 2;
        if (x < left - precision || x > right + precision
          || (paired ? box.bottom < top - precision || box.top > bottom + precision : y < top - precision || y > bottom + precision)) return false;
      } else if (box.right < left - precision || box.left > right + precision || box.bottom < top - precision || box.top > bottom + precision) return false;
    }
    return true;
  };
  const shapePaint = (node, styles, subject = node) => {
    if (!node || !node.matches(paintShapes) || node.closest('[data-cfx-browser-hit-area]') || node.classList.contains('cfx-prepared-point-marker')) return null;
    if (/-(highlight|halo|shadow(?:-soft)?)$/.test((node.dataset || {}).cfxRole || '')
      || !paintNodeVisible(node, styles) || !paintWithinNativeClips(node)) return null;
    // SVG omits zero-radius circles and ellipses, including their otherwise visible stroke paint.
    if (node.matches('circle,ellipse')) {
      const box = node.getBBox();
      if (!(box.width > 0 && box.height > 0)) return null;
    }
    const paint = paintStyle(node, styles);
    const stroke = parseFloat(paint.strokeWidth) > 0 ? paintValue(node, paint.stroke, paint.strokeOpacity, styles) : null;
    // Open line marks never paint their inherited default black fill.
    const resolved = /^(line|polyline)$/i.test(node.tagName) ? stroke
      : (node.dataset.cfxFillArea !== 'false' && paintValue(node, paint.fill, paint.fillOpacity, styles)) || stroke;
    return resolved && (subject === node || paintWithinNativeClips(node, subject)) ? resolved : null;
  };
  const primaryTextPaint = (node, decoration, styles) => {
    const role = (node.dataset || {}).cfxRole;
    // Text is the mark for a word-cloud term or annotation caption; ordinary point labels are decoration.
    if (role !== 'word-cloud-term' && role !== 'annotation' && !(decoration && role === 'legend-item')) return null;
    for (const text of node.querySelectorAll('text')) {
      if (!text.textContent.trim() || !paintNodeVisible(text, styles) || !paintWithinNativeClips(text)
        || !decoration && text.closest('[data-cfx-label-decoration]')) continue;
      const style = paintStyle(text, styles);
      if (!(parseFloat(style.fontSize) > 0)) continue;
      const stroke = parseFloat(style.strokeWidth) > 0 ? paintValue(text, style.stroke, style.strokeOpacity, styles) : null;
      const paint = paintValue(text, style.fill, style.fillOpacity, styles) || stroke;
      if (paint) return paint;
    }
    return null;
  };
  const childPaint = (node, decoration, styles, subject) => {
    if (!node) return null;
    const shapes = node.matches(paintShapes) ? [node] : Array.from(node.querySelectorAll(paintShapes));
    const primary = shapes.filter((shape) => shape.matches('[data-cfx-role^="circle-value"],[data-cfx-role^="gauge-value"],[data-cfx-role="gauge-needle"],[data-cfx-role="bullet-value"]'));
    const authoredPattern = (shape) => /-pattern$/.test(shape.dataset.cfxRole || '');
    const patterns = shapes.filter(authoredPattern);
    for (const shape of primary.concat(shapes.filter((shape) => !authoredPattern(shape)))) {
      if (!decoration && shape.closest('[data-cfx-label-decoration]')) continue;
      const paint = shapePaint(shape, styles, subject || shape);
      if (paint) return paint;
    }
    const text = primaryTextPaint(node, decoration, styles);
    if (text) return text;
    // Authored hatches can be the only mark ink; captions and ordinary surfaces keep their paint precedence.
    for (const shape of patterns) {
      if (!decoration && shape.closest('[data-cfx-label-decoration]')) continue;
      const paint = shapePaint(shape, styles, subject || shape);
      if (paint) return paint;
    }
    return null;
  };
  const seriesPaint = (node, styles) => {
    const owner = node.closest('[data-cfx-role="series"],[data-cfx-role="radar-series"],[data-cfx-role="polar-series"]');
    if (!owner) return null;
    const data = node.dataset || {};
    const paired = (data.cfxKind || '').toLowerCase() === 'differencearea' && data.cfxPoint !== undefined;
    const connected = !pairedExtentObservation(node) || pairedConnectedSpan(node, owner);
    const relevant = (layer) => {
      const role = layer.dataset.cfxRole || '';
      if (!connected && /^(difference-(positive|negative)|range-(area|band))(-pattern)?$/.test(role)) return false;
      if (!paired) return true;
      if (role === 'difference-positive' || role === 'difference-positive-pattern') return Number(data.cfxFirst) > Number(data.cfxSecond);
      if (role === 'difference-negative' || role === 'difference-negative-pattern') return Number(data.cfxFirst) < Number(data.cfxSecond);
      return true;
    };
    for (const layer of owner.querySelectorAll('[data-cfx-role="line"],[data-cfx-role="trend-line"],[data-cfx-role="slope-line"],[data-cfx-role="area"],[data-cfx-role="range-area"],[data-cfx-role="range-band"],[data-cfx-role="range-upper"],[data-cfx-role="range-lower"],[data-cfx-role="range-midline"],[data-cfx-role="difference-positive"],[data-cfx-role="difference-negative"],[data-cfx-role="difference-first"],[data-cfx-role="difference-second"],[data-cfx-role="radar-outline"],[data-cfx-role="radar-area"],[data-cfx-role="polar-line"]')) {
      if (!relevant(layer)) continue;
      const paint = childPaint(layer, false, styles, node.hasAttribute('data-cfx-point') ? node : undefined);
      if (paint) return paint;
    }
    for (const layer of owner.querySelectorAll('[data-cfx-role="area-pattern"],[data-cfx-role="range-area-pattern"],[data-cfx-role="range-band-pattern"],[data-cfx-role="difference-positive-pattern"],[data-cfx-role="difference-negative-pattern"],[data-cfx-role="radar-pattern"]')) {
      if (!relevant(layer)) continue;
      const paint = childPaint(layer, false, styles, node.hasAttribute('data-cfx-point') ? node : undefined);
      if (paint) return paint;
    }
    return null;
  };
  const observationPaint = (node, styles) => withNativeHighlightPaint(node, () => {
    // A probe suspends adapter opacity only; use fresh live style values after the suspension.
    if (node.closest('[data-cfx-highlight-managed]')) styles = new Map();
    return paintAncestorsVisible(node, styles) ? childPaint(node, false, styles) || seriesPaint(node, styles) : null;
  });
  const retainedGeometryFact = (node) => ['zero', 'precision-collapse', 'layout-collapse'].includes((node.dataset || {}).cfxGeometryStatus);
  // Geometry alone is not evidence of paint: transparent browser hit areas and retained facts have boxes too.
  // Legend summaries remain usable when their data is muted; data targets obey ancestor muting.
  const pointerTargetPaint = (node, styles = new Map()) => {
    if (!node || !isInteractiveTarget(node)) return null;
    const legend = (node.dataset || {}).cfxRole === 'legend-item';
    if (legend) return childPaint(node, true, styles);
    if (node.closest('.cfx-series-muted') || retainedGeometryFact(node)) return null;
    return observationPaint(node, styles);
  };
  const tooltipReadoutAvailable = (node, event, styles = new Map()) => {
    if (pointerTargetPaint(node, styles)) return true;
    // Authored facts without painted geometry remain a keyboard readout, without becoming pointer targets.
    const pointer = event instanceof PointerEvent || event instanceof MouseEvent && event.detail > 0;
    return !pointer && retainedGeometryFact(node)
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
