
  const capturedHierarchy = root => capturedCartesianMetadata(root).hierarchy || null;
  const hierarchyConnected = root => hasFeature(root, 'HierarchyNavigation') && !!capturedHierarchy(root) && !!nativePreparationBinding(root, 'hierarchy');
  const hierarchyViewChanged = root => {
    const view = capturedHierarchy(root);
    return view?.kind === 'topology' ? !!(view.collapsedNodeIds.length || view.nodeOrder.length || view.layoutDirection !== null) : view?.rootId != null;
  };
  const hierarchySnapshot = root => {
    const view = capturedHierarchy(root);
    return view?.kind === 'topology' ? { kind: view.kind, sourceId: view.sourceId, collapsedNodeIds: [...view.collapsedNodeIds], nodeOrder: [...view.nodeOrder], layoutDirection: view.layoutDirection }
      : view ? { rootId: view.rootId, kind: view.kind, seriesKey: view.seriesKey, path: copyHostValue(view.path) } : null;
  };
  const syncHierarchyNavigation = root => {
    const panel = root.querySelector('[data-cfx-hierarchy-navigation]');
    if (!panel) return;
    const view = capturedHierarchy(root), connected = hierarchyConnected(root);
    const pending = !!chartLifetimes.get(root)?.preparationPending || nativePreparationBackpressureBusy(root);
    panel.querySelectorAll('[data-cfx-hierarchy-control]').forEach(button => {
      button.disabled = !connected || pending || (view?.kind === 'topology' ? button.dataset.cfxHierarchyControl === 'home' && !hierarchyViewChanged(root) : view?.rootId == null);
    });
    const status = panel.querySelector('[data-cfx-hierarchy-status]');
    if (status) status.textContent = !connected ? uiText(root, 'HierarchyDisconnected', 'Connect native preparation to navigate the hierarchy.')
      : pending ? uiText(root, 'HierarchyUpdating', 'Updating hierarchy…') : root._cfxHierarchyError || '';
  };
  const hierarchyHasSource = (root, id) => interactiveTargets(root, '[data-cfx-target-kind="node"][data-cfx-target-id]')
    .some(node => node.dataset.cfxTargetId === id) || Array.from(root.querySelectorAll('.cfx-stage [data-cfx-node]')).some(node => node.dataset.cfxNode === id);
  const requestNativeHierarchy = (root, view, sync = true, accepted) => {
    const current = capturedHierarchy(root);
    if (current?.kind === 'topology') return requestNativeTopologyHierarchy(root, view, sync, accepted);
    if (!hierarchyConnected(root) || !current || view !== null && (!view || typeof view !== 'object'
      || Object.keys(view).some(key => key !== 'rootId') || typeof view.rootId !== 'string' || !view.rootId.trim() || !hierarchyHasSource(root, view.rootId))) return false;
    const rootId = view === null ? null : view.rootId;
    root._cfxHierarchyError = '';
    const visibility = nativeLegendConnected(root) && capturedNativeVisibility(root);
    const sourceVisibility = visibility?.layout === 'reflow' && visibility.hierarchyRootId !== undefined
      ? rootId === null ? nativeVisibilityResetDescriptor(root) : Object.freeze({ layout: 'reflow', hierarchyRootId: rootId,
          hiddenTargets: visibility.hiddenTargets, hiddenSeries: visibility.hiddenSeries, labelLayering: visibility.labelLayering }) : null;
    return requestNativePreparation(root, 'hierarchy', { kind: rootId === null ? 'reset' : 'root', rootId,
      ...(sourceVisibility ? { sourceVisibility } : {}) }, {
      eventPrefix: 'cfxhierarchy', resetView: rootId === null, accepted,
      started: () => syncHierarchyNavigation(root), cancelled: () => syncHierarchyNavigation(root),
      validate: drawing => {
        const reply = capturedHierarchy(drawing);
        if (!reply || reply.kind !== current.kind || reply.seriesKey !== current.seriesKey || reply.rootId !== rootId)
          throw new TypeError('Hierarchy reply: family/series/requested-root mismatch.');
      },
      beforeCommit: () => rootId === null ? selectionSignature(root) : '',
      committed: (_, previousSelection) => {
        syncHierarchyNavigation(root); syncResetControl(root);
        if (rootId === null) { emitHostEvent(root, 'cfxreset', {}); publishResetSelection(root, previousSelection); }
        publishInteractionState(root, 'hierarchy', sync);
      },
      failed: () => { root._cfxHierarchyError = uiText(root, 'HierarchyFailed', 'The hierarchy view could not be updated.'); syncHierarchyNavigation(root); }
    });
  };
  const activateHierarchyTarget = (root, node) => hierarchyConnected(root) && node.dataset.cfxLeaf === 'false'
    && (capturedHierarchy(root)?.kind === 'topology' ? toggleTopologyHierarchy(root, node.dataset.cfxTargetId)
      : node.dataset.cfxTargetId !== capturedHierarchy(root).rootId && requestNativeHierarchy(root, { rootId: node.dataset.cfxTargetId }));
  const bindHierarchyNavigation = root => {
    syncHierarchyNavigation(root);
    root.querySelectorAll('[data-cfx-hierarchy-control]').forEach(button => listenChart(root, button, 'click', () => {
      if (button.disabled) return;
      if (capturedHierarchy(root)?.kind === 'topology') {
        if (button.dataset.cfxHierarchyControl === 'toggle') toggleTopologyHierarchy(root, button.dataset.cfxHierarchyNode);
        else requestNativeHierarchy(root, null);
        return;
      }
      const path = capturedHierarchy(root)?.path || [];
      const parent = path.length > 1 ? path[path.length - 2].id : null;
      requestNativeHierarchy(root, button.dataset.cfxHierarchyControl === 'up' && parent !== null ? { rootId: parent } : null);
    }));
  };

  const topologyHierarchyView = (current, view) => {
    if (view === null) return { collapsedNodeIds: [], nodeOrder: [], layoutDirection: null };
    if (!view || typeof view !== 'object' || Array.isArray(view) || Object.keys(view).some(key => !['collapsedNodeIds', 'nodeOrder', 'layoutDirection'].includes(key))) return null;
    const known = new Set(current.nodes.filter(node => node.hierarchy).map(node => node.id));
    const ids = value => value === undefined ? [] : Array.isArray(value) && value.every(id => typeof id === 'string' && known.has(id)) && new Set(value).size === value.length ? [...value] : null;
    const collapsedNodeIds = ids(view.collapsedNodeIds), nodeOrder = ids(view.nodeOrder), layoutDirection = view.layoutDirection ?? null;
    return collapsedNodeIds && nodeOrder && (layoutDirection === null || ['TopToBottom', 'BottomToTop', 'LeftToRight', 'RightToLeft'].includes(layoutDirection))
      ? { collapsedNodeIds, nodeOrder, layoutDirection } : null;
  };
  const toggleTopologyHierarchy = (root, id) => {
    const current = capturedHierarchy(root); if (!current?.nodes.some(node => node.hierarchy && node.id === id)) return false;
    const collapsedNodeIds = current.collapsedNodeIds.includes(id) ? current.collapsedNodeIds.filter(value => value !== id) : [...current.collapsedNodeIds, id];
    return requestNativeHierarchy(root, { collapsedNodeIds, nodeOrder: current.nodeOrder, layoutDirection: current.layoutDirection });
  };
  const requestNativeTopologyHierarchy = (root, view, sync, accepted) => {
    const current = capturedHierarchy(root); if (!hierarchyConnected(root)) return false;
    const request = topologyHierarchyView(current, view); if (!request) return false;
    const reset = !request.collapsedNodeIds.length && !request.nodeOrder.length && request.layoutDirection === null;
    root._cfxHierarchyError = '';
    return requestNativePreparation(root, 'hierarchy', { kind: reset ? 'reset' : 'topology', ...request }, {
      eventPrefix: 'cfxhierarchy', resetView: reset, accepted,
      started: () => syncHierarchyNavigation(root), cancelled: () => syncHierarchyNavigation(root),
      validate: drawing => {
        const reply = capturedHierarchy(drawing);
        if (!reply || reply.kind !== 'topology' || reply.sourceId !== current.sourceId || JSON.stringify(reply.nodes) !== JSON.stringify(current.nodes)
          || JSON.stringify(reply.collapsedNodeIds) !== JSON.stringify(request.collapsedNodeIds) || JSON.stringify(reply.nodeOrder) !== JSON.stringify(request.nodeOrder)
          || reply.layoutDirection !== request.layoutDirection) throw new TypeError('Hierarchy reply: source/view mismatch.');
      },
      beforeCommit: () => reset ? selectionSignature(root) : '',
      committed: (_, previousSelection) => {
        syncHierarchyNavigation(root); syncResetControl(root);
        if (reset) { emitHostEvent(root, 'cfxreset', {}); publishResetSelection(root, previousSelection); }
        publishInteractionState(root, 'hierarchy', sync);
      },
      failed: () => { root._cfxHierarchyError = uiText(root, 'HierarchyFailed', 'The hierarchy view could not be updated.'); syncHierarchyNavigation(root); }
    });
  };
