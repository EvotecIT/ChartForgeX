  // Persist catalog IDs, not hints.
  const annotationCatalog = (svg, metadata) => {
    const entries = metadata.annotations ?? [], ids = new Set(), sources = new Set(), bindings = new Map();
    const nodes = Array.from(svg.querySelectorAll('[data-cfx-role^="annotation"],[data-cfx-target-kind="annotation"]'));
    const invalid = () => { throw new TypeError('The chart scene does not match its prepared annotation metadata.'); };
    if (!Array.isArray(entries) || nodes.filter(node => node.dataset.cfxAnnotationId !== undefined).length !== entries.length) invalid();
    for (const item of entries) {
      if (!item || typeof item.id !== 'string' || !item.id.trim() || item.id !== item.id.trim()
        || typeof item.sourceId !== 'string' || !item.sourceId.startsWith('annotation-id-')
        || !['HorizontalLine', 'VerticalLine', 'HorizontalBand', 'VerticalBand', 'Note', 'LineSegment', 'ParallelChannel'].includes(item.kind)
        || ids.has(item.id) || sources.has(item.sourceId)) invalid();
      const node = nodes.find(node => node.dataset.cfxSourceId === item.sourceId), data = node?.dataset;
      if (!data || data.cfxAnnotationId !== item.id || data.cfxKind !== item.kind
        || data.cfxId !== undefined && data.cfxId !== item.sourceId
        || data.cfxTargetId !== undefined && data.cfxTargetId !== item.sourceId
        || data.cfxTargetKind !== undefined && data.cfxTargetKind !== 'annotation'
        || metadata.regions.filter(region => region.id === item.sourceId && region.role === 'annotation').length !== 1) invalid();
      ids.add(item.id); sources.add(item.sourceId); bindings.set(node, item.id);
    }

    for (const node of nodes) if (!bindings.has(node) && (node.dataset.cfxAnnotationId !== undefined
      || [node.dataset.cfxSourceId, node.dataset.cfxId, node.dataset.cfxTargetId].some(id => id?.startsWith('annotation-id-')))) invalid();
    return bindings;
  };
