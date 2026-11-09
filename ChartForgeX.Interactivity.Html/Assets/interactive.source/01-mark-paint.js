
  const paintShapes = 'rect,circle,ellipse,line,polyline,path,polygon,image';


  const paintAvailabilityAttributes = ['class', 'style', 'hidden', 'aria-hidden', 'display', 'visibility',
    'fill', 'fill-opacity', 'stroke', 'stroke-width', 'stroke-opacity', 'opacity', 'clip-path', 'clipPathUnits',
    'x', 'y', 'x1', 'x2', 'y1', 'y2', 'width', 'height', 'cx', 'cy', 'r', 'rx', 'ry', 'd', 'points', 'transform',
    'stop-color', 'stop-opacity', 'font-size', 'fill-rule', 'clip-rule', 'slot', 'name', 'data-cfx-image-alpha-bounds', 'href'];

  const composedParent = (node) => node.assignedSlot || node.parentElement || node.getRootNode().host;
  const composedAncestry = (root) => {
    const nodes = [], trees = new Set();
    for (let node = root; node; node = composedParent(node)) {
      nodes.push(node); trees.add(node.getRootNode());
    }
    return { nodes, trees: Array.from(trees) };
  };

  const paintStyle = (node, styles) => {
    if (styles.has(node)) return styles.get(node);
    const style = getComputedStyle(node); styles.set(node, style); return style;
  };
  // A read-only pass may give its styles map a _cfxShown set, so each shown ancestor is checked once per pass.
  const paintAncestorsVisible = (node, styles) => {
    if (!node || node.closest('defs,[hidden]')) return false;
    const shown = styles._cfxShown, path = [];
    for (let ancestor = node; ancestor && !shown?.has(ancestor); ancestor = composedParent(ancestor)) {
      const style = paintStyle(ancestor, styles);
      if (style.display === 'none' || Number(style.opacity) === 0) return false;
      path.push(ancestor);
    }
    path.forEach(ancestor => shown?.add(ancestor));
    return true;
  };
  const paintNodeVisible = (node, styles) => {
    if (!paintAncestorsVisible(node, styles)) return false;

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

    let server;
    try {
      const url = new URL(reference[1], node.ownerDocument.baseURI);
      if (url.href.split('#')[0] !== node.ownerDocument.URL.split('#')[0]) return null;
      const id = decodeURIComponent(url.hash.slice(1));
      server = node.ownerSVGElement.getElementById(id)
        || node.closest('.cfx-interactive-chart')?.querySelector('.cfx-stage svg')?.getElementById(id);
    } catch (_) { return null; }

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

  const pairedExtentObservation = (node) => node.hasAttribute('data-cfx-point')
    && ['differencearea', 'rangearea', 'rangeband'].includes((node.dataset.cfxKind || '').toLowerCase());
  const pairedConnectedSpan = (node, owner) => {
    const index = Number(node.dataset.cfxPoint);
    const followsPrevious = (point) => point
      && point.getAttribute('data-cfx-break-before') !== 'true';
    const previous = owner.querySelector('[data-cfx-role="point"][data-cfx-point="' + (index - 1) + '"]');
    const next = owner.querySelector('[data-cfx-role="point"][data-cfx-point="' + (index + 1) + '"]');

    return !!(previous && followsPrevious(node) || followsPrevious(next));
  };

  const pairedExtentScreenBox = (node) => nativeRegionScreenBox(node, node._cfxPairedRegion);

  const nativePaintClips = (node) => {
    const clips = [];
    for (let parent = node; parent && parent.ownerSVGElement; parent = parent.parentElement) {
      const reference = /^url\(#([^)]*)\)$/.exec(parent.getAttribute('clip-path') || '');
      if (!reference) continue;
      const clip = parent.ownerSVGElement.getElementById(reference[1]);
      if (!clip || clip.getAttribute('clipPathUnits') !== 'userSpaceOnUse' || clip.children.length !== 1
        || !clip.firstElementChild.matches('rect')) continue;

      const shape = clip.firstElementChild, matrix = parent.getScreenCTM();
      const rect = { x: shape.x.baseVal.value, y: shape.y.baseVal.value,
        width: shape.width.baseVal.value, height: shape.height.baseVal.value };
      if (!matrix || !Object.values(rect).every(Number.isFinite) || !(rect.width > 0 && rect.height > 0)) return null;
      clips.push({ rect, matrix });
    }
    return clips;
  };

  // A layer tested against each of its observations measures its own clipped box once per pass: the box of a long
  // path costs as much as the path.
  const paintWithinNativeClips = (node, subject = node, styles) => {
    const own = subject === node && (styles._cfxOwnClips ||= new Map());
    return own ? own.has(node) ? own.get(node) : own.set(node, clippedPaint(node, node, styles)).get(node) : clippedPaint(node, subject, styles);
  };
  const clippedPaint = (node, subject, styles) => {
    const paired = subject !== node && pairedExtentObservation(subject);
    // A node's clip bounds do not depend on the observation, so a pass measures them once per node.
    const cache = styles._cfxClipBounds ||= new Map();
    if (!cache.has(node)) {
      const clips = nativePaintClips(node), projection = clips?.length && svgScreenProjection(node.ownerSVGElement, styles);
      cache.set(node, clips && [projection, clips.map(({ rect, matrix }) => {
        if (projection) matrix = projection.multiply(matrix);
        const { left, right, top, bottom } = screenBounds(rect, matrix), allowance = svgCoordinatePrecision(matrix);
        return [left, right, top, bottom, Math.max(allowance.x, allowance.y)];
      })]);
    }
    const [projection, bounds] = cache.get(node) || [];
    if (!bounds) return false;
    let box;
    for (const [left, right, top, bottom, precision] of bounds) {
      box ||= paired ? nativeRegionScreenBox(subject, subject._cfxPairedRegion, projection)
        : subject !== node ? nativeRegionScreenBox(subject, subject._cfxNativeRegion, projection) || subject.getBoundingClientRect() : subject.getBoundingClientRect();
      if (!box) return false;
      if (subject !== node) {

        const x = (box.left + box.right) / 2, y = (box.top + box.bottom) / 2;
        if (x < left - precision || x > right + precision
          || (paired ? box.bottom < top - precision || box.top > bottom + precision : y < top - precision || y > bottom + precision)) return false;
      } else if (box.right < left - precision || box.left > right + precision || box.bottom < top - precision || box.top > bottom + precision) return false;
    }
    return true;
  };
  const pairedBoundaryRole = (node) => /^(difference-(first|second)|range-(upper|lower|midline))$/.test(node.dataset.cfxRole || '');
  const pairedBoundaryInk = (node, subject, paint, region = subject._cfxPairedRegion) => {
    const sourceMatrix = subject.getScreenCTM(), shapeMatrix = node.getScreenCTM();
    const clips = nativePaintClips(node);
    if (!region || !sourceMatrix || !shapeMatrix || !clips || typeof node.isPointInStroke !== 'function') return false;

    const x = region.x + region.width / 2;
    const ys = node.dataset.cfxRole === 'range-midline' ? [Number(subject.dataset.cfxMidlineY)]
      : [region.y + region.width / 2, region.y + region.height - region.width / 2];
    for (const y of ys) {
      if (!Number.isFinite(y)) continue;

      for (const [px, py] of [[x, y], [Number(x.toFixed(3)), Number(y.toFixed(3))]]) {
        let point = new DOMPoint(px, py).matrixTransform(sourceMatrix);
        const source = point.matrixTransform(shapeMatrix.inverse());

        for (const { rect, matrix } of clips) {
          const local = point.matrixTransform(matrix.inverse());
          point = new DOMPoint(Math.max(rect.x, Math.min(rect.x + rect.width, local.x)),
            Math.max(rect.y, Math.min(rect.y + rect.height, local.y))).matrixTransform(matrix);
        }
        if (clips.some(({ rect, matrix }) => {
          const local = point.matrixTransform(matrix.inverse());

          return local.x < rect.x - .001 || local.x > rect.x + rect.width + .001
            || local.y < rect.y - .001 || local.y > rect.y + rect.height + .001;
        })) continue;
        const sample = point.matrixTransform(shapeMatrix.inverse());
        const radius = parseFloat(paint.strokeWidth) / 2 * (paint.strokeLinecap === 'square' ? Math.SQRT2 : 1);

        if (Math.hypot(sample.x - source.x, sample.y - source.y) <= radius + .001 && node.isPointInStroke(sample)) return true;
      }
    }
    return false;
  };
  const nativeImagePaintBounds = node => {
    const a=(node.dataset.cfxImageAlphaBounds||'').trim().split(/\s+/).map(Number),b=node.getBBox();
    if(a.length!==6||!a.every(Number.isSafeInteger)||a.some(n=>n<0)||!a[2]||!a[3]||!a[4]||!a[5]
      ||a[0]+a[2]>a[4]||a[1]+a[3]>a[5]||![b.x,b.y,b.width,b.height].every(Number.isFinite)||!(b.width>0&&b.height>0)
      ||node.getAttribute('preserveAspectRatio')!=='none'||!node.getAttribute('href')?.startsWith('data:image/png;base64,'))return null;
    let x=b.x+b.width*a[0]/a[4],y=b.y+b.height*a[1]/a[5],right=x+b.width*a[2]/a[4],bottom=y+b.height*a[3]/a[5];
    const matrix=node.getScreenCTM(),clips=nativePaintClips(node);if(!matrix||!clips||![matrix.a,matrix.b,matrix.c,matrix.d,matrix.e,matrix.f].every(Number.isFinite)||matrix.a*matrix.d===matrix.b*matrix.c)return null;
    for(const clip of clips){const m=matrix.inverse().multiply(clip.matrix),r=clip.rect;
      if(![m.a,m.b,m.c,m.d,m.e,m.f].every(Number.isFinite))return null;
      if(m.b||m.c){
        // A wholly contained rotated alpha extent needs no clipping approximation.
        const toClip=clip.matrix.inverse().multiply(matrix);
        const corners=[[x,y],[right,y],[x,bottom],[right,bottom]].map(([px,py])=>new DOMPoint(px,py).matrixTransform(toClip));
        if(corners.some(p=>![p.x,p.y].every(Number.isFinite)||p.x<r.x||p.y<r.y||p.x>r.x+r.width||p.y>r.y+r.height))return null;
        continue;
      }
      const x1=m.a*r.x+m.e,x2=m.a*(r.x+r.width)+m.e,y1=m.d*r.y+m.f,y2=m.d*(r.y+r.height)+m.f;
      x=Math.max(x,Math.min(x1,x2));right=Math.min(right,Math.max(x1,x2));y=Math.max(y,Math.min(y1,y2));bottom=Math.min(bottom,Math.max(y1,y2));
    }
    return right>x&&bottom>y?{x,y,width:right-x,height:bottom-y}:null;
  };
  const shapePaint = (node, styles, subject = node) => {
    if (!node || !node.matches(paintShapes) || node.closest('[data-cfx-browser-hit-area]') || node.classList.contains('cfx-prepared-point-marker')) return null;
    const pairedBoundary = subject !== node && pairedExtentObservation(subject) && pairedBoundaryRole(node);
    // A marker's surface ring (design rule 5) is backdrop like a halo, never the observation's colour.
    if (/-(highlight|halo|shadow(?:-soft)?)$|(?:^|-)(?:marker|swatch)-ring$/.test((node.dataset || {}).cfxRole || '')
      || !paintNodeVisible(node, styles) || !pairedBoundary && !paintWithinNativeClips(node, node, styles)) return null;

    let box;
    if (node.matches('rect,circle,ellipse')) {
      box = node.getBBox();
      if (!(box.width > 0 && box.height > 0)) return null;
    }
    if(node.localName==='image'){const bounds=nativeImagePaintBounds(node);return bounds?{colour:'',imageNode:node,bounds}:null;}
    const paint = paintStyle(node, styles);
    const stroke = parseFloat(paint.strokeWidth) > 0 ? paintValue(node, paint.stroke, paint.strokeOpacity, styles) : null;

    if (pairedBoundary) return stroke && pairedBoundaryInk(node, subject, paint) ? stroke : null;

    let fill = /^(line|polyline)$/i.test(node.tagName) || node.dataset.cfxFillArea === 'false' ? null
      : paintValue(node, paint.fill, paint.fillOpacity, styles);

    if (fill) {
      box ||= node.getBBox();
      if (!(box.width > 0 && box.height > 0)) fill = null;
    }
    if (cartesianFillObservation(node, subject)) {
      if ((node.dataset.cfxRole || '').endsWith('-pattern')) return stroke && cartesianFillInk(node, subject, styles) ? stroke : null;
      if (fill && cartesianFillInk(node, subject, styles)) return fill;
      return stroke && cartesianFillStrokeInk(node, subject, paint) ? stroke : null;
    }
    const resolved = fill || stroke;
    return resolved && (subject === node || paintWithinNativeClips(node, subject, styles)) ? resolved : null;
  };
  const primaryTextPaint = (node, decoration, styles) => {
    const role = (node.dataset || {}).cfxRole;

    if (decoration && role === 'legend-item' && !node.ownerSVGElement) {
      const label = node.querySelector('.cfx-responsive-legend__label');
      if (label && !label.querySelector('[data-cfx-native-text-ink]') && label.textContent.trim() && paintNodeVisible(label, styles)) {
        const box = label.getBoundingClientRect(), colour = solidPaintColour(paintStyle(label, styles).color, 1);
        if (box.width > 0 && box.height > 0 && colour) return { colour };
      }
    }

    if (role !== 'word-cloud-term' && role !== 'annotation' && !(decoration && role === 'legend-item')) return null;
    for (const text of node.querySelectorAll('text,[data-cfx-native-text-ink]')) {
      if (!paintNodeVisible(text, styles) || !paintWithinNativeClips(text, text, styles)
        || !decoration && text.closest('[data-cfx-label-decoration]')) continue;
      if (text.localName !== 'text') {
        for (const glyph of text.querySelectorAll('use,image[data-cfx-image-alpha-bounds]')) {
          if (glyph.localName === 'image') {
            const paint = shapePaint(glyph, styles);
            if (paint) return { ...paint, textNode: text };
            continue;
          }
          const href = glyph.getAttribute('href') || '', path = href.startsWith('#') && text.ownerSVGElement.getElementById(href.slice(1));
          if (!path || path.localName !== 'path' || !path.getAttribute('d') || !paintNodeVisible(glyph, styles)
            || !paintWithinNativeClips(glyph, glyph, styles)) continue;
          const box = glyph.getBBox(), style = paintStyle(glyph, styles);
          if (!(box.width > 0 && box.height > 0)) continue;
          const stroke = parseFloat(style.strokeWidth) > 0 ? paintValue(glyph, style.stroke, style.strokeOpacity, styles) : null;
          const paint = paintValue(glyph, style.fill, style.fillOpacity, styles) || stroke;
          if (paint) return { ...paint, textNode: text };
        }
        continue;
      }
      if (!text.textContent.trim()) continue;
      const style = paintStyle(text, styles);
      if (!(parseFloat(style.fontSize) > 0)) continue;
      const stroke = parseFloat(style.strokeWidth) > 0 ? paintValue(text, style.stroke, style.strokeOpacity, styles) : null;
      const paint = paintValue(text, style.fill, style.fillOpacity, styles) || stroke;
      if (paint) return { ...paint, textNode: text };
    }
    return null;
  };
  const childPaint = (node, decoration, styles, subject) => {
    if (!node) return null;
    const authoredPattern = (shape) => /-pattern$/.test(shape.dataset.cfxRole || '');
    // A series target holds every mark of the series; its shape lists are kept with the chart model.
    const build = () => {
      const shapes = node.matches(paintShapes) ? [node] : Array.from(node.querySelectorAll(paintShapes));
      return { shapes, patterns: shapes.filter(authoredPattern),
        primary: shapes.filter((shape) => shape.matches('[data-cfx-role^="circle-value"],[data-cfx-role^="gauge-value"],[data-cfx-role="gauge-needle"],[data-cfx-role="bullet-value"]')) };
    };
    const lists = chartModel(node.closest('.cfx-interactive-chart'))?.shapes, { shapes, primary, patterns } = lists ? lists.get(node) || lists.set(node, build()).get(node) : build();
    const skipped = (shape) => shape.closest('[data-cfx-native-text-ink]') || !decoration && shape.closest('[data-cfx-label-decoration]');
    // Primary value shapes first, then every non-pattern shape, stopping at the first that paints.
    for (const list of [primary, shapes]) for (const shape of list) {
      if (list === shapes && authoredPattern(shape) || skipped(shape)) continue;
      const paint = shapePaint(shape, styles, subject || shape);
      if (paint) return paint;
    }
    const text = primaryTextPaint(node, decoration, styles);
    if (text) return text;

    for (const shape of patterns) {
      if (skipped(shape)) continue;
      const paint = shapePaint(shape, styles, subject || shape);
      if (paint) return paint;
    }
    return null;
  };
  const roleSelector = roles => roles.split(' ').map(role => '[data-cfx-role="' + role + '"]').join();
  const seriesPaintLayers = roleSelector('line trend-line slope-line area range-area range-band range-upper range-lower range-midline difference-positive difference-negative difference-first difference-second radar-outline radar-area polar-line');
  const seriesPatternLayers = roleSelector('area-pattern range-area-pattern range-band-pattern difference-positive-pattern difference-negative-pattern radar-pattern');
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
    // Each series is searched for its paint layers once per pass, or once per chart model while the chart is unchanged.
    const layers = styles._cfxLayers ||= chartModel(owner.closest('.cfx-interactive-chart'))?.layers || new Map();
    if (!layers.has(owner)) layers.set(owner, [...owner.querySelectorAll(seriesPaintLayers), ...owner.querySelectorAll(seriesPatternLayers)]);
    for (const layer of layers.get(owner)) {
      if (!relevant(layer)) continue;
      const paint = childPaint(layer, false, styles, node.hasAttribute('data-cfx-point') ? node : undefined);
      if (paint) return paint;
    }
    return null;
  };
  const observationPaint = (node, styles) => withNativeHighlightPaint(node, () => {

    if (node.closest('[data-cfx-highlight-managed]')) styles = new Map();
    return paintAncestorsVisible(node, styles) ? childPaint(node, false, styles) || seriesPaint(node, styles) : null;
  });
  const retainedGeometryFact = (node) => {
    const data = node.dataset || {};
    return ['zero', 'precision-collapse', 'layout-collapse'].includes(data.cfxGeometryStatus)
      || data.cfxDerived === 'histogram-bin' && data.cfxBinHasValue === 'false';
  };


  const pointerTargetPaint = (node, styles = new Map()) => {
    if (!node || !isInteractiveTarget(node)) return null;
    const legend = (node.dataset || {}).cfxRole === 'legend-item';
    if (legend) return childPaint(node, true, styles);
    if (node.dataset.cfxVisible === 'false' || node.closest('.cfx-series-muted') || retainedGeometryFact(node)) return null;
    return observationPaint(node, styles);
  };
  const tooltipReadoutAvailable = (node, event, styles = new Map()) => {
    if (pointerTargetPaint(node, styles)) return true;

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
