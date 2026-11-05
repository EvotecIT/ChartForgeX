window[Symbol.for('ChartForgeX.uiText')] ||= (root, key, text, values) => {
  const source = root?.dataset.cfxUi || '';
  let bundle = root?._cfxUi;
  if (!bundle || bundle.source !== source) {
    let messages;
    try { messages = source ? JSON.parse(source) : {}; } catch { messages = {}; }
    bundle = { source, messages };
    if (root) root._cfxUi = bundle;
  }
  const value = bundle.messages?.[key];
  if (typeof value === 'string') text = value;
  return values ? text.replace(/\{([^{}]+)\}/g, (token, name) => Object.prototype.hasOwnProperty.call(values, name) ? String(values[name]) : token) : text;
};
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
  const svgNamespace = 'http://www.w3.org/2000/svg';

  const unionRect = elements => {
    let left = Infinity, top = Infinity, right = -Infinity, bottom = -Infinity;
    for (const element of elements) {
      const box = element.getBoundingClientRect();
      if (!box.width && !box.height) continue;
      left = Math.min(left, box.left); top = Math.min(top, box.top);
      right = Math.max(right, box.right); bottom = Math.max(bottom, box.bottom);
    }
    return Number.isFinite(left) ? { left, top, right, bottom, width: right - left, height: bottom - top } : null;
  };

  // Steps and participants receive a focus ring in SVG units; lines glow instead because a box around a route reads as clutter.
  const paintRing = (svg, item) => {
    if (item.kind === 'connector' || item.kind === 'message') return null;
    const box = unionRect(item.elements), matrix = svg.getScreenCTM()?.inverse();
    if (!box || !matrix) return null;
    const point = (x, y) => new DOMPoint(x, y).matrixTransform(matrix);
    const start = point(box.left, box.top), end = point(box.right, box.bottom), pad = 4;
    const ring = document.createElementNS(svgNamespace, 'rect');
    ring.setAttribute('data-cfx-diagram-ring', 'true');
    ring.setAttribute('aria-hidden', 'true');
    ring.setAttribute('x', String(start.x - pad)); ring.setAttribute('y', String(start.y - pad));
    ring.setAttribute('width', String(end.x - start.x + pad * 2)); ring.setAttribute('height', String(end.y - start.y + pad * 2));
    ring.setAttribute('rx', '6');
    svg.append(ring);
    return ring;
  };

  const fillReadout = (target, description, strongTitle) => {
    target.replaceChildren();
    const parts = [description.title, description.eyebrow, ...description.lines].filter(Boolean);
    parts.forEach((part, index) => {
      if (index) target.append(document.createTextNode(' · '));
      if (index === 0 && strongTitle) {
        const title = document.createElement('strong'); title.textContent = part; target.append(title);
      } else target.append(document.createTextNode(part));
    });
  };

  const fillTooltip = (tooltip, description) => {
    tooltip.replaceChildren();
    const line = (className, value) => {
      if (!value) return;
      const element = document.createElement('div'); element.className = className; element.textContent = value; tooltip.append(element);
    };
    line('cfx-diagram-tooltip__eyebrow', description.eyebrow);
    line('cfx-diagram-tooltip__title', description.title);
    description.lines.forEach(value => line('cfx-diagram-tooltip__line', value));
  };

  // Above the target when there is room, otherwise below; always inside the visible part of the (possibly scrolled) stage.
  const placeTooltip = (stage, tooltip, item) => {
    const box = unionRect(item.all), host = stage.getBoundingClientRect();
    if (!box) { tooltip.hidden = true; return; }
    tooltip.hidden = false;
    const width = tooltip.offsetWidth, height = tooltip.offsetHeight, gap = 8, scroll = stage.scrollLeft;
    const centre = (box.left + box.right) / 2 - host.left + scroll;
    const left = Math.max(scroll + 4, Math.min(scroll + stage.clientWidth - width - 4, centre - width / 2));
    let top = box.top - host.top - height - gap;
    if (top < 0) top = box.bottom - host.top + gap;
    tooltip.style.left = left + 'px';
    tooltip.style.top = top + 'px';
  };
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
    // Sequence playback walks the messages in order, like use case playback on topology pages.
    const toolbar = root.querySelector('.cfx-diagram-toolbar');
    const control = name => toolbar?.querySelector(`[data-cfx-diagram-step="${name}"]`);
    const progress = toolbar?.querySelector('[data-cfx-diagram-progress]');
    const playButton = control('play');
    const updatePlayback = () => {
      if (!toolbar) return;
      const count = graph.messages.length, playing = !!state.timer;
      control('previous').disabled = state.step <= 1;
      control('next').disabled = state.step >= count;
      playButton.setAttribute('aria-pressed', playing ? 'true' : 'false');
      playButton.textContent = playing ? text('Pause', 'Pause') : text('Play', 'Play');
      if (progress) progress.textContent = state.step
        ? text('StepProgress', 'Step {current} / {count}', { current: state.step, count })
        : text('Steps', '{count} steps', { count });
    };
    const stop = () => {
      if (state.timer) lifetime.clearTimeout(state.timer);
      state.timer = 0;
      updatePlayback();
    };
    const setStep = step => {
      state.step = Math.max(0, Math.min(graph.messages.length, step));
      state.played = state.step ? graph.messages[state.step - 1] : null;
      if (state.played) rove(state.played);
      render(); updatePlayback();
    };
    const tick = () => {
      state.timer = 0;
      setStep(state.step + 1);
      if (state.step < graph.messages.length) { state.timer = lifetime.timeout(tick, playbackDelay); updatePlayback(); }
    };
    if (toolbar && graph.messages.length) {
      toolbar.hidden = false;
      listen(control('previous'), 'click', () => { stop(); setStep(state.step - 1); });
      listen(control('next'), 'click', () => { stop(); setStep(state.step + 1); });
      listen(playButton, 'click', () => {
        if (state.timer) { stop(); return; }
        if (state.step >= graph.messages.length) state.step = 0;
        tick();
      });
      updatePlayback();
    }
    const itemFor = target => target?.closest?.('[data-cfx-diagram-item]') ? graph.owners.get(target.closest('[data-cfx-diagram-item]')) : null;
    listen(svg, 'pointerover', event => {
      const item = itemFor(event.target);
      if (item && item !== state.hovered) { state.hovered = item; render(); }
    });
    listen(stage, 'pointerleave', () => { if (state.hovered) { state.hovered = null; render(); } });
    listen(svg, 'click', event => {
      const item = itemFor(event.target);
      if (item && document.activeElement === item.focus) { state.focused = item; render(); return; }
      if (item) { item.focus.focus({ preventScroll: true }); return; }
      if (state.focused) state.focused.focus.blur();
      state.focused = null; state.hovered = null; render();
    });
    listen(root, 'focusin', event => {
      const item = itemFor(event.target);
      if (!item || item.focus !== event.target) return;
      rove(item); state.focused = item; render();
    });
    listen(root, 'focusout', event => {
      if (!state.focused || root.contains(event.relatedTarget) && itemFor(event.relatedTarget)) return;
      state.focused = null; render();
    });
    listen(root, 'keydown', event => {
      if (event.key === 'Escape') {
        if (!state.hovered && !state.focused && !state.played && !state.timer) return;
        stop();
        state.hovered = null; state.focused = null; state.played = null; state.step = 0;
        render(); updatePlayback();
        event.preventDefault();
        return;
      }
      const item = itemFor(event.target);
      if (!item || item.focus !== event.target || event.altKey || event.ctrlKey || event.metaKey) return;
      const index = graph.items.indexOf(item), last = graph.items.length - 1;
      const next = { ArrowRight: index + 1, ArrowDown: index + 1, ArrowLeft: index - 1, ArrowUp: index - 1, Home: 0, End: last }[event.key];
      if (next === undefined) return;
      event.preventDefault();
      const target = graph.items[Math.max(0, Math.min(last, next))];
      // Moving onto the already focused item after Escape restores its readout.
      if (target === item) { state.focused = item; render(); return; }
      target.focus.focus({ preventScroll: false });
    });
    listen(window, 'resize', () => { if (!tooltip.hidden) render(); });
    lifetime.own(() => {
      for (const element of root.querySelectorAll('[data-cfx-diagram-item]')) {
        element.removeAttribute('data-cfx-diagram-item'); element.removeAttribute('data-cfx-diagram-state');
      }
      for (const item of graph.items) ['tabindex', 'role', 'aria-label'].forEach(name => item.focus.removeAttribute(name));
      state.ring?.remove();
      root.removeAttribute('data-cfx-diagram-active');
    });
  };

  const mount = scope => {
    const roots = scope?.matches?.(selector) ? [scope] : Array.from((scope || document).querySelectorAll(selector));
    roots.forEach(bind);
  };
  window[key] = Object.freeze({ mount });
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', () => mount(document), { once: true });
  else mount(document);
})();
