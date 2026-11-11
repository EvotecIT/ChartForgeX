  // Native glyph images include raster padding. Only their alpha ink belongs to
  // the label's hit box; a transparent glyph pixel must never steal another row.
  const legendInkBounds = node => {
    const inverse = node.getScreenCTM?.()?.inverse();
    if (!inverse) return null;
    let bounds;
    for (const shape of node.querySelectorAll('text,rect,circle,ellipse,line,polyline,path,polygon,use,image')) {
      if (shape.closest('[data-cfx-browser-hit-area],[data-cfx-keyboard-focus-outline],[data-cfx-highlight-outline]')
        || getComputedStyle(shape).display === 'none') continue;
      const reference = shape.localName === 'use' && shape.getAttribute('href')?.startsWith('#')
        ? node.ownerSVGElement.getElementById(shape.getAttribute('href').slice(1)) : shape;
      if (!reference) continue;
      let box = reference.getBBox?.(), offset = shape.localName === 'use';
      // Firefox measures an unrendered definition as empty; the placed <use> already includes its x/y.
      if (offset && !(box?.width > 0 || box?.height > 0)) { box = shape.getBBox?.(); offset = false; }
      if (!box) continue;
      if (reference.localName === 'image') {
        const alpha = (reference.dataset.cfxImageAlphaBounds || '').trim().split(/\s+/).map(Number);
        if (alpha.length === 6 && alpha.every(Number.isFinite) && alpha[4] > 0 && alpha[5] > 0) box = {
          x: box.x + box.width * alpha[0] / alpha[4], y: box.y + box.height * alpha[1] / alpha[5],
          width: box.width * alpha[2] / alpha[4], height: box.height * alpha[3] / alpha[5]
        };
      }
      const screen = shape.getScreenCTM?.();
      if (!screen || !(box.width > 0 || box.height > 0)) continue;
      const matrix = inverse.multiply(screen);
      const x = offset ? shape.x.baseVal.value : 0;
      const y = offset ? shape.y.baseVal.value : 0;
      for (const [px, py] of [[box.x, box.y], [box.x + box.width, box.y], [box.x, box.y + box.height], [box.x + box.width, box.y + box.height]]) {
        const p = new DOMPoint(px + x, py + y).matrixTransform(matrix);
        if (![p.x, p.y].every(Number.isFinite)) continue;
        if (!bounds) bounds = { x: p.x, y: p.y, right: p.x, bottom: p.y };
        else { bounds.x = Math.min(bounds.x, p.x); bounds.y = Math.min(bounds.y, p.y); bounds.right = Math.max(bounds.right, p.x); bounds.bottom = Math.max(bounds.bottom, p.y); }
      }
    }
    return bounds && { x: bounds.x, y: bounds.y, width: bounds.right - bounds.x, height: bounds.bottom - bounds.y };
  };
  const legendEntryShown = (root, node) => {
    const native = nativeLegendConnected(root) && capturedNativeVisibility(root);
    if (native) {
      const target = native.layout === 'reflow' && nativeReflowTarget(native, node);
      return !(node.dataset.cfxTargetHidden === 'true' || native.hiddenSeries.includes(seriesKey(node))
        || target && native.hiddenIndex.has(nativeTargetToken(target)));
    }
    return node.dataset.cfxMuted !== 'true' && (!root.dataset.cfxIsolatedSeries || node.dataset.cfxIsolated === 'true');
  };
  const syncLegendButtons = root => {
    if (hasFeature(root, 'LegendToggles')) root.querySelectorAll('[data-cfx-role="legend-item"]').forEach(node => {
      node.setAttribute('role', 'button');
      node.setAttribute('aria-pressed', String(legendEntryShown(root, node)));
    });
  };
  const announceChartTarget = (root, node, suffix = '') => {
    const live = root.querySelector('[data-cfx-live]');
    if (!live) return;
    const data = node.dataset, label = data.cfxLabel || seriesLabel(node);
    const value = data.cfxFormattedValue ?? data.cfxValue ?? data.cfxY;
    const message = [seriesLabel(node), data.cfxXLabel || data.cfxCategoryLabel || label,
      renderedTargetKind(node) === 'legend' ? '' : value, suffix].filter(value => value !== undefined && value !== '')
      .filter((value, index, values) => !index || value !== values[index - 1]).join(', ');
    live.setAttribute('aria-atomic', 'true');
    live.textContent = message;
  };
  const clearLegendReadout = root => {
    delete root._cfxKeyboardReadout;
    clearHover(root, false, false);
    hideTip(root, root.querySelector('.cfx-tooltip'), true);
    hideCrosshair(root, root.querySelector('.cfx-crosshair'));
  };
  const activateLegendItem = (root, node, event) => {
    if (event.altKey || event.shiftKey) { delete root._cfxLegendActivation; toggleSeriesFocus(root, node, true, true); return; }
    const target = seriesTarget(node), token = seriesTargetToken(target), previous = root._cfxLegendActivation;
    const touch = event.pointerType === 'touch' || root._cfxLegendTouch === true;
    const double = !(event instanceof KeyboardEvent) && previous?.token === token
      && (event.detail === 2 || touch && event.timeStamp - previous.time < 350);
    if (double && !nativeLegendConnected(root)) {
      delete root._cfxLegendActivation;
      // The first click already toggled. Undo it before applying isolation so
      // both another double-click and another double-tap restore the old view.
      setSeriesMuted(root, target, previous.muted);
      emitHostEvent(root, 'cfxseries', { ...target, muted: previous.muted });
      emitSync(root, { action: 'series', target, muted: previous.muted });
      const isolated = previous.isolated !== token;
      setSeriesIsolation(root, target, isolated);
      announceChartTarget(root, node, isolated ? uiText(root, 'Isolated', 'isolated') : uiText(root, 'Restored', 'restored'));
      emitHostEvent(root, 'cfxseriesfocus', { ...target, isolated });
      emitSync(root, { action: 'series-focus', target, isolated });
      return;
    }
    const original = root._cfxIsolationMutes?.get(node);
    // Event timestamps keep a quick double-tap together when a busy main thread handles the second tap late.
    root._cfxLegendActivation = event instanceof KeyboardEvent ? null : { token, time: event.timeStamp,
      isolated: root.dataset.cfxIsolatedSeries, muted: (original ? original.legend : node.dataset.cfxMuted) === 'true' };
    toggleSeries(root, node);
  };
  const bindLegendTargets = root => {
    const stage = root.querySelector('.cfx-stage'), svg = stage?.querySelector('svg');
    if (typeof svg?.getScreenCTM !== 'function' || typeof svg?.getBBox !== 'function') return;
    // Specification-driven drawings own their native legend geometry on every resize.
    if (svg.closest('[data-cfx-responsive-chart]')) { syncLegendButtons(root); return; }
    const sources = Array.from(svg.querySelectorAll('[data-cfx-role="legend-item"]')).filter(isInteractiveTarget);
    if (!sources.length) return;
    const ns = 'http://www.w3.org/2000/svg';
    const tray = document.createElement('div');
    tray.className = 'cfx-responsive-legend'; tray.setAttribute('aria-label', uiText(root, 'Legend', 'Legend'));
    // Flow layout is essential here: scaling the scene cannot create touch-sized labels.
    tray.style.cssText = 'display:none;flex-wrap:wrap;gap:2px;align-items:center';
    stage.before(tray);
    // Each tray row copies its source's key colour beside real HTML text, so labels stay crisp at any device scale.
    const trayItem = (source, tag, selector, text) => {
      const item = document.createElement(tag); item.className = 'cfx-responsive-legend__item';
      const swatch = document.createElementNS(ns, 'svg'); swatch.setAttribute('width', '12'); swatch.setAttribute('height', '12');
      swatch.setAttribute('viewBox', '0 0 12 12'); swatch.setAttribute('aria-hidden', 'true');
      const mark = document.createElementNS(ns, 'rect'); mark.setAttribute('x', '1'); mark.setAttribute('y', '1');
      mark.setAttribute('width', '10'); mark.setAttribute('height', '10'); mark.setAttribute('data-cfx-role', 'legend-swatch');
      const nativeSwatch = source.querySelector(selector);
      const painted = nativeSwatch?.matches('[fill],[stroke]') ? nativeSwatch : nativeSwatch?.querySelector('[fill],[stroke]');
      const style = painted && getComputedStyle(painted);
      const fill = painted?.style.fill || painted?.getAttribute('fill'), stroke = painted?.style.stroke || painted?.getAttribute('stroke');
      mark.setAttribute('fill', fill && fill !== 'none' && solidPaintColour(style.fill, style.fillOpacity) ? fill
        : stroke && stroke !== 'none' && solidPaintColour(style.stroke, style.strokeOpacity) ? stroke : paintColour(source) || 'currentColor');
      // The outline keeps a hidden series' marker visible once the stylesheet hollows it.
      mark.setAttribute('stroke', mark.getAttribute('fill')); mark.setAttribute('stroke-width', '1.5');
      swatch.appendChild(mark);
      const label = document.createElement('span'); label.className = 'cfx-responsive-legend__label';
      label.dataset.cfxRole = 'legend-label'; label.textContent = text;
      tray.appendChild(item).append(swatch, label);
      return item;
    };
    const entries = sources.map(source => {
      source.querySelectorAll('[data-cfx-browser-hit-area]').forEach(hit => hit.remove());
      const bounds = legendInkBounds(source) || source.getBBox();
      const relative = source.parentElement.getScreenCTM?.()?.inverse().multiply(source.getScreenCTM?.() || new DOMMatrix());
      const display = source.style.display, ariaHidden = source.getAttribute('aria-hidden'), nativeRegion = source._cfxNativeRegion;
      const pointerStyles = Array.from(source.querySelectorAll('*'), node => [node, node.style.pointerEvents]);
      pointerStyles.forEach(([node]) => { node.style.pointerEvents = 'none'; });
      const hit = document.createElementNS(ns, 'rect');
      hit.setAttribute('data-cfx-browser-hit-area', 'true'); hit.setAttribute('fill', 'transparent');
      hit.setAttribute('pointer-events', 'all'); source.appendChild(hit);
      const button = trayItem(source, 'button', '[data-cfx-role="legend-swatch"],[data-cfx-role="legend-symbol"],[data-cfx-role="legend-marker"]',
        source.dataset.cfxLabel || seriesLabel(source));
      button.type = 'button'; button.classList.add('cfx-tool');
      for (const [key, value] of Object.entries(source.dataset)) button.dataset[key] = value;
      button.setAttribute('aria-label', source.dataset.cfxLabel || source.getAttribute('aria-label') || seriesLabel(source));
      for (const role of ['legend-value', 'legend-percentage']) {
        const detail = source.querySelector('[data-cfx-role="' + role + '"]');
        const value = detail?.dataset.cfxCaption || detail?.textContent.trim();
        if (value) { const caption = document.createElement('span'); caption.className = 'cfx-responsive-legend__value'; caption.dataset.cfxRole = role; caption.textContent = value; button.appendChild(caption); }
      }
      return { source, button, hit, bounds, relative, display, ariaHidden, nativeRegion, pointerStyles };
    });
    // Custom entries describe the chart rather than a series, so they follow the toggles as plain, non-interactive rows.
    // Their first child is the clipped key, ahead of the label.
    const statics = Array.from(svg.querySelectorAll('[data-cfx-legend-custom]'), source =>
      trayItem(source, 'span', '*', source.getAttribute('aria-label')) && [source, source.style.display]);
    let frame = 0, compact = false;
    const refresh = () => {
      frame = 0;
      if (!root.isConnected || chartLifetimes.get(root)?.disposed !== false) return;
      const minimum = window.matchMedia?.('(pointer: coarse)').matches || stage.getBoundingClientRect().width < 560 ? 44 : 24;
      const boxes = entries.map(entry => {
        const parent = svgScreenMatrix(entry.source.parentElement);
        const matrix = parent && entry.relative ? parent.multiply(entry.relative) : parent;
        // The item may be display:none in compact mode, but its parent is still measurable.
        const scaleX = Math.hypot(matrix?.a || 0, matrix?.b || 0), scaleY = Math.hypot(matrix?.c || 0, matrix?.d || 0);
        const width = Math.max(entry.bounds.width + 12 / (scaleX || 1), minimum / (scaleX || 1));
        const height = Math.max(entry.bounds.height + 8 / (scaleY || 1), minimum / (scaleY || 1));
        const box = { x: entry.bounds.x + (entry.bounds.width - width) / 2, y: entry.bounds.y + (entry.bounds.height - height) / 2, width, height };
        entry.source._cfxNativeRegion = Object.freeze(box);
        for (const [key, value] of Object.entries(box)) if (entry.hit.getAttribute(key) !== String(value)) entry.hit.setAttribute(key, value);
        entry.button.style.minHeight = minimum + 'px';
        const corners = matrix && [[box.x, box.y], [box.x + box.width, box.y], [box.x, box.y + box.height], [box.x + box.width, box.y + box.height]]
          .map(([x, y]) => new DOMPoint(x, y).matrixTransform(matrix));
        const clips = nativePaintClips(entry.source);
        const projection = svgScreenProjection(svg);
        if (projection && clips) clips.forEach(clip => clip.matrix = projection.multiply(clip.matrix));
        // Only clipped label ink forces the flowing legend; the enlarged hit box may exceed a tight legend band.
        const ink = matrix && [[entry.bounds.x, entry.bounds.y], [entry.bounds.x + entry.bounds.width, entry.bounds.y + entry.bounds.height]]
          .map(([x, y]) => new DOMPoint(x, y).matrixTransform(matrix));
        // Ink that merely touches the band's edge, such as a swatch drawn at the clip origin, is not cut off;
        // half a unit absorbs the browsers' differing matrix rounding.
        const clipped = !clips || ink && clips.some(clip => ink.some(point => {
          const local = point.matrixTransform(clip.matrix.inverse()), r = clip.rect;
          return local.x < r.x - .5 || local.x > r.x + r.width + .5 || local.y < r.y - .5 || local.y > r.y + r.height + .5;
        }));
        const x = corners?.map(point => point.x), y = corners?.map(point => point.y);
        return { box: corners ? { x: Math.min(...x), y: Math.min(...y), width: Math.max(...x) - Math.min(...x), height: Math.max(...y) - Math.min(...y) } : box, scaleY, clipped };
      });
      const collision = boxes.some((first, index) => boxes.slice(index + 1).some(second => {
        const a = first.box, b = second.box;
        return a.x < b.x + b.width && a.x + a.width > b.x && a.y < b.y + b.height && a.y + a.height > b.y;
      }));
      const next = stage.getBoundingClientRect().width < 560 || collision || boxes.some(box => box.scaleY < 0.85 || box.clipped);
      tray.style.display = next ? 'flex' : 'none'; tray.toggleAttribute('data-cfx-shown', next);
      entries.forEach(entry => {
        entry.source.style.display = next ? 'none' : entry.display;
        entry.source.dataset.cfxRole = next ? 'legend-native' : 'legend-item';
        entry.button.dataset.cfxRole = next ? 'legend-item' : 'legend-proxy';
        if (next) entry.source.setAttribute('aria-hidden', 'true');
        else if (entry.ariaHidden === null) entry.source.removeAttribute('aria-hidden');
        else entry.source.setAttribute('aria-hidden', entry.ariaHidden);
      });
      statics.forEach(([source, display]) => source.style.display = next ? 'none' : display);
      syncLegendButtons(root);
      if (compact !== next) { compact = next; refreshKeyboardNavigation(root, undefined, true); }
    };
    const queue = () => { if (!frame) frame = requestAnimationFrame(refresh); };
    const observer = typeof ResizeObserver === 'undefined' ? null : new ResizeObserver(queue);
    observer?.observe(stage); listenChart(root, window, 'resize', queue);
    ownChartWork(root, () => {
      if (frame) cancelAnimationFrame(frame); observer?.disconnect();
      tray.remove();
      statics.forEach(([source, display]) => source.style.display = display);
      delete root._cfxLegendActivation; delete root._cfxLegendTouch;
      entries.forEach(entry => {
        entry.hit.remove(); entry.source.style.display = entry.display;
        entry.source.dataset.cfxRole = 'legend-item';
        if (entry.nativeRegion === undefined) delete entry.source._cfxNativeRegion; else entry.source._cfxNativeRegion = entry.nativeRegion;
        if (entry.ariaHidden === null) entry.source.removeAttribute('aria-hidden'); else entry.source.setAttribute('aria-hidden', entry.ariaHidden);
        entry.pointerStyles.forEach(([node, value]) => { node.style.pointerEvents = value; });
      });
    });
    syncLegendButtons(root); refresh();
  };
