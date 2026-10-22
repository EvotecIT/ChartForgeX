  // Native area-chart views share the existing managed preparation transaction.
  const capturedHierarchy = root => capturedCartesianMetadata(root).hierarchy || null;
  const hierarchyConnected = root => hasFeature(root, 'HierarchyNavigation') && !!capturedHierarchy(root) && !!nativePreparationBinding(root, 'hierarchy');
  const hierarchyViewChanged = root => capturedHierarchy(root)?.rootId != null;
  const hierarchySnapshot = root => {
    const view = capturedHierarchy(root);
    return view ? { rootId: view.rootId, kind: view.kind, seriesKey: view.seriesKey, path: copyHostValue(view.path) } : null;
  };
  const syncHierarchyNavigation = root => {
    const panel = root.querySelector('[data-cfx-hierarchy-navigation]');
    if (!panel) return;
    const view = capturedHierarchy(root), connected = hierarchyConnected(root);
    const pending = !!chartLifetimes.get(root)?.preparationPending;
    panel.querySelectorAll('[data-cfx-hierarchy-control]').forEach(button => { button.disabled = !connected || pending || view?.rootId == null; });
    const status = panel.querySelector('[data-cfx-hierarchy-status]');
    if (status) status.textContent = !connected ? uiText(root, 'HierarchyDisconnected', 'Connect native preparation to navigate the hierarchy.')
      : pending ? uiText(root, 'HierarchyUpdating', 'Updating hierarchy…') : root._cfxHierarchyError || '';
  };
  const hierarchyHasSource = (root, id) => interactiveTargets(root, '[data-cfx-target-kind="node"][data-cfx-target-id]')
    .some(node => node.dataset.cfxTargetId === id) || Array.from(root.querySelectorAll('.cfx-stage [data-cfx-node]')).some(node => node.dataset.cfxNode === id);
  const requestNativeHierarchy = (root, view, sync = true, accepted) => {
    const current = capturedHierarchy(root);
    if (!hierarchyConnected(root) || !current || view !== null && (!view || typeof view !== 'object'
      || Object.keys(view).some(key => key !== 'rootId') || typeof view.rootId !== 'string' || !view.rootId.trim() || !hierarchyHasSource(root, view.rootId))) return false;
    const rootId = view === null ? null : view.rootId;
    root._cfxHierarchyError = '';
    return requestNativePreparation(root, 'hierarchy', { kind: rootId === null ? 'reset' : 'root', rootId }, {
      eventPrefix: 'cfxhierarchy', resetView: rootId === null, accepted,
      started: () => syncHierarchyNavigation(root), cancelled: () => syncHierarchyNavigation(root),
      validate: next => {
        const adaptive = readAdaptiveLayout(next);
        for (const drawing of adaptive ? [adaptive.wide, adaptive.narrow] : [next]) {
          const reply = capturedHierarchy(drawing);
          if (!reply || reply.kind !== current.kind || reply.seriesKey !== current.seriesKey || reply.rootId !== rootId)
            throw new TypeError('Every native hierarchy drawing must retain the source family and series and accept the exact requested root.');
        }
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
    && node.dataset.cfxTargetId !== capturedHierarchy(root).rootId && requestNativeHierarchy(root, { rootId: node.dataset.cfxTargetId });
  const bindHierarchyNavigation = root => {
    syncHierarchyNavigation(root);
    root.querySelectorAll('[data-cfx-hierarchy-control]').forEach(button => listenChart(root, button, 'click', () => {
      if (button.disabled) return;
      const path = capturedHierarchy(root)?.path || [];
      const parent = path.length > 1 ? path[path.length - 2].id : null;
      requestNativeHierarchy(root, button.dataset.cfxHierarchyControl === 'up' && parent !== null ? { rootId: parent } : null);
    }));
  };
