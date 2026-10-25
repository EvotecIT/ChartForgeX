
  const nativeAnnotationNode = (root, id) => Array.from(root.querySelectorAll('[data-cfx-role=annotation][data-cfx-annotation-id]'))
    .find(node => node.dataset.cfxAnnotationId === id);
  const sourceAnnotationProjection = (root, yAxis) => {
    let metadata;
    try { metadata = JSON.parse(root.dataset.cfxPreparedChart); } catch { return null; }
    const x = metadata.xProjection, y = yAxis === 'Secondary' ? metadata.secondaryYProjection : metadata.yProjection;
    const valid = axis => axis && [axis.minimum, axis.maximum, axis.transformedMinimum, axis.transformedMaximum].every(Number.isFinite)
      && axis.maximum > axis.minimum && axis.transformedMaximum > axis.transformedMinimum;
    const plot = x?.plotBounds;
    if (!valid(x) || !valid(y) || !plot || ![plot.x, plot.y, plot.width, plot.height].every(Number.isFinite)
      || plot.width <= 0 || plot.height <= 0 || metadata.orientation === 'horizontal'
      || ['x', 'y', 'width', 'height'].some(key => y.plotBounds?.[key] !== plot[key])) return null;
    return { x, y, plot };
  };
  const sourceAnnotationDrawing = value => {
    const point = source => {
      if (!source || !Number.isFinite(source.x) || !Number.isFinite(source.y))
        throw new TypeError('Drawing: nonfinite coordinate.');
      return Object.freeze({ x: source.x, y: source.y });
    };
    if (!value || !['Note', 'LineSegment', 'ParallelChannel'].includes(value.kind)
      || value.label !== undefined && typeof value.label !== 'string'
      || value.yAxis !== undefined && !['Primary', 'Secondary'].includes(value.yAxis))
      throw new TypeError('Drawing kind: Note/LineSegment/ParallelChannel required.');
    const start = point(value.start), end = value.kind === 'Note' ? undefined : point(value.end);
    if (end && start.x === end.x && start.y === end.y) throw new TypeError('Source line: endpoints must differ.');
    if (value.kind === 'Note' && value.end !== undefined) throw new TypeError('Note: one source anchor required.');
    const yOffset = value.kind === 'ParallelChannel' ? value.yOffset : 0;
    if (!Number.isFinite(yOffset) || value.kind === 'ParallelChannel' && yOffset === 0
      || !Number.isFinite(start.y + yOffset) || end && !Number.isFinite(end.y + yOffset))
      throw new TypeError('Channel: finite nonzero Y offset/bounds required.');
    return Object.freeze({ kind: value.kind, start, ...(end ? { end } : {}),
      ...(value.kind === 'ParallelChannel' ? { yOffset } : {}), label: value.label ?? '', yAxis: value.yAxis ?? 'Primary' });
  };
  const requestAnnotationChange = (root, change, editorReplay = null) => {
    if (!nativePreparationBinding(root, 'annotation')) throw new TypeError('Annotation: callback required.');
    if (!change || !['add', 'replace', 'remove'].includes(change.operation)
      || typeof change.id !== 'string' || !change.id.trim()) throw new TypeError('Annotation: operation/nonempty authored ID.');
    const annotationId = change.id.trim(), existing = nativeAnnotationNode(root, annotationId);
    if (change.operation === 'add' ? !!existing : !existing)
      throw new TypeError('Annotation ID: add unused; edit existing.');
    const drawing = change.operation === 'remove' ? undefined : sourceAnnotationDrawing(change.drawing);
    if (drawing && !sourceAnnotationProjection(root, drawing.yAxis))
      throw new TypeError('Drawing: vertical Cartesian axes missing.');
    return requestNativePreparation(root, 'annotation', { kind: change.operation, annotationId, ...(drawing ? { sourceDrawing: drawing } : {}) }, {
      eventPrefix: 'cfxannotation', preserveViewport: true,
      ...annotationEditorChangeHooks(root, annotationId, drawing, editorReplay),
      validate: (next, request) => {
        const node = nativeAnnotationNode(next, request.annotationId), source = request.sourceDrawing;
        if (request.kind === 'remove') {
          if (node) throw new TypeError('Native reply: annotation removal failed.');
          return;
        }
        const data = node?.dataset;
        if (!data || data.cfxKind !== source.kind || data.cfxLabel !== source.label || data.cfxYAxis !== source.yAxis
          || Number(data.cfxSourceX) !== source.start.x || Number(data.cfxSourceY) !== source.start.y
          || (source.end ? Number(data.cfxSourceEndX) !== source.end.x || Number(data.cfxSourceEndY) !== source.end.y
            : data.cfxSourceEndX !== '' || data.cfxSourceEndY !== '')
          || Number(data.cfxChannelOffset) !== (source.yOffset ?? 0))
          throw new TypeError('Native reply: source drawing changed.');
      }
    });
  };
