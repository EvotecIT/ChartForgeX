  const captureNativeValueMotion = (root, next, value, request, adaptive) => {
    if (value === undefined || value === null) return null;
    if (typeof value !== 'string' || value.length > 1048576)
      throw new TypeError('Native transition: bounded captured value JSON required.');
    let payload;
    try { payload = JSON.parse(value); } catch { throw new TypeError('Native transition: invalid JSON.'); }
    if (payload?.kind === 'native-transition') return captureNativeTransitionMotion(root, next, payload, request, adaptive);
    if (!payload || !['progress-value', 'layered-radial-value', 'circle-value', 'progress-ring-value', 'gauge-value', 'bullet-value', 'cartesian-value', 'radial-value', 'hierarchy-value'].includes(payload.kind)
        || typeof payload.seriesKey !== 'string' || !payload.seriesKey || (payload.kind === 'hierarchy-value' ? typeof payload.nodeId !== 'string' || !payload.nodeId || payload.observationId !== undefined : typeof payload.observationId !== 'string' || !payload.observationId || payload.nodeId !== undefined)
        || ![payload.fromValue, payload.toValue, payload.minimum, payload.maximum].every(Number.isFinite)
        || payload.maximum <= payload.minimum || !Number.isFinite(payload.maximum - payload.minimum))
      throw new TypeError('Native transition: source identity/value/domain mismatch.');
    if (request && (request.channel !== 'progress' || request.kind !== 'replace-value' || payload.kind !== 'progress-value'
        || payload.seriesKey !== request.seriesKey || payload.observationId !== request.observationId || payload.toValue !== request.value))
      throw new TypeError('Native transition: request identity/value mismatch.');
    const previous = chartLifetimes.get(root).adaptive;
    if (!!previous !== !!adaptive || !!payload.narrow !== !!adaptive)
      throw new TypeError('Native transition: adaptive drawings changed.');
    const cartesian = payload.kind === 'cartesian-value';
    const selector = payload.kind === 'hierarchy-value' ? '[data-cfx-role="hierarchy-series"] [data-cfx-node]' : payload.kind === 'radial-value' ? '[data-cfx-role="radial-point"]' : cartesian ? '[data-cfx-role="point"]' : payload.kind === 'progress-value' ? '[data-cfx-role="progress-row"]'
      : payload.kind === 'layered-radial-value' ? '[data-cfx-role="layered-radial-point"]'
      : payload.kind === 'circle-value' ? '[data-cfx-role="circle-chart"]'
      : payload.kind === 'progress-ring-value' ? '[data-cfx-role="progress-ring-point"]'
      : payload.kind === 'gauge-value' ? '[data-cfx-role="gauge"]' : '[data-cfx-role="bullet-row"]';
    const rowFor = drawing => interactiveTargets(drawing, selector).find(row => {
      const identity = targetIdentity(row);
      return identity.seriesKey === payload.seriesKey && (payload.kind === 'hierarchy-value' ? row.dataset.cfxNode === payload.nodeId : identity.observationId === payload.observationId);
    });
    const valueFor = row => Number(payload.kind === 'hierarchy-value' ? row?.dataset.cfxAuthoredValue : cartesian ? row?.dataset.cfxY : row?.dataset.cfxValue);
    const domainFor = (drawing, row) => ['radial-value', 'hierarchy-value'].includes(payload.kind) ? { minimum: 0, maximum: Number.MAX_VALUE } : cartesian ? chartAxisProjection(drawing, row.dataset.cfxAxis === 'secondary' ? 'secondaryY' : 'y')
      : payload.kind === 'progress-value' ? progressDomain(drawing, row)
      : { minimum: Number(row.dataset.cfxMin), maximum: Number(row.dataset.cfxMax) };
    const read = (frames, before, after) => {
      if (!Array.isArray(frames) || frames.length < 2 || frames.length > 16 || !frames.every(svg => typeof svg === 'string'))
        throw new TypeError('Native transition: 2..16 complete frames required.');
      const first = rowFor(before), last = rowFor(after);
      const beforeDomain = first && domainFor(before, first), afterDomain = last && domainFor(after, last);
      if (!first || !last || !beforeDomain || !afterDomain || valueFor(first) !== payload.fromValue || valueFor(last) !== payload.toValue
          || beforeDomain.minimum !== payload.minimum || afterDomain.minimum !== payload.minimum
          || beforeDomain.maximum !== payload.maximum || afterDomain.maximum !== payload.maximum)
        throw new TypeError('Native transition: retained endpoints changed.');
      const destination = after.querySelector('.cfx-stage svg');
      return frames.map((markup, index) => {
        const parsed = new DOMParser().parseFromString(markup, 'image/svg+xml'), svg = parsed.documentElement;
        if (parsed.querySelector('parsererror') || svg.localName !== 'svg' || svg.namespaceURI !== 'http://www.w3.org/2000/svg'
            || svg.getAttribute('viewBox') !== destination.getAttribute('viewBox') || svg.querySelector('script,foreignObject')
            || [svg, ...svg.querySelectorAll('*')].some(node => [...node.attributes].some(attribute => /^on/i.test(attribute.name))))
          throw new TypeError('Native transition: complete native SVG frame required.');
        const row = [...svg.querySelectorAll(selector)].find(node => node.dataset.cfxSeriesKey === payload.seriesKey
          && (payload.kind === 'hierarchy-value' ? node.dataset.cfxNode === payload.nodeId : node.dataset.cfxObservationId === payload.observationId));
        const reading = valueFor(row), domain = row && domainFor(cartesian ? after : svg, row);
        if (!row || !domain || !Number.isFinite(reading) || domain.minimum !== payload.minimum || domain.maximum !== payload.maximum
            || ['progress-value', 'progress-ring-value', 'cartesian-value', 'radial-value', 'hierarchy-value'].includes(payload.kind) && (reading < payload.minimum || reading > payload.maximum)
            || index === 0 && reading !== payload.fromValue || index === frames.length - 1 && reading !== payload.toValue)
          throw new TypeError('Native transition: captured frame identity/value mismatch.');
        // Every potentially installed frame uses the same generated-section
        // validation as the accepted drawing, including inactive responsive frames.
        const candidate = after.cloneNode(true);
        candidate.querySelector('.cfx-stage svg').replaceWith(document.importNode(svg, true));
        readChartSection(candidate, true);
        return document.importNode(svg, true);
      });
    };
    const wide = read(payload.wide, previous ? previous.wide : root, adaptive ? adaptive.wide : next);
    const narrow = adaptive ? read(payload.narrow, previous.narrow, adaptive.narrow) : null;
    if (narrow && narrow.length !== wide.length) throw new TypeError('Native transition: frame counts differ.');
    return Object.freeze({ wide, narrow });
  };
  const beginCapturedValueMotion = (root, motion, captured, svg, media) => {
    const lifetime = chartLifetimes.get(root), frames = lifetime.adaptive?.current === 'narrow' ? captured.narrow : captured.wide;
    if (!frames) return;
    const parent = svg.parentElement; if (!parent) return;
    const bounds = svg.getBoundingClientRect(), origin = stagePoint(parent, { x: bounds.left, y: bounds.top });
    const { scaleX, scaleY } = stageProjection(parent), width = bounds.width / scaleX, height = bounds.height / scaleY;
    if (![origin.x, origin.y, width, height].every(Number.isFinite) || width <= 0 || height <= 0) return;
    const layer = document.createElement('div');
    layer.className = 'cfx-native-motion-frame'; layer.setAttribute('aria-hidden', 'true'); layer.setAttribute('inert', '');
    layer.style.cssText = 'position:absolute;pointer-events:none';
    layer.style.left = origin.x + 'px'; layer.style.top = origin.y + 'px';
    layer.style.width = width + 'px'; layer.style.height = height + 'px';
    for (const attribute of [...root.attributes]) if (attribute.name.startsWith('data-cfx-')) layer.setAttribute(attribute.name, attribute.value);
    const opacity = svg.style.getPropertyValue('opacity'), priority = svg.style.getPropertyPriority('opacity');
    const state = chartUpdateState(root, true); const cleanups = [];
    let frame = 0, firstTime, shown = -1, released = false;
    const release = ownChartWork(root, () => {
      released = true; if (frame) cancelAnimationFrame(frame); layer.remove();
      if (opacity) svg.style.setProperty('opacity', opacity, priority); else svg.style.removeProperty('opacity');
      delete svg.dataset.cfxUpdateMotion;
      for (const cleanup of cleanups.splice(0)) cleanup();
      if (lifetime.updateMotionRelease === release) lifetime.updateMotionRelease = null;
    });
    lifetime.updateMotionRelease = release; svg.dataset.cfxUpdateMotion = 'true';
    const show = index => {
      const drawing = frames[index].cloneNode(true);
      // The layer uses the accepted SVG's rendered viewport. Its captured child
      // must not apply that viewport's inherited camera transform a second time.
      for (const [name, value] of Object.entries({ transform: 'none', transition: 'none', margin: '0', display: 'block',
        width: '100%', height: '100%', 'min-width': '0', 'max-width': 'none' })) drawing.style.setProperty(name, value, 'important');
      layer.removeAttribute('data-cfx-passive'); layer.replaceChildren(drawing);
      // Reuse the accepted effects-state owners; this layer contributes no hit targets,
      // command handlers, source values, geometry calculations or independent selection.
      applySelectionSetByTargets(layer, state.selected, true);
      for (const target of state.muted) { const found = resolveSeriesTarget(layer, target); if (found) setSeriesMuted(layer, found, true); }
      for (const target of state.isolated) { const found = resolveSeriesTarget(layer, target); if (found) setSeriesIsolation(layer, found, true); }
      applyHostHighlight(layer, state.highlight);
      layer.setAttribute('data-cfx-passive', 'true'); layer.dataset.cfxUpdateFrame = String(index); shown = index;
    };
    const tick = timestamp => {
      frame = 0; if (released) return;
      firstTime ??= timestamp;
      const ratio = Math.min(1, (timestamp - firstTime) / motion.duration);
      if (ratio >= 1) { release(); return; }
      const index = Math.floor(ratio * (frames.length - 1)); if (index !== shown) show(index);
      frame = requestAnimationFrame(tick);
    };
    const reduced = event => { if (event.matches) release(); }, hidden = () => { if (document.hidden) release(); };
    media?.addEventListener('change', reduced); document.addEventListener('visibilitychange', hidden);
    cleanups.push(() => media?.removeEventListener('change', reduced), () => document.removeEventListener('visibilitychange', hidden));
    for (const name of ['pointermove', 'pointerdown', 'keydown', 'wheel']) cleanups.push(listenChart(root, root, name, release, true));
    cleanups.push(listenChart(root, window, 'resize', release));
    try {
      svg.parentElement.appendChild(layer); show(0); svg.style.setProperty('opacity', '0', 'important');
      frame = requestAnimationFrame(tick);
    } catch { release(); } // Optional presentation cannot undo an already accepted native scene.
  };
