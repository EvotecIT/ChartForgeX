  const graphVirtualClassList = values => ({
    add: (...names) => names.forEach(name => values.add(name)),
    remove: (...names) => names.forEach(name => values.delete(name)),
    contains: name => values.has(name),
    toggle: (name, force) => {
      const enabled = force === undefined ? !values.has(name) : !!force;
      if (enabled) values.add(name); else values.delete(name);
      return enabled;
    },
    toString: () => Array.from(values).join(' ')
  });
  const graphVirtualElement = (role, attributes, classes) => {
    const values = { 'data-cfx-role': role, ...attributes };
    const classValues = new Set(classes || []);
    const element = {
      __cfxVirtual: true,
      __cfxRemoved: false,
      classList: graphVirtualClassList(classValues),
      getAttribute: name => name === 'class' ? Array.from(classValues).join(' ') : values[name] ?? null,
      setAttribute: (name, value) => { if (name === 'class') { classValues.clear(); String(value).split(/\s+/).filter(Boolean).forEach(item => classValues.add(item)); } else values[name] = String(value); },
      removeAttribute: name => { if (name === 'class') classValues.clear(); else delete values[name]; },
      querySelector: () => null,
      addEventListener: () => {},
      remove: () => { element.__cfxRemoved = true; }
    };
    return element;
  };
  const graphVirtualMatches = (element, selector) => selector.split(',').some(raw => {
    const part = raw.trim();
    const role = part.match(/\[data-cfx-role=["']?([^"'\]]+)/)?.[1];
    if (role && attr(element, 'data-cfx-role') !== role) return false;
    const attributes = Array.from(part.matchAll(/\[([^=\]]+)=['"]?([^'"\]]+)/g));
    if (attributes.some(match => attr(element, match[1]) !== match[2])) return false;
    const classes = Array.from(part.matchAll(/\.([a-zA-Z0-9_-]+)/g), match => match[1]);
    return classes.every(name => element.classList.contains(name));
  });
  const graphDocumentAttributes = (row, role) => role === 'graph-node' ? {
    'data-node-id': row[0], 'data-node-label': row[1], 'data-node-kind': row[2], 'data-node-group': row[3], 'data-node-cluster': row[4], 'data-node-parent': row[5], 'data-cfx-status': row[6], 'data-node-size': row[7], 'data-node-fixed': row[8], 'data-node-hidden': row[9], 'data-node-level': row[10], 'data-node-shape': row[11], 'data-node-image-url': row[12], 'data-node-image-alt': row[13], 'data-node-icon': row[14], 'data-node-secondary-label': row[15], 'data-node-badge': row[16], 'data-node-background-color': row[17], 'data-node-border-color': row[18], 'data-node-label-color': row[19], 'data-node-label-background-color': row[20], 'data-node-shadow': row[21], 'data-cfx-search': row[22], 'data-cfx-metadata': row[23], 'data-node-x': row[24], 'data-node-y': row[25], transform: `translate(${row[24]} ${row[25]})`, tabindex: '-1', role: 'button', 'aria-pressed': 'false', 'aria-label': row[1]
  } : {
    'data-edge-id': row[0], 'data-edge-label': row[1], 'data-edge-kind': row[2], 'data-cfx-status': row[3], 'data-source-node-id': row[4], 'data-target-node-id': row[5], 'data-source-cluster-id': row[6], 'data-target-cluster-id': row[7], 'data-edge-weight': row[8], 'data-edge-length': row[9], 'data-edge-directed': row[10], 'data-edge-source-arrow': row[11], 'data-edge-target-arrow': row[12], 'data-edge-shape': row[13], 'data-edge-curvature': row[14], 'data-edge-route-points': row[15], 'data-edge-dashed': row[16], 'data-edge-dash-pattern': row[17], 'data-edge-show-label': row[18], 'data-edge-width': row[19], 'data-edge-color': row[20], 'data-edge-label-color': row[21], 'data-edge-physics': row[22], 'data-edge-hidden': row[23], 'data-cfx-search': row[24], 'data-cfx-metadata': row[25], tabindex: '-1', role: 'button', 'aria-pressed': 'false', 'aria-label': row[27], 'marker-start': row[28], 'marker-end': row[29]
  };
  const cleanGraphDocumentAttributes = attributes => Object.fromEntries(Object.entries(attributes).filter(([, value]) => value !== null && value !== undefined && value !== '').map(([key, value]) => [key, typeof value === 'boolean' ? (value ? 'true' : 'false') : String(value)]));
  const ensureGraphDocument = root => {
    if (root.__cfxGraphVirtualItems) return root.__cfxGraphVirtualItems;
    const source = root.querySelector('[data-cfx-role="graph-document"]');
    if (!source) return [];
    const data = JSON.parse(source.textContent || '{}');
    const nodes = (data.n || []).map(row => graphVirtualElement('graph-node', cleanGraphDocumentAttributes(graphDocumentAttributes(row, 'graph-node')), ['cfx-graph-node', ...(row[9] ? ['cfx-graph-hidden'] : []), ...(row[26] ? ['cfx-graph-cluster-collapsed-member'] : [])]));
    const edges = (data.e || []).map(row => graphVirtualElement('graph-edge', cleanGraphDocumentAttributes(graphDocumentAttributes(row, 'graph-edge')), ['cfx-graph-edge', ...(row[23] ? ['cfx-graph-hidden'] : []), ...(row[26] ? ['cfx-graph-cluster-collapsed-member'] : [])]));
    root.__cfxGraphVirtualItems = [...nodes, ...edges];
    return root.__cfxGraphVirtualItems;
  };
