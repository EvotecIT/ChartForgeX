    // Each filter owns its hidden class. Report their intersection without
    // copying one filter's transient visibility into the other's state.
    const publishTopologyFilterSummary = () => {
      const force = forceGraphControls && forceGraphPanel ? forceGraphState() : null;
      const active = state => !!(state && (String(state.query || '').trim() || state.status || state.group || state.kind || state.edges === false || state.labels === false || state.groups === false));
      const count = role => Array.from(wrapper.querySelectorAll('[data-cfx-role="' + role + '"]')).filter(isViewportVisible).length;
      const detail = {
        chartId: attr(wrapper, 'data-chart-id'), nodes: count('topology-node'), edges: count('topology-edge'),
        query: topologyFilterState.query || force?.query || '',
        status: topologyFilterState.status || force?.status || '',
        group: topologyFilterState.group || force?.group || '',
        kind: topologyFilterState.kind || '',
        active: active(topologyFilterState) || active(force),
        filters: { topology: { ...topologyFilterState }, force: force ? { ...force } : null }
      };
      setTopologyFilterAttributes(detail);
      const summary = forceGraphPanel?.querySelector('[data-cfx-force-summary]');
      if (summary) summary.textContent = detail.nodes + ' nodes / ' + detail.edges + ' edges visible';
      wrapper.dispatchEvent(new CustomEvent('cfx-topology-filter', { bubbles: true, detail }));
      return detail;
    };
