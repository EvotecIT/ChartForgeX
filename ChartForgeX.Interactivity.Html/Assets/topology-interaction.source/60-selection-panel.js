    const edgeIdentity = edge => ({
      id: attr(edge, 'data-edge-id'),
      kind: attr(edge, 'data-edge-kind'),
      status: attr(edge, 'data-cfx-status'),
      sourceNodeId: attr(edge, 'data-source-node-id'),
      targetNodeId: attr(edge, 'data-target-node-id'),
      sourceGroupId: attr(edge, 'data-source-group-id'),
      targetGroupId: attr(edge, 'data-target-group-id')
    });
    const related = detail => {
      if (detail.kind === 'node') {
        const edges = Array.from(wrapper.querySelectorAll('[data-cfx-role="topology-edge"]'))
          .filter(edge => attr(edge, 'data-source-node-id') === detail.id || attr(edge, 'data-target-node-id') === detail.id);
        return {
          nodeIds: unique(edges.flatMap(edge => [attr(edge, 'data-source-node-id'), attr(edge, 'data-target-node-id')]).filter(id => id !== detail.id)),
          edgeIds: edges.map(edge => attr(edge, 'data-edge-id')),
          groupIds: unique([detail.groupId].concat(edges.flatMap(edge => [attr(edge, 'data-source-group-id'), attr(edge, 'data-target-group-id')]))),
          edges: edges.map(edgeIdentity)
        };
      }
      if (detail.kind === 'edge') {
        return {
          nodeIds: unique([detail.sourceNodeId, detail.targetNodeId]),
          edgeIds: [detail.id],
          groupIds: unique([detail.sourceGroupId, detail.targetGroupId]),
          edges: [edgeIdentity(detail.element)]
        };
      }
      if (detail.kind === 'group') {
        const nodes = Array.from(wrapper.querySelectorAll('[data-cfx-role="topology-node"]'))
          .filter(node => attr(node, 'data-group-id') === detail.id);
        const edges = Array.from(wrapper.querySelectorAll('[data-cfx-role="topology-edge"]'))
          .filter(edge => attr(edge, 'data-source-group-id') === detail.id || attr(edge, 'data-target-group-id') === detail.id);
        return {
          nodeIds: nodes.map(node => attr(node, 'data-node-id')),
          edgeIds: edges.map(edge => attr(edge, 'data-edge-id')),
          groupIds: unique(edges.flatMap(edge => [attr(edge, 'data-source-group-id'), attr(edge, 'data-target-group-id')]).filter(id => id !== detail.id)),
          edges: edges.map(edgeIdentity)
        };
      }
      return { nodeIds: [], edgeIds: [], groupIds: [], edges: [] };
    };
    const statusColor = status => {
      const key = String(status || '').toLowerCase();
      if (key === 'healthy') return 'var(--cfx-topology-healthy,#16a34a)';
      if (key === 'warning') return 'var(--cfx-topology-warning,#f97316)';
      if (key === 'critical') return 'var(--cfx-topology-critical,#ef4444)';
      if (key === 'disabled') return 'var(--cfx-topology-disabled,#94a3b8)';
      return 'var(--cfx-topology-unknown,#64748b)';
    };
    const panelRowClass = () => {
      if (!selectionPanel) return 'cfx-topology-selection-panel__row';
      const base = (selectionPanel.getAttribute('class') || 'cfx-topology-selection-panel').split(/\s+/)[0] || 'cfx-topology-selection-panel';
      return base + '__row';
    };
    const addSelectionRow = (container, label, value) => {
      if (!container || value === undefined || value === null || value === '') return;
      const row = document.createElement('div');
      row.className = panelRowClass();
      const left = document.createElement('span');
      const right = document.createElement('span');
      left.textContent = label;
      right.textContent = String(value);
      row.append(left, right);
      container.appendChild(row);
    };
    const setSelectionFacts = (container, rows) => {
      if (!container) return;
      container.replaceChildren();
      rows.forEach(row => {
        if (!row || row[1] === undefined || row[1] === null || row[1] === '') return;
        const dt = document.createElement('dt');
        const dd = document.createElement('dd');
        dt.textContent = row[0];
        dd.textContent = String(row[1]);
        container.append(dt, dd);
      });
    };
    const renderSelectionPanel = detail => {
      if (!selectionPanel) return;
      const title = selectionPanel.querySelector('[data-cfx-selection-title]');
      const kind = selectionPanel.querySelector('[data-cfx-selection-kind]');
      const status = selectionPanel.querySelector('[data-cfx-selection-status]');
      const facts = selectionPanel.querySelector('[data-cfx-selection-facts]');
      const meta = selectionPanel.querySelector('[data-cfx-selection-meta]');
      const relatedPanel = selectionPanel.querySelector('[data-cfx-selection-related]');
      if (!detail) {
        selectionPanel.hidden = true;
        selectionPanel.style.removeProperty('--cfx-topology-selection-status-color');
        if (title) title.textContent = 'No item selected';
        if (kind) kind.textContent = 'Selection';
        if (status) status.textContent = '';
        if (facts) facts.replaceChildren();
        if (meta) meta.replaceChildren();
        if (relatedPanel) relatedPanel.replaceChildren();
        return;
      }

      selectionPanel.hidden = false;
      selectionPanel.style.setProperty('--cfx-topology-selection-status-color', statusColor(detail.status));
      const displayTitle = detail.kind === 'node'
        ? (detail.metadata && detail.metadata.name) || (detail.element ? attr(detail.element, 'data-node-label') : '') || detail.id
        : detail.kind === 'edge'
          ? (detail.element ? attr(detail.element, 'data-edge-label') : '') || detail.id
          : (detail.element ? attr(detail.element, 'data-group-label') : '') || detail.id;
      if (title) title.textContent = displayTitle || detail.id || 'Selection';
      if (kind) kind.textContent = detail.kind === 'node' ? (detail.nodeKind || 'Node') : detail.kind === 'edge' ? (detail.edgeKind || 'Edge') : 'Group';
      if (status) status.textContent = detail.status || '';
      setSelectionFacts(facts, [
        ['ID', detail.id],
        ['Status', detail.status],
        ['Group', detail.groupId || detail.sourceGroupId || detail.targetGroupId],
        ['Related nodes', (detail.related && detail.related.nodeIds || []).length],
        ['Related edges', (detail.related && detail.related.edgeIds || []).length]
      ]);
      if (meta) {
        meta.replaceChildren();
        Object.entries(detail.metadata || {}).slice(0, 6).forEach(([key, value]) => addSelectionRow(meta, key, value));
        Object.entries(detail.metrics || {}).slice(0, 4).forEach(([key, value]) => addSelectionRow(meta, key, value));
      }
      if (relatedPanel) {
        relatedPanel.replaceChildren();
        (detail.related && detail.related.edges || []).slice(0, 5).forEach(edge => {
          const label = edge.kind || 'Edge';
          const value = [edge.sourceNodeId, edge.targetNodeId].filter(Boolean).join(' -> ');
          addSelectionRow(relatedPanel, label, value || edge.id);
        });
      }
    };
    const clear = () => {
      wrapper.removeAttribute('data-cfx-selection-kind');
      wrapper.removeAttribute('data-cfx-selection-id');
      wrapper.removeAttribute('data-cfx-selection-status');
      wrapper.querySelectorAll('.cfx-topology-html-selected,.cfx-topology-html-muted,.cfx-topology-html-related').forEach(item => {
        item.classList.remove('cfx-topology-html-selected', 'cfx-topology-html-muted', 'cfx-topology-html-related');
        item.removeAttribute('aria-selected');
      });
      clearForceFocusLabels();
      renderSelectionPanel(null);
    };
    const clearHover = () => {
      wrapper.removeAttribute('data-cfx-hover-kind');
      wrapper.removeAttribute('data-cfx-hover-id');
      wrapper.querySelectorAll('.cfx-topology-html-hovered,.cfx-topology-html-hover-muted,.cfx-topology-html-hover-related').forEach(item => {
        item.classList.remove('cfx-topology-html-hovered', 'cfx-topology-html-hover-muted', 'cfx-topology-html-hover-related');
      });
      restoreForceFocusLabels();
    };
    const identity = element => {
      const role = element.getAttribute('data-cfx-role') || '';
      const base = {
        role,
        elementId: attr(element, 'id'),
        status: attr(element, 'data-cfx-status'),
        selected: attr(element, 'data-cfx-selected') === 'true',
        metadata: collectPrefixed(element, 'data-cfx-meta-'),
        metrics: collectPrefixed(element, 'data-cfx-metric-'),
        element
      };
      if (role === 'topology-node') {
        return {
          ...base,
          kind: 'node',
          id: attr(element, 'data-node-id'),
          label: attr(element, 'data-node-label'),
          nodeKind: attr(element, 'data-node-kind'),
          groupId: attr(element, 'data-group-id'),
          displayMode: attr(element, 'data-node-display-mode'),
          badge: attr(element, 'data-node-badge'),
          color: attr(element, 'data-node-color'),
          iconId: attr(element, 'data-node-icon-id'),
          iconPack: attr(element, 'data-node-icon-pack'),
          iconLabel: attr(element, 'data-node-icon-label'),
          iconShape: attr(element, 'data-node-icon-shape'),
          iconArtwork: attr(element, 'data-node-icon-artwork'),
          artworkSource: attr(element, 'data-node-artwork-source'),
          longitude: attr(element, 'data-node-longitude'),
          latitude: attr(element, 'data-node-latitude'),
          geoVisible: attr(element, 'data-node-geo-visible')
        };
      }
      if (role === 'topology-edge') {
        return {
          ...base,
          kind: 'edge',
          id: attr(element, 'data-edge-id'),
          label: attr(element, 'data-edge-label'),
          edgeKind: attr(element, 'data-edge-kind'),
          sourceNodeId: attr(element, 'data-source-node-id'),
          targetNodeId: attr(element, 'data-target-node-id'),
          sourceGroupId: attr(element, 'data-source-group-id'),
          targetGroupId: attr(element, 'data-target-group-id'),
          sourcePort: attr(element, 'data-source-port'),
          targetPort: attr(element, 'data-target-port'),
          routeLane: attr(element, 'data-route-lane'),
          layoutInference: attr(element, 'data-edge-layout-inference'),
          muted: attr(element, 'data-edge-muted') === 'true',
          lineStyle: attr(element, 'data-edge-line-style'),
          route: {
            strategy: attr(element, 'data-route-strategy'),
            curve: attr(element, 'data-route-curve'),
            controlX: attr(element, 'data-route-control-x'),
            controlY: attr(element, 'data-route-control-y'),
            corridor: attr(element, 'data-route-corridor'),
            candidateCount: attr(element, 'data-route-candidate-count'),
            fallbackReason: attr(element, 'data-route-fallback-reason'),
            segmentCount: attr(element, 'data-route-segment-count'),
            obstacleCount: attr(element, 'data-route-obstacle-count'),
            obstacleHits: attr(element, 'data-route-obstacle-hits'),
            labelObstacleHits: attr(element, 'data-route-label-obstacle-hits'),
            overlapScore: attr(element, 'data-route-overlap-score'),
            offset: attr(element, 'data-route-offset'),
            waypointCount: attr(element, 'data-waypoint-count')
          }
        };
      }
      if (role === 'topology-group') {
        return {
          ...base,
          kind: 'group',
          id: attr(element, 'data-group-id'),
          label: attr(element, 'data-group-label'),
          symbol: attr(element, 'data-group-symbol'),
          color: attr(element, 'data-group-color'),
          iconId: attr(element, 'data-group-icon-id'),
          iconPack: attr(element, 'data-group-icon-pack'),
          iconLabel: attr(element, 'data-group-icon-label'),
          iconShape: attr(element, 'data-group-icon-shape'),
          iconArtwork: attr(element, 'data-group-icon-artwork'),
          longitude: attr(element, 'data-group-longitude'),
          latitude: attr(element, 'data-group-latitude'),
          geoVisible: attr(element, 'data-group-geo-visible'),
          layoutPolicy: attr(element, 'data-group-layout-policy'),
          appliedLayoutPolicy: attr(element, 'data-group-applied-layout-policy'),
          callout: {
            visualRole: attr(element, 'data-cfx-visual-role'),
            placement: attr(element, 'data-callout-placement'),
            anchorX: attr(element, 'data-callout-anchor-x'),
            anchorY: attr(element, 'data-callout-anchor-y'),
            nodeCount: attr(element, 'data-callout-node-count'),
            healthyCount: attr(element, 'data-callout-healthy-count'),
            warningCount: attr(element, 'data-callout-warning-count'),
            criticalCount: attr(element, 'data-callout-critical-count'),
            unknownCount: attr(element, 'data-callout-unknown-count'),
            disabledCount: attr(element, 'data-callout-disabled-count')
          }
        };
      }
      return { ...base, kind: 'unknown', id: '' };
    };
    const findSelection = request => {
      if (!request || !request.kind || !request.id) return null;
      return Array.from(wrapper.querySelectorAll(selectables)).find(element => {
        const detail = identity(element);
        return detail.kind === request.kind && detail.id === request.id;
      }) || null;
    };
    const elementKey = element => {
      const detail = identity(element);
      return detail.kind + ':' + detail.id;
    };
    const publicDetail = element => {
      const detail = identity(element);
      detail.related = related(detail);
      delete detail.element;
      return detail;
    };
    const relatedElements = detail => {
      const items = [];
      const seen = new Set();
      const add = (kind, id) => {
        if (!id) return;
        const key = kind + ':' + id;
        if (seen.has(key) || (detail.kind === kind && detail.id === id)) return;
        const element = findSelection({ kind, id });
        if (!element) return;
        seen.add(key);
        items.push(element);
      };
      (detail.related.nodeIds || []).forEach(id => add('node', id));
      (detail.related.edgeIds || []).forEach(id => add('edge', id));
      (detail.related.groupIds || []).forEach(id => add('group', id));
      return items;
    };
    const focusRelated = (element, offset) => {
      const detail = identity(element);
      detail.related = related(detail);
      const candidates = relatedElements(detail);
      if (!candidates.length) return false;
      const sourceKey = detail.kind + ':' + detail.id;
      const previousTarget = wrapper.getAttribute('data-cfx-navigation-source') === sourceKey ? wrapper.getAttribute('data-cfx-navigation-target') || '' : '';
      const previousIndex = candidates.findIndex(candidate => elementKey(candidate) === previousTarget);
      const nextIndex = previousIndex >= 0 ? (previousIndex + offset + candidates.length) % candidates.length : (offset > 0 ? 0 : candidates.length - 1);
      const target = candidates[nextIndex];
      const targetKey = elementKey(target);
      wrapper.setAttribute('data-cfx-navigation-source', sourceKey);
      wrapper.setAttribute('data-cfx-navigation-target', targetKey);
      if (target.focus) target.focus({ preventScroll: true });
      wrapper.dispatchEvent(new CustomEvent('cfx-topology-navigate', { bubbles: true, detail: { chartId: attr(wrapper, 'data-chart-id'), from: publicDetail(element), to: publicDetail(target) } }));
      return true;
    };
    const applyRelatedClasses = (detail, classPrefix, selfClass) => {
      const relatedEdges = new Set(detail.related.edgeIds || []);
      const relatedNodes = new Set(detail.related.nodeIds || []);
      const relatedGroups = new Set(detail.related.groupIds || []);
      const relatedClass = classPrefix + 'related';
      const mutedClass = classPrefix + 'muted';
      wrapper.querySelectorAll('[data-cfx-role="topology-edge"]').forEach(edge => {
        const isSelf = detail.kind === 'edge' && attr(edge, 'data-edge-id') === detail.id;
        const isRelated = isSelf || relatedEdges.has(attr(edge, 'data-edge-id'));
        edge.classList.toggle(relatedClass, isRelated);
        edge.classList.toggle(mutedClass, !isRelated);
      });
      wrapper.querySelectorAll('[data-cfx-role="topology-edge-label"]').forEach(label => {
        const isSelf = detail.kind === 'edge' && attr(label, 'data-edge-id') === detail.id;
        const isRelated = isSelf || relatedEdges.has(attr(label, 'data-edge-id'));
        label.classList.toggle(relatedClass, isRelated);
        label.classList.toggle(mutedClass, !isRelated);
      });
      wrapper.querySelectorAll('[data-cfx-role="topology-node"]').forEach(node => {
        const isSelf = detail.kind === 'node' && attr(node, 'data-node-id') === detail.id;
        const isRelated = isSelf || relatedNodes.has(attr(node, 'data-node-id'));
        node.classList.toggle(relatedClass, isRelated);
        node.classList.toggle(mutedClass, !isRelated && detail.kind !== 'group');
      });
      wrapper.querySelectorAll('[data-cfx-role="topology-group"]').forEach(group => {
        const isSelf = detail.kind === 'group' && attr(group, 'data-group-id') === detail.id;
        const isRelated = isSelf || relatedGroups.has(attr(group, 'data-group-id'));
        group.classList.toggle(relatedClass, isRelated);
      });
      if (detail.element && selfClass) detail.element.classList.add(selfClass);
    };
    const hover = element => {
      const detail = identity(element);
      detail.related = related(detail);
      clearHover();
      wrapper.setAttribute('data-cfx-hover-kind', detail.kind);
      wrapper.setAttribute('data-cfx-hover-id', detail.id);
      applyRelatedClasses(detail, 'cfx-topology-html-hover-', 'cfx-topology-html-hovered');
      applyForceFocusLabels(detail);
      delete detail.element;
      wrapper.dispatchEvent(new CustomEvent('cfx-topology-hover', { bubbles: true, detail }));
    };
    const select = (element, emit = true, sync = true) => {
      const detail = identity(element);
      detail.related = related(detail);
      clear();
      wrapper.setAttribute('data-cfx-selection-kind', detail.kind);
      wrapper.setAttribute('data-cfx-selection-id', detail.id);
      wrapper.setAttribute('data-cfx-selection-status', detail.status);
      element.classList.add('cfx-topology-html-selected');
      element.setAttribute('aria-selected', 'true');
      applyRelatedClasses({ ...detail, element }, 'cfx-topology-html-', '');
      applyForceFocusLabels(detail);
      renderSelectionPanel({ ...detail, element });
      delete detail.element;
      if (emit) wrapper.dispatchEvent(new CustomEvent('cfx-topology-select', { bubbles: true, detail }));
      if (sync) emitSync('selection', { selection: { kind: detail.kind, id: detail.id, status: detail.status } });
    };
    const hydrateSelected = element => {
      const detail = identity(element);
      detail.related = related(detail);
      wrapper.setAttribute('data-cfx-selection-kind', detail.kind);
      wrapper.setAttribute('data-cfx-selection-id', detail.id);
      wrapper.setAttribute('data-cfx-selection-status', detail.status);
      element.setAttribute('aria-selected', 'true');
      renderSelectionPanel({ ...detail, element });
      delete detail.element;
    };
