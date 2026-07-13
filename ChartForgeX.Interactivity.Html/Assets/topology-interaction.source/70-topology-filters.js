    const topologyFilterSearchText = element => [
      attr(element, 'data-node-id'), attr(element, 'data-node-label'), attr(element, 'data-node-kind'), attr(element, 'data-group-id'),
      attr(element, 'data-edge-id'), attr(element, 'data-edge-label'), attr(element, 'data-edge-secondary-label'), attr(element, 'data-edge-tertiary-label'), attr(element, 'data-edge-kind'), attr(element, 'data-source-node-id'), attr(element, 'data-target-node-id'),
      attr(element, 'data-group-label'), attr(element, 'data-group-symbol'), attr(element, 'data-cfx-status'), element.textContent || ''
    ].join(' ').toLowerCase();
    const topologyFilterEscape = value => window.CSS && CSS.escape ? CSS.escape(value) : String(value).replace(/["\\]/g, '\\$&');
    let topologyFilterState = { query: '', status: '', group: '', kind: '', edges: true, labels: true, groups: true };
    const setTopologyFilterHidden = (selector, predicate) => {
      wrapper.querySelectorAll(selector).forEach(element => element.classList.toggle('cfx-topology-html-filter-hidden', predicate(element)));
    };
    const setTopologyFilterAttributes = detail => {
      wrapper.setAttribute('data-cfx-filter-query', detail.query || '');
      wrapper.setAttribute('data-cfx-filter-status', detail.status || '');
      wrapper.setAttribute('data-cfx-filter-group', detail.group || '');
      wrapper.setAttribute('data-cfx-filter-kind', detail.kind || '');
      wrapper.setAttribute('data-cfx-filter-active', detail.active ? 'true' : 'false');
      wrapper.setAttribute('data-cfx-visible-nodes', String(detail.nodes));
      wrapper.setAttribute('data-cfx-visible-edges', String(detail.edges));
    };
    const applyTopologyFilter = state => {
      topologyFilterState = Object.assign({}, topologyFilterState, state || {});
      const query = String(topologyFilterState.query || '').trim().toLowerCase();
      const status = String(topologyFilterState.status || '').trim();
      const group = String(topologyFilterState.group || '').trim();
      const kind = String(topologyFilterState.kind || '').trim();
      const showEdges = topologyFilterState.edges !== false;
      const showLabels = topologyFilterState.labels !== false;
      const showGroups = topologyFilterState.groups !== false;
      const active = !!(query || status || group || kind || !showEdges || !showLabels || !showGroups);
      const visibleNodes = new Set();
      const queryNodes = new Set();
      const statusNodes = new Set();
      if (query) {
        wrapper.querySelectorAll('[data-cfx-role="topology-edge"]').forEach(edge => {
          if (!topologyFilterSearchText(edge).includes(query)) return;
          queryNodes.add(attr(edge, 'data-source-node-id'));
          queryNodes.add(attr(edge, 'data-target-node-id'));
        });
        wrapper.querySelectorAll('[data-cfx-role="topology-group"]').forEach(groupElement => {
          if (!topologyFilterSearchText(groupElement).includes(query)) return;
          const groupId = attr(groupElement, 'data-group-id');
          wrapper.querySelectorAll('[data-cfx-role="topology-node"][data-group-id="' + topologyFilterEscape(groupId) + '"]').forEach(node => queryNodes.add(attr(node, 'data-node-id')));
        });
      }
      if (status) {
        wrapper.querySelectorAll('[data-cfx-role="topology-edge"]').forEach(edge => {
          if (attr(edge, 'data-cfx-status') !== status) return;
          statusNodes.add(attr(edge, 'data-source-node-id'));
          statusNodes.add(attr(edge, 'data-target-node-id'));
        });
      }
      wrapper.querySelectorAll('[data-cfx-role="topology-node"]').forEach(node => {
        const nodeId = attr(node, 'data-node-id');
        const groupOk = !group || attr(node, 'data-group-id') === group;
        const statusOk = !status || attr(node, 'data-cfx-status') === status || statusNodes.has(nodeId);
        const kindOk = !kind || attr(node, 'data-node-kind') === kind;
        const queryOk = !query || topologyFilterSearchText(node).includes(query) || queryNodes.has(nodeId);
        const visible = groupOk && statusOk && kindOk && queryOk;
        node.classList.toggle('cfx-topology-html-filter-hidden', !visible);
        wrapper.querySelectorAll('[data-cfx-role="topology-node-status"][data-node-id="' + topologyFilterEscape(nodeId) + '"]').forEach(statusBadge => statusBadge.classList.toggle('cfx-topology-html-filter-hidden', !visible));
        if (visible) visibleNodes.add(nodeId);
      });
      const nodeById = id => wrapper.querySelector('[data-cfx-role="topology-node"][data-node-id="' + topologyFilterEscape(id) + '"]');
      setTopologyFilterHidden('[data-cfx-role="topology-edge"]', edge => {
        const sourceId = attr(edge, 'data-source-node-id');
        const targetId = attr(edge, 'data-target-node-id');
        const source = nodeById(sourceId);
        const target = nodeById(targetId);
        const endpointsVisible = visibleNodes.has(sourceId) && visibleNodes.has(targetId);
        const edgeQueryOk = !query || topologyFilterSearchText(edge).includes(query) || (source && topologyFilterSearchText(source).includes(query)) || (target && topologyFilterSearchText(target).includes(query));
        const edgeStatusOk = !status || attr(edge, 'data-cfx-status') === status || (source && attr(source, 'data-cfx-status') === status) || (target && attr(target, 'data-cfx-status') === status);
        return !showEdges || !endpointsVisible || !edgeQueryOk || !edgeStatusOk;
      });
      setTopologyFilterHidden('[data-cfx-role="topology-edge-label"]', label => !showLabels || !showEdges || !wrapper.querySelector('[data-cfx-role="topology-edge"][data-edge-id="' + topologyFilterEscape(attr(label, 'data-edge-id')) + '"]:not(.cfx-topology-html-filter-hidden):not(.cfx-topology-html-force-hidden)'));
      setTopologyFilterHidden('[data-cfx-role="topology-group"]', groupElement => {
        const groupId = attr(groupElement, 'data-group-id');
        const hasVisibleNodes = !!wrapper.querySelector('[data-cfx-role="topology-node"][data-group-id="' + topologyFilterEscape(groupId) + '"]:not(.cfx-topology-html-filter-hidden):not(.cfx-topology-html-force-hidden)');
        return !showGroups || !hasVisibleNodes || (group && groupId !== group);
      });
      const detail = {
        chartId: attr(wrapper, 'data-chart-id'),
        nodes: visibleNodes.size,
        edges: wrapper.querySelectorAll('[data-cfx-role="topology-edge"]:not(.cfx-topology-html-filter-hidden):not(.cfx-topology-html-force-hidden)').length,
        query: topologyFilterState.query || '',
        status,
        group,
        kind,
        active
      };
      setTopologyFilterAttributes(detail);
      wrapper.dispatchEvent(new CustomEvent('cfx-topology-filter', { bubbles: true, detail }));
      if (active && visibleNodes.size) fitVisibleTopology(false);
      restoreForceFocusLabels();
    };
    const clearTopologyFilter = () => applyTopologyFilter({ query: '', status: '', group: '', kind: '', edges: true, labels: true, groups: true });
