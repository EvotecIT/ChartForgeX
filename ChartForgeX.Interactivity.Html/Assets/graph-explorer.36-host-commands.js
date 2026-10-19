  const graphLinkedItems = root => items(root, '[data-cfx-role="graph-node"],[data-cfx-role="graph-edge"],[data-cfx-role="graph-cluster"]');
  const graphRequestedItems = (root, requested) => {
    const keys = new Set((Array.isArray(requested) ? requested : []).filter(item => item && typeof item.id === 'string' && typeof item.role === 'string').map(item => item.role + ':' + item.id));
    return graphLinkedItems(root).filter(item => keys.has(graphKeyboardItemKey(item)));
  };
  const redrawGraphLinkedState = root => {
    const state = root.__cfxGraphState || graphState(root);
    drawCanvas(root, state);
    if (typeof updateOverview === 'function') updateOverview(root, state);
  };
  const selectGraphItems = (root, requested, options) => {
    if (!hasFeature(root, 'Selection')) return false;
    const mode = options?.mode || 'replace';
    if (!['replace', 'add', 'remove'].includes(mode)) return false;
    const multiple = hasFeature(root, 'MultiSelection');
    const requestedItems = graphRequestedItems(root, requested);
    const matches = new Set(!multiple && mode !== 'remove' ? requestedItems.slice(0, 1) : requestedItems);
    // Ordinary activation replaces selection without MultiSelection; unmatched additions remain a no-op.
    const replace = mode === 'replace' || mode === 'add' && !multiple && matches.size > 0;
    graphLinkedItems(root).forEach(item => {
      const selected = replace ? matches.has(item) : mode === 'add' ? matches.has(item) || item.classList.contains('cfx-graph-selected') : !matches.has(item) && item.classList.contains('cfx-graph-selected');
      item.classList.toggle('cfx-graph-selected', selected);
    });
    const details = updateSelectionState(root, options?.emit === true);
    const tip = root.querySelector('.cfx-graph-tooltip'); if (tip) tip.hidden = true;
    const announcer = root.querySelector('[data-cfx-role="graph-announcer"]');
    if (announcer) announcer.textContent = uiText(root, 'GraphItemsSelected', '{count} items selected.', { count: details.length });
    redrawGraphLinkedState(root);
    return details;
  };
  const syncGraphHighlightAppearance = root => {
    const state = root.__cfxGraphHighlight;
    const matches = new Set(graphRequestedItems(root, state?.requested));
    // SVG proxy marks also represent Canvas/WebGL items. Use logical graph visibility,
    // rather than DOM display or requested-ID count, to find an available foreground.
    const dim = state?.dim === true && Array.from(matches).some(visible);
    const matchingKeys = new Set(Array.from(matches, graphKeyboardItemKey));
    graphLinkedItems(root).forEach(item => {
      const highlighted = matches.has(item);
      item.classList.toggle('cfx-graph-highlighted', highlighted);
      item.classList.toggle('cfx-graph-link-dim', dim && !highlighted);
      if (highlighted) item.setAttribute('data-cfx-highlighted', 'true'); else item.removeAttribute('data-cfx-highlighted');
    });
    // Labels and card details are separate SVG siblings; keep them with their owning mark.
    items(root, '[data-cfx-role="graph-edge-label"],[data-cfx-role="graph-node-details"]').forEach(item => {
      const node = attr(item, 'data-cfx-role') === 'graph-node-details';
      const id = attr(item, node ? 'data-node-details-for' : 'data-edge-label-for');
      const matching = matchingKeys.has((node ? 'graph-node:' : 'graph-edge:') + id);
      item.classList.toggle('cfx-graph-link-dim', dim && !matching);
    });
    return Array.from(matches);
  };
  const highlightGraphItems = (root, requested, options) => {
    root.__cfxGraphHighlight = { requested: (Array.isArray(requested) ? requested : [])
      .filter(item => item && typeof item.id === 'string' && typeof item.role === 'string')
      .map(item => ({ id: item.id, role: item.role })), dim: options?.dim === true };
    const matches = syncGraphHighlightAppearance(root);
    const announcer = root.querySelector('[data-cfx-role="graph-announcer"]');
    if (announcer) announcer.textContent = uiText(root, 'GraphItemsHighlighted', '{count} items highlighted.', { count: matches.length });
    redrawGraphLinkedState(root);
    return matches.map(item => selectionDetail(root, item));
  };
  const graphHover = (root, item) => {
    const key = item ? graphKeyboardItemKey(item) : '';
    if (root.__cfxGraphHoverKey === key || !key && !root.__cfxGraphHoverKey) return;
    const previous = root.__cfxGraphHoverDetail;
    root.__cfxGraphHoverKey = key;
    root.__cfxGraphHoverDetail = item ? selectionDetail(root, item) : null;
    if (item) emit(root, 'cfxgraphhover', selectionDetail(root, item));
    else emit(root, 'cfxgraphhoverclear', previous || { graphId: attr(root, 'data-cfx-graph-id') });
  };
