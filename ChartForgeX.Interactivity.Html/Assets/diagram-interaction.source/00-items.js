(() => {
  // Flow and sequence exploration over native script-free SVG. Facts come from the model the adapter
  // captured beside the SVG; the runtime only finds the matching native groups and paints state.
  const key = Symbol.for('ChartForgeX.diagramInteraction');
  if (window[key]) return;
  const selector = '.cfx-diagram[data-cfx-diagram-model]';
  const playbackDelay = 900;
  const quote = value => window.CSS?.escape ? CSS.escape(value) : String(value).replace(/["\\]/g, '\\$&');
  const sourceElements = (svg, id, role) => Array.from(svg.querySelectorAll(`[data-cfx-source-id="${quote(id)}"]`))
    .filter(element => !role || element.getAttribute('data-cfx-role') === role);

  const readModel = root => {
    try { return JSON.parse(root.getAttribute('data-cfx-diagram-model') || '{}'); } catch { return {}; }
  };

  const buildItems = (svg, model) => {
    const items = [], byId = new Map(), owners = new WeakMap();
    for (const fact of Array.isArray(model.items) ? model.items : []) {
      const elements = sourceElements(svg, fact.id, fact.role);
      if (!elements.length || byId.has(fact.id)) continue;
      const parts = fact.part ? sourceElements(svg, fact.part) : [];
      const item = { ...fact, elements, all: elements.concat(parts), related: new Set(), incoming: [], outgoing: [] };
      for (const element of item.all) owners.set(element, item);
      items.push(item); byId.set(fact.id, item);
    }
    const messages = items.filter(item => item.kind === 'message');
    messages.forEach((item, index) => { item.order = index + 1; });
    for (const item of items) {
      if (item.source == null) continue;
      const source = byId.get(item.source), target = byId.get(item.target);
      if (source) { item.related.add(source); source.related.add(item); source.outgoing.push(item); }
      if (target) { item.related.add(target); target.related.add(item); target.incoming.push(item); }
    }
    return { items, byId, owners, messages };
  };

  // One readout per item: eyebrow, title and supporting lines, reused by the tooltip, readout and accessible name.
  const describe = (item, graph, text) => {
    const name = id => graph.byId.get(id)?.label || id;
    const route = item.source != null ? text('DiagramRoute', '{source} → {target}', { source: name(item.source), target: name(item.target) }) : '';
    const list = links => links.map(link => {
      const other = name(link.source === item.id ? link.target : link.source);
      return link.label ? other + ' (' + link.label + ')' : other;
    }).join(', ');
    switch (item.kind) {
      case 'message':
        return { eyebrow: text('DiagramMessage', 'Message {current} of {count}', { current: item.order, count: graph.messages.length }),
          title: item.label || route, lines: item.label ? [route] : [] };
      case 'participant': {
        const sent = item.outgoing.filter(link => link.kind === 'message').length;
        const received = item.incoming.filter(link => link.kind === 'message').length;
        return { eyebrow: '', title: item.label, lines: [text('DiagramParticipantMessages', '{sent} sent · {received} received', { sent, received })] };
      }
      case 'connector':
        return { eyebrow: item.label ? route : '', title: item.label || route, lines: [] };
      default: {
        const lines = [];
        if (item.detail) lines.push(item.detail);
        if (item.incoming.length) lines.push(text('DiagramIncoming', 'From {items}', { items: list(item.incoming) }));
        if (item.outgoing.length) lines.push(text('DiagramOutgoing', 'To {items}', { items: list(item.outgoing) }));
        return { eyebrow: item.group || '', title: item.label, lines };
      }
    }
  };
