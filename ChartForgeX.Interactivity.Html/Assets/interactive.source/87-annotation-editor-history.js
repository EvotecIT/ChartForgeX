  const replayAnnotationEdit = (root, state, redo) => {
    if (state.draft) throw new TypeError('History replay: commit/cancel draft first.');
    const stack = redo ? state.redo : state.undo, entry = stack.at(-1);
    if (!entry) throw new TypeError('No accepted annotation edit.');
    const expected = redo ? entry.before : entry.after, desired = redo ? entry.after : entry.before;
    if (!sameAnnotationDrawing(annotationDrawingAt(root, entry.id), expected))
      throw new TypeError('History source changed; refusing annotation overwrite.');
    state.replay = { entry, redo };
    try {
      if (!requestAnnotationChange(root, { id: entry.id, operation: desired ? expected ? 'replace' : 'add' : 'remove', ...(desired ? { drawing: desired } : {}) }, { entry, redo }))
        throw new TypeError('History: native request unavailable.');
    } catch (error) { state.replay = null; throw error; }
  };
  const annotationEditorChangeHooks = (root, id, drawing, replay) => {
    const state = annotationEditorState(root);
    if (!state) return {};
    const before = annotationDrawingAt(root, id), after = drawing ?? null;

    const representable = before !== null || !nativeAnnotationNode(root, id);
    if (!state.enabled) return representable ? {} : { committed: () => { state.undo = []; state.redo = []; } };

    const release = () => { state.pending = false; state.replay = null; refreshAnnotationEditor(root); };
    return {
      started: () => { state.pending = true; state.error = ''; refreshAnnotationEditor(root); },
      cancelled: release,
      failed: () => { release(); state.error = uiText(root, 'AnnotationFailed', 'The annotation could not be updated.'); refreshAnnotationEditor(root); },
      committed: () => {

        if (!representable) {
          state.undo = []; state.redo = [];
        } else if (replay) {
          const from = replay.redo ? state.redo : state.undo, to = replay.redo ? state.undo : state.redo;
          if (from.at(-1) === replay.entry) { from.pop(); to.push(replay.entry); }
        } else {
          state.redo = [];
          if (state.limit > 0) {
            state.undo.push(Object.freeze({ id, before, after }));
            if (state.undo.length > state.limit) state.undo.splice(0, state.undo.length - state.limit);
          }
        }
        state.draft = null; release();
        if (representable) emitHostEvent(root, 'cfxannotationcommit', { id, before, after, history: replay ? replay.redo ? 'redo' : 'undo' : 'edit' });
      }
    };
  };
