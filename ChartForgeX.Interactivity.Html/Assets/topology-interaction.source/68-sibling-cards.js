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
    const setSiblingCardExpanded = (card, expanded, emit) => {
      const layout = siblingCardLayout(card);
      const rows = card.querySelector('[data-cfx-role="topology-sibling-rows"]');
      const header = card.querySelector('[data-cfx-role="topology-sibling-header"]');
      if (rows) { rows.style.display = expanded ? '' : 'none'; if (expanded) rows.removeAttribute('opacity'); }
      // A card that started collapsed opens over its neighbours, so it is drawn last while open.
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
