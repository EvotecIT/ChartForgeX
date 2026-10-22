

  const withNativeHighlightPaint = (node, read) => {
    const root = node && node.closest('.cfx-interactive-chart');
    if (!root || !root._cfxBranchHighlights || root.hasAttribute('data-cfx-highlight-probe')) return read();
    root.setAttribute('data-cfx-highlight-probe', '');
    try { return read(); } finally { root.removeAttribute('data-cfx-highlight-probe'); }
  };


  const syncCartesianLinkedPaint = (root, nodes) => {
    const owners = new Map();
    for (const owner of root.querySelectorAll('[data-cfx-role="series"][data-cfx-coordinate-system="cartesian"]')) {
      const connected = ['line', 'stepline', 'area', 'steparea', 'stackedarea', 'trendline', 'slope'].includes((owner.dataset.cfxKind || '').toLowerCase());
      owners.set(owner, { active: false, dim: false, connected, isolated: owner.classList.contains('cfx-series-isolated-out') });
    }
    const observations = new Map();
    for (const node of nodes) {
      const owner = node.closest('[data-cfx-role="series"]');
      const state = owners.get(owner);
      if (!state) continue;
      const active = node.classList.contains('cfx-selected') || node.classList.contains('cfx-highlighted');
      const dim = node.classList.contains('cfx-selection-dim') || node.classList.contains('cfx-linked-dim');
      state.active ||= active;
      state.dim ||= dim;
      if (node.dataset.cfxPoint !== undefined) observations.set(owner.dataset.cfxSeries + ':' + node.dataset.cfxPoint,
        { active, dim, isolated: node.classList.contains('cfx-series-isolated-out') });
    }
    for (const [owner, state] of owners) {
      owner.classList.toggle('cfx-linked-series-active', state.connected && state.active);
      owner.classList.toggle('cfx-linked-series-dim', state.connected && state.dim && !state.active);
    }


    const series = new Map(Array.from(owners, ([owner, state]) => [owner.dataset.cfxSeries, state]));
    for (const legend of root.querySelectorAll('[data-cfx-role="legend-item"][data-cfx-series]')) {
      const state = legend.dataset.cfxPoint === undefined ? series.get(legend.dataset.cfxSeries)
        : observations.get(legend.dataset.cfxSeries + ':' + legend.dataset.cfxPoint);
      legend.classList.toggle('cfx-linked-series-active', state?.active === true);
      legend.classList.toggle('cfx-linked-series-dim', state?.dim === true && !state.active);
      if (state) legend.classList.toggle('cfx-series-isolated-out', state.isolated);
    }
  };
  const prepareRootBranchHighlights = (root) => {
    if (root.dataset.cfxHighlightMode !== 'root-branch') return;

    const nodes = Array.from(root.querySelectorAll('[data-cfx-kind="sunburst" i][data-cfx-target-kind="node"]')).filter(node => !isPassiveTarget(node));
    if (!nodes.length) return;
    root._cfxBranchHighlights = nodes;
    nodes.forEach((node) => {
      node.dataset.cfxHighlightManaged = 'true';
      const paint = document.createElementNS('http://www.w3.org/2000/svg', 'g');
      paint.setAttribute('data-cfx-highlight-paint', '');

      Array.from(node.children).filter(child => !child.matches('title,desc')).forEach(child => paint.appendChild(child));
      node.appendChild(paint);
    });
  };
  const highlightStyle = (root, group) => {
    const prefix = 'data-cfx-highlight-' + group + '-';
    const result = {};
    ['stroke', 'stroke-width', 'opacity'].forEach(key => {
      const value = root.getAttribute(prefix + key);
      if (value !== null) result[key] = value;
    });
    return result;
  };
  const interactionOutlineNode = (kind, attribute, role = 'interaction-highlight') => {
    const outline = document.createElementNS('http://www.w3.org/2000/svg', kind);
    outline.setAttribute('data-cfx-role', role);
    outline.setAttribute(attribute, '');
    outline.setAttribute('aria-hidden', 'true');
    outline.setAttribute('focusable', 'false');
    return outline;
  };

  const interactionOutlineFootprint = (node) => {
    const svg = node?.ownerSVGElement, matrix = svgScreenMatrix(svg);
    if (!matrix) return null;
    const box = nativeRegionScreenBox(node) || nativeRegionScreenBox(node, node.getBBox());
    if (!box || !Object.values(box).every(Number.isFinite)) return null;
    const centerX = (box.left + box.right) / 2, centerY = (box.top + box.bottom) / 2;
    const halfWidth = Math.max(5, (box.right - box.left) / 2 + 3), halfHeight = Math.max(5, (box.bottom - box.top) / 2 + 3);
    const inverse = matrix.inverse();
    const points = [[centerX - halfWidth, centerY - halfHeight], [centerX + halfWidth, centerY - halfHeight],
      [centerX + halfWidth, centerY + halfHeight], [centerX - halfWidth, centerY + halfHeight]]
      .map(([x, y]) => new DOMPoint(x, y).matrixTransform(inverse));
    return { svg, path: points.map((point, index) => (index ? 'L ' : 'M ') + point.x + ' ' + point.y).join(' ') + ' Z' };
  };
  const highlightOutline = (paint, style) => {
    const primary = paint.querySelector('[data-cfx-role="sunburst-segment-mark"]');
    let outline = paint.querySelector('[data-cfx-highlight-outline]');
    if (!primary || style.stroke === undefined && style['stroke-width'] === undefined) {
      if (outline) outline.remove();
      return;
    }
    // Clone geometry, not IDs/targets/hatch/focus/paint.
    if (!outline) {
      outline = interactionOutlineNode(primary.localName, 'data-cfx-highlight-outline');
      paint.insertBefore(outline, paint.firstChild);
    }
    ['d', 'transform', 'fill-rule'].forEach(key => {
      const value = primary.getAttribute(key);
      if (value === null) outline.removeAttribute(key); else outline.setAttribute(key, value);
    });
    const native = getComputedStyle(primary);
    const width = Number(style['stroke-width'] ?? 2.5);

    paint.style.setProperty('--cfx-highlight-stroke', style.stroke ?? native.stroke);
    paint.style.setProperty('--cfx-highlight-stroke-width', width > 0 ? String(Math.min(Number.MAX_VALUE, (parseFloat(native.strokeWidth) || 0) + width * 2)) : '0');
  };
  const clearRootBranchHighlight = (root) => {
    (root._cfxBranchHighlights || []).forEach(node => {
      delete node.dataset.cfxHighlightState;
      const paint = node.querySelector('[data-cfx-highlight-paint]');
      if (!paint) return;
      paint.style.removeProperty('--cfx-highlight-opacity');
      paint.style.removeProperty('--cfx-highlight-stroke');
      paint.style.removeProperty('--cfx-highlight-stroke-width');
      const outline = paint.querySelector('[data-cfx-highlight-outline]');
      if (outline) outline.remove();
    });
  };
  const refreshRootBranchHighlightAvailability = (root) => {
    let hiddenActive = false;
    withNativeHighlightPaint(root, () => (root._cfxBranchHighlights || []).forEach(node => {
      const available = !!pointerTargetPaint(node);
      node.toggleAttribute('data-cfx-highlight-unavailable', !available);

      if (!available && node.dataset.cfxHighlightState === 'item' && node.dataset.cfxGeometryStatus === 'visible') hiddenActive = true;
    }));
    // Local paint loss keeps peer hover.
    if (hiddenActive) clearHover(root, true, false);
  };
  const applyRootBranchHighlight = (root, active) => {
    const nodes = root._cfxBranchHighlights;
    if (!nodes || !nodes.includes(active)) return false;
    if (!tooltipReadoutAvailable(active, null)) return false;
    const branch = active.dataset.cfxBranchRoot;
    const graphite = root.dataset.cfxLook === 'graphite';
    const branchStyle = highlightStyle(root, 'branch'), itemStyle = highlightStyle(root, 'item');
    const otherItemStyle = highlightStyle(root, 'other-item'), otherBranchStyle = highlightStyle(root, 'other-branch');
    withNativeHighlightPaint(root, () => nodes.forEach(node => {
      const ownBranch = node.dataset.cfxBranchRoot;
      const highlighted = node === active;
      const related = branch && ownBranch === branch;

      let style = {}, state = 'neutral';
      if (highlighted) { style = { opacity: '1', ...(branch ? branchStyle : {}), ...itemStyle }; state = 'item'; }
      else if (related) { style = { opacity: graphite ? '1' : '.82', ...branchStyle, ...otherItemStyle }; state = 'branch'; }
      else if (branch && ownBranch) { style = { opacity: graphite ? '.3' : '.38', ...otherBranchStyle }; state = 'other-branch'; }
      node.dataset.cfxHighlightState = state;
      setNodeHovered(node, highlighted, !!related && !highlighted);
      const paint = node.querySelector('[data-cfx-highlight-paint]');
      paint.style.setProperty('--cfx-highlight-opacity', style.opacity ?? '1');
      highlightOutline(paint, style);
    }));
    root.dataset.cfxHovering = 'true';
    root.dataset.cfxHoverMode = 'root-branch';
    root.dataset.cfxHoverUnit = 'branch';
    root.dataset.cfxHoverLabel = text(active);
    refreshRootBranchHighlightAvailability(root);
    return true;
  };
