  const svgNode = (document, name, attributes = {}) => {
    const element = document.createElementNS('http:' + '//www.w3.org/2000/svg', name);
    Object.entries(attributes).forEach(([key, value]) => element.setAttribute(key, String(value)));
    return element;
  };
  const exportedNodeStyle = (node, root) => {
    const selected = node.el.classList.contains('cfx-graph-selected') || node.el.classList.contains('cfx-graph-highlighted');
    const compact = root.classList.contains('cfx-graph-lod-compact') || root.classList.contains('cfx-graph-semantic-overview');
    const paint = graphNodeMarkPaint(node, selected, compact, graphThemePalette(root).selected);
    return `fill:${paint.fill};stroke:${paint.stroke};stroke-width:${paint.width}`;
  };
  const appendExportedNodeMark = (document, group, node, root) => {
    const size = node.size;
    const style = exportedNodeStyle(node, root);
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
        preserveAspectRatio: 'none'
      });
      if (rectangular) image.classList.add('cfx-graph-node-image-rect');
      const alt = attr(node.el, 'data-node-image-alt');
      if (alt) image.setAttribute('aria-label', alt);
      group.appendChild(image);
    } else if (node.shape === 'box' || node.shape === 'square') {
      const wide = node.shape === 'box' ? 1.45 : 1;
      const high = node.shape === 'box' ? (node.card ? Math.min(size * 1.05, 36) : size * 1.05) : size;
      group.appendChild(svgNode(document, 'rect', { x: -size * wide, y: -high, width: size * wide * 2, height: high * 2, rx: node.shape === 'square' ? 4 : node.card ? 10 : 6, style }));
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
      const card = node.card === true;
      const icon = svgNode(document, 'text', { class: card ? 'cfx-graph-node-icon cfx-graph-node-card-icon' : 'cfx-graph-node-icon', x: card ? -node.size * 1.45 + 28 : 0, y: 4 });
      icon.textContent = node.icon;
      group.appendChild(icon);
    }
  };
  const appendExportedNodeDetails = (document, group, node, root, palette = graphThemePalette(root)) => {
    const colors = graphReadableNodeColors(root, node.el, palette);
    group.style.setProperty('--cfx-node-label-adaptive', colors.label);
    group.style.setProperty('--cfx-node-secondary-adaptive', colors.secondary);
    group.style.setProperty('--cfx-node-label-halo', colors.halo);
    const textShape = node.shape === 'text';
    const card = node.card === true;
    const cardX = -node.size * 1.45 + 52;
    const cardHalfHeight = Math.min(node.size * 1.05, 36);
    if (node.labelBackgroundColor && !card) {
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
    const label = svgNode(document, 'text', { class: card ? 'cfx-graph-node-label cfx-graph-node-card-label' : 'cfx-graph-node-label', x: card ? cardX : 0, y: card ? -5 : textShape ? 4 : node.size + 18, ...(card ? { 'data-cfx-full-label': node.label } : {}) });
    label.textContent = card ? graphCardText(node.label, node.size) : node.label;
    group.appendChild(label);
    if (node.secondaryLabel) {
      const secondary = svgNode(document, 'text', { class: card ? 'cfx-graph-node-secondary cfx-graph-node-card-secondary' : 'cfx-graph-node-secondary', x: card ? cardX : 0, y: card ? 14 : textShape ? 18 : node.size + 32, ...(card ? { 'data-cfx-full-label': node.secondaryLabel } : {}) });
      secondary.textContent = card ? graphCardText(node.secondaryLabel, node.size, true) : node.secondaryLabel;
      group.appendChild(secondary);
    }
    if (node.badge) {
      const geometry = graphBadgeGeometry(node, root);
      const badge = svgNode(document, 'g', { class: 'cfx-graph-node-badge', transform: `translate(${geometry.x} ${geometry.y})` });
      badge.appendChild(svgNode(document, geometry.width <= 16 ? 'circle' : 'rect', geometry.width <= 16 ? { r: 8 }
        : { x: -geometry.width / 2, y: -8, width: geometry.width, height: 16, rx: 8 }));
      const text = svgNode(document, 'text', { y: 3.5 });
      text.textContent = geometry.text;
      badge.appendChild(text);
      group.appendChild(badge);
    }
    const status = attr(node.el, 'data-cfx-status').toLowerCase();
    if (status && status !== 'unknown') group.appendChild(svgNode(document, 'circle', { class: 'cfx-graph-node-status', cx: card ? node.size * 1.45 - 15 : -node.size * 0.8, cy: card ? cardHalfHeight - 14 : -node.size * 0.8, r: 4.5 }));
    if (node.el.classList.contains('cfx-graph-neighborhood-primary')) {
      const ring = graphNodePrimaryRingPaint(node);
      group.appendChild(svgNode(document, 'circle', { 'data-cfx-role': 'graph-node-primary-ring', r: ring.radius,
        style: `fill:none;stroke:${ring.color};stroke-width:${ring.width}` }));
    }
    applySvgNodeDetailPaint(root, group, node, palette);
  };
  const applySvgNodeDetailPaint = (root, group, node, palette) => {
    const badge = group.querySelector('.cfx-graph-node-badge');
    const surface = badge?.firstElementChild, text = badge?.querySelector('text');
    if (surface) { surface.style.fill = palette.text; surface.style.stroke = palette.halo; surface.style.strokeWidth = '2'; }
    if (text) { text.style.fill = palette.paper; text.style.stroke = 'none'; }
    const status = group.querySelector('.cfx-graph-node-status');
    if (status) {
      const value = attr(node.el, 'data-cfx-status').toLowerCase();
      if (!value || value === 'unknown') status.remove();
      else {
        status.style.fill = graphPatchStatusColor(value);
        status.style.stroke = graphReadableNodeColors(root, node.el, palette).halo;
        status.style.strokeWidth = '2';
      }
    }
  };
  const applySvgEdgePaint = (path, edge, paint) => {
    path.style.stroke = paint.color;
    path.style.strokeWidth = String(paint.width);
    path.style.opacity = String(paint.alpha);
    path.style.strokeLinecap = 'round';
    path.style.strokeMiterlimit = '4';
    path.style.strokeDasharray = edge.dashed ? edge.dashPattern.join(' ') : 'none';
    path.removeAttribute('marker-start'); path.removeAttribute('marker-end');
  };
  const appendExportedEdgeArrows = (document, group, edge, paint) => {
    if (!paint.arrows || !edge.sourceArrow && !edge.targetArrow && !edge.directed) return;
    const arrows = svgNode(document, 'g', {
      class: attr(edge.el, 'class'), 'data-cfx-role': 'graph-edge-arrows',
      'data-cfx-runtime-overlay': 'true', 'data-edge-arrows-for': edge.id,
      style: `opacity:${paint.arrowAlpha};pointer-events:none`
    });
    for (const side of ['source', 'target']) {
      if (side === 'source' ? !edge.sourceArrow : !edge.targetArrow && !edge.directed) continue;
      const arrow = edgeArrowGeometry(paint.rendered, paint.control, side);
      arrows.appendChild(svgNode(document, 'polygon', {
        'data-arrow-side': side,
        points: [arrow.tip, arrow.left, arrow.right].map(point => `${point.x},${point.y}`).join(' '),
        style: `fill:${paint.color};stroke:none`
      }));
    }
    group.appendChild(arrows);
    return arrows;
  };
  const applySvgEdgeLabelPaint = (root, label, edge, paint, palette) => {
    label.style.fill = graphAdaptiveTextColor(root, edge.labelColor, palette.edgeLabel);
    label.style.opacity = String((paint.dimmed ? .16 : 1) * graphLinkOpacity(edge.el));
  };
  const appendExportedEdgeLabel = (document, group, edge, paint, root, palette) => {
    if (!edge.label || !edge.showLabel) return null;
    const point = edgeLabelPoint(paint.rendered, paint.control);
    const classes = ['cfx-graph-edge-label'];
    if (edge.el.classList.contains('cfx-graph-selected') || edge.el.classList.contains('cfx-graph-highlighted')) classes.push('cfx-graph-label-selected');
    if (edge.el.classList.contains('cfx-graph-neighborhood-related')) classes.push('cfx-graph-neighborhood-related');
    ['cfx-graph-hidden', 'cfx-graph-cluster-collapsed-member', 'cfx-graph-hierarchy-hidden', 'cfx-graph-neighborhood-hidden',
      'cfx-graph-bundle-member', 'cfx-graph-overview-member'].forEach(name => {
      if (edge.el.classList.contains(name)) classes.push(name);
    });
    const label = svgNode(document, 'text', {
      class: classes.join(' '),
      'data-cfx-role': 'graph-edge-label',
      'data-edge-label-for': edge.id,
      x: point.x.toFixed(3),
      y: point.y.toFixed(3)
    });
    applySvgEdgeLabelPaint(root, label, edge, paint, palette);
    label.textContent = edge.label;
    group.appendChild(label);
    return label;
  };
  // Physical SVG and accelerated materialization consume the same stationary paint as Canvas/PNG.
  const syncPhysicalSvgEdgePaint = (root, state, scene = root.querySelector('[data-cfx-role="graph-scene"]')) => {
    if (!scene || attr(root, 'data-cfx-graph-accelerated-markup') === 'true') return;
    const arrowGroups = new Map(Array.from(scene.querySelectorAll('[data-cfx-role="graph-edge-arrows"]')).map(group => [attr(group, 'data-edge-arrows-for'), group]));
    const edgeIds = new Set(state.edges.map(edge => edge.id));
    arrowGroups.forEach((group, id) => { if (!edgeIds.has(id)) group.remove(); });
    const paths = new Map(Array.from(scene.querySelectorAll('[data-cfx-role="graph-edge"]')).map(path => [attr(path, 'data-edge-id'), path]));
    const labels = new Map(Array.from(scene.querySelectorAll('[data-cfx-role="graph-edge-label"]')).map(label => [attr(label, 'data-edge-label-for'), label]));
    const palette = graphThemePalette(root), dense = root.classList.contains('cfx-graph-lod-compact') || root.classList.contains('cfx-graph-semantic-overview') || state.edges.length > 250;
    state.edges.forEach(edge => {
      const path = paths.get(edge.id);
      if (!path) return;
      const paint = graphEdgePaint(root, edge, state.byId, palette, dense, false);
      const stamp = JSON.stringify([attr(path, 'd'), attr(edge.el, 'class'), paint.color, paint.width, paint.alpha, paint.arrowAlpha,
        edge.sourceArrow, edge.targetArrow, edge.directed, edge.dashed && edge.dashPattern, edge.labelColor, palette.edgeLabel, palette.paper]);
      const label = labels.get(edge.id);
      if (path.__cfxSvgPaintStamp !== stamp || path.__cfxSvgPaintLabel !== label) {
        applySvgEdgePaint(path, edge, paint);
        arrowGroups.get(edge.id)?.remove();
        const arrows = appendExportedEdgeArrows(scene.ownerDocument, path.parentNode, edge, paint);
        if (arrows) path.after(arrows);
        if (label) applySvgEdgeLabelPaint(root, label, edge, paint, palette);
        path.__cfxSvgPaintStamp = stamp; path.__cfxSvgPaintLabel = label;
      }
    });
    const marks = new Map(Array.from(scene.querySelectorAll('[data-cfx-role="graph-node"]')).map(mark => [attr(mark, 'data-node-id'), mark]));
    const details = new Map(Array.from(scene.querySelectorAll('[data-cfx-role="graph-node-details"]')).map(detail => [attr(detail, 'data-node-details-for'), detail]));
    state.nodes.forEach(node => {
      const mark = marks.get(node.id), detail = details.get(node.id);
      const opacity = String((node.el.classList.contains('cfx-graph-neighborhood-dim') ? .18 : 1) * graphLinkOpacity(node.el));
      const paint = graphNodeMarkPaint(node, node.el.classList.contains('cfx-graph-selected') || node.el.classList.contains('cfx-graph-highlighted'),
        root.classList.contains('cfx-graph-lod-compact') || root.classList.contains('cfx-graph-semantic-overview'), palette.selected);
      const stamp = JSON.stringify([attr(node.el, 'class'), paint, opacity, attr(node.el, 'data-cfx-status'), node.backgroundColor, node.labelBackgroundColor,
        palette.text, palette.paper, palette.halo]);
      if (mark && (mark.__cfxSvgPaintStamp !== stamp || mark.__cfxSvgPaintSurface !== mark.firstElementChild)) {
        mark.style.opacity = opacity;
        const surface = mark.firstElementChild;
        if (surface) {
          surface.style.fill = paint.fill; surface.style.stroke = paint.stroke; surface.style.strokeWidth = String(paint.width);
          if (node.el.classList.contains('cfx-graph-neighborhood-primary')) {
            surface.style.removeProperty('stroke'); surface.style.removeProperty('stroke-width');
          }
        }
        mark.__cfxSvgPaintStamp = stamp; mark.__cfxSvgPaintSurface = surface;
      }
      if (detail && (detail.__cfxSvgPaintStamp !== stamp || detail.__cfxSvgPaintSurface !== detail.firstElementChild)) {
        detail.style.opacity = opacity;
        applySvgNodeDetailPaint(root, detail, node, palette);
        detail.__cfxSvgPaintStamp = stamp; detail.__cfxSvgPaintSurface = detail.firstElementChild;
      }
    });
  };
  const appendAcceleratedSvgScene = (root, viewport, state, visibleOnly) => {
    viewport.querySelector('[data-cfx-role="graph-accelerated-runtime"]')?.remove();
    const document = viewport.ownerDocument;
    const runtime = svgNode(document, 'g', { 'data-cfx-role': 'graph-accelerated-runtime', 'data-cfx-runtime-overlay': 'true' });
    const edges = svgNode(document, 'g', { 'data-cfx-runtime-overlay': 'true' });
    const edgeLabels = svgNode(document, 'g', { 'data-cfx-runtime-overlay': 'true', 'pointer-events': 'none' });
    const marks = svgNode(document, 'g', { 'data-cfx-runtime-overlay': 'true' });
    const details = svgNode(document, 'g', { class: 'cfx-graph-node-details-layer', 'data-cfx-runtime-overlay': 'true', 'pointer-events': 'none' });
    const palette = graphThemePalette(root), dense = root.classList.contains('cfx-graph-lod-compact') || root.classList.contains('cfx-graph-semantic-overview') || state.edges.length > 250;
    state.edges.filter(edge => !visibleOnly || visible(edge.el)).forEach(edge => {
      const paint = graphEdgePaint(root, edge, state.byId, palette, dense, false);
      const path = svgNode(document, 'path', {
        class: attr(edge.el, 'class') || 'cfx-graph-edge',
        'data-cfx-role': 'graph-edge',
        'data-cfx-runtime-overlay': 'true',
        'data-edge-id': edge.id,
        'aria-label': attr(edge.el, 'aria-label'),
        d: edgePathData(paint.rendered, paint.control)
      });
      applySvgEdgePaint(path, edge, paint);
      edges.appendChild(path);
      appendExportedEdgeArrows(document, edges, edge, paint);
      if (visible(edge.el) && (!visibleOnly || paint.labels))
        appendExportedEdgeLabel(document, edgeLabels, edge, paint, root, palette);
    });
    state.nodes.filter(node => !visibleOnly || visible(node.el)).forEach(node => {
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
      group.style.opacity = String((node.el.classList.contains('cfx-graph-neighborhood-dim') ? .18 : 1) * graphLinkOpacity(node.el));
      appendExportedNodeMark(document, group, node, root);
      marks.appendChild(group);
      const detail = svgNode(document, 'g', {
        class: `${attr(node.el, 'class') || 'cfx-graph-node'} cfx-graph-node-details`,
        'data-cfx-role': 'graph-node-details',
        'data-cfx-runtime-overlay': 'true',
        'data-node-details-for': node.id,
        'data-cfx-status': attr(node.el, 'data-cfx-status'),
        transform
      });
      detail.style.opacity = group.style.opacity;
      appendExportedNodeDetails(document, detail, node, root, palette);
      details.appendChild(detail);
    });
    runtime.append(edges, edgeLabels, marks, details);
    viewport.appendChild(runtime);
  };
  const drawAcceleratedSvgRuntime = (root, state) => {
    if (attr(root, 'data-cfx-graph-accelerated-markup') !== 'true' || root.dataset.cfxGraphRendererActive !== 'svg') return false;
    const viewport = root.querySelector('[data-cfx-role="graph-viewport"]');
    if (!viewport) return false;
    appendAcceleratedSvgScene(root, viewport, state, true);
    return true;
  };
  const materializeAcceleratedSvg = (root, clone, state) => {
    if (attr(root, 'data-cfx-graph-accelerated-markup') !== 'true') {
      syncPhysicalSvgEdgePaint(root, state, clone);
      return;
    }
    const viewport = clone.querySelector('[data-cfx-role="graph-viewport"]');
    if (!viewport) return;
    appendAcceleratedSvgScene(root, viewport, state, false);
  };
