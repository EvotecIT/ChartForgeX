  const graphKeyboardItemKey = (item) => `${attr(item, 'data-cfx-role')}:${attr(item, 'data-node-id') || attr(item, 'data-edge-id') || attr(item, 'data-cluster-id')}`;
  const graphKeyboardPoint = (root, item) => {
    const role = attr(item, 'data-cfx-role');
    if (role === 'graph-node') return { x: num(item, 'data-node-x', 0), y: num(item, 'data-node-y', 0) };
    if (role === 'graph-edge') {
      const state = root.__cfxGraphState || graphState(root);
      const edge = state.edges.find(candidate => candidate.el === item);
      return edge ? { x: (edge.source.x + edge.target.x) / 2, y: (edge.source.y + edge.target.y) / 2 } : { x: 0, y: 0 };
    }
    const match = attr(item, 'transform').match(/translate\(\s*([-\d.]+)[ ,]+([-\d.]+)/);
    return match ? { x: Number(match[1]), y: Number(match[2]) } : { x: 0, y: 0 };
  };
  const focusGraphItem = (root, item) => {
    items(root, '[data-cfx-role="graph-node"],[data-cfx-role="graph-edge"],[data-cfx-role="graph-cluster"]').forEach(candidate => {
      candidate.setAttribute('tabindex', candidate === item ? '0' : '-1');
      if (candidate === item) candidate.setAttribute('aria-keyshortcuts', 'ArrowUp ArrowDown ArrowLeft ArrowRight Home End Enter Space');
      else candidate.removeAttribute('aria-keyshortcuts');
    });
    root.dataset.cfxGraphKeyboardItem = graphKeyboardItemKey(item);
    item.focus();
    const label = attr(item, 'data-node-label') || attr(item, 'data-edge-label') || attr(item, 'data-cluster-label') || 'Graph item';
    const announcer = root.querySelector('[data-cfx-role="graph-announcer"]');
    if (announcer) announcer.textContent = label;
  };
  const moveGraphItemFocus = (root, item, key) => {
    const candidates = items(root, '[data-cfx-role="graph-node"],[data-cfx-role="graph-edge"],[data-cfx-role="graph-cluster"]').filter(candidate => visible(candidate) && (attr(candidate, 'data-cfx-role') !== 'graph-cluster' || attr(candidate, 'data-cluster-collapsed') === 'true'));
    if (!candidates.length) return false;
    if (key === 'Home' || key === 'End') {
      focusGraphItem(root, key === 'Home' ? candidates[0] : candidates[candidates.length - 1]);
      return true;
    }
    const origin = graphKeyboardPoint(root, item);
    const vertical = key === 'ArrowUp' || key === 'ArrowDown';
    const direction = key === 'ArrowLeft' || key === 'ArrowUp' ? -1 : 1;
    let best = null;
    let bestScore = Number.POSITIVE_INFINITY;
    candidates.forEach(candidate => {
      if (candidate === item) return;
      const point = graphKeyboardPoint(root, candidate);
      const forward = (vertical ? point.y - origin.y : point.x - origin.x) * direction;
      if (forward <= .5) return;
      const across = Math.abs(vertical ? point.x - origin.x : point.y - origin.y);
      const score = forward + across * 1.8 + Math.hypot(point.x - origin.x, point.y - origin.y) * .08;
      if (score < bestScore) { best = candidate; bestScore = score; }
    });
    if (!best) {
      const index = Math.max(0, candidates.indexOf(item));
      best = candidates[(index + direction + candidates.length) % candidates.length];
    }
    focusGraphItem(root, best);
    return true;
  };
  const moveAcceleratedGraphSelection = (root, event) => {
    if (!['ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown', 'Home', 'End'].includes(event.key)) return false;
    const nodes = (root.__cfxGraphState || graphState(root)).nodes.filter(node => visible(node.el));
    if (!nodes.length) return false;
    const current = nodes.findIndex(node => node.id === root.dataset.cfxGraphSelectionPrimary);
    const next = event.key === 'Home' ? 0 : event.key === 'End' ? nodes.length - 1 : event.key === 'ArrowLeft' || event.key === 'ArrowUp' ? (current <= 0 ? nodes.length - 1 : current - 1) : (current + 1) % nodes.length;
    event.preventDefault();
    select(root, nodes[next].el);
    const surface = event.currentTarget;
    surface?.setAttribute('aria-label', `${attr(root, 'data-cfx-graph-title') || 'Graph'}. Current item: ${nodes[next].label || nodes[next].id}. Use arrow keys to move and Enter or Space to select.`);
    return true;
  };
  const bindGraphItemSelection = (root, item) => {
    if (attr(item, 'data-cfx-graph-selection-bound') === 'true') return;
    item.setAttribute('data-cfx-graph-selection-bound', 'true');
    item.addEventListener('focus', () => {
      root.dataset.cfxGraphKeyboardItem = graphKeyboardItemKey(item);
      items(root, '[data-cfx-role="graph-node"],[data-cfx-role="graph-edge"],[data-cfx-role="graph-cluster"]').forEach(candidate => {
        candidate.setAttribute('tabindex', candidate === item ? '0' : '-1');
        if (candidate === item) candidate.setAttribute('aria-keyshortcuts', 'ArrowUp ArrowDown ArrowLeft ArrowRight Home End Enter Space');
        else candidate.removeAttribute('aria-keyshortcuts');
      });
    });
    item.addEventListener('click', event => {
      const id = attr(item, 'data-node-id') || attr(item, 'data-edge-id') || attr(item, 'data-cluster-id');
      if (id && root.__cfxGraphSuppressClickId === id) {
        root.__cfxGraphSuppressClickId = '';
        event.preventDefault();
        return;
      }
      if (id && root.__cfxGraphPointerSelectionId === id && Date.now() - (root.__cfxGraphPointerSelectionTick || 0) < 250) return;
      select(root, item, { additive: event.ctrlKey || event.metaKey || event.shiftKey, toggle: event.ctrlKey || event.metaKey || event.shiftKey });
    });
    item.addEventListener('keydown', event => {
      if (['ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown', 'Home', 'End'].includes(event.key)) {
        event.preventDefault();
        moveGraphItemFocus(root, item, event.key);
        return;
      }
      if (event.key === 'Enter' || event.key === ' ') {
        event.preventDefault();
        select(root, item, { additive: event.ctrlKey || event.metaKey || event.shiftKey, toggle: event.ctrlKey || event.metaKey || event.shiftKey });
      }
    });
  };
  const bindGraphSearch = (root) => {
    const search = root.querySelector('[data-cfx-graph-search]');
    if (!search) return;
    search.addEventListener('input', () => applyFilters(root));
    search.addEventListener('keydown', event => {
      if (event.key !== 'Enter') return;
      const query = search.value.trim().toLowerCase();
      const match = (root.__cfxGraphState || graphState(root)).nodes.find(node => visible(node.el) && (!query || searchable(node.el).includes(query)));
      if (!match) return;
      event.preventDefault();
      select(root, match.el);
      centerGraphNode(root, match.id);
    });
  };
