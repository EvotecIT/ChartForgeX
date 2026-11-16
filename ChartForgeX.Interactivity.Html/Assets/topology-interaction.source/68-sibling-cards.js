    // Sibling group cards collapse to their header and expand back to the member rows. A card shrinks only as far as
    // the links attached to it allow, so a link entering its side never ends in empty space.
    const siblingCardLayout = card => {
      const surface = card.querySelector(':scope > [data-cfx-role="topology-node-surface"]');
      if (!surface || surface.tagName.toLowerCase() !== 'rect') return null;
      // The expanded top is captured once: a bottom-anchored collapse moves the surface, and expanding must return it.
      if (!surface.hasAttribute('data-sibling-top')) surface.setAttribute('data-sibling-top', surface.getAttribute('y') || '0');
      const top = parseFloat(surface.getAttribute('data-sibling-top')) || 0;
      const full = parseFloat(attr(card, 'data-sibling-expanded-height')) || parseFloat(surface.getAttribute('height')) || 0;
      const header = parseFloat(attr(card, 'data-sibling-collapsed-height')) || full;
      const id = attr(card, 'data-node-id');
      const ends = [];
      wrapper.querySelectorAll('[data-cfx-role="topology-edge"]').forEach(edge => {
        if (attr(edge, 'data-source-node-id') === id) ends.push(parseFloat(attr(edge, 'data-route-start-y')) - top);
        if (attr(edge, 'data-target-node-id') === id) ends.push(parseFloat(attr(edge, 'data-route-end-y')) - top);
      });
      const finite = ends.filter(Number.isFinite);
      const lowest = finite.length ? Math.max(...finite) : 0;
      const highest = finite.length ? Math.min(...finite) : 0;
      if (finite.length && highest >= full - 6) return { surface, top, full, height: header, shift: full - header };
      return { surface, top, full, height: Math.min(full, Math.max(header, lowest + 6)), shift: 0 };
    };
    const siblingClip = card => {
      for (let element = card.parentElement; element; element = element.parentElement) {
        const reference = element.getAttribute && element.getAttribute('clip-path');
        const id = reference && (reference.match(/url\(#?["']?#?([^"')]+)["']?\)/) || [])[1];
        const rect = id && element.ownerDocument.getElementById(id)?.querySelector('rect');
        if (rect) return { top: parseFloat(rect.getAttribute('y')) || 0, bottom: (parseFloat(rect.getAttribute('y')) || 0) + (parseFloat(rect.getAttribute('height')) || 0) };
        if (element.tagName && element.tagName.toLowerCase() === 'svg') {
          const box = element.viewBox && element.viewBox.baseVal;
          if (box && box.height > 0) return { top: box.y, bottom: box.y + box.height };
        }
      }
      return { top: -Infinity, bottom: Infinity };
    };
    const siblingClipTop = card => siblingClip(card).top;
    const siblingClipBottom = card => siblingClip(card).bottom;
    // Rows of a card that started collapsed carry their member's safe link and tooltip as data until the card first opens.
    const attachSiblingRowLinks = rows => rows.querySelectorAll('[data-cfx-role="topology-sibling-row"][data-sibling-href],[data-cfx-role="topology-sibling-row"][data-sibling-tooltip]').forEach(row => {
      const namespace = 'http://www.w3.org/2000/svg';
      const href = attr(row, 'data-sibling-href'), tip = attr(row, 'data-sibling-tooltip');
      const host = href ? row.ownerDocument.createElementNS(namespace, 'a') : row.ownerDocument.createElementNS(namespace, 'g');
      if (href) host.setAttribute('href', href);
      if (href && attr(row, 'data-sibling-new-tab') === 'true') { host.setAttribute('target', '_blank'); host.setAttribute('rel', 'noopener noreferrer'); }
      if (tip) { const title = row.ownerDocument.createElementNS(namespace, 'title'); title.textContent = tip; host.appendChild(title); }
      host.setAttribute('data-cfx-role', 'topology-sibling-row-host');
      row.parentNode.insertBefore(host, row); host.appendChild(row);
      row.removeAttribute('data-sibling-href'); row.removeAttribute('data-sibling-tooltip');
    });
    const setSiblingCardExpanded = (card, expanded, emit) => {
      const layout = siblingCardLayout(card);
      const rows = card.querySelector('[data-cfx-role="topology-sibling-rows"]');
      const header = card.querySelector('[data-cfx-role="topology-sibling-header"]');
      if (rows) { rows.style.display = expanded ? '' : 'none'; if (expanded) { rows.removeAttribute('opacity'); attachSiblingRowLinks(rows); } }
      // A card that started collapsed opens over its neighbours, so it is drawn last while open.
      // An open card must stay inside the clipped diagram: lift it by however far its rows would reach past the clip.
      const clipBottom = siblingClipBottom(card);
      const overflow = expanded && layout && Number.isFinite(clipBottom) ? Math.max(0, Math.min(layout.top + layout.full - clipBottom, layout.top - siblingClipTop(card))) : 0;
      overflow > 0 ? card.setAttribute('transform', 'translate(0 ' + (-overflow) + ')') : card.removeAttribute('transform');
      const front = card.closest('[data-cfx-role="topology-node-host"]') || card;
      if (expanded && emit && front.parentNode && front.parentNode.lastElementChild !== front) front.parentNode.appendChild(front);
      if (layout) {
        layout.surface.setAttribute('height', String(expanded ? layout.full : layout.height));
        layout.surface.setAttribute('y', String(expanded ? layout.top : layout.top + layout.shift));
        if (header) expanded || !layout.shift ? header.removeAttribute('transform') : header.setAttribute('transform', 'translate(0 ' + layout.shift + ')');
      }
      const chevron = card.querySelector('[data-cfx-role="topology-sibling-toggle"]');
      if (chevron && typeof chevron.getBBox === 'function') {
        const box = chevron.getBBox();
        expanded ? chevron.removeAttribute('transform') : chevron.setAttribute('transform', 'rotate(-90 ' + (box.x + box.width / 2) + ' ' + (box.y + box.height / 2) + ')');
      }
      card.setAttribute('data-sibling-expanded', expanded ? 'true' : 'false');
      if (header) {
        header.setAttribute('aria-expanded', expanded ? 'true' : 'false');
        const label = attr(card, 'data-node-label');
        header.setAttribute('aria-label', expanded ? uiText('TopologyCollapseGroup', 'Collapse {label}', { label }) : uiText('TopologyExpandGroup', 'Expand {label}', { label }));
      }
      if (emit) wrapper.dispatchEvent(new CustomEvent('cfx-topology-sibling-toggle', { bubbles: true, composed: true, detail: { id: attr(card, 'data-node-id'), expanded } }));
    };
    wrapper.querySelectorAll('[data-cfx-role="topology-node"][data-sibling-count]').forEach(card => {
      const header = card.querySelector('[data-cfx-role="topology-sibling-header"]');
      if (!header) return;
      header.setAttribute('role', 'button');
      header.setAttribute('tabindex', '0');
      header.style.cursor = 'pointer';
      setSiblingCardExpanded(card, attr(card, 'data-sibling-expanded') !== 'false', false);
      const toggle = event => {
        event.stopPropagation();
        setSiblingCardExpanded(card, attr(card, 'data-sibling-expanded') === 'false', true);
      };
      listen(header, 'click', toggle);
      listen(header, 'keydown', event => {
        if (event.key !== 'Enter' && event.key !== ' ') return;
        event.preventDefault();
        toggle(event);
      });
    });
