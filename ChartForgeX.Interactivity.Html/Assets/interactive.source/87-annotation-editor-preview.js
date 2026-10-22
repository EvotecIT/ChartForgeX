  const annotationEditorPoint = (projection, point) => ({
    x: projection.plot.x + projectedAxisRatio(projection.x, point.x) * projection.plot.width,
    y: projection.plot.y + (1 - projectedAxisRatio(projection.y, point.y)) * projection.plot.height
  });
  const annotationEditorSource = (projection, point) => ({
    x: projectedAxisValue(projection.x, clamp((point.x - projection.plot.x) / projection.plot.width, 0, 1)),
    y: projectedAxisValue(projection.y, clamp(1 - (point.y - projection.plot.y) / projection.plot.height, 0, 1))
  });
  const drawAnnotationEditorPreview = root => {
    const state = annotationEditorState(root), svg = root.querySelector('.cfx-stage svg');
    svg?.querySelector('[data-cfx-annotation-editor]')?.remove();
    const drawing = state?.enabled && state.draft?.drawing, projection = drawing && sourceAnnotationProjection(root, drawing.yAxis);
    if (!svg || !drawing || !projection) return;
    const element = (tag, attributes) => {
      const node = document.createElementNS('http://www.w3.org/2000/svg', tag);
      for (const [name, value] of Object.entries(attributes)) node.setAttribute(name, value);
      return node;
    };
    const group = element('g', { 'data-cfx-annotation-editor': '', 'class': 'cfx-annotation-preview' });
    svg.append(group);
    const start = annotationEditorPoint(projection, drawing.start), end = drawing.end && annotationEditorPoint(projection, drawing.end);
    if (end) {
      const line = (first, last) => group.append(element('line', { x1: first.x, y1: first.y, x2: last.x, y2: last.y, 'class': 'cfx-annotation-preview__line' }));
      line(start, end);
      if (drawing.kind === 'ParallelChannel') {
        const upperStart = annotationEditorPoint(projection, { x: drawing.start.x, y: drawing.start.y + drawing.yOffset });
        const upperEnd = annotationEditorPoint(projection, { x: drawing.end.x, y: drawing.end.y + drawing.yOffset });
        group.append(element('path', { d: `M${start.x} ${start.y}L${end.x} ${end.y}L${upperEnd.x} ${upperEnd.y}L${upperStart.x} ${upperStart.y}Z`, 'class': 'cfx-annotation-preview__channel' }));
        line(upperStart, upperEnd);
        handle('offset', upperStart);
      }
      handle('end', end);
    }
    handle('start', start);
    function handle(part, point) {
      const label = uiText(root, part === 'start' ? 'AnnotationStart' : part === 'end' ? 'AnnotationEnd' : 'AnnotationOffset', part === 'start' ? 'Start anchor' : part === 'end' ? 'End anchor' : 'Channel offset');
      const node = element('circle', { cx: point.x, cy: point.y, r: 7, tabindex: state.pending ? -1 : 0,
        role: 'button', 'aria-label': label, 'data-cfx-editor-handle': part, 'class': 'cfx-annotation-preview__handle' });
      group.append(node);
    }
  };
  const moveAnnotationEditorHandle = (root, part, point) => {
    const state = annotationEditorState(root), drawing = state?.draft?.drawing;
    if (!drawing || state.pending) return;
    const projection = sourceAnnotationProjection(root, drawing.yAxis);
    if (!projection) return;
    const source = annotationEditorSource(projection, point);
    const next = part === 'offset' ? { ...drawing, yOffset: source.y - drawing.start.y }
      : { ...drawing, [part]: source };
    try { state.api.preview(next); }
    catch (error) { if (!(error instanceof TypeError)) throw error; } // A degenerate pointer position retains the last valid draft.
  };
  const bindAnnotationEditorGestures = root => {
    const stage = root.querySelector('.cfx-stage');
    if (!stage) return;
    let drag = null;
    const logical = event => {
      const matrix = svgScreenMatrix(root.querySelector('.cfx-stage svg'));
      if (!matrix) return null;
      try { const point = new DOMPoint(event.clientX, event.clientY).matrixTransform(matrix.inverse());
        return Number.isFinite(point.x) && Number.isFinite(point.y) ? point : null; } catch { return null; }
    };
    listenChart(root, stage, 'pointerdown', event => {
      const handle = event.target.closest?.('[data-cfx-editor-handle]'), state = annotationEditorState(root);
      if (!handle || !state?.enabled || state.pending || event.button !== 0) return;
      event.preventDefault(); event.stopImmediatePropagation();
      drag = { pointerId: event.pointerId, part: handle.dataset.cfxEditorHandle };
      stage.setPointerCapture(event.pointerId); handle.focus({ preventScroll: true });
    }, true);
    listenChart(root, stage, 'pointermove', event => {
      if (!drag || event.pointerId !== drag.pointerId) return;
      event.preventDefault(); event.stopImmediatePropagation();
      const point = logical(event); if (point) moveAnnotationEditorHandle(root, drag.part, point);
    }, true);
    const stop = event => { if (drag && event.pointerId === drag.pointerId) {
      if (stage.hasPointerCapture(event.pointerId)) stage.releasePointerCapture(event.pointerId); drag = null;
      event.preventDefault(); event.stopImmediatePropagation(); } };
    listenChart(root, stage, 'pointerup', stop, true); listenChart(root, stage, 'pointercancel', stop, true);
    ownChartWork(root, () => { if (drag && stage.hasPointerCapture(drag.pointerId)) stage.releasePointerCapture(drag.pointerId); drag = null; });
    listenChart(root, root, 'keydown', event => {
      const state = annotationEditorState(root); if (!state?.enabled || !state.draft) return;
      if (event.key === 'Escape' || event.key === 'Enter' && !event.target.matches('button,input,select,textarea')) {
        event.preventDefault(); event.stopImmediatePropagation();
        if (event.key === 'Escape') state.api.cancel(); else if (!state.pending) state.api.commit(); return;
      }
      const handle = event.target.closest?.('[data-cfx-editor-handle]');
      if (!handle || state.pending || !['ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown'].includes(event.key)) return;
      event.preventDefault(); event.stopImmediatePropagation();
      const part = handle.dataset.cfxEditorHandle, step = event.shiftKey ? 10 : 1;
      moveAnnotationEditorHandle(root, part, { x: Number(handle.getAttribute('cx')) + (event.key === 'ArrowLeft' ? -step : event.key === 'ArrowRight' ? step : 0),
        y: Number(handle.getAttribute('cy')) + (event.key === 'ArrowUp' ? -step : event.key === 'ArrowDown' ? step : 0) });
      root.querySelector(`[data-cfx-editor-handle=${part}]`)?.focus({ preventScroll: true });
    }, true);
  };
