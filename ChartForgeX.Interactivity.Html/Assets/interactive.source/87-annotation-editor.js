
  const annotationEditorState = root => chartLifetimes.get(root)?.annotationEditor;
  const annotationDrawingAt = (root, id) => {
    const data = nativeAnnotationNode(root, id)?.dataset;
    if (!data || !['Note', 'LineSegment', 'ParallelChannel'].includes(data.cfxKind)) return null;
    return sourceAnnotationDrawing({ kind: data.cfxKind, label: data.cfxLabel, yAxis: data.cfxYAxis,
      start: { x: Number(data.cfxSourceX), y: Number(data.cfxSourceY) },
      ...(data.cfxKind === 'Note' ? {} : { end: { x: Number(data.cfxSourceEndX), y: Number(data.cfxSourceEndY) } }),
      ...(data.cfxKind === 'ParallelChannel' ? { yOffset: Number(data.cfxChannelOffset) } : {}) });
  };
  const sameAnnotationDrawing = (left, right) => JSON.stringify(left) === JSON.stringify(right);
  const annotationEditorSnapshot = state => copyHostValue({ enabled: state.enabled, pending: state.pending,
    draft: state.draft, error: state.error, canUndo: !state.pending && state.undo.length > 0,
    canRedo: !state.pending && state.redo.length > 0 });
  const resetAnnotationEditor = root => {
    const state = annotationEditorState(root);
    if (!state) return;
    state.draft = null; state.pending = false; state.replay = null; state.undo = []; state.redo = []; state.error = '';
    refreshAnnotationEditor(root);
  };
  const createAnnotationEditor = (root, requireMounted, options = {}) => {
    const lifetime = chartLifetimes.get(root);
    const limit = options.historyLimit === undefined ? 32 : options.historyLimit;
    if (!Number.isInteger(limit) || limit < 0 || limit > 256 || options.createId !== undefined && typeof options.createId !== 'function')
      throw new TypeError('historyLimit: int 0..256; createId: function.');
    if (!nativePreparationBinding(root, 'annotation')) throw new TypeError('Editor: native callback required.');
    if (lifetime.annotationEditor) {
      const state = lifetime.annotationEditor;
      if (state.pending) throw new TypeError('Editor: finish pending native edit.');
      state.enabled = true; state.limit = limit; state.createId = options.createId; state.undo = state.undo.slice(-limit || state.undo.length); state.redo = [];
      if (limit === 0) state.undo = [];
      bindAnnotationEditor(root); return state.api;
    }
    const state = { enabled: true, limit, createId: options.createId, draft: null, pending: false,
      undo: [], redo: [], replay: null, error: '', view: null, api: null };
    const ready = () => { requireMounted(); requireNativePreparationIdle(root); if (!state.enabled || state.pending || !nativePreparationBinding(root, 'annotation')) throw new TypeError('Editor: disabled/native pending.'); };
    const api = Object.freeze({
      getState: () => { requireMounted(); return annotationEditorSnapshot(state); },
      begin: change => {
        ready();
        if (!change || !['add', 'replace', 'remove'].includes(change.operation) || typeof change.id !== 'string' || !change.id.trim())
          throw new TypeError('Edit: add/replace/remove; authored ID.');
        const id = change.id.trim(), existing = nativeAnnotationNode(root, id), before = annotationDrawingAt(root, id);
        if (change.operation === 'add' ? !!existing : !before) throw new TypeError('Edit ID: add unused; edit existing.');
        const drawing = change.operation === 'remove' ? null : sourceAnnotationDrawing(change.drawing ?? before);
        if (drawing && !sourceAnnotationProjection(root, drawing.yAxis)) throw new TypeError('Drawing: captured unambiguous axes.');
        state.draft = Object.freeze({ operation: change.operation, id, ...(drawing ? { drawing } : {}) }); state.error = '';
        refreshAnnotationEditor(root); return api;
      },
      preview: drawing => {
        ready(); if (!state.draft || state.draft.operation === 'remove') throw new TypeError('Begin drawable edit first.');
        const captured = sourceAnnotationDrawing(drawing);
        if (!sourceAnnotationProjection(root, captured.yAxis)) throw new TypeError('Drawing: captured axes missing.');
        state.draft = Object.freeze({ ...state.draft, drawing: captured }); state.error = ''; refreshAnnotationEditor(root); return api;
      },
      commit: () => { ready(); if (!state.draft) throw new TypeError('Begin annotation edit first.');
        if (!requestAnnotationChange(root, state.draft)) throw new TypeError('Annotation: request unavailable.'); return api; },
      cancel: () => { requireMounted(); if (state.pending) cancelNativePreparation(lifetime);
        state.draft = null; state.error = ''; refreshAnnotationEditor(root); return api; },
      undo: () => { ready(); replayAnnotationEdit(root, state, false); return api; },
      redo: () => { ready(); replayAnnotationEdit(root, state, true); return api; },
      disable: () => { requireMounted(); if (state.pending) cancelNativePreparation(lifetime);
        state.enabled = false; state.draft = null; refreshAnnotationEditor(root); return api; }
    });
    state.api = api; lifetime.annotationEditor = state; bindAnnotationEditor(root); return api;
  };
