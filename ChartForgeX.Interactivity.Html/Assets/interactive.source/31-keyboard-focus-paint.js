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
    if (!node || !focused.matches(':focus-visible') || !keyboardTargetAvailable(node)) {
      clearKeyboardFocusPaint(root); return;
    }
    const footprint = interactionOutlineFootprint(node);
    if (!footprint) { clearKeyboardFocusPaint(root); return; }
    const { svg, path } = footprint;
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
