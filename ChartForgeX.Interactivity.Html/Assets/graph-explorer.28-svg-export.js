  const svgNode = (document, name, attributes = {}) => {
    const element = document.createElementNS('http:' + '//www.w3.org/2000/svg', name);
    Object.entries(attributes).forEach(([key, value]) => element.setAttribute(key, String(value)));
    return element;
  };
  const exportedNodeStyle = node => [
    `fill:${node.backgroundColor || '#eff6ff'}`,
    `stroke:${node.borderColor || '#2563eb'}`,
    'stroke-width:1.5'
  ].join(';');
  const appendExportedNodeMark = (document, group, node) => {
    const size = node.size;
    const style = exportedNodeStyle(node);
    if ((node.shape === 'image' || node.shape === 'imageRect') && node.imageUrl) {
      const rectangular = node.shape === 'imageRect';
      if (rectangular) {
        group.appendChild(svgNode(document, 'rect', { x: -size * 1.3, y: -size * .9, width: size * 2.6, height: size * 1.8, rx: Math.min(8, size * .35), style }));
      } else {
        group.appendChild(svgNode(document, 'circle', { r: size + 4, style }));
      }
      const width = rectangular ? Math.max(1, size * 2.6 - 6) : size * 2;
      const height = rectangular ? Math.max(1, size * 1.8 - 6) : size * 2;
      const image = svgNode(document, 'image', {
        href: node.imageUrl,
        x: -width / 2,
        y: -height / 2,
        width,
        height,
        preserveAspectRatio: 'xMidYMid slice'
      });
      if (rectangular) image.classList.add('cfx-graph-node-image-rect');
      const alt = attr(node.el, 'data-node-image-alt');
      if (alt) image.setAttribute('aria-label', alt);
      group.appendChild(image);
    } else if (node.shape === 'box' || node.shape === 'square') {
      const wide = node.shape === 'box' ? 1.45 : 1;
      const high = node.shape === 'box' ? 1.05 : 1;
      group.appendChild(svgNode(document, 'rect', { x: -size * wide, y: -size * high, width: size * wide * 2, height: size * high * 2, rx: node.shape === 'square' ? 4 : 6, style }));
    } else if (node.shape === 'ellipse') {
      group.appendChild(svgNode(document, 'ellipse', { rx: size * 1.55, ry: size, style }));
    } else if (node.shape === 'database') {
      group.appendChild(svgNode(document, 'path', { d: nodeDatabasePath(size), style }));
    } else if (node.shape === 'diamond' || node.shape === 'triangle' || node.shape === 'triangleDown' || node.shape === 'star') {
      group.appendChild(svgNode(document, 'polygon', { points: nodePolygonPoints(node.shape, size), style }));
    } else if (node.shape === 'text') {
      group.appendChild(svgNode(document, 'circle', { r: Math.max(1, size * .18), opacity: 0, style }));
    } else if (node.shape !== 'text') {
      group.appendChild(svgNode(document, 'circle', { r: size, style }));
    }
    if (node.icon && node.shape !== 'image' && node.shape !== 'imageRect') {
      const icon = svgNode(document, 'text', { class: 'cfx-graph-node-icon', y: 4 });
      icon.textContent = node.icon;
      group.appendChild(icon);
    }
  };
  const appendExportedNodeDetails = (document, group, node) => {
    const textShape = node.shape === 'text';
    if (node.labelBackgroundColor) {
      group.appendChild(svgNode(document, 'rect', {
        class: 'cfx-graph-node-label-bg',
        x: -Math.max(24, node.label.length * 3.8),
        y: textShape ? -9 : node.size + 7,
        width: Math.max(48, node.label.length * 7.6),
        height: 18,
        rx: 5,
        style: `fill:${node.labelBackgroundColor};stroke:none;stroke-width:0;pointer-events:none`
      }));
    }
    const label = svgNode(document, 'text', { class: 'cfx-graph-node-label', y: textShape ? 4 : node.size + 18 });
    if (node.labelColor) label.setAttribute('style', `fill:${node.labelColor}`);
    label.textContent = node.label;
    group.appendChild(label);
    if (node.secondaryLabel) {
      const secondary = svgNode(document, 'text', { class: 'cfx-graph-node-secondary', y: textShape ? 18 : node.size + 32 });
      secondary.textContent = node.secondaryLabel;
      group.appendChild(secondary);
    }
    if (node.badge) {
      const badge = svgNode(document, 'g', { class: 'cfx-graph-node-badge', transform: `translate(${node.size * 0.82} ${-node.size * 0.82})` });
      badge.appendChild(svgNode(document, 'circle', { r: 8 }));
      const text = svgNode(document, 'text', { y: 3.5 });
      text.textContent = node.badge.slice(0, 5);
      badge.appendChild(text);
      group.appendChild(badge);
    }
    const status = attr(node.el, 'data-cfx-status');
    if (status && status !== 'unknown') group.appendChild(svgNode(document, 'circle', { class: 'cfx-graph-node-status', cx: -node.size * 0.8, cy: -node.size * 0.8, r: 4.5 }));
  };
  const appendExportedEdgeLabel = (document, group, edge, rendered) => {
    if (!edge.label || !edge.showLabel) return null;
    const point = edgeLabelPoint(rendered, edgeControl(rendered));
    const classes = ['cfx-graph-edge-label'];
    ['cfx-graph-hidden', 'cfx-graph-cluster-collapsed-member', 'cfx-graph-hierarchy-hidden'].forEach(name => {
      if (edge.el.classList.contains(name)) classes.push(name);
    });
    const label = svgNode(document, 'text', {
      class: classes.join(' '),
      'data-cfx-role': 'graph-edge-label',
      'data-edge-label-for': edge.id,
      x: point.x.toFixed(3),
      y: point.y.toFixed(3)
    });
    if (edge.labelColor) label.style.setProperty('--cfx-edge-label-explicit', edge.labelColor);
    label.textContent = edge.label;
    group.appendChild(label);
    return label;
  };
  const drawAcceleratedSvgRuntime = (root, state) => {
    if (attr(root, 'data-cfx-graph-accelerated-markup') !== 'true' || root.dataset.cfxGraphRendererActive !== 'svg') return false;
    const viewport = root.querySelector('[data-cfx-role="graph-viewport"]');
    if (!viewport) return false;
    viewport.querySelector('[data-cfx-role="graph-accelerated-runtime"]')?.remove();
    const document = root.ownerDocument;
    const runtime = svgNode(document, 'g', { 'data-cfx-role': 'graph-accelerated-runtime', 'data-cfx-runtime-overlay': 'true' });
    const edges = svgNode(document, 'g', { 'data-cfx-runtime-overlay': 'true' });
    const edgeLabels = svgNode(document, 'g', { 'data-cfx-runtime-overlay': 'true', 'pointer-events': 'none' });
    const marks = svgNode(document, 'g', { 'data-cfx-runtime-overlay': 'true' });
    const details = svgNode(document, 'g', { class: 'cfx-graph-node-details-layer', 'data-cfx-runtime-overlay': 'true', 'pointer-events': 'none' });
    state.edges.filter(edge => visible(edge.el)).forEach(edge => {
      const rendered = visualEdge(edge, state.byId);
      const path = svgNode(document, 'path', {
        class: attr(edge.el, 'class') || 'cfx-graph-edge',
        'data-cfx-role': 'graph-edge',
        'data-cfx-runtime-overlay': 'true',
        'data-edge-id': edge.id,
        d: edgePathData(rendered, edgeControl(rendered))
      });
      const style = [`stroke:${edge.strokeColor || '#64748b'}`, `stroke-width:${edge.strokeWidth || 1.25}`, edge.dashed ? `stroke-dasharray:${edge.dashPattern.join(' ')}` : ''].filter(Boolean).join(';');
      if (style) path.setAttribute('style', style);
      ['marker-start', 'marker-end'].forEach(name => { const value = attr(edge.el, name); if (value) path.setAttribute(name, value); });
      edges.appendChild(path);
      if (!root.classList.contains('cfx-graph-lod-hide-edge-labels')) appendExportedEdgeLabel(document, edgeLabels, edge, rendered);
    });
    state.nodes.filter(node => visible(node.el)).forEach(node => {
      const transform = `translate(${node.x.toFixed(3)} ${node.y.toFixed(3)})`;
      const group = svgNode(document, 'g', {
        class: attr(node.el, 'class') || 'cfx-graph-node',
        'data-cfx-role': 'graph-node',
        'data-cfx-runtime-overlay': 'true',
        'data-node-id': node.id,
        'data-node-label': node.label,
        'data-cfx-status': attr(node.el, 'data-cfx-status'),
        transform,
        tabindex: -1,
        'aria-hidden': 'true',
        'aria-label': node.label
      });
      appendExportedNodeMark(document, group, node);
      marks.appendChild(group);
      const detail = svgNode(document, 'g', {
        class: `${attr(node.el, 'class') || 'cfx-graph-node'} cfx-graph-node-details`,
        'data-cfx-role': 'graph-node-details',
        'data-cfx-runtime-overlay': 'true',
        'data-node-details-for': node.id,
        'data-cfx-status': attr(node.el, 'data-cfx-status'),
        transform
      });
      appendExportedNodeDetails(document, detail, node);
      details.appendChild(detail);
    });
    runtime.append(edges, edgeLabels, marks, details);
    viewport.appendChild(runtime);
    return true;
  };
  const materializeAcceleratedSvg = (root, clone, state) => {
    if (attr(root, 'data-cfx-graph-accelerated-markup') !== 'true') return;
    const document = clone.ownerDocument;
    const viewport = clone.querySelector('[data-cfx-role="graph-viewport"]');
    if (!viewport) return;
    const edgeLabels = new Map(Array.from(viewport.querySelectorAll('[data-cfx-role="graph-edge-label"]')).map(label => [attr(label, 'data-edge-label-for'), label]));
    state.edges.forEach(edge => {
      const rendered = visualEdge(edge, state.byId);
      const path = svgNode(document, 'path', { class: attr(edge.el, 'class') || 'cfx-graph-edge', 'data-cfx-role': 'graph-edge', 'data-edge-id': edge.id, d: attr(edge.el, 'd') });
      const style = [`stroke:${edge.strokeColor || '#64748b'}`, `stroke-width:${edge.strokeWidth || 1.25}`, edge.dashed ? `stroke-dasharray:${edge.dashPattern.join(' ')}` : ''].filter(Boolean).join(';');
      if (style) path.setAttribute('style', style);
      ['marker-start', 'marker-end'].forEach(name => { const value = attr(edge.el, name); if (value) path.setAttribute(name, value); });
      viewport.appendChild(path);
      if (!edgeLabels.has(edge.id)) appendExportedEdgeLabel(document, viewport, edge, rendered);
    });
    const groups = new Map(Array.from(viewport.querySelectorAll('[data-cfx-role="graph-node"]')).map(group => [attr(group, 'data-node-id'), group]));
    let detailsLayer = viewport.querySelector('[data-cfx-role="graph-node-details-layer"]');
    if (!detailsLayer) {
      detailsLayer = svgNode(document, 'g', { class: 'cfx-graph-node-details-layer', 'data-cfx-role': 'graph-node-details-layer', 'pointer-events': 'none' });
      viewport.appendChild(detailsLayer);
    }
    state.nodes.forEach(node => {
      let group = groups.get(node.id);
      if (!group) {
        group = svgNode(document, 'g', { class: attr(node.el, 'class') || 'cfx-graph-node', 'data-cfx-role': 'graph-node', 'data-node-id': node.id, transform: attr(node.el, 'transform') });
        viewport.insertBefore(group, detailsLayer);
      }
      if (!group.childElementCount) appendExportedNodeMark(document, group, node);
      const details = svgNode(document, 'g', { class: `${attr(node.el, 'class') || 'cfx-graph-node'} cfx-graph-node-details`, 'data-cfx-role': 'graph-node-details', 'data-node-details-for': node.id, 'data-cfx-status': attr(node.el, 'data-cfx-status'), transform: attr(node.el, 'transform') });
      appendExportedNodeDetails(document, details, node);
      detailsLayer.appendChild(details);
    });
    viewport.appendChild(detailsLayer);
  };
