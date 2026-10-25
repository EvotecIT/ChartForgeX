// Region presentation bindings share the adapter's mount/dispose lifetime and public event contract.
(() => {
  window[Symbol.for('ChartForgeX.topologyRegions')] = (wrapper, lifetime) => {
    const listen = (target, type, handler) => lifetime.listen(target, type, handler);
    const roots = [...wrapper.querySelectorAll('.cfx-topo')];
    const nodes = [...wrapper.querySelectorAll('[data-cfx-role="topology-node"]')];
    const tip = document.createElement('div');
    tip.className = 'cfx-region-tip'; tip.hidden = true; tip.setAttribute('role', 'tooltip');
    wrapper.append(tip); lifetime.own(() => tip.remove());
    const visible = element => element.getBoundingClientRect().width > 0;
    let current = null;
    const prefixed = (element, prefix) => Object.fromEntries([...element.attributes].filter(attribute => attribute.name.startsWith(prefix))
      .map(attribute => [attribute.name.slice(prefix.length).replace(/-([a-z0-9])/g, (_, letter) => letter.toUpperCase()), attribute.value]));
    const data = element => {
      const kind = element.hasAttribute('data-node-id') ? 'node' : 'edge';
      return { chartId: wrapper.dataset.chartId, kind, id: element.getAttribute(`data-${kind}-id`),
        label: element.getAttribute(`data-${kind}-label`) || '', subtitle: element.dataset.nodeSubtitle || '',
        sourceNodeId: element.dataset.sourceNodeId, targetNodeId: element.dataset.targetNodeId,
        sourceGroupId:element.dataset.sourceGroupId, targetGroupId:element.dataset.targetGroupId,
        groupId:element.dataset.groupId, groupLabel:element.dataset.groupLabel,
        nodeKind:element.dataset.nodeKind, edgeKind:element.dataset.edgeKind, direction:element.dataset.direction,
        accessibleName: element.getAttribute('aria-label') || '', healthRing: kind === 'node',
        status: element.dataset.cfxStatus || '', metadata:prefixed(element,'data-cfx-meta-'), metrics:prefixed(element,'data-cfx-metric-') };
    };
    const emit = (name, element) => wrapper.dispatchEvent(new CustomEvent(name, { bubbles:true, composed:true, detail:data(element) }));
    const clear = () => {
      roots.forEach(root => { root.classList.remove('dimming'); root.querySelectorAll('.lit,.focus').forEach(part => part.classList.remove('lit','focus')); });
      current = null; tip.hidden = true;
    };
    lifetime.own(clear);
    const show = (element, x, y, keyboard = false) => {
      clear(); current = element;
      const root = element.closest('.cfx-topo');
      if (element.hasAttribute('data-node-id')) {
        root.classList.add('dimming'); element.classList.add('lit');
        if (keyboard) element.classList.add('focus');
        const id = element.dataset.nodeId;
        root.querySelectorAll('[data-a],[data-source-node-id]').forEach(edge => {
          if ((edge.dataset.a || edge.dataset.sourceNodeId) === id || (edge.dataset.b || edge.dataset.targetNodeId) === id) {
            edge.classList.add('lit');
            const other = (edge.dataset.a || edge.dataset.sourceNodeId) === id ? (edge.dataset.b || edge.dataset.targetNodeId) : (edge.dataset.a || edge.dataset.sourceNodeId);
            root.querySelectorAll('[data-node-id]').forEach(node => { if (node.dataset.nodeId === other) node.classList.add('lit'); });
          }
        });
      }
      const title = document.createElement('b'); title.textContent = data(element).label;
      const details = document.createElement('small'); details.textContent = element.dataset.regionTip || title.textContent;
      tip.replaceChildren(title, details); tip.hidden = false;
      const bounds = tip.getBoundingClientRect();
      tip.style.left = Math.max(8, Math.min(innerWidth - bounds.width - 8, x + 14)) + 'px';
      tip.style.top = Math.max(8, Math.min(innerHeight - bounds.height - 8, y + 14)) + 'px';
      emit('cfx-topology-hover', element);
    };
    const select = element => {
      wrapper.querySelectorAll('[aria-selected]').forEach(part => part.removeAttribute('aria-selected'));
      const selected = data(element);
      wrapper.querySelectorAll('[data-cfx-role="topology-node"],[data-cfx-role="topology-edge"]').forEach(part => {
        const candidate = data(part);
        if (candidate.kind === selected.kind && candidate.id === selected.id) part.setAttribute('aria-selected','true');
      });
      emit('cfx-topology-select', element);
      emit(element.hasAttribute('data-node-id') ? 'node:select' : 'edge:select', element);
    };
    wrapper.querySelectorAll('[data-cfx-role="topology-node"],[data-cfx-role="topology-edge"]').forEach(element => {
      element.setAttribute('tabindex','-1');
      listen(element,'pointerenter', event => show(element,event.clientX,event.clientY));
      listen(element,'pointerleave',clear);
      listen(element,'click', () => select(element));
      const path = element.querySelector('path');
      if (path && element.hasAttribute('data-edge-id')) {
        const width = Number(path.getAttribute('stroke-width') || 3);
        listen(element,'pointerenter', () => path.setAttribute('stroke-width', String(width + 1)));
        listen(element,'pointerleave', () => path.setAttribute('stroke-width', String(width)));
      }
    });
    const refresh = () => {
      const bands = [...wrapper.querySelectorAll('[data-cfx-region-band]')];
      if (bands.length) {
        const width = wrapper.getBoundingClientRect().width;
        const active = bands.filter(band => Number(band.dataset.cfxRegionBand) <= width + 1).sort((a,b) => Number(b.dataset.cfxRegionBand) - Number(a.dataset.cfxRegionBand))[0] || bands[0];
        bands.forEach(band => band.style.display = width >= 640 && band === active ? 'block' : 'none');
        const mobile = wrapper.querySelector('.cfx-region-mobile');
        if (mobile) mobile.style.display = width < 640 ? 'block' : 'none';
      }
      roots.forEach(root => root.closest('svg').setAttribute('tabindex', visible(root) ? '0' : '-1'));
    };
    const observer = new ResizeObserver(() => { clear(); refresh(); });
    observer.observe(wrapper); lifetime.own(() => observer.disconnect()); refresh();
    roots.forEach(root => {
      const svg = root.closest('svg');
      listen(svg,'focus', event => {
        if (event.target !== svg) return;
        const node = nodes.find(node => root.contains(node));
        if (node) { const bounds = node.getBoundingClientRect(); show(node,bounds.right,bounds.top,true); }
      });
      listen(svg,'keydown', event => {
        if (event.key === 'Escape') { clear(); event.preventDefault(); return; }
        if (event.key === 'Enter' || event.key === ' ') { if (current) { select(current); event.preventDefault(); } return; }
        const vector = {ArrowRight:[1,0],ArrowLeft:[-1,0],ArrowDown:[0,1],ArrowUp:[0,-1]}[event.key];
        if (!vector) return;
        event.preventDefault();
        const candidates = nodes.filter(node => root.contains(node));
        const origin = current && current.hasAttribute('data-node-id') ? current : candidates[0];
        if (!origin) return;
        const x = Number(origin.dataset.nodeX); const y = Number(origin.dataset.nodeY);
        const next = candidates.filter(node => node !== origin).map(node => {
          const dx = Number(node.dataset.nodeX) - x; const dy = Number(node.dataset.nodeY) - y;
          const along = dx * vector[0] + dy * vector[1]; const cross = Math.abs(dx * vector[1] - dy * vector[0]);
          return {node,along,score:Math.hypot(dx,dy) + cross * 2};
        }).filter(candidate => candidate.along > 1).sort((a,b) => a.score - b.score)[0]?.node;
        if (next) { next.focus(); const bounds = next.getBoundingClientRect(); show(next,bounds.right,bounds.top,true); }
      });
      listen(svg,'focusout', event => { if (!svg.contains(event.relatedTarget)) clear(); });
    });
    listen(wrapper,'cfx-topology-clear-selection',clear);
    listen(wrapper,'cfx-topology-set-selection',event => {
      const request = event.detail || {};
      const element = nodes.find(node => visible(node) && node.dataset.nodeId === (request.nodeId || request.id));
      if (element) select(element);
    });
    listen(wrapper,'cfx-topology-update-tones',event => {
      const updates = event.detail?.nodes || [];
      // Updating a tone preserves all prepared coordinates. Membership changes require a host rerender.
      for (const update of updates) nodes.filter(node => node.dataset.nodeId === update.id).forEach(node => {
        const tone = {Healthy:'tone-good',Warning:'tone-warn',Critical:'tone-bad',Unknown:'tone-neutral',Disabled:'tone-neutral'}[update.status];
        if (!tone) return;
        node.classList.remove('tone-good','tone-warn','tone-bad','tone-info','tone-neutral'); node.classList.add(tone);
        node.dataset.cfxStatus = update.status;
        node.dataset.regionTip = [node.dataset.nodeLabel,node.dataset.nodeSubtitle,node.dataset.nodeBadgeMeanings,update.statusText || update.status].filter(Boolean).join(' · ');
        if (node.dataset.regionGeneratedName === 'true') node.setAttribute('aria-label',node.dataset.regionTip);
      });
    });
  };
})();
