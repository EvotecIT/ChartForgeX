  // Retained native facts need a keyboard readout without becoming pointer targets.
  const keyboardTargetAvailable = (node) => {
    if (node.closest('[aria-hidden="true"]') || !paintAncestorsVisible(node, new Map())) return false;
    // Muted data leaves navigation; its legend remains an entry point for restoring the series.
    if (renderedTargetKind(node) !== 'legend' && node.closest('.cfx-series-muted')) return false;
    const style = getComputedStyle(node);
    if (style.display === 'none' || style.visibility === 'hidden' || style.visibility === 'collapse') return false;
    if (pointerTargetPaint(node)) return true;
    // Retained authored facts can have no filled geometry, while still belonging to the data component.
    const data = node.dataset;
    if (!data.cfxTargetKind || !data.cfxTargetId || !['zero', 'precision-collapse'].includes(data.cfxGeometryStatus)) return false;
    const svg = node.ownerSVGElement;
    if (!svg) return false;
    // The viewport detects hidden hosts; CSS-hidden inner groups need their own ancestor check.
    for (let parent = node.parentElement; parent && parent !== svg; parent = parent.parentElement) {
      if (getComputedStyle(parent).display === 'none') return false;
    }
    const viewport = svg.getBoundingClientRect();
    return viewport.width > 0 && viewport.height > 0;
  };
  const keyboardTargets = (root) => interactiveTargets(root).filter(keyboardTargetAvailable);
  const refreshKeyboardNavigation = (root) => {
    if (!hasFeature(root, 'KeyboardNavigation')) return;
    const active = root.ownerDocument.activeElement;
    const targets = interactiveTargets(root);
    let unavailableFocus = false;
    targets.forEach((node) => {
      const focusNode = targetFocusNode(node);
      const available = keyboardTargetAvailable(node);
      focusNode.setAttribute('tabindex', available ? '0' : '-1');
      if (focusNode === active && !available) unavailableFocus = true;
    });
    if (unavailableFocus) {
      const replacement = targets.find(keyboardTargetAvailable);
      if (replacement) {
        const focusNode = targetFocusNode(replacement);
        try { focusNode.focus({ preventScroll: true }); } catch { focusNode.focus(); }
      }
      else if (active && active.blur) active.blur();
    }
  };
  const prepareKeyboardNavigation = (root) => {
    if (!hasFeature(root, 'KeyboardNavigation')) return;
    refreshKeyboardNavigation(root);
    // Host styles and native paint can change after binding. Tab order and arrows share the same eligibility owner.
    const observer = new MutationObserver(() => {
      if (!root.isConnected) { observer.disconnect(); return; }
      refreshKeyboardNavigation(root);
    });
    observer.observe(root, { subtree: true, attributes: true,
      attributeFilter: ['class', 'style', 'hidden', 'aria-hidden', 'fill', 'stroke', 'opacity', 'fill-opacity', 'stroke-opacity', 'clip-path'] });
    const stage = root.querySelector('.cfx-stage');
    if (stage && typeof ResizeObserver !== 'undefined') {
      const resize = new ResizeObserver(() => {
        if (!root.isConnected) { resize.disconnect(); observer.disconnect(); return; }
        refreshKeyboardNavigation(root);
      });
      resize.observe(stage);
    }
  };
