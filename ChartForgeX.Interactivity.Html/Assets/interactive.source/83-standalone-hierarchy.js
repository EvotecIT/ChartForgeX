  // Retained hierarchy facts are sufficient for a saved-page drill view. The
  // original drawing stays available for an exact Back-to-all restoration.
  const standaloneHierarchyModel = root => {
    if (nativePreparationBinding(root, 'hierarchy')) return null;
    if (root._cfxStandaloneHierarchy) return root._cfxStandaloneHierarchy;
    const svg = root.querySelector('.cfx-stage svg');
    const nodes = Array.from(svg?.querySelectorAll('[data-cfx-node][data-cfx-kind]') || [])
      .filter(node => ['treemap', 'sunburst'].includes(node.dataset.cfxKind.toLowerCase()));
    if (!nodes.length) return null;
    if (new Set(nodes.map(node => seriesKey(node))).size !== 1) return null;
    const state = { svg, nodes, byId: new Map(nodes.map(node => [node.dataset.cfxNode, node])), rootId: null, overlay: null, panel: null };
    root._cfxStandaloneHierarchy = state;
    const display = svg.style.display;
    ownChartWork(root, () => { state.overlay?.remove(); state.panel?.remove(); svg.style.display = display; delete root._cfxStandaloneHierarchy; });
    return state;
  };
  const standaloneHierarchyChildren = (state, id) => state.nodes.filter(node => node.dataset.cfxParent === id);
  const standaloneHierarchyPath = (state, id) => {
    const path = [], seen = new Set();
    for (let node = state.byId.get(id); node && !seen.has(node); node = state.byId.get(node.dataset.cfxParent)) { seen.add(node); path.unshift(node); }
    return path;
  };
  const standaloneArcPath = (cx, cy, inner, outer, start, sweep) => {
    const at = (radius, angle) => [cx + radius * Math.cos(angle), cy + radius * Math.sin(angle)].join(' ');
    const end = start + Math.min(sweep, Math.PI * 2 - .000001), large = sweep > Math.PI ? 1 : 0;
    return 'M ' + at(outer, start) + ' A ' + outer + ' ' + outer + ' 0 ' + large + ' 1 ' + at(outer, end)
      + (inner > 0 ? ' L ' + at(inner, end) + ' A ' + inner + ' ' + inner + ' 0 ' + large + ' 0 ' + at(inner, start) : ' L ' + cx + ' ' + cy) + ' Z';
  };
  const navigateStandaloneHierarchy = (root, id) => {
    const state = standaloneHierarchyModel(root);
    if (!state || id !== null && (!standaloneHierarchyChildren(state, id).length || !(Number(state.byId.get(id)?.dataset.cfxValue) > 0))) return false;
    clearHover(root, true, true); hideTip(root, root.querySelector('.cfx-tooltip'), true);
    state.overlay?.remove(); state.panel?.remove(); state.overlay = null; state.panel = null;
    state.rootId = id;
    if (id === null) { state.svg.style.display = ''; refreshKeyboardNavigation(root); syncResetControl(root); return true; }
    const current = state.byId.get(id), stage = root.querySelector('.cfx-stage');
    const ns = 'http://www.w3.org/2000/svg', create = name => document.createElementNS(ns, name);
    const view = state.svg.viewBox.baseVal, overlay = create('svg');
    overlay.setAttribute('viewBox', [view.x, view.y, view.width, view.height].join(' '));
    overlay.setAttribute('width', view.width); overlay.setAttribute('height', view.height);
    overlay.dataset.cfxStandaloneHierarchy = id; overlay.setAttribute('role', 'group');
    overlay.setAttribute('aria-label', current.dataset.cfxLabel);
    state.svg.style.display = 'none'; stage.appendChild(overlay); state.overlay = overlay;
    const panel = document.createElement('nav'); panel.className = 'cfx-hierarchy-breadcrumb'; panel.setAttribute('aria-label', 'Chart hierarchy');
    const addCrumb = (label, nodeId) => {
      const button = document.createElement('button'); button.type = 'button'; button.className = 'cfx-tool'; button.textContent = label;
      button.addEventListener('click', () => navigateStandaloneHierarchy(root, nodeId)); panel.appendChild(button);
    };
    addCrumb('All', null); standaloneHierarchyPath(state, id).forEach(node => addCrumb(node.dataset.cfxLabel, node.dataset.cfxNode));
    stage.before(panel); state.panel = panel;
    const font = state.svg.querySelector('text'), size = 12 * view.width / Math.max(240, stage.clientWidth - 24);
    const addMark = (source, shape, labelX, labelY, labelWidth) => {
      const node = source.cloneNode(false); node.removeAttribute('id'); node.style.display = ''; node.style.visibility = '';
      node.removeAttribute('data-cfx-highlight-managed'); node.removeAttribute('data-cfx-highlight-unavailable');
      node.classList.remove('cfx-hovered', 'cfx-hover-series', 'cfx-hover-column');
      const original = source.querySelector('[data-cfx-role$="-mark"]');
      shape.setAttribute('fill', original ? getComputedStyle(original).fill : 'currentColor');
      shape.setAttribute('stroke', 'var(--cfx-tooltip-surface)'); shape.setAttribute('stroke-width', '1'); node.appendChild(shape);
      node.setAttribute('tabindex', '-1'); node.setAttribute('role', 'button');
      if (labelWidth > size * 3) {
        const label = create('text'); label.setAttribute('x', labelX); label.setAttribute('y', labelY);
        label.setAttribute('font-size', size); label.setAttribute('font-family', font?.getAttribute('font-family') || 'inherit');
        label.setAttribute('fill', 'var(--cfx-tooltip-text)');
        const textValue = source.dataset.cfxLabel || '', max = Math.max(3, Math.floor(labelWidth / (size * .6)));
        label.textContent = textValue.length > max ? textValue.slice(0, max - 1) + '…' : textValue; label.setAttribute('pointer-events', 'none'); node.appendChild(label);
      }
      overlay.appendChild(node);
    };
    const drawTiles = (parent, box, depth) => {
      const children = standaloneHierarchyChildren(state, parent), total = children.reduce((sum, node) => sum + Math.max(0, Number(node.dataset.cfxValue)), 0);
      if (!(total > 0)) return;
      let offset = 0;
      children.forEach(node => {
        const fraction = Math.max(0, Number(node.dataset.cfxValue)) / total;
        const horizontal = depth % 2 === 0, rect = create('rect');
        const tile = { x: box.x + (horizontal ? offset * box.width : 0), y: box.y + (horizontal ? 0 : offset * box.height),
          width: box.width * (horizontal ? fraction : 1), height: box.height * (horizontal ? 1 : fraction) };
        offset += fraction; for (const name of ['x', 'y', 'width', 'height']) rect.setAttribute(name, tile[name]);
        addMark(node, rect, tile.x + 6, tile.y + size + 4, tile.width - 12);
        if (standaloneHierarchyChildren(state, node.dataset.cfxNode).length && tile.width > size * 5 && tile.height > size * 4)
          drawTiles(node.dataset.cfxNode, { x: tile.x + 4, y: tile.y + size + 10, width: Math.max(0, tile.width - 8), height: Math.max(0, tile.height - size - 14) }, depth + 1);
      });
    };
    if (current.dataset.cfxKind.toLowerCase() === 'treemap') drawTiles(id, { x: 8, y: 8, width: view.width - 16, height: view.height - 16 }, 0);
    else {
      const descendants = state.nodes.filter(node => standaloneHierarchyPath(state, node.dataset.cfxNode).includes(current));
      const levels = Math.max(1, ...descendants.map(node => Number(node.dataset.cfxDepth) - Number(current.dataset.cfxDepth)));
      const radius = Math.min(view.width, view.height) / 2 - 16, band = radius / (levels + 1), cx = view.width / 2, cy = view.height / 2;
      const draw = (node, start, sweep, depth) => {
        const shape = create('path'); shape.setAttribute('d', standaloneArcPath(cx, cy, depth * band, (depth + 1) * band, start, sweep));
        const middle = start + sweep / 2, distance = (depth + .5) * band;
        addMark(node, shape, cx + Math.cos(middle) * distance - band / 3, cy + Math.sin(middle) * distance, sweep * distance * .65);
        const children = standaloneHierarchyChildren(state, node.dataset.cfxNode), total = Number(node.dataset.cfxValue); let angle = start;
        if (!(total > 0)) return;
        children.forEach(child => { const next = sweep * Math.max(0, Number(child.dataset.cfxValue)) / total; if (next > 0) draw(child, angle, next, depth + 1); angle += next; });
      };
      draw(current, -Math.PI / 2, Math.PI * 2, 0);
    }
    const resolve = event => event.target instanceof Element ? event.target.closest('[data-cfx-node]') : null;
    const hover = event => { const node = resolve(event); if (node) { setHover(root, node, true, true); showTip(root, root.querySelector('.cfx-tooltip'), node, event); } };
    overlay.addEventListener('pointermove', hover); overlay.addEventListener('focusin', hover);
    overlay.addEventListener('pointerleave', () => { clearHover(root, true, true); hideTip(root, root.querySelector('.cfx-tooltip'), false); });
    const activate = event => {
      const node = resolve(event); if (!node) return; event.stopPropagation();
      if (event instanceof KeyboardEvent && event.key === ' ' || !navigateStandaloneHierarchy(root, node.dataset.cfxNode)) {
        toggleSelection(root, node, event); pinTip(root, root.querySelector('.cfx-tooltip'), node, event);
      }
    };
    overlay.addEventListener('click', activate); overlay.addEventListener('keydown', event => { if (['Enter', ' '].includes(event.key)) { event.preventDefault(); activate(event); } });
    refreshKeyboardNavigation(root); syncResetControl(root);
    emitHostEvent(root, 'cfxhierarchy', { rootId: id, path: standaloneHierarchyPath(state, id).map(node => ({ id: node.dataset.cfxNode, label: node.dataset.cfxLabel })) });
    return true;
  };
