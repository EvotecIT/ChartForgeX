  // A collapsed cluster overview shows one labelled relationship per site pair,
  // kind, and direction. The original edges remain in the document for expansion.
  const syncBundledEdgePresentation = (root, state) => {
    if (!state) return;
    state.edges.forEach(edge => { edge.label = attr(edge.el, 'data-cfx-bundle-label') || attr(edge.el, 'data-edge-label'); });
  };
  const graphOverviewDisclosure = root => {
    const shown = root.dataset.cfxGraphOverviewShown;
    const total = root.dataset.cfxGraphOverviewTotal;
    return shown && total ? `Priority overview: ${shown} routes from ${total} relationships` : '';
  };
  const applyCollapsedEdgeBundles = (root) => {
    root.querySelector('[data-cfx-role="graph-overview-note"]')?.remove();
    root.classList.remove('cfx-graph-priority-overview');
    delete root.dataset.cfxGraphOverviewShown;
    delete root.dataset.cfxGraphOverviewTotal;
    items(root, '[data-cfx-role="graph-edge"]').forEach(edge => {
      edge.classList.remove('cfx-graph-bundle-member');
      edge.classList.remove('cfx-graph-overview-member');
      if (attr(edge, 'data-cfx-bundle-count')) {
        edge.setAttribute('aria-label', attr(edge, 'data-cfx-bundle-original-aria'));
        edge.removeAttribute('data-cfx-bundle-count');
        edge.removeAttribute('data-cfx-bundle-original-aria');
        edge.removeAttribute('data-cfx-bundle-label');
      }
    });
    items(root, '[data-cfx-role="graph-edge-label"]').forEach(label => {
      if (attr(label, 'data-cfx-bundle-generated')) label.remove();
      else if (label.getAttribute('data-cfx-bundle-original-text') !== null) {
        label.textContent = attr(label, 'data-cfx-bundle-original-text');
        label.removeAttribute('data-cfx-bundle-original-text');
      }
    });

    const query = (root.querySelector('[data-cfx-graph-search]')?.value || '').trim();
    const filtered = items(root, '[data-cfx-graph-filter]').some(filter => filter.value);
    if (query || filtered) return;

    const collapsed = new Set(items(root, '[data-cfx-role="graph-cluster"][data-cluster-collapsed="true"]').map(cluster => attr(cluster, 'data-cluster-id')));
    if (collapsed.size < 2) return;
    const groups = new Map();
    items(root, '[data-cfx-role="graph-edge"]').forEach(edge => {
      const source = attr(edge, 'data-source-cluster-id');
      const target = attr(edge, 'data-target-cluster-id');
      if (!source || !target || source === target || !collapsed.has(source) || !collapsed.has(target) || attr(edge, 'data-edge-hidden') === 'true' || edge.classList.contains('cfx-graph-hierarchy-hidden')) return;
      const directed = attr(edge, 'data-edge-source-arrow') === 'true' || attr(edge, 'data-edge-target-arrow') === 'true';
      const pair = directed ? `${source}>${target}` : [source, target].sort().join('~');
      const key = JSON.stringify([pair, attr(edge, 'data-edge-kind'), attr(edge, 'data-edge-source-arrow'), attr(edge, 'data-edge-target-arrow')]);
      if (!groups.has(key)) groups.set(key, []);
      groups.get(key).push(edge);
    });
    const severity = edge => ({ critical: 3, warning: 2, healthy: 1 })[attr(edge, 'data-cfx-status')] || 0;
    const labels = new Map(items(root, '[data-cfx-role="graph-edge-label"]').map(label => [attr(label, 'data-edge-label-for'), label]));
    const leads = [];
    let positionedState;
    groups.forEach(edges => {
      const lead = edges.reduce((best, edge) => severity(edge) > severity(best) ? edge : best, edges[0]);
      leads.push(lead);
      if (edges.length < 2) return;
      edges.forEach(edge => { if (edge !== lead) edge.classList.add('cfx-graph-bundle-member'); });
      const count = edges.length;
      const summary = `${count} relationships. Select this route to expand both sites and inspect individual relationships.`;
      lead.setAttribute('data-cfx-bundle-count', String(count));
      lead.setAttribute('data-cfx-bundle-original-aria', attr(lead, 'aria-label'));
      lead.setAttribute('aria-label', summary);
      lead.setAttribute('data-cfx-bundle-label', `${count} relationships`);
      let label = labels.get(attr(lead, 'data-edge-id'));
      if (!label && root.dataset.cfxGraphRendererActive === 'svg' && attr(root, 'data-cfx-graph-accelerated-markup') !== 'true') {
        const viewport = root.querySelector('[data-cfx-role="graph-viewport"]');
        if (viewport) {
          label = root.ownerDocument.createElementNS('http:' + '//www.w3.org/2000/svg', 'text');
          label.classList.add('cfx-graph-edge-label');
          label.setAttribute('data-cfx-role', 'graph-edge-label');
          label.setAttribute('data-edge-label-for', attr(lead, 'data-edge-id'));
          label.setAttribute('data-cfx-bundle-generated', 'true');
          viewport.appendChild(label);
          positionedState ||= root.__cfxGraphState || graphState(root);
          const edge = positionedState.edges.find(item => item.el === lead);
          if (edge) {
            const rendered = visualEdge(edge, positionedState.byId);
            const point = edgeLabelPoint(rendered, edgeControl(rendered));
            label.setAttribute('x', point.x.toFixed(3));
            label.setAttribute('y', point.y.toFixed(3));
          }
        }
      }
      if (label) {
        if (!attr(label, 'data-cfx-bundle-generated')) label.setAttribute('data-cfx-bundle-original-text', label.textContent || '');
        label.textContent = `${count} links`;
      }
    });
    if (collapsed.size < 6 || leads.length <= collapsed.size * 2.5) return;
    const parent = new Map([...collapsed].map(id => [id, id]));
    const find = id => {
      while (parent.get(id) !== id) id = parent.get(id);
      return id;
    };
    const connect = edge => {
      const source = find(attr(edge, 'data-source-cluster-id'));
      const target = find(attr(edge, 'data-target-cluster-id'));
      if (source === target) return false;
      parent.set(source, target);
      return true;
    };
    const priority = leads.filter(edge => severity(edge) >= 2 || severity(edge) === 0);
    priority.forEach(connect);
    const shown = new Set(priority);
    leads.filter(edge => !shown.has(edge)).forEach(edge => { if (connect(edge)) shown.add(edge); });
    leads.forEach(edge => { if (!shown.has(edge)) edge.classList.add('cfx-graph-overview-member'); });
    const stage = root.querySelector('.cfx-graph-stage');
    if (stage && shown.size < leads.length) {
      root.classList.add('cfx-graph-priority-overview');
      root.dataset.cfxGraphOverviewShown = String(shown.size);
      root.dataset.cfxGraphOverviewTotal = String(items(root, '[data-cfx-role="graph-edge"]').length);
      const note = root.ownerDocument.createElement('div');
      note.className = 'cfx-graph-overview-note';
      note.setAttribute('data-cfx-role', 'graph-overview-note');
      note.textContent = `Showing ${shown.size} priority routes from ${items(root, '[data-cfx-role="graph-edge"]').length} relationships. Colors show the worst status. Select a route, expand a site, or filter to inspect all.`;
      stage.appendChild(note);
    }
  };
