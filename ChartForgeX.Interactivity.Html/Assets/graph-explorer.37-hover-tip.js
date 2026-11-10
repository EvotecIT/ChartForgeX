  // Hover text follows the chart tooltip: plain text, a pointer-only delay, a 12px gap and an 8px viewport margin.
  let graphTipCount = 0;
  const hideGraphTip = root => {
    clearTimeout(root.__cfxGraphTipTimer);
    const tip = root.querySelector('.cfx-graph-hover-tip');
    if (tip) tip.hidden = true;
    root.__cfxGraphTipOwner?.removeAttribute('aria-describedby');
  };
  // A pointer event anchors edge text at the pointer; keyboard focus passes none and anchors at the item.
  const syncGraphHoverTip = (root, item, event) => {
    hideGraphTip(root);
    const text = item && attr(item, 'data-cfx-hover-text');
    if (!text) return;
    let tip = root.querySelector('.cfx-graph-hover-tip');
    if (!tip) {
      tip = Object.assign(root.ownerDocument.createElement('div'), { className: 'cfx-graph-hover-tip', id: `cfx-graph-tip-${++graphTipCount}`, hidden: true });
      tip.setAttribute('role', 'tooltip');
      root.append(tip);
      graphLifetime(root)?.own(() => tip.remove());
      ['pointerdown', 'wheel'].forEach(name => listenGraph(root, root.querySelector('.cfx-graph-stage'), name, () => hideGraphTip(root), { passive: true }));
    }
    const show = () => {
      if (root.matches('.cfx-graph-panning,.cfx-graph-dragging-node')) return;
      tip.textContent = text; tip.hidden = false;
      item.setAttribute('aria-describedby', tip.id);
      root.__cfxGraphTipOwner = item;
      const node = attr(item, 'data-cfx-role') === 'graph-node' && (root.__cfxGraphState || graphState(root)).byId.get(attr(item, 'data-node-id'));
      const rect = graphPointerSurface(root).getBoundingClientRect(), fit = graphSurfaceFit(sceneSize(root), rect.width, rect.height), view = viewport(root);
      const at = node || graphKeyboardPoint(root, item), pointer = event && !node, w = tip.offsetWidth, h = tip.offsetHeight, win = root.ownerDocument.defaultView;
      const cx = pointer ? event.clientX : rect.left + fit.offsetX + (at.x * view.scale + view.x) * fit.scale;
      const cy = pointer ? event.clientY : rect.top + fit.offsetY + (at.y * view.scale + view.y) * fit.scale;
      // Prefer below-right; flip an axis that would leave the viewport, then keep an 8px margin.
      const reach = (node ? (node.size * 1.6 + 4) * view.scale * fit.scale : 12) + 12;
      const place = (center, size, limit) => Math.min(Math.max(8, center + reach + size > limit - 8 ? center - reach - size : center + reach), Math.max(8, limit - size - 8));
      const x = place(cx, w, win.innerWidth), y = place(cy, h, win.innerHeight);
      tip.style.left = `${x}px`; tip.style.top = `${y}px`;
      // A transformed host contains fixed content; correct by the measured offset.
      const placed = tip.getBoundingClientRect();
      tip.style.left = `${2 * x - placed.left}px`; tip.style.top = `${2 * y - placed.top}px`;
    };
    const delay = num(root, 'data-cfx-tooltip-delay', 0);
    if (delay > 0 && event) root.__cfxGraphTipTimer = setTimeout(show, delay); else show();
  };
