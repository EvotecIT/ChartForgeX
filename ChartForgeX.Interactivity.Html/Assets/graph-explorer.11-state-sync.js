  const syncGraphItemTabStops = (root) => {
    const focusableSvg = hasFeature(root, 'Selection') && root.dataset.cfxGraphRendererActive === 'svg';
    const acceleratedSvg = focusableSvg && attr(root, 'data-cfx-graph-accelerated-markup') === 'true';
    const canvas = root.querySelector('[data-cfx-role="graph-canvas"]');
    const webgl = root.querySelector('[data-cfx-role="graph-webgl"]');
    const scene = root.querySelector('[data-cfx-role="graph-scene"]');
    if (canvas) {
      canvas.setAttribute('tabindex', hasFeature(root, 'Selection') && root.dataset.cfxGraphRendererActive === 'canvas' ? '0' : '-1');
      canvas.setAttribute('role', 'img');
      canvas.setAttribute('aria-label', canvas.getAttribute('aria-label') || attr(root, 'data-cfx-graph-title') || attr(root, 'data-cfx-graph-id') || 'Graph canvas');
    }
    if (webgl) {
      webgl.setAttribute('tabindex', hasFeature(root, 'Selection') && root.dataset.cfxGraphRendererActive === 'webgl' ? '0' : '-1');
      webgl.setAttribute('role', 'img');
      webgl.setAttribute('aria-label', webgl.getAttribute('aria-label') || attr(root, 'data-cfx-graph-title') || attr(root, 'data-cfx-graph-id') || 'Graph WebGL canvas');
    }
    if (scene) {
      scene.setAttribute('tabindex', acceleratedSvg ? '0' : '-1');
      if (acceleratedSvg) scene.setAttribute('aria-keyshortcuts', 'ArrowUp ArrowDown ArrowLeft ArrowRight Home End Enter Space');
      else scene.removeAttribute('aria-keyshortcuts');
    }
    const graphItems = items(root, '[data-cfx-role="graph-node"],[data-cfx-role="graph-edge"],[data-cfx-role="graph-cluster"]');
    const focusableItems = graphItems.filter(item => visible(item) && (attr(item, 'data-cfx-role') !== 'graph-cluster' || attr(item, 'data-cluster-collapsed') === 'true'));
    const itemKey = item => `${attr(item, 'data-cfx-role')}:${attr(item, 'data-node-id') || attr(item, 'data-edge-id') || attr(item, 'data-cluster-id')}`;
    const preferred = root.dataset.cfxGraphKeyboardItem || '';
    const active = focusableItems.find(item => itemKey(item) === preferred)
      || focusableItems.find(item => item.classList.contains('cfx-graph-selected'))
      || focusableItems[0];
    graphItems.forEach(item => {
      item.setAttribute('tabindex', focusableSvg && !acceleratedSvg && item === active ? '0' : '-1');
      if (focusableSvg && !acceleratedSvg && item === active) item.setAttribute('aria-keyshortcuts', 'ArrowUp ArrowDown ArrowLeft ArrowRight Home End Enter Space');
      else item.removeAttribute('aria-keyshortcuts');
    });
    if (focusableSvg && active) root.dataset.cfxGraphKeyboardItem = itemKey(active);
    items(root, '[data-cfx-role="graph-cluster"]').forEach(cluster => {
      const collapsed = attr(cluster, 'data-cluster-collapsed') === 'true';
      cluster.setAttribute('aria-hidden', root.dataset.cfxGraphRendererActive === 'canvas' || !collapsed ? 'true' : 'false');
    });
  };
  const syncSelectedEdgeLabels = (root) => { const labels = new Map(items(root, '[data-cfx-role="graph-edge-label"]').map(label => [attr(label, 'data-edge-label-for'), label])); labels.forEach(label => label.classList.remove('cfx-graph-label-selected')); items(root, '[data-cfx-role="graph-edge"].cfx-graph-selected').forEach(edge => labels.get(attr(edge, 'data-edge-id'))?.classList.add('cfx-graph-label-selected')); };
  const clearHiddenSelections = (root) => {
    let changed = false;
    let focusedSelectionHidden = false;
    const focusNode = root.dataset.cfxGraphFocusNode || '';
    items(root, '.cfx-graph-selected').forEach(item => {
      const expandedCluster = attr(item, 'data-cfx-role') === 'graph-cluster' && item.classList.contains('cfx-graph-cluster-expanded');
      if (visible(item) && !expandedCluster) return;
      const id = attr(item, 'data-node-id') || attr(item, 'data-edge-id') || attr(item, 'data-cluster-id');
      focusedSelectionHidden = focusedSelectionHidden || (root.dataset.cfxGraphFocus === 'active' && id === focusNode);
      item.classList.remove('cfx-graph-selected');
      changed = true;
    });
    if (!changed) return false;
    const details = updateSelectionState(root);
    syncSelectionTooltip(root, details);
    if (focusedSelectionHidden) clearNeighborhoodFocus(root);
    return true;
  };
  const syncSelectionTooltip = (root, details, fallback) => {
    const tip = root.querySelector('.cfx-graph-tooltip');
    if (!tip) return;
    const tooltipDetail = details.length === 1 ? details[0] : fallback;
    tip.textContent = details.length === 1 ? [tooltipDetail.label || tooltipDetail.id, tooltipDetail.secondaryLabel, tooltipDetail.kind, tooltipDetail.status, tooltipDetail.badge ? `Badge ${tooltipDetail.badge}` : ''].filter(Boolean).join(' / ') : details.length ? `${details.length} selected` : '';
    tip.hidden = details.length === 0;
  };
  const syncClusterControls = (root) => {
    const pressed = root.dataset.cfxGraphClusters === 'collapsed' ? 'true' : 'false';
    items(root, "[data-cfx-graph-action='clusters']").forEach(button => {
      button.setAttribute('aria-pressed', pressed);
      button.setAttribute('aria-label', pressed === 'true' ? 'Expand clusters' : 'Collapse clusters');
      button.setAttribute('title', pressed === 'true' ? 'Expand clusters' : 'Collapse clusters');
    });
  };
  const syncFocusControls = (root) => {
    const pressed = root.dataset.cfxGraphFocus === 'active' ? 'true' : 'false';
    items(root, "[data-cfx-graph-action='focus']").forEach(button => {
      button.setAttribute('aria-pressed', pressed);
      button.setAttribute('aria-label', pressed === 'true' ? 'Clear neighborhood focus' : 'Focus selected node neighborhood');
      button.setAttribute('title', pressed === 'true' ? 'Clear neighborhood focus' : 'Focus selected node neighborhood');
    });
  };
