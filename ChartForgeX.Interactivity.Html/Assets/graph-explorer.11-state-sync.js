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
    items(root, '.cfx-graph-selected').forEach(item => {
      if (visible(item)) return;
      item.classList.remove('cfx-graph-selected');
      changed = true;
    });
    if (!changed) return false;
    updateSelectionState(root);
    return true;
  };
