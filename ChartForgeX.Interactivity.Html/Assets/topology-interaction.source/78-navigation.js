    // Large-topology navigation. A dense drawing that would shrink its node captions below a readable
    // size opens magnified inside a bounded viewport; Fit still shows the whole drawing as an overview.
    const readableTextPixels = 10;
    let readableStart = null;
    let viewportTouched = false;
    // A host that rendered its own zoom state keeps it; the readable start only replaces the default view.
    const hostViewport = wrapper.hasAttribute('data-cfx-topology-zoom');
    const nodeCaptionSize = () => {
      const svg = topologySvg();
      if (!svg) return 0;
      let smallest = 0;
      svg.querySelectorAll('[data-cfx-role="topology-node-label"]').forEach(label => {
        // Captions can paint as glyph ink beside a hidden text twin; the label's own box says whether it shows.
        const text = label.matches('text') ? label : label.querySelector('text,[font-size]');
        if (!text || !isViewportVisible(label) || !(label.getBoundingClientRect().width > 0)) return;
        const size = Number.parseFloat(text.getAttribute('font-size') || window.getComputedStyle(text).fontSize);
        if (Number.isFinite(size) && size > 0 && (!smallest || size < smallest)) smallest = size;
      });
      return smallest;
    };
    const drawingUnit = metrics => {
      const viewBox = topologySvg()?.viewBox?.baseVal;
      return viewBox && viewBox.width > 0 && viewBox.height > 0 ? Math.min(metrics.svgWidth / viewBox.width, metrics.svgHeight / viewBox.height) : 0;
    };
    // The zoom at which the smallest visible node caption reaches the readable size (1 when it already does).
    const readableZoom = () => {
      const unit = drawingUnit(viewportMetrics());
      const caption = nodeCaptionSize() * unit;
      // Rounded up to the zoom attribute's precision so the stored zoom never lands a hair below the readable size.
      return caption > 0 && caption < readableTextPixels - 0.01 ? Math.ceil(readableTextPixels / caption * 1000) / 1000 : 1;
    };
    const findPanel = wrapper.querySelector('[data-cfx-topology-find-panel]');
    const cssPrefix = ((viewport.className || '').match(/(?:^|\s)(\S+)-viewport(?:\s|$)/) || [])[1] || 'cfx-topology';
    // The find row, any lifted heading and the plot share one card, so the controls read as part of the diagram.
    const ensureCard = () => {
      if (viewport === wrapper) return null;
      if (viewport.parentElement?.hasAttribute('data-cfx-topology-card')) return viewport.parentElement;
      const card = document.createElement('div');
      card.className = cssPrefix + '-card';
      card.setAttribute('data-cfx-topology-card', 'true');
      (findPanel && findPanel.parentElement === viewport.parentElement ? findPanel : viewport).before(card);
      if (findPanel) card.append(findPanel);
      card.append(viewport);
      return card;
    };
    const inkText = element => {
      if (!element) return '';
      const lines = Array.from(element.querySelectorAll('[data-cfx-ink-line]')).map(line => line.getAttribute('data-cfx-ink-line') || '');
      return (lines.length ? lines : Array.from(element.querySelectorAll('text')).map(text => text.textContent || '')).join(' ').replace(/\s+/g, ' ').trim();
    };
    const unionBounds = elements => elements.map(elementBounds).filter(box => box).reduce((all, box) => all
      ? { x: Math.min(all.x, box.x), y: Math.min(all.y, box.y), right: Math.max(all.right, box.x + box.width), bottom: Math.max(all.bottom, box.y + box.height) }
      : { x: box.x, y: box.y, right: box.x + box.width, bottom: box.y + box.height }, null);
    // A magnified plot would carry the heading and legend out of view, so they are lifted into HTML at their normal size.
    const liftHeading = () => {
      const svg = topologySvg();
      if (!svg || wrapper.querySelector('[data-cfx-topology-heading]')) return;
      const headings = Array.from(svg.querySelectorAll('[data-cfx-role="frame-heading"]'));
      const entries = Array.from(svg.querySelectorAll('[data-cfx-role="legend-entry"]'));
      if (!headings.length && !entries.length) return;
      const heading = document.createElement('div');
      heading.className = cssPrefix + '-heading';
      heading.setAttribute('data-cfx-topology-heading', 'true');
      heading.setAttribute('aria-hidden', 'true');
      const line = (name, text) => {
        if (!text) return;
        const element = document.createElement('div');
        element.className = cssPrefix + '-heading__' + name;
        element.textContent = text;
        heading.append(element);
      };
      line('title', inkText(headings[0]));
      line('subtitle', inkText(headings[1]));
      if (entries.length) {
        const legend = document.createElement('div');
        legend.className = cssPrefix + '-heading__legend';
        const title = inkText(svg.querySelector('[data-cfx-role="legend-title"]'));
        if (title) {
          const caption = document.createElement('span');
          caption.className = cssPrefix + '-heading__legend-title';
          caption.textContent = title;
          legend.append(caption);
        }
        entries.forEach(entry => {
          const label = entry.querySelector('[data-cfx-role="legend-label"]');
          const marks = Array.from(entry.children).filter(child => child !== label && !child.contains(label));
          const item = document.createElement('span');
          item.className = cssPrefix + '-heading__item';
          const bounds = unionBounds(marks);
          if (bounds) {
            // The swatch is the legend's own artwork, so any status, node or edge mark carries over unchanged.
            const swatch = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
            swatch.setAttribute('viewBox', [bounds.x - 1, bounds.y - 1, bounds.right - bounds.x + 2, bounds.bottom - bounds.y + 2].map(value => value.toFixed(2)).join(' '));
            swatch.setAttribute('aria-hidden', 'true');
            marks.forEach(mark => swatch.append(mark.cloneNode(true)));
            item.append(swatch);
          }
          item.append(document.createTextNode(inkText(label)));
          legend.append(item);
        });
        heading.append(legend);
      }
      (ensureCard() || wrapper).prepend(heading);
      wrapper.setAttribute('data-cfx-html-heading', 'true');
    };
    const dropHeading = () => {
      wrapper.querySelector('[data-cfx-topology-heading]')?.remove();
      wrapper.removeAttribute('data-cfx-html-heading');
    };
    const applyReadableStart = () => {
      const zoom = readableZoom();
      if (zoom <= 1.001) {
        // A widened page can make an earlier magnified start unnecessary; return to the whole drawing.
        if (readableStart) applyViewport({ zoom: 1, panX: 0, panY: 0 });
        readableStart = null;
        wrapper.removeAttribute('data-cfx-readable-zoom');
        wrapper.style.removeProperty('--cfx-topology-readable-height');
        dropHeading();
        return false;
      }
      ensureCard();
      liftHeading();
      const before = viewportMetrics();
      const height = Math.round(Math.min(before.svgHeight * zoom, Math.max(320, Math.min((window.innerHeight || 800) * 0.72, 760))));
      wrapper.setAttribute('data-cfx-readable-zoom', zoom.toFixed(3));
      wrapper.style.setProperty('--cfx-topology-readable-height', height + 'px');
      // Taking the frame height can add or remove a page scrollbar and narrow the drawing, so measure again inside it.
      const framed = Math.max(zoom, readableZoom());
      wrapper.setAttribute('data-cfx-readable-zoom', framed.toFixed(3));
      // Start where reading starts: the top of the diagram below its lifted heading, at the inline-start edge.
      const metrics = viewportMetrics();
      const unit = drawingUnit(metrics);
      const originX = metrics.svgWidth / 2;
      const originY = metrics.svgHeight / 2;
      const rtl = window.getComputedStyle(wrapper).direction === 'rtl';
      const viewBox = topologySvg()?.viewBox?.baseVal;
      const content = wrapper.hasAttribute('data-cfx-html-heading') ? elementBounds(wrapper.querySelector('[data-cfx-role="topology"]')) : null;
      const margin = 12;
      const top = content ? Math.max(0, content.y - margin) : 0;
      const left = content ? Math.max(0, content.x - margin) : 0;
      const right = content && viewBox ? Math.min(viewBox.width, content.x + content.width + margin) : (viewBox ? viewBox.width : metrics.svgWidth / (unit || 1));
      readableStart = {
        zoom: framed,
        panX: rtl ? metrics.width - originX - framed * (right * unit - originX) : framed * (originX - left * unit) - originX,
        panY: framed * (originY - top * unit) - originY
      };
      // The opening view is placed, not animated: settle the transform before transitions resume.
      wrapper.setAttribute('data-cfx-topology-settling', 'true');
      applyViewport(readableStart);
      const settled = topologySvg();
      if (settled) window.getComputedStyle(settled).transform;
      wrapper.removeAttribute('data-cfx-topology-settling');
      wrapper.dispatchEvent(new CustomEvent('cfx-topology-readable-start', { bubbles: true, composed: true, detail: { chartId: attr(wrapper, 'data-chart-id'), state: viewportState() } }));
      return true;
    };    // Moves the viewport to elements without dropping below the readable zoom: everything when it fits, otherwise the first one.
    const revealElements = elements => {
      const items = Array.from(elements || []).filter(isViewportVisible);
      if (!items.length || !viewportControls) return false;
      const svg = topologySvg();
      const viewBox = svg?.viewBox?.baseVal;
      const boxes = items.map(elementBounds).filter(box => box);
      if (!viewBox || !boxes.length) return false;
      const metrics = viewportMetrics();
      const unit = drawingUnit(metrics);
      const frame = list => list.reduce((current, box) => ({
        x: Math.min(current.x, box.x), y: Math.min(current.y, box.y),
        right: Math.max(current.right, box.x + box.width), bottom: Math.max(current.bottom, box.y + box.height)
      }), { x: list[0].x, y: list[0].y, right: list[0].x + list[0].width, bottom: list[0].y + list[0].height });
      const padding = 44;
      const all = frame(boxes);
      const fitZoom = Math.min(metrics.width / ((all.right - all.x) * unit + padding * 2), metrics.availableHeight / ((all.bottom - all.y) * unit + padding * 2));
      const minimum = readableZoom();
      const target = fitZoom >= minimum ? all : frame([boxes[0]]);
      const zoom = clamp(fitZoom >= minimum ? Math.min(fitZoom, Math.max(minimum, 2)) : minimum, 0.001, maximumZoom());
      const offsetX = (metrics.svgWidth - viewBox.width * unit) / 2;
      const offsetY = (metrics.svgHeight - viewBox.height * unit) / 2;
      const centerX = ((target.x + target.right) / 2 - viewBox.x) * unit + offsetX;
      const centerY = ((target.y + target.bottom) / 2 - viewBox.y) * unit + offsetY;
      const originX = metrics.svgWidth / 2;
      const originY = metrics.svgHeight / 2;
      applyViewport({ zoom, panX: metrics.width / 2 - originX - zoom * (centerX - originX), panY: metrics.availableHeight / 2 - originY - zoom * (centerY - originY) });
      emitViewport();
      return true;
    };
    const findState = () => ({
      query: String(findPanel?.querySelector('[data-cfx-topology-find]')?.value || '').trim(),
      group: findPanel?.querySelector('[data-cfx-topology-find-group]')?.value || '',
      kind: findPanel?.querySelector('[data-cfx-topology-find-kind]')?.value || ''
    });
    const findMatches = state => {
      const query = state.query.toLowerCase();
      return Array.from(wrapper.querySelectorAll('[data-cfx-role="topology-node"]')).filter(node => isViewportVisible(node)
        && (!state.group || attr(node, 'data-group-id') === state.group)
        && (!state.kind || attr(node, 'data-node-kind') === state.kind)
        && (!query || topologyFilterSearchText(node).includes(query)));
    };
    // Matches stay in context: everything else dims, edges between two matches stay lit, and the viewport moves to them.
    const applyFind = reveal => {
      const state = findState();
      const summary = findPanel?.querySelector('[data-cfx-topology-find-summary]');
      const active = !!(state.query || state.group || state.kind);
      if (!active) {
        highlight({});
        wrapper.removeAttribute('data-cfx-find-count');
        if (summary) summary.textContent = '';
        wrapper.dispatchEvent(new CustomEvent('cfx-topology-find', { bubbles: true, composed: true, detail: { chartId: attr(wrapper, 'data-chart-id'), ...state, nodeIds: [] } }));
        return [];
      }
      const matches = findMatches(state);
      const nodeIds = matches.map(node => attr(node, 'data-node-id'));
      const matched = new Set(nodeIds);
      const edgeIds = Array.from(wrapper.querySelectorAll('[data-cfx-role="topology-edge"]'))
        .filter(edge => matched.has(attr(edge, 'data-source-node-id')) && matched.has(attr(edge, 'data-target-node-id')))
        .map(edge => attr(edge, 'data-edge-id'));
      highlight({ nodeIds, edgeIds, dim: true });
      wrapper.setAttribute('data-cfx-find-count', String(matches.length));
      const total = Array.from(wrapper.querySelectorAll('[data-cfx-role="topology-node"]')).filter(isViewportVisible).length;
      if (summary) summary.textContent = matches.length
        ? uiText('TopologyFindSummary', '{count} of {total} nodes match', { count: matches.length, total })
        : uiText('TopologyFindNone', 'No matching nodes');
      if (reveal && matches.length) revealElements(matches);
      wrapper.dispatchEvent(new CustomEvent('cfx-topology-find', { bubbles: true, composed: true, detail: { chartId: attr(wrapper, 'data-chart-id'), ...state, nodeIds, edgeIds } }));
      return matches;
    };
    const clearFind = () => {
      findPanel?.querySelectorAll('input,select').forEach(control => { control.value = ''; });
      applyFind(false);
    };
    const bindViewportNavigation = () => {
      if (!hostViewport) applyReadableStart();
      if (typeof ResizeObserver === 'function') {
        let width = viewport.clientWidth;
        const observer = new ResizeObserver(() => {
          if (viewport.clientWidth === width) return;
          width = viewport.clientWidth;
          if (!viewportTouched && !hostViewport) applyReadableStart();
        });
        observer.observe(viewport);
        lifetime.own(() => observer.disconnect());
      }
      const editable = target => target instanceof Element && target.matches('input,textarea,select,[contenteditable="true"]');
      listen(wrapper, 'keydown', event => {
        if (event.altKey || event.ctrlKey || event.metaKey || editable(event.target)) return;
        const key = event.key;
        if (key === '+' || key === '=') zoomBy(1.2);
        else if (key === '-' || key === '_') zoomBy(0.8333333333);
        else if (key === '0') fitViewport();
        else if (event.target === viewport && key.startsWith('Arrow')) {
          const step = event.shiftKey ? 160 : 48;
          const state = viewportState();
          applyViewport({ zoom: state.zoom, panX: state.panX + (key === 'ArrowLeft' ? step : key === 'ArrowRight' ? -step : 0), panY: state.panY + (key === 'ArrowUp' ? step : key === 'ArrowDown' ? -step : 0) });
          emitViewport();
        } else return;
        event.preventDefault();
      });
    };
    const bindFindControls = () => {
      if (!findPanel) return;
      ensureCard();
      const search = findPanel.querySelector('[data-cfx-topology-find]');
      findPanel.querySelectorAll('select').forEach(control => listen(control, 'change', () => applyFind(true)));
      if (search) {
        listen(search, 'input', () => applyFind(true));
        listen(search, 'keydown', event => {
          if (event.key === 'Escape' && search.value) {
            event.preventDefault();
            event.stopPropagation();
            clearFind();
          } else if (event.key === 'Enter') {
            event.preventDefault();
            const first = applyFind(true)[0];
            if (!first) return;
            select(first);
            if (first.focus) first.focus({ preventScroll: true });
          }
        });
      }
      listen(wrapper, 'cfx-topology-set-find', event => {
        const detail = event.detail || {};
        if (search && detail.query !== undefined) search.value = detail.query || '';
        const group = findPanel.querySelector('[data-cfx-topology-find-group]');
        if (group && detail.group !== undefined) group.value = detail.group || '';
        const kind = findPanel.querySelector('[data-cfx-topology-find-kind]');
        if (kind && detail.kind !== undefined) kind.value = detail.kind || '';
        applyFind(detail.reveal !== false);
      });
    };
