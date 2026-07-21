  const graphCapability = (root, capability) => (attr(root, 'data-cfx-graph-manipulation-capabilities') || '').split(',').includes(capability);
  const graphEditor = root => root.querySelector('[data-cfx-role="graph-editor"]');
  const graphEditorStatus = (root, message, error) => {
    const output = root.querySelector('[data-cfx-graph-editor-status]');
    if (output) { output.textContent = message || ''; output.dataset.cfxGraphEditorError = error ? 'true' : 'false'; }
  };
  const graphEditorForm = (root, kind) => root.querySelector(`[data-cfx-graph-editor-form="${kind}"]`);
  const graphEditorField = (form, name) => form?.querySelector(`[data-cfx-graph-editor-field="${name}"]`);
  const validateManipulationPatch = (root, patch) => {
    const nodeIds = new Set(items(root, '[data-cfx-role="graph-node"]').map(node => attr(node, 'data-node-id')));
    const edgeIds = new Set(items(root, '[data-cfx-role="graph-edge"]').map(edge => attr(edge, 'data-edge-id')));
    (patch.upsertNodes || []).forEach(node => {
      const capability = nodeIds.has(String(node?.id || '')) ? 'editNodes' : 'addNodes';
      if (!graphCapability(root, capability)) throw new Error(`Graph manipulation does not allow ${capability}.`);
    });
    (patch.upsertEdges || []).forEach(edge => {
      const capability = edgeIds.has(String(edge?.id || '')) ? 'editEdges' : 'addEdges';
      if (!graphCapability(root, capability)) throw new Error(`Graph manipulation does not allow ${capability}.`);
    });
    if ((patch.removeNodeIds || []).length && !graphCapability(root, 'deleteNodes')) throw new Error('Graph manipulation does not allow deleteNodes.');
    if ((patch.removeEdgeIds || []).length && !graphCapability(root, 'deleteEdges')) throw new Error('Graph manipulation does not allow deleteEdges.');
    if ((patch.upsertClusters || []).length || (patch.removeClusterIds || []).length) throw new Error('User-level graph manipulation does not allow cluster structure changes. Use the host update API.');
    validateGraphPatch(root, patch);
  };
  const graphSelectedElement = root => items(root, '.cfx-graph-selected')[0] || null;
  const graphSelectedDocumentItem = root => {
    const selected = graphSelectedElement(root); if (!selected) return null;
    const document = exportGraphJson(root), role = attr(selected, 'data-cfx-role');
    const id = attr(selected, 'data-node-id') || attr(selected, 'data-edge-id') || attr(selected, 'data-cluster-id');
    return role === 'graph-node' ? document.nodes.find(item => item.id === id) : role === 'graph-edge' ? document.edges.find(item => item.id === id) : null;
  };
  const optionalGraphCoordinate = field => {
    const value = field?.value?.trim() || '';
    if (!value) return null;
    const number = Number(value);
    return Number.isFinite(number) ? number : null;
  };
  const showGraphEditorForm = (root, kind, item, mode) => {
    ['node', 'edge'].forEach(name => { const form = graphEditorForm(root, name); if (form) form.hidden = name !== kind; });
    const form = graphEditorForm(root, kind); if (!form) return false;
    form.dataset.cfxGraphEditorMode = mode;
    const title = form.querySelector('[data-cfx-graph-editor-title]'); if (title) title.textContent = `${mode === 'add' ? 'Add' : 'Edit'} ${kind}`;
    ['id', 'label', 'kind', 'x', 'y', 'source', 'target'].forEach(name => { const field = graphEditorField(form, name); if (field) field.value = item?.[name] ?? ''; });
    const id = graphEditorField(form, 'id'); if (id) id.readOnly = mode === 'edit';
    form.hidden = false; graphEditor(root)?.removeAttribute('hidden'); graphEditorField(form, 'id')?.focus();
    return true;
  };
  const requestGraphChange = (root, patch, source, label) => {
    if (!hasFeature(root, 'Manipulation') || attr(root, 'data-cfx-graph-manipulation') !== 'true') throw new Error('This graph scene does not enable manipulation.');
    validateManipulationPatch(root, patch || {});
    const detail = { graphId: attr(root, 'data-cfx-graph-id'), source: source || 'api', label: label || 'Graph change', patch };
    if (!emit(root, 'cfxgraphbeforechange', detail, { cancelable: true })) return false;
    checkpointGraphState(root, detail.label);
    const result = applyGraphRuntimePatch(root, patch);
    persistGraphInteractionState(root, detail.source);
    emit(root, 'cfxgraphchange', { ...detail, result });
    return result;
  };
  const submitGraphNode = (root, form) => {
    const mode = form.dataset.cfxGraphEditorMode || 'add', existing = mode === 'edit' ? graphSelectedDocumentItem(root) : null;
    const id = graphEditorField(form, 'id').value.trim(), label = graphEditorField(form, 'label').value.trim();
    const x = optionalGraphCoordinate(graphEditorField(form, 'x')), y = optionalGraphCoordinate(graphEditorField(form, 'y'));
    const node = { ...(existing || {}), id, label, kind: graphEditorField(form, 'kind').value.trim(), x: x ?? existing?.x ?? Number(root.dataset.cfxGraphLastPointerX || sceneSize(root).centerX), y: y ?? existing?.y ?? Number(root.dataset.cfxGraphLastPointerY || sceneSize(root).centerY) };
    return requestGraphChange(root, { upsertNodes: [node] }, `editor-${mode}-node`, `${mode === 'add' ? 'Add' : 'Edit'} node ${id}`);
  };
  const submitGraphEdge = (root, form) => {
    const mode = form.dataset.cfxGraphEditorMode || 'add', existing = mode === 'edit' ? graphSelectedDocumentItem(root) : null;
    const id = graphEditorField(form, 'id').value.trim(), source = graphEditorField(form, 'source').value.trim(), target = graphEditorField(form, 'target').value.trim();
    const edge = { ...(existing || {}), id, source, target, label: graphEditorField(form, 'label').value.trim(), directed: existing?.directed ?? true };
    return requestGraphChange(root, { upsertEdges: [edge] }, `editor-${mode}-edge`, `${mode === 'add' ? 'Add' : 'Edit'} edge ${id}`);
  };
  const deleteGraphSelection = root => {
    const selected = selectedItems(root); if (!selected.length) { graphEditorStatus(root, 'Select a node or edge first.', true); return false; }
    const nodeIds = selected.filter(item => item.role === 'graph-node' && graphCapability(root, 'deleteNodes')).map(item => item.id);
    const edgeIds = selected.filter(item => item.role === 'graph-edge' && graphCapability(root, 'deleteEdges')).map(item => item.id);
    if (!nodeIds.length && !edgeIds.length) { graphEditorStatus(root, 'The selected items cannot be deleted.', true); return false; }
    if (attr(root, 'data-cfx-graph-confirm-destructive') === 'true' && !window.confirm(`Delete ${nodeIds.length + edgeIds.length} selected graph item${nodeIds.length + edgeIds.length === 1 ? '' : 's'}?`)) return false;
    return requestGraphChange(root, { removeNodeIds: nodeIds, removeEdgeIds: edgeIds, removeIncidentReferences: true }, 'editor-delete', 'Delete graph selection');
  };
  const handleGraphEditorAction = (root, action) => {
    try {
      if (action === 'add-node' && graphCapability(root, 'addNodes')) showGraphEditorForm(root, 'node', { id: `node-${Date.now().toString(36)}`, label: '' }, 'add');
      else if (action === 'add-edge' && graphCapability(root, 'addEdges')) showGraphEditorForm(root, 'edge', { id: `edge-${Date.now().toString(36)}` }, 'add');
      else if (action === 'edit-node' && graphCapability(root, 'editNodes')) { const item = graphSelectedDocumentItem(root); if (!item || !Object.prototype.hasOwnProperty.call(item, 'x')) throw new Error('Select one node to edit.'); showGraphEditorForm(root, 'node', item, 'edit'); }
      else if (action === 'edit-edge' && graphCapability(root, 'editEdges')) { const item = graphSelectedDocumentItem(root); if (!item || Object.prototype.hasOwnProperty.call(item, 'x')) throw new Error('Select one edge to edit.'); showGraphEditorForm(root, 'edge', item, 'edit'); }
      else if (action === 'delete') deleteGraphSelection(root);
      graphEditorStatus(root, '');
    } catch (error) { graphEditorStatus(root, error?.message || 'The graph change was rejected.', true); }
  };
  const bindGraphManipulation = root => {
    const editor = graphEditor(root); if (!editor) return;
    root.querySelector('[data-cfx-graph-editor-close]')?.addEventListener('click', () => {
      editor.setAttribute('hidden', '');
      items(root, "[data-cfx-graph-action='edit']").forEach(button => button.setAttribute('aria-pressed', 'false'));
    });
    items(root, '[data-cfx-graph-editor-action]').forEach(button => button.addEventListener('click', () => handleGraphEditorAction(root, attr(button, 'data-cfx-graph-editor-action'))));
    items(root, '[data-cfx-graph-editor-form]').forEach(form => form.addEventListener('submit', event => {
      event.preventDefault();
      try {
        const result = attr(form, 'data-cfx-graph-editor-form') === 'node' ? submitGraphNode(root, form) : submitGraphEdge(root, form);
        if (result) { graphEditorStatus(root, 'Graph updated.'); form.hidden = true; }
      } catch (error) { graphEditorStatus(root, error?.message || 'The graph change was rejected.', true); }
    }));
  };
