  const refreshAnnotationEditor = root => {
    const state = annotationEditorState(root), view = state?.view;
    if (!state) return;
    if (view && root.contains(view.element)) {
      const admissionBusy = nativePreparationBackpressureBusy(root);
      view.element.hidden = !state.enabled;
      view.element.setAttribute('aria-busy', state.pending || admissionBusy ? 'true' : 'false');
      const connected = !!nativePreparationBinding(root, 'annotation');
      const selected = view.select.value, ids = Array.from(root.querySelectorAll('[data-cfx-role=annotation][data-cfx-annotation-id]'))
        .filter(node => ['Note', 'LineSegment', 'ParallelChannel'].includes(node.dataset.cfxKind));
      view.select.replaceChildren();
      for (const node of ids) { const option = document.createElement('option'); option.value = node.dataset.cfxAnnotationId;
        option.textContent = node.dataset.cfxLabel ? `${node.dataset.cfxLabel} (${option.value})` : option.value; view.select.append(option); }
      if (ids.some(node => node.dataset.cfxAnnotationId === selected)) view.select.value = selected;
      view.select.disabled = state.pending || admissionBusy || !!state.draft || ids.length === 0;
      for (const [kind, button] of Object.entries(view.buttons)) button.disabled = !connected || state.pending || admissionBusy
        || ['Note', 'LineSegment', 'ParallelChannel'].includes(kind) && (!state.createId || !!state.draft || !sourceAnnotationProjection(root, 'Primary'))
        || ['edit', 'remove'].includes(kind) && (!!state.draft || ids.length === 0)
        || kind === 'commit' && !state.draft || kind === 'cancel' && !state.draft
        || kind === 'undo' && (!!state.draft || state.undo.length === 0) || kind === 'redo' && (!!state.draft || state.redo.length === 0);

      view.buttons.cancel.disabled = admissionBusy || !state.draft && !state.pending;
      view.label.disabled = state.pending || admissionBusy || !state.draft?.drawing;
      view.label.value = state.draft?.drawing?.label ?? '';
      view.status.textContent = !connected ? uiText(root, 'AnnotationDisconnected', 'Connect native preparation to edit annotations.') : state.pending ? uiText(root, 'AnnotationUpdating', 'Updating annotation…') : state.error
        || (state.draft ? uiText(root, 'AnnotationDraft', 'Draft: move an anchor, then apply or cancel.') : '');
    }
    drawAnnotationEditorPreview(root);
  };
  const bindAnnotationEditor = root => {
    const state = annotationEditorState(root);
    if (!state?.enabled) return;
    if (state.view && root.contains(state.view.element)) { refreshAnnotationEditor(root); return; }
    const panel = document.createElement('section'); panel.className = 'cfx-annotation-editor';
    panel.setAttribute('aria-label', uiText(root, 'AnnotationEditor', 'Annotation editor'));
    const selectLabel = document.createElement('label'), select = document.createElement('select');
    selectLabel.append(document.createTextNode(uiText(root, 'AnnotationChoose', 'Drawing')), select); panel.append(selectLabel);
    const buttons = {}, control = (kind, key, fallback, action) => {
      const button = document.createElement('button'); button.type = 'button'; button.dataset.cfxAnnotationControl = kind;
      button.textContent = uiText(root, key, fallback); panel.append(button); buttons[kind] = button;
      listenChart(root, button, 'click', () => { try { action(); } catch (error) { state.error = error.message; refreshAnnotationEditor(root); } });
    };
    const create = kind => {
      const projection = sourceAnnotationProjection(root, 'Primary'); if (!projection || !state.createId) return;
      const source = (x, y) => annotationEditorSource(projection, { x: projection.plot.x + x * projection.plot.width, y: projection.plot.y + y * projection.plot.height });
      const start = source(.35, .55), end = source(.65, .35);
      const drawing = { kind, start, ...(kind === 'Note' ? {} : { end }), ...(kind === 'ParallelChannel' ? { yOffset: source(.35, .35).y - start.y } : {}) };
      state.api.begin({ operation: 'add', id: state.createId(kind), drawing });
    };
    control('Note', 'AnnotationNote', 'Add note', () => create('Note'));
    control('LineSegment', 'AnnotationLine', 'Add line', () => create('LineSegment'));
    control('ParallelChannel', 'AnnotationChannel', 'Add channel', () => create('ParallelChannel'));
    control('edit', 'AnnotationEdit', 'Edit', () => state.api.begin({ operation: 'replace', id: select.value }));
    control('remove', 'AnnotationDelete', 'Delete', () => state.api.begin({ operation: 'remove', id: select.value }));
    const caption = document.createElement('label'), input = document.createElement('input'); input.type = 'text'; input.maxLength = 4096;
    caption.append(document.createTextNode(uiText(root, 'AnnotationCaption', 'Caption')), input); panel.append(caption);
    listenChart(root, input, 'input', () => {
      if (!state.draft?.drawing || state.pending) return;

      const start = input.selectionStart, end = input.selectionEnd;
      state.api.preview({ ...state.draft.drawing, label: input.value }); input.setSelectionRange(start, end);
    });
    control('commit', 'AnnotationApply', 'Apply', () => state.api.commit());
    control('cancel', 'AnnotationCancel', 'Cancel', () => state.api.cancel());
    control('undo', 'AnnotationUndo', 'Undo', () => state.api.undo());
    control('redo', 'AnnotationRedo', 'Redo', () => state.api.redo());
    control('close', 'AnnotationClose', 'Close editor', () => state.api.disable());
    const status = document.createElement('span'); status.className = 'cfx-annotation-editor__status'; status.setAttribute('role', 'status'); status.setAttribute('aria-live', 'polite'); panel.append(status);
    const stage = root.querySelector('.cfx-stage'); if (!stage) return;
    stage.before(panel); state.view = { element: panel, select, label: input, buttons, status };
    ownChartWork(root, () => { panel.remove(); if (state.view?.element === panel) state.view = null;
      root.querySelector('[data-cfx-annotation-editor]')?.remove(); });
    bindAnnotationEditorGestures(root); refreshAnnotationEditor(root);
  };
