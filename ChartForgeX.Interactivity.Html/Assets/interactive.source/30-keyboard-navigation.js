  // Retained native facts need a keyboard readout without becoming pointer targets.
  const keyboardTargetAvailable = (node) => {
    if (node.closest('[aria-hidden="true"]') || !paintAncestorsVisible(node, new Map())) return false;
    // Muted data leaves navigation; its legend remains an entry point for restoring the series.
    if (renderedTargetKind(node) !== 'legend' && node.closest('.cfx-series-muted')) return false;
    const style = getComputedStyle(node);
    if (style.display === 'none' || style.visibility === 'hidden' || style.visibility === 'collapse') return false;
    const box = node.getBoundingClientRect();
    if (box.width > 0 || box.height > 0) return true;
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
