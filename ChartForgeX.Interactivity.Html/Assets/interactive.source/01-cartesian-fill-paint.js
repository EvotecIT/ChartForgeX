  const cartesianFillObservation = (node, subject) => subject !== node && subject.hasAttribute('data-cfx-point')
    && ['area', 'steparea', 'stackedarea', 'differencearea', 'rangearea', 'rangeband'].includes((subject.dataset.cfxKind || '').toLowerCase())
    && /^(area|difference-(positive|negative)|range-(area|band))(-pattern)?$/.test(node.dataset.cfxRole || '');
  const cartesianFillOwnsObservation = (node, subject) => {
    // Fill and hatch share non-paint ownership by observation index, independently of their coordinates.
    const segment = node.closest('[data-cfx-role="fill-segment"]');
    if (!segment) return false;
    const first = Number(segment.dataset.cfxFillPointStart), count = Number(segment.dataset.cfxFillPointCount);
    const index = Number(subject.dataset.cfxPoint);
    return Number.isInteger(first) && Number.isInteger(count) && Number.isInteger(index)
      && first >= 0 && count > 0 && index >= first && index - first < count;
  };
  const cartesianFillStrokeInk = (node, subject, paint) => {
    if (!cartesianFillOwnsObservation(node, subject)) return false;
    if (pairedExtentObservation(subject)) return pairedBoundaryInk(node, subject, paint);
    const region = subject._cfxNativeRegion;
    // Reuse the source-bounded stroke/clip query with this scalar's captured centre as both endpoints.
    return region && pairedBoundaryInk(node, subject, paint, {
      x: region.x + region.width / 2, y: region.y + region.height / 2, width: 0, height: 0
    });
  };
  let nativeFillClipContext;
  const nativeFillEnvelope = (node, styles) => {
    const role = node.dataset.cfxRole || '';
    if (!role.endsWith('-pattern')) return typeof node.isPointInFill === 'function'
      ? { key: node, matrix: node.getScreenCTM(), contains: point => node.isPointInFill(point) } : null;
    // Native hatches are open strokes clipped by an envelope. The browser's native path parser
    // queries that clip with its clip-rule, independently of the base fill's visibility or fill-rule.
    for (let parent = node.parentElement; parent && parent.ownerSVGElement; parent = parent.parentElement) {
      const reference = /^url\(#([^)]*)\)$/.exec(parent.getAttribute('clip-path') || '');
      const clip = reference && parent.ownerSVGElement.getElementById(reference[1]);
      if (!clip || clip.getAttribute('clipPathUnits') !== 'userSpaceOnUse' || clip.children.length !== 1
        || !clip.firstElementChild.matches('path')) continue;
      const shape = clip.firstElementChild;
      let matrix = parent.getScreenCTM();
      if (!matrix) return null;
      for (const element of [clip, shape]) for (let i = 0; i < element.transform.baseVal.numberOfItems; i++)
        matrix = matrix.multiply(element.transform.baseVal.getItem(i).matrix);
      const paths = styles._cfxFillClipPaths ||= new WeakMap();
      let path = paths.get(shape);
      if (!path) {
        path = { geometry: new Path2D(shape.getAttribute('d') || ''), rule: paintStyle(shape, styles).clipRule };
        paths.set(shape, path);
      }
      if (!nativeFillClipContext) {
        const canvas = document.createElement('canvas'); canvas.width = canvas.height = 1;
        nativeFillClipContext = canvas.getContext('2d');
      }
      return { key: shape, matrix, contains: point => nativeFillClipContext.isPointInPath(path.geometry, point.x, point.y, path.rule) };
    }
    return null;
  };
  const cartesianFillInk = (node, subject, styles) => {
    if (!cartesianFillOwnsObservation(node, subject)) return false;
    const surface = nativeFillEnvelope(node, styles), region = subject._cfxNativeRegion;
    const sourceMatrix = subject.getScreenCTM(), shapeMatrix = surface && surface.matrix, clips = nativePaintClips(node);
    if (!surface || !region || !sourceMatrix || !shapeMatrix || !clips) return false;
    // This cache lives only as long as the current paint/style probe. Hatches share one native envelope.
    const cache = styles._cfxFillInk ||= new WeakMap();
    let subjects = cache.get(surface.key);
    if (!subjects) { subjects = new WeakMap(); cache.set(surface.key, subjects); }
    if (subjects.has(subject)) return subjects.get(subject);
    const sourceInverse = sourceMatrix.inverse(), shapeInverse = shapeMatrix.inverse();
    const paired = pairedExtentObservation(subject), x = region.x + region.width / 2;
    // Scalar sources retain their source-location clip contract, even when their footprint overlaps paint.
    if (!paired && !paintWithinNativeClips(node, subject, styles)) return false;
    const low = paired ? region.y + region.width / 2 : region.y + region.height / 2;
    const high = paired ? region.y + region.height - region.width / 2 : low;
    // Native SVG uses three decimals. Interior probes below that quantum distinguish a real held
    // surface from a zero-width endpoint stem, which isPointInFill alone considers inside the path.
    const inset = .00025;
    const contains = (px, py) => {
      const screen = new DOMPoint(px, py).matrixTransform(sourceMatrix);
      if (clips.some(({ rect, matrix }) => {
        const point = screen.matrixTransform(matrix.inverse());
        return point.x <= rect.x || point.x >= rect.x + rect.width || point.y <= rect.y || point.y >= rect.y + rect.height;
      })) return false;
      return surface.contains(screen.matrixTransform(shapeInverse));
    };
    const atY = (y, halfHeight) => [x, Number(x.toFixed(3))].some(px => [-1, 1].some(direction =>
      [px + direction * inset / 2, px + direction * inset].every(sx =>
        [y - halfHeight, y + halfHeight].every(sy => contains(sx, sy)))));
    let visible = false;
    if (!paired) {
      visible = [low, Number(low.toFixed(3))].some(y => atY(y - inset, inset / 2) || atY(y + inset, inset / 2));
      if (!visible) visible = (subject._cfxFillInteriors || []).some(([px, py, radius]) =>
        [px, py, radius].every(Number.isFinite) && radius >= inset / 2
        && px - radius >= region.x && px + radius <= region.x + region.width
        && py - radius >= region.y && py + radius <= region.y + region.height
        && contains(px, py) && [-1, 1].every(dx => [-1, 1].every(dy => contains(px + dx * radius, py + dy * radius))));
    } else if (high > low) {
      // Most observations contain their own middle. A shifted Step hold can overlap only a different
      // part of the paired extent, so partition it using captured neighboring boundaries and clips.
      visible = atY(low / 2 + high / 2, Math.min(inset, (high - low) / 4));
      if (!visible) {
        const knots = new Set([low, high]);
        const add = (y) => { for (const value of [y, Number(y.toFixed(3))]) if (value > low && value < high) knots.add(value); };
        const owner = subject.closest('[data-cfx-role="series"]'), index = Number(subject.dataset.cfxPoint);
        const connected = point => point && point.getAttribute('data-cfx-break-before') !== 'true';
        for (const offset of [-1, 1]) {
          const neighbor = owner && owner.querySelector('[data-cfx-role="point"][data-cfx-point="' + (index + offset) + '"]');
          if (!neighbor || !connected(offset < 0 ? subject : neighbor) || !neighbor._cfxNativeRegion) continue;
          const bounds = neighbor._cfxNativeRegion, matrix = neighbor.getScreenCTM();
          if (!matrix) continue;
          for (const y of [bounds.y + bounds.width / 2, bounds.y + bounds.height - bounds.width / 2])
            add(new DOMPoint(bounds.x + bounds.width / 2, y).matrixTransform(matrix).matrixTransform(sourceInverse).y);
        }
        for (const { rect, matrix } of clips) for (const cy of [rect.y, rect.y + rect.height])
          add(new DOMPoint(rect.x, cy).matrixTransform(matrix).matrixTransform(sourceInverse).y);
        const sorted = Array.from(knots).sort((a, b) => a - b);
        for (let i = 1; i < sorted.length && !visible; i++)
          visible = atY(sorted[i - 1] / 2 + sorted[i] / 2, Math.min(inset, (sorted[i] - sorted[i - 1]) / 4));
      }
    }
    subjects.set(subject, visible);
    return visible;
  };
