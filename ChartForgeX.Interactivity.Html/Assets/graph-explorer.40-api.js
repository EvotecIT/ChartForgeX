  const graphApiRoot = (target) => {
    if (target?.matches?.('.cfx-graph-explorer')) return target;
    if (target?.closest) return target.closest('.cfx-graph-explorer');
    if (typeof target !== 'string') return null;
    return Array.from(document.querySelectorAll('.cfx-graph-explorer')).find(root => attr(root, 'data-cfx-graph-id') === target || root.id === target) || document.querySelector(target);
  };
  const svgElement = (root, name) => root.ownerDocument.createElementNS('http:' + '//www.w3.org/2000/svg', name);
  const setGraphAttribute = (element, name, value) => {
    if (value === undefined || value === null || value === '') element.removeAttribute(name);
    else element.setAttribute(name, String(value));
  };
  const graphMetadata = (value) => {
    if (!value || typeof value !== 'object' || Array.isArray(value)) return '';
    const ordered = {};
    Object.keys(value).sort().forEach(key => { if (value[key] !== undefined && value[key] !== null) ordered[key] = String(value[key]); });
    return JSON.stringify(ordered);
  };
  const graphSearchText = (value) => !value || typeof value !== 'object' ? '' : Object.keys(value).sort().map(key => `${key} ${value[key]}`).join(' ');
  const graphPatchPosition = (element, value, name, fallback) => Number.isFinite(Number(value)) ? Number(value) : num(element, name, fallback);
  const graphPatchRoutePoints = (values) => (values || []).map(point => {
    const x = Number(point?.x ?? point?.X ?? point?.[0]);
    const y = Number(point?.y ?? point?.Y ?? point?.[1]);
    return Number.isFinite(x) && Number.isFinite(y) ? `${x},${y}` : '';
  }).filter(Boolean).join(';');
  const graphPatchStatusColor = (value) => {
    const status = String(value || '').toLowerCase();
    return status === 'healthy' ? '#22c55e' : status === 'warning' ? '#f59e0b' : status === 'critical' ? '#ef4444' : '#94a3b8';
  };
  const graphPatchNodeCard = (node, element) => node.card === true
    || String(node.metadata?.['topology.card'] || '').toLowerCase() === 'true'
    || (node.card === undefined && !node.metadata && attr(element, 'data-node-card') === 'true');
  const graphPatchClusterStatus = (cluster) => cluster.status || cluster.metadata?.['topology.status'];
  const graphPatchEdgeStyle = (edge, style) => [
    style.color ? `--cfx-edge-stroke:${style.color}` : '',
    style.width !== undefined && style.width !== null && style.width !== '' && Number.isFinite(Number(style.width)) ? `--cfx-edge-width:${Number(style.width)}` : '',
    edge.dashed === true ? `stroke-dasharray:${style.dashPattern || edge.dashPattern || '8 6'}` : '',
    style.hidden === true || edge.hidden === true ? 'display:none' : ''
  ].filter(Boolean).join(';');
  const graphPatchEdgeLabelStyle = (edge, style) => [
    style.labelColor ? `--cfx-edge-label-explicit:${style.labelColor}` : '',
    style.hidden === true || edge.hidden === true ? 'display:none' : ''
  ].filter(Boolean).join(';');
  const graphPatchArrowMarker = (root, edgeId, color) => {
    const scene = root.querySelector('[data-cfx-role="graph-scene"]');
    const template = scene?.querySelector('defs marker[id]');
    if (!template || !color) return template?.id || '';
    const markerId = `${template.id}-patch-${String(edgeId).replace(/[^a-zA-Z0-9_-]/g, '-')}`;
    let marker = scene.querySelector(`marker[id="${markerId}"]`);
    if (!marker) {
      marker = template.cloneNode(true);
      marker.id = markerId;
      template.parentNode?.appendChild(marker);
    }
    const path = marker.querySelector('path');
    if (path) path.setAttribute('style', `fill:${color};stroke:${color}`);
    return markerId;
  };
  const graphPatchNodeMark = (root, element, node) => {
    element.classList.toggle('cfx-graph-node-shadow', node.style?.shadow === true);
    if (element.__cfxVirtual) return;
    while (element.firstChild) element.removeChild(element.firstChild);
    const size = Math.max(4, Number(node.size) || 8);
    const shape = node.shape || 'circle';
    let mark;
    if (shape === 'box' || shape === 'square' || shape === 'imageRect') {
      mark = svgElement(root, 'rect');
      const width = shape === 'square' ? size * 2 : shape === 'imageRect' ? size * 2.6 : size * 2.9;
      const height = shape === 'square' ? size * 2 : shape === 'imageRect' ? size * 1.8 : node.card ? Math.min(size * 2.1, 72) : size * 2.1;
      setGraphAttribute(mark, 'x', -width / 2); setGraphAttribute(mark, 'y', -height / 2); setGraphAttribute(mark, 'width', width); setGraphAttribute(mark, 'height', height); setGraphAttribute(mark, 'rx', Math.min(8, size * .4));
    } else if (shape === 'ellipse') {
      mark = svgElement(root, 'ellipse'); setGraphAttribute(mark, 'rx', size * 1.55); setGraphAttribute(mark, 'ry', size);
    } else if (shape === 'database') {
      mark = svgElement(root, 'path'); setGraphAttribute(mark, 'd', nodeDatabasePath(size));
    } else if (shape === 'diamond' || shape === 'triangle' || shape === 'triangleDown' || shape === 'star') {
      mark = svgElement(root, 'polygon'); setGraphAttribute(mark, 'points', nodePolygonPoints(shape, size));
    } else if (shape === 'text') {
      mark = svgElement(root, 'circle'); setGraphAttribute(mark, 'r', Math.max(1, size * .18)); setGraphAttribute(mark, 'opacity', '0');
    } else {
      mark = svgElement(root, 'circle'); setGraphAttribute(mark, 'r', shape === 'image' && node.imageUrl ? size + 4 : size);
    }
    const style = node.style || {};
    const markStyle = [style.backgroundColor ? `--cfx-node-fill:${style.backgroundColor}` : '', style.borderColor ? `--cfx-node-stroke:${style.borderColor}` : ''].filter(Boolean).join(';');
    if (markStyle) mark.setAttribute('style', markStyle);
    element.appendChild(mark);
    if ((shape === 'image' || shape === 'imageRect') && node.imageUrl) {
      const image = svgElement(root, 'image');
      const width = shape === 'imageRect' ? size * 2.6 - 6 : size * 2;
      const height = shape === 'imageRect' ? size * 1.8 - 6 : size * 2;
      setGraphAttribute(image, 'x', -width / 2); setGraphAttribute(image, 'y', -height / 2); setGraphAttribute(image, 'width', width); setGraphAttribute(image, 'height', height); setGraphAttribute(image, 'href', node.imageUrl); setGraphAttribute(image, 'aria-label', node.imageAlt || node.label || node.id);
      if (shape === 'imageRect') image.classList.add('cfx-graph-node-image-rect');
      element.appendChild(image);
    } else if (node.icon) {
      const icon = svgElement(root, 'text'); icon.classList.add('cfx-graph-node-icon');
      if (node.card) { icon.classList.add('cfx-graph-node-card-icon'); icon.setAttribute('x', String(-size * 1.45 + 28)); }
      icon.setAttribute('y', '4'); icon.textContent = node.icon; element.appendChild(icon);
    }
  };
  const graphPatchNodeDetails = (root, element, node, x, y) => {
    if (element.__cfxVirtual) return;
    const viewportGroup = root.querySelector('[data-cfx-role="graph-viewport"]');
    let layer = viewportGroup?.querySelector('[data-cfx-role="graph-node-details-layer"]');
    if (!layer && viewportGroup) {
      layer = svgElement(root, 'g'); layer.classList.add('cfx-graph-node-details-layer'); layer.setAttribute('data-cfx-role', 'graph-node-details-layer'); layer.setAttribute('pointer-events', 'none'); viewportGroup.appendChild(layer);
    }
    let details = items(root, '[data-cfx-role="graph-node-details"]').find(item => attr(item, 'data-node-details-for') === node.id);
    if (!details) { details = svgElement(root, 'g'); details.classList.add('cfx-graph-node-details'); details.setAttribute('data-cfx-role', 'graph-node-details'); layer?.appendChild(details); }
    while (details.firstChild) details.removeChild(details.firstChild);
    const size = Math.max(4, Number(node.size) || 8); const shape = node.shape || 'circle';
    const card = node.card === true; const cardX = -size * 1.45 + 52; const cardHalfHeight = Math.min(size * 1.05, 36);
    details.setAttribute('data-node-details-for', node.id); details.setAttribute('data-cfx-status', node.status || ''); details.setAttribute('transform', `translate(${x} ${y})`);
    const labelText = node.label || node.id;
    if (node.style?.labelBackgroundColor && !card) {
      const background = svgElement(root, 'rect'); background.classList.add('cfx-graph-node-label-bg'); background.setAttribute('x', String(-Math.max(24, labelText.length * 3.8))); background.setAttribute('y', String(shape === 'text' ? -9 : size + 7)); background.setAttribute('width', String(Math.max(48, labelText.length * 7.6))); background.setAttribute('height', '18'); background.setAttribute('rx', '5'); background.setAttribute('style', `fill:${node.style.labelBackgroundColor};stroke:none;stroke-width:0;pointer-events:none`); details.appendChild(background);
    }
    const label = svgElement(root, 'text'); label.classList.add('cfx-graph-node-label');
    if (card) { label.classList.add('cfx-graph-node-card-label'); label.setAttribute('x', String(cardX)); label.setAttribute('data-cfx-full-label', labelText); }
    label.setAttribute('y', card ? '-5' : shape === 'text' ? '4' : String(size + 18)); label.textContent = card ? graphCardText(labelText, size) : labelText;
    if (node.style?.labelColor) label.setAttribute('style', `--cfx-node-label-explicit:${node.style.labelColor}`);
    details.appendChild(label);
    if (node.secondaryLabel) {
      const secondary = svgElement(root, 'text'); secondary.classList.add('cfx-graph-node-secondary');
      if (card) { secondary.classList.add('cfx-graph-node-card-secondary'); secondary.setAttribute('x', String(cardX)); secondary.setAttribute('data-cfx-full-label', node.secondaryLabel); }
      secondary.setAttribute('y', String(card ? 14 : shape === 'text' ? 18 : size + 32)); secondary.textContent = card ? graphCardText(node.secondaryLabel, size, true) : node.secondaryLabel; details.appendChild(secondary);
    }
    if (node.badge) {
      const badge = svgElement(root, 'g'); badge.classList.add('cfx-graph-node-badge'); badge.setAttribute('transform', card ? `translate(${(size * 1.45 - 18).toFixed(3)} ${(-cardHalfHeight + 18).toFixed(3)})` : `translate(${(size * .82).toFixed(3)} ${(-size * .82).toFixed(3)})`);
      const circle = svgElement(root, 'circle'); circle.setAttribute('r', '8'); circle.setAttribute('style', 'fill:var(--cfx-color-text);stroke:var(--cfx-color-paper);stroke-width:2'); badge.appendChild(circle);
      const text = svgElement(root, 'text'); text.setAttribute('y', '3.5'); text.setAttribute('style', 'fill:var(--cfx-color-paper);stroke:none'); text.textContent = String(node.badge).slice(0, 5); badge.appendChild(text); details.appendChild(badge);
    }
    const status = String(node.status || '').toLowerCase();
    if (status && status !== 'unknown') {
      const indicator = svgElement(root, 'circle'); indicator.classList.add('cfx-graph-node-status'); indicator.setAttribute('cx', String(card ? size * 1.45 - 15 : -size * .8)); indicator.setAttribute('cy', String(card ? cardHalfHeight - 14 : -size * .8)); indicator.setAttribute('r', String(Math.min(4.5, Math.max(1.35, size * .28)))); indicator.setAttribute('style', `fill:${graphPatchStatusColor(status)};stroke:var(--cfx-color-paper);stroke-width:2`); details.appendChild(indicator);
    }
  };
  const graphPatchVirtualElement = (root, role, className) => {
    const virtualItems = ensureGraphDocument(root);
    const element = graphVirtualElement(role, {}, [className]);
    virtualItems.push(element);
    root.__cfxGraphVirtualItems = virtualItems;
    return element;
  };
  const upsertGraphNode = (root, node) => {
    const viewportGroup = root.querySelector('[data-cfx-role="graph-viewport"]');
    let element = items(root, '[data-cfx-role="graph-node"]').find(item => attr(item, 'data-node-id') === node.id);
    if (!element) {
      if (attr(root, 'data-cfx-graph-accelerated-markup') === 'true') element = graphPatchVirtualElement(root, 'graph-node', 'cfx-graph-node');
      else { element = svgElement(root, 'g'); element.classList.add('cfx-graph-node'); element.setAttribute('data-cfx-role', 'graph-node'); viewportGroup?.insertBefore(element, viewportGroup.querySelector('[data-cfx-role="graph-node-details-layer"]')); }
    }
    const patchedNode = { ...node, card: graphPatchNodeCard(node, element) };
    const index = items(root, '[data-cfx-role="graph-node"]').indexOf(element);
    const x = graphPatchPosition(element, node.x, 'data-node-x', 160 + Math.max(0, index) * 22);
    const y = graphPatchPosition(element, node.y, 'data-node-y', 160 + Math.max(0, index) * 17);
    setGraphAttribute(element, 'data-node-id', node.id); setGraphAttribute(element, 'data-node-label', node.label || node.id); setGraphAttribute(element, 'data-node-secondary-label', node.secondaryLabel); setGraphAttribute(element, 'data-node-badge', node.badge); setGraphAttribute(element, 'data-node-parent', node.parentId); setGraphAttribute(element, 'data-node-kind', node.kind); setGraphAttribute(element, 'data-node-group', node.groupId); setGraphAttribute(element, 'data-node-cluster', node.clusterId); setGraphAttribute(element, 'data-cfx-status', node.status); setGraphAttribute(element, 'data-node-size', Math.max(4, Number(node.size) || 8)); setGraphAttribute(element, 'data-node-card', patchedNode.card ? 'true' : 'false'); setGraphAttribute(element, 'data-node-fixed', node.fixed === true ? 'true' : 'false'); setGraphAttribute(element, 'data-node-hidden', node.hidden === true ? 'true' : 'false'); setGraphAttribute(element, 'data-node-level', node.level); setGraphAttribute(element, 'data-node-shape', node.shape || 'circle'); setGraphAttribute(element, 'data-node-image-url', node.imageUrl); setGraphAttribute(element, 'data-node-image-alt', node.imageAlt); setGraphAttribute(element, 'data-node-icon', node.icon); setGraphAttribute(element, 'data-node-background-color', node.style?.backgroundColor); setGraphAttribute(element, 'data-node-border-color', node.style?.borderColor); setGraphAttribute(element, 'data-node-label-color', node.style?.labelColor); setGraphAttribute(element, 'data-node-label-background-color', node.style?.labelBackgroundColor); setGraphAttribute(element, 'data-node-shadow', node.style?.shadow === true ? 'true' : 'false'); setGraphAttribute(element, 'data-cfx-search', graphSearchText(node.metadata)); setGraphAttribute(element, 'data-cfx-metadata', graphMetadata(node.metadata)); setGraphAttribute(element, 'data-node-x', x); setGraphAttribute(element, 'data-node-y', y); setGraphAttribute(element, 'transform', `translate(${x} ${y})`); setGraphAttribute(element, 'role', 'button'); setGraphAttribute(element, 'aria-pressed', element.classList.contains('cfx-graph-selected') ? 'true' : 'false'); setGraphAttribute(element, 'aria-label', node.label || node.id); setGraphAttribute(element, 'tabindex', '-1');
    element.classList.toggle('cfx-graph-hidden', node.hidden === true);
    graphPatchNodeMark(root, element, patchedNode);
    graphPatchNodeDetails(root, element, patchedNode, x, y);
    bindGraphItemSelection(root, element);
    return element;
  };
  const upsertGraphEdge = (root, edge) => {
    const viewportGroup = root.querySelector('[data-cfx-role="graph-viewport"]');
    let element = items(root, '[data-cfx-role="graph-edge"]').find(item => attr(item, 'data-edge-id') === edge.id);
    if (!element) {
      if (attr(root, 'data-cfx-graph-accelerated-markup') === 'true') element = graphPatchVirtualElement(root, 'graph-edge', 'cfx-graph-edge');
      else {
        element = svgElement(root, 'path'); element.classList.add('cfx-graph-edge'); element.setAttribute('data-cfx-role', 'graph-edge');
        const firstNode = viewportGroup?.querySelector('[data-cfx-role="graph-node"],[data-cfx-role="graph-node-details-layer"]');
        viewportGroup?.insertBefore(element, firstNode || null);
      }
    }
    const style = edge.style || {};
    // A live patch replaces the canonical label and accessible name; old bundle
    // presentation state must not restore values from before this patch.
    if (attr(element, 'data-cfx-bundle-count')) {
      element.removeAttribute('data-cfx-bundle-count');
      element.removeAttribute('data-cfx-bundle-original-aria');
      element.removeAttribute('data-cfx-bundle-label');
    }
    const sourceArrow = edge.sourceArrow === true, targetArrow = edge.targetArrow === true || edge.directed === true;
    setGraphAttribute(element, 'data-edge-id', edge.id); setGraphAttribute(element, 'data-source-node-id', edge.sourceNodeId || edge.source); setGraphAttribute(element, 'data-target-node-id', edge.targetNodeId || edge.target); setGraphAttribute(element, 'data-edge-label', edge.label); setGraphAttribute(element, 'data-edge-kind', edge.kind); setGraphAttribute(element, 'data-cfx-status', edge.status); setGraphAttribute(element, 'data-edge-weight', Number(edge.weight) || 1); setGraphAttribute(element, 'data-edge-length', Number(edge.length) || 0); setGraphAttribute(element, 'data-edge-shape', edge.shape || 'line'); setGraphAttribute(element, 'data-edge-curvature', Number(edge.curvature) || 0); setGraphAttribute(element, 'data-edge-route-points', graphPatchRoutePoints(edge.routePoints)); setGraphAttribute(element, 'data-edge-dashed', edge.dashed === true ? 'true' : 'false'); setGraphAttribute(element, 'data-edge-dash-pattern', style.dashPattern || edge.dashPattern); setGraphAttribute(element, 'data-edge-show-label', edge.showLabel === false ? 'false' : 'true'); setGraphAttribute(element, 'data-edge-directed', edge.directed === true ? 'true' : 'false'); setGraphAttribute(element, 'data-edge-source-arrow', sourceArrow ? 'true' : 'false'); setGraphAttribute(element, 'data-edge-target-arrow', targetArrow ? 'true' : 'false'); setGraphAttribute(element, 'data-edge-physics', style.physics === false || edge.physics === false ? 'false' : 'true'); setGraphAttribute(element, 'data-edge-color', style.color); setGraphAttribute(element, 'data-edge-label-color', style.labelColor); setGraphAttribute(element, 'data-edge-width', style.width); setGraphAttribute(element, 'data-edge-hidden', style.hidden === true || edge.hidden === true ? 'true' : 'false'); setGraphAttribute(element, 'data-cfx-search', graphSearchText(edge.metadata)); setGraphAttribute(element, 'data-cfx-metadata', graphMetadata(edge.metadata)); setGraphAttribute(element, 'role', 'button'); setGraphAttribute(element, 'aria-pressed', element.classList.contains('cfx-graph-selected') ? 'true' : 'false'); setGraphAttribute(element, 'aria-label', edge.label || `${edge.sourceNodeId || edge.source} to ${edge.targetNodeId || edge.target}`); setGraphAttribute(element, 'tabindex', '-1'); setGraphAttribute(element, 'style', graphPatchEdgeStyle(edge, style));
    const markerId = graphPatchArrowMarker(root, edge.id, style.color);
    setGraphAttribute(element, 'marker-start', sourceArrow && markerId ? `url(#${markerId})` : null);
    setGraphAttribute(element, 'marker-end', targetArrow && markerId ? `url(#${markerId})` : null);
    element.classList.toggle('cfx-graph-hidden', style.hidden === true || edge.hidden === true);
    let label = items(root, '[data-cfx-role="graph-edge-label"]').find(item => attr(item, 'data-edge-label-for') === edge.id);
    label?.removeAttribute('data-cfx-bundle-generated');
    label?.removeAttribute('data-cfx-bundle-original-text');
    // Accelerated SVG materializes labels from the virtual edge state. A physical
    // label here would sit outside its runtime layer and can target a nested overlay.
    if (edge.label && edge.showLabel !== false && attr(root, 'data-cfx-graph-accelerated-markup') !== 'true') {
      if (!label) { label = svgElement(root, 'text'); label.classList.add('cfx-graph-edge-label'); label.setAttribute('data-cfx-role', 'graph-edge-label'); viewportGroup?.insertBefore(label, viewportGroup.querySelector('[data-cfx-role="graph-node"]')); }
      label.setAttribute('data-edge-label-for', edge.id); label.textContent = edge.label; label.classList.toggle('cfx-graph-hidden', style.hidden === true || edge.hidden === true); setGraphAttribute(label, 'style', graphPatchEdgeLabelStyle(edge, style));
    } else label?.remove();
    bindGraphItemSelection(root, element);
    return element;
  };
  const upsertGraphCluster = (root, cluster) => {
    const viewportGroup = root.querySelector('[data-cfx-role="graph-viewport"]');
    let element = items(root, '[data-cfx-role="graph-cluster"]').find(item => attr(item, 'data-cluster-id') === cluster.id);
    if (!element) {
      element = svgElement(root, 'g'); element.classList.add('cfx-graph-cluster'); element.setAttribute('data-cfx-role', 'graph-cluster');
      const firstItem = viewportGroup?.querySelector('[data-cfx-role="graph-edge"],[data-cfx-role="graph-node"]'); viewportGroup?.insertBefore(element, firstItem || null);
      const circle = svgElement(root, 'circle'); circle.setAttribute('r', '38'); element.appendChild(circle);
      const label = svgElement(root, 'text'); label.classList.add('cfx-graph-cluster-label'); label.setAttribute('y', '-2'); element.appendChild(label);
      const count = svgElement(root, 'text'); count.classList.add('cfx-graph-cluster-count'); count.setAttribute('y', '14'); element.appendChild(count);
    }
    const memberCount = (cluster.nodeIds || []).length; const memberLabel = `${memberCount} ${memberCount === 1 ? 'object' : 'objects'}`; const clusterLabel = cluster.label || cluster.id; const clusterStatus = graphPatchClusterStatus(cluster);
    setGraphAttribute(element, 'data-cluster-id', cluster.id); setGraphAttribute(element, 'data-cluster-label', clusterLabel); setGraphAttribute(element, 'data-cluster-kind', cluster.kind); setGraphAttribute(element, 'data-cfx-status', clusterStatus); setGraphAttribute(element, 'data-cluster-parent', cluster.parentClusterId); setGraphAttribute(element, 'data-cluster-node-ids', (cluster.nodeIds || []).join(',')); setGraphAttribute(element, 'data-cluster-node-count', memberCount); setGraphAttribute(element, 'data-cluster-collapsed', cluster.collapsed === true ? 'true' : 'false'); setGraphAttribute(element, 'data-cfx-search', graphSearchText(cluster.metadata)); setGraphAttribute(element, 'data-cfx-metadata', graphMetadata(cluster.metadata)); setGraphAttribute(element, 'role', 'button'); setGraphAttribute(element, 'aria-pressed', element.classList.contains('cfx-graph-selected') ? 'true' : 'false'); setGraphAttribute(element, 'aria-label', `${clusterLabel}, ${memberLabel}${clusterStatus ? `, ${clusterStatus}` : ''}`); setGraphAttribute(element, 'tabindex', '-1');
    const label = element.querySelector('.cfx-graph-cluster-label'); if (label) label.textContent = clusterLabel;
    const count = element.querySelector('.cfx-graph-cluster-count'); if (count) count.textContent = memberLabel;
    bindGraphItemSelection(root, element);
    return element;
  };
  const applyGraphRuntimePatch = (target, patch, options) => {
    const root = graphApiRoot(target);
    if (!root) throw new Error('ChartForgeX graph explorer was not found.');
    if (!hasFeature(root, 'IncrementalUpdates')) throw new Error('This graph scene does not enable IncrementalUpdates.');
    patch = patch || {};
    const graphChanged = ['upsertNodes', 'upsertEdges', 'upsertClusters', 'removeNodeIds', 'removeEdgeIds', 'removeClusterIds'].some(name => Array.isArray(patch[name]) && patch[name].length > 0);
    validateGraphPatch(root, patch);
    if (graphChanged) { stopWorkerPhysics(root, true); stopMainPhysics(root, true); }
    const removeNodes = graphPatchIds(patch.removeNodeIds), removeEdges = graphPatchIds(patch.removeEdgeIds), removeClusters = graphPatchIds(patch.removeClusterIds);
    if (patch.removeIncidentReferences !== false && removeNodes.size) items(root, '[data-cfx-role="graph-edge"]').forEach(edge => { if (removeNodes.has(attr(edge, 'data-source-node-id')) || removeNodes.has(attr(edge, 'data-target-node-id'))) removeEdges.add(attr(edge, 'data-edge-id')); });
    items(root, '[data-cfx-role="graph-edge-label"]').forEach(label => { if (removeEdges.has(attr(label, 'data-edge-label-for'))) label.remove(); });
    items(root, '[data-cfx-role="graph-edge"]').forEach(edge => { if (removeEdges.has(attr(edge, 'data-edge-id'))) edge.remove(); });
    items(root, '[data-cfx-role="graph-node"]').forEach(node => { if (removeNodes.has(attr(node, 'data-node-id'))) node.remove(); });
    items(root, '[data-cfx-role="graph-node-details"]').forEach(details => { if (removeNodes.has(attr(details, 'data-node-details-for'))) details.remove(); });
    items(root, '[data-cfx-role="graph-cluster"]').forEach(cluster => { if (removeClusters.has(attr(cluster, 'data-cluster-id'))) cluster.remove(); });
    detachGraphPatchRemovedClusters(root, removeClusters, patch.upsertClusters);
    if (patch.removeIncidentReferences !== false && removeNodes.size) items(root, '[data-cfx-role="graph-cluster"]').forEach(cluster => setGraphAttribute(cluster, 'data-cluster-node-ids', idList(attr(cluster, 'data-cluster-node-ids')).filter(id => !removeNodes.has(id)).join(',')));
    (patch.upsertClusters || []).forEach(cluster => upsertGraphCluster(root, cluster));
    (patch.upsertEdges || []).forEach(edge => upsertGraphEdge(root, edge));
    (patch.upsertNodes || []).forEach(node => upsertGraphNode(root, node));
    syncGraphPatchClusterMembership(root, patch.upsertNodes);
    syncSvgThemeColors(root);
    root.setAttribute('data-cfx-graph-node-count', String(items(root, '[data-cfx-role="graph-node"]').length)); root.setAttribute('data-cfx-graph-edge-count', String(items(root, '[data-cfx-role="graph-edge"]').length)); root.setAttribute('data-cfx-graph-cluster-count', String(items(root, '[data-cfx-role="graph-cluster"]').length));
    const state = graphState(root); root.__cfxGraphState = state;
    if (hasFeature(root, 'LevelOfDetail')) applyLod(root);
    performanceGate(root);
    const clustered = hasFeature(root, 'Clustering');
    if (clustered) applyClusterState(root, undefined, undefined, { reheat: false });
    if (hasFeature(root, 'HierarchyNavigation')) applyHierarchyView(root, root.dataset.cfxGraphHierarchyRoot || '', Number(root.dataset.cfxGraphHierarchyDepth || num(root, 'data-cfx-graph-hierarchy-depth', 2)), { fit: patch.fit === true, restartPhysics: false });
    else if (!clustered) { applyFilters(root); applyLayout(root, state); if (patch.fit === true && hasFeature(root, 'Viewport')) fitViewport(root); }
    else if (patch.fit === true && hasFeature(root, 'Viewport')) fitViewport(root);
    syncGraphItemTabStops(root);
    if (graphChanged && options?.reheat === false && hasFeature(root, 'RuntimePhysics')) { root.dataset.cfxGraphPhysicsReason = options.reason || 'graph-patch'; pausePhysics(root); }
    else if (graphChanged && attr(root, 'data-cfx-graph-reheat-patch') !== 'false' && hasFeature(root, 'RuntimePhysics')) reheatPhysics(root, 'graph-patch', { rebuild: true, fit: false });
    emit(root, 'cfxgraphpatch', { graphId: attr(root, 'data-cfx-graph-id'), nodeCount: state.nodes.length, edgeCount: state.edges.length, clusterCount: state.clusters.length });
    return { nodeCount: state.nodes.length, edgeCount: state.edges.length, clusterCount: state.clusters.length };
  };
  const graphExplorerApi = {
    get: target => { const root = graphApiRoot(target); return root ? exportGraphJson(root) : null; },
    update: (target, patch) => applyGraphRuntimePatch(target, patch),
    change: (target, patch, source, label) => { const root = graphApiRoot(target); return root ? requestGraphChange(root, patch, source || 'api', label || 'Graph change') : false; },
    captureState: (target, source) => { const root = graphApiRoot(target); return root ? captureGraphInteractionState(root, source || 'api') : null; },
    applyState: (target, state) => { const root = graphApiRoot(target); return root ? applyGraphInteractionState(root, state, { source: 'api', persist: true }) : false; },
    undo: target => { const root = graphApiRoot(target); return root ? traverseGraphHistory(root, 'undo') : false; },
    redo: target => { const root = graphApiRoot(target); return root ? traverseGraphHistory(root, 'redo') : false; },
    positions: target => { const root = graphApiRoot(target); return root ? captureGraphInteractionState(root, 'positions').positions : []; },
    navigate: (target, rootNodeId, depth) => { const root = graphApiRoot(target); return root ? applyHierarchyView(root, rootNodeId || '', depth) : false; },
    focus: (target, nodeId, options) => { const root = graphApiRoot(target); return root ? (nodeId ? applyNeighborhoodFocus(root, nodeId, options) : clearNeighborhoodFocus(root)) : false; },
    focusPage: (target, direction) => { const root = graphApiRoot(target); return root && (direction === -1 || direction === 1) ? pageGraphNeighborhood(root, direction) : false; },
    focusBack: target => { const root = graphApiRoot(target); return root ? backGraphNeighborhood(root) : false; },
    physics: (target, configuration) => { const root = graphApiRoot(target); return root ? applyPhysicsConfiguration(root, configuration) : null; },
    theme: (target, mode) => { const root = graphApiRoot(target); return root ? (mode === undefined ? { mode: graphThemeMode(root), active: attr(root, 'data-cfx-graph-theme-active') } : setGraphTheme(root, mode)) : null; },
    fit: target => { const root = graphApiRoot(target); if (!root) return false; fitViewport(root); return true; },
    export: (target, format) => { const root = graphApiRoot(target); return root ? exportGraph(root, format || 'json') : Promise.resolve(); }
  };
  window.ChartForgeXGraphExplorer = Object.freeze(graphExplorerApi);
