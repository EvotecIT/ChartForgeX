  const syncGraphItemTabStops = (root) => {
    const focusableSvg = hasFeature(root, 'Selection') && root.dataset.cfxGraphRendererActive !== 'canvas';
    items(root, '[data-cfx-role="graph-node"],[data-cfx-role="graph-edge"]').forEach(item => item.setAttribute('tabindex', focusableSvg ? '0' : '-1'));
    items(root, '[data-cfx-role="graph-cluster"]').forEach(cluster => {
      const collapsed = attr(cluster, 'data-cluster-collapsed') === 'true';
      cluster.setAttribute('tabindex', focusableSvg && collapsed ? '0' : '-1');
      cluster.setAttribute('aria-hidden', root.dataset.cfxGraphRendererActive === 'canvas' || !collapsed ? 'true' : 'false');
    });
  };
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
    tip.textContent = details.length === 1 ? [tooltipDetail.label || tooltipDetail.id, tooltipDetail.kind, tooltipDetail.status].filter(Boolean).join(' / ') : details.length ? `${details.length} selected` : '';
    tip.hidden = details.length === 0;
  };
  const syncClusterControls = (root) => {
    const pressed = root.dataset.cfxGraphClusters === 'collapsed' ? 'true' : 'false';
    items(root, "[data-cfx-graph-action='clusters']").forEach(button => button.setAttribute('aria-pressed', pressed));
  };
  const syncFocusControls = (root) => {
    const pressed = root.dataset.cfxGraphFocus === 'active' ? 'true' : 'false';
    items(root, "[data-cfx-graph-action='focus']").forEach(button => button.setAttribute('aria-pressed', pressed));
  };
