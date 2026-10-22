
  const stateCueGeometry = ['x', 'y', 'width', 'height', 'rx', 'ry', 'cx', 'cy', 'r', 'x1', 'y1', 'x2', 'y2',
    'd', 'points', 'transform', 'fill-rule', 'clip-path'];
  const stateCueStyle = (node, name, value) => {
    if (node.style.getPropertyValue(name) !== value) node.style.setProperty(name, value);
  };
  const createSelectionCuePaint = (kind) => {
    const paint = interactionOutlineNode('g', 'data-cfx-state-outline');
    for (const key of ['halo', 'ink']) {
      const shape = interactionOutlineNode(kind, 'data-cfx-state-' + key);
      shape.setAttribute('fill', 'none'); shape.setAttribute('vector-effect', 'non-scaling-stroke');
      paint.appendChild(shape);
    }
    return paint;
  };
  const styleSelectionCuePaint = (paint, selected, width) => {
    stateCueStyle(paint, '--cfx-state-color', selected ? 'rgb(56, 189, 248)' : 'var(--cfx-host-accent, var(--cfx-default-host-accent))');
    stateCueStyle(paint, '--cfx-state-halo-width', String(width + 6));
    stateCueStyle(paint, '--cfx-state-ink-width', String(width + 3));
  };
  const refreshSelectionCuePaint = (root, nodes) => {
    let cache = root._cfxSelectionCuePaint;
    if (!cache && !root.hasAttribute('data-cfx-has-selection') && !root._cfxHostHighlight) return;
    cache ||= root._cfxSelectionCuePaint = new Map();
    const desired = new Set(), styles = new Map();
    for (const node of nodes || dataTargets(root)) {
      const selected = node.classList.contains('cfx-selected');
      if ((!selected && !node.classList.contains('cfx-highlighted')) || !pointerTargetPaint(node, styles)) continue;
      const shapes = node.matches(paintShapes) ? [node] : Array.from(node.querySelectorAll(paintShapes));
      const primary = shapes.filter(shape => shape.matches('[data-cfx-role^="circle-value"],[data-cfx-role^="gauge-value"],[data-cfx-role="gauge-needle"],[data-cfx-role="bullet-value"],[data-cfx-role="sunburst-segment-mark"],[data-cfx-role="treemap-tile-mark"]'));
      const geometryByParent = new Map();
      let geometricCue = false;
      for (const source of primary.length ? primary : shapes) {
        if (!shapePaint(source, styles, node)) continue;

        const signature = source.localName + stateCueGeometry.map(key => source.getAttribute(key) || '').join('|');
        let seen = geometryByParent.get(source.parentNode);
        if (!seen) geometryByParent.set(source.parentNode, seen = new Set());
        if (seen.has(signature)) continue;
        seen.add(signature); desired.add(source); geometricCue = true;
        let paint = cache.get(source);
        if (!paint || paint.parentNode !== source.parentNode) {
          paint?.remove(); paint = createSelectionCuePaint(source.localName);
          source.parentNode.insertBefore(paint, source); cache.set(source, paint);
        }
        const native = paintStyle(source, styles), width = parseFloat(native.strokeWidth) || 0;
        styleSelectionCuePaint(paint, selected, width);
        for (const shape of paint.children) {
          for (const key of stateCueGeometry) {
            const value = source.getAttribute(key);
            if (value === null) { if (shape.hasAttribute(key)) shape.removeAttribute(key); }
            else if (shape.getAttribute(key) !== value) shape.setAttribute(key, value);
          }
          for (const key of ['stroke-dasharray', 'stroke-dashoffset', 'stroke-linecap', 'stroke-linejoin'])
            stateCueStyle(shape, key, native.getPropertyValue(key));
        }
      }
      if (!geometricCue) {
        const text = primaryTextPaint(node, false, styles)?.textNode;
        const footprint = text && interactionOutlineFootprint(text);
        if (footprint) {
          desired.add(node);
          let paint = cache.get(node);
          if (!paint || paint.parentNode !== footprint.svg) {
            paint?.remove(); paint = createSelectionCuePaint('path');
            paint.setAttribute('data-cfx-state-footprint', ''); footprint.svg.appendChild(paint); cache.set(node, paint);
          }
          styleSelectionCuePaint(paint, selected, 0);
          for (const shape of paint.children) if (shape.getAttribute('d') !== footprint.path) shape.setAttribute('d', footprint.path);
        }
      }
    }
    for (const [source, paint] of cache) if (!desired.has(source)) { paint.remove(); cache.delete(source); }
  };
