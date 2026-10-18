  // WebKit needs native SVG focus paint; source geometry and hit surfaces stay intact.
  const clearKeyboardFocusPaint = (root) => {
    root._cfxKeyboardFocusPaint?.remove();
    delete root._cfxKeyboardFocusPaint;
    root.removeAttribute('data-cfx-keyboard-focus');
  };
  const refreshKeyboardFocusPaint = (root) => {
    if (!hasFeature(root, 'KeyboardNavigation') || root._cfxPointerFocus === true) { clearKeyboardFocusPaint(root); return; }
    const focused = root.getRootNode().activeElement;
    const node = interactiveTargets(root).find(node => targetFocusNode(node) === focused);
    const svg = node?.ownerSVGElement, matrix = svg?.getScreenCTM();
    if (!node || !focused.matches(':focus-visible') || !matrix || !keyboardTargetAvailable(node)) {
      clearKeyboardFocusPaint(root); return;
    }
    const box = nativeRegionScreenBox(node) || nativeRegionScreenBox(node, node.getBBox());
    if (!box || !Object.values(box).every(Number.isFinite)) { clearKeyboardFocusPaint(root); return; }
    // Keep compact marks visible without shrinking larger targets.
    const centerX = (box.left + box.right) / 2, centerY = (box.top + box.bottom) / 2;
    const halfWidth = Math.max(5, (box.right - box.left) / 2 + 3), halfHeight = Math.max(5, (box.bottom - box.top) / 2 + 3);
    const inverse = matrix.inverse();
    const points = [[centerX - halfWidth, centerY - halfHeight], [centerX + halfWidth, centerY - halfHeight],
      [centerX + halfWidth, centerY + halfHeight], [centerX - halfWidth, centerY + halfHeight]]
      .map(([x, y]) => new DOMPoint(x, y).matrixTransform(inverse));
    const path = points.map((point, index) => (index ? 'L ' : 'M ') + point.x + ' ' + point.y).join(' ') + ' Z';
    let paint = root._cfxKeyboardFocusPaint;
    if (!paint || paint.ownerSVGElement !== svg) {
      clearKeyboardFocusPaint(root);
      paint = interactionOutlineNode('g', 'data-cfx-keyboard-focus-outline', 'interaction-focus');
      for (const attribute of ['data-cfx-focus-halo', 'data-cfx-focus-ink']) {
        const shape = interactionOutlineNode('path', attribute, 'interaction-focus');
        shape.setAttribute('fill', 'none'); shape.setAttribute('vector-effect', 'non-scaling-stroke');
        paint.appendChild(shape);
      }
      svg.appendChild(paint); root._cfxKeyboardFocusPaint = paint;
    }
    for (const shape of paint.children) if (shape.getAttribute('d') !== path) shape.setAttribute('d', path);
    root.setAttribute('data-cfx-keyboard-focus', 'true');
  };
