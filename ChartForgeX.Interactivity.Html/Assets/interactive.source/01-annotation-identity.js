  // A declared native binding, rather than a caller scope hint, makes an annotation durable.
  const annotationCatalog = (svg, metadata) => {
    const entries = metadata.annotations ?? [], ids = new Set(), bindings = new Map();
    const nodes = Array.from(svg.querySelectorAll('[data-cfx-role="annotation"][data-cfx-annotation-id]'));
    const invalid = () => { throw new TypeError('The chart scene does not match its prepared annotation metadata.'); };
    if (!Array.isArray(entries) || nodes.length !== entries.length) invalid();
    for (const item of entries) {
      if (!item || typeof item.id !== 'string' || !item.id.trim() || item.id !== item.id.trim()
        || typeof item.sourceId !== 'string' || !item.sourceId.startsWith('annotation-id-')
        || !['HorizontalLine', 'VerticalLine', 'HorizontalBand', 'VerticalBand'].includes(item.kind)
        || ids.has(item.id) || bindings.has(item.sourceId)) invalid();
      const node = nodes.find(node => node.dataset.cfxSourceId === item.sourceId), data = node?.dataset;
      if (!data || data.cfxAnnotationId !== item.id || data.cfxKind !== item.kind
        || data.cfxId !== undefined && data.cfxId !== item.sourceId
        || data.cfxTargetId !== undefined && data.cfxTargetId !== item.sourceId
        || data.cfxTargetKind !== undefined && data.cfxTargetKind !== 'annotation'
        || metadata.regions.filter(region => region.id === item.sourceId && region.role === 'annotation').length !== 1) invalid();
      ids.add(item.id); bindings.set(item.sourceId, item.id);
    }
    return bindings;
  };
