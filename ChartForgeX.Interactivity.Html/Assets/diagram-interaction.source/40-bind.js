  const bind = root => {
    const stage = root.querySelector('[data-cfx-diagram-stage]'), svg = stage?.querySelector('svg');
    if (!svg || root.getAttribute('data-cfx-runtime-bound') === 'true') return;
    const lifetime = window[Symbol.for('ChartForgeX.adapterLifetimes')].create(root, 'diagram');
    window[Symbol.for('ChartForgeX.nativeTextExport')]?.mount(root, release => lifetime.own(release));
    const listen = (target, name, listener, options) => lifetime.listen(target, name, listener, options);
    const text = (message, fallback, values) => window[Symbol.for('ChartForgeX.uiText')](root, message, fallback, values);
    const graph = buildItems(svg, readModel(root));
    if (!graph.items.length) return;
    root.setAttribute('data-cfx-runtime-bound', 'true');
    lifetime.own(() => root.removeAttribute('data-cfx-runtime-bound'));

    // The static SVG is one image; once its parts are focusable the SVG becomes a labelled group of them.
    const svgRole = svg.getAttribute('role');
    svg.setAttribute('role', 'group');
    lifetime.own(() => { if (svgRole == null) svg.removeAttribute('role'); else svg.setAttribute('role', svgRole); });
    const readout = root.querySelector('[data-cfx-diagram-readout]');
    const hint = readout?.textContent || text('DiagramHint', 'Hover or tap a shape, or use Tab and the arrow keys. Escape clears.');
    if (readout) readout.hidden = false;
    const tooltip = document.createElement('div');
    tooltip.className = 'cfx-diagram-tooltip'; tooltip.hidden = true; tooltip.setAttribute('aria-hidden', 'true');
    stage.append(tooltip);
    lifetime.own(() => tooltip.remove());

    for (const item of graph.items) {
      const description = describe(item, graph, text);
      item.description = description;
      item.focus = item.elements[0];
      item.focus.setAttribute('tabindex', '-1');
      item.focus.setAttribute('role', 'button');
      item.focus.setAttribute('aria-label', [description.title, description.eyebrow, ...description.lines].filter(Boolean).join('. '));
      for (const element of item.all) element.setAttribute('data-cfx-diagram-item', item.id);
      // Thin connector and message lines get a transparent pointer band so they are as easy to hover as shapes.
      if (item.kind === 'connector' || item.kind === 'message') for (const line of item.elements.flatMap(element =>
        Array.from(element.querySelectorAll('path[data-cfx-role$="-line"]')))) {
        const band = line.cloneNode(false);
        for (const name of ['id', 'data-cfx-role', 'marker-start', 'marker-end', 'stroke-dasharray', 'class', 'style']) band.removeAttribute(name);
        band.setAttribute('data-cfx-diagram-hit', 'true'); band.setAttribute('aria-hidden', 'true');
        band.setAttribute('fill', 'none'); band.setAttribute('stroke', 'transparent'); band.setAttribute('stroke-width', '14');
        band.setAttribute('pointer-events', 'stroke');
        line.before(band);
        lifetime.own(() => band.remove());
      }
    }
    graph.items[0].focus.setAttribute('tabindex', '0');
    const state = { hovered: null, focused: null, played: null, step: 0, timer: 0, ring: null, current: graph.items[0] };

    const rove = item => {
      if (!item || state.current === item) return;
      state.current.focus.setAttribute('tabindex', '-1');
      item.focus.setAttribute('tabindex', '0');
      state.current = item;
    };

    const render = () => {
      const item = state.hovered || state.focused || state.played;
      for (const element of root.querySelectorAll('[data-cfx-diagram-state]')) element.removeAttribute('data-cfx-diagram-state');
      state.ring?.remove(); state.ring = null;
      if (!item) {
        root.removeAttribute('data-cfx-diagram-active');
        tooltip.hidden = true;
        if (readout) readout.textContent = hint;
        return;
      }
      root.setAttribute('data-cfx-diagram-active', item.id);
      const mark = (target, value) => target.all.forEach(element => {
        if (element.getAttribute('data-cfx-diagram-state') !== 'active') element.setAttribute('data-cfx-diagram-state', value);
      });
      mark(item, 'active');
      for (const related of item.related) {
        mark(related, 'related');
        // A step also lights the steps at the far end of its connectors.
        if (item.kind === 'step') related.related.forEach(other => { if (other.kind === 'step') mark(other, 'related'); });
      }
      state.ring = paintRing(svg, item);
      fillTooltip(tooltip, item.description);
      placeTooltip(stage, tooltip, item);
      if (readout) fillReadout(readout, item.description, true);
    };
