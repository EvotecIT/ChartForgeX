  // Exports retain the committed native scene and persistent reader state. The standalone
  // geometric camera, hover, focus outlines and focus trails are browser presentation only.
  const withPersistentExportPaint = (root, svg, read) => {
    const transientClasses = ['cfx-hovered', 'cfx-hover-related', 'cfx-hover-column', 'cfx-hover-series', 'cfx-trail'];
    const classes = Array.from(svg.querySelectorAll('[class]')).map(node => [node, node.getAttribute('class')]);
    const attributes = ['data-cfx-hovering', 'data-cfx-hover-mode', 'data-cfx-hover-unit'];
    const rootState = attributes.map(name => [name, root.getAttribute(name)]);
    const branchPaint = Array.from(svg.querySelectorAll('[data-cfx-highlight-paint]'))
      .map(node => [node, node.getAttribute('style')]);
    try {
      for (const [node] of classes) node.classList.remove(...transientClasses);
      for (const name of attributes) root.removeAttribute(name);
      for (const [node] of branchPaint) node.style.removeProperty('--cfx-highlight-opacity');
      return read();
    } finally {
      for (const [node, value] of classes) node.setAttribute('class', value);
      for (const [name, value] of rootState) {
        if (value === null) root.removeAttribute(name); else root.setAttribute(name, value);
      }
      for (const [node, value] of branchPaint) {
        if (value === null) node.removeAttribute('style'); else node.setAttribute('style', value);
      }
    }
  };
  const exportPaintProperties = ['color', 'display', 'visibility', 'opacity', 'fill', 'fill-opacity', 'fill-rule',
    'stroke', 'stroke-width', 'stroke-opacity', 'stroke-dasharray', 'stroke-dashoffset', 'stroke-linecap',
    'stroke-linejoin', 'stroke-miterlimit', 'paint-order', 'vector-effect', 'clip-path', 'clip-rule', 'filter',
    'marker-start', 'marker-mid', 'marker-end', 'stop-color', 'stop-opacity', 'flood-color', 'flood-opacity',
    'font-family', 'font-size', 'font-weight', 'font-style', 'font-stretch', 'font-variant', 'letter-spacing',
    'word-spacing', 'text-anchor', 'dominant-baseline', 'text-decoration-line', 'text-decoration-color',
    'text-decoration-style', 'text-decoration-thickness'];
  const serializeSvg = (root) => {
    settleChartUpdateMotion(root);
    const svg = root.querySelector('.cfx-stage svg');
    if (!svg) return null;
    return withPersistentExportPaint(root, svg, () => {
      const copy = svg.cloneNode(true), sources = [svg, ...svg.querySelectorAll('*')], copies = [copy, ...copy.querySelectorAll('*')];
      const ids = new Set(Array.from(svg.querySelectorAll('[id]'), node => node.id));
      sources.forEach((source, index) => {
        const target = copies[index], style = getComputedStyle(source);
        for (const property of exportPaintProperties) {
          // Browser computed URLs are absolute; retain local resources inside this standalone SVG.
          const value = style.getPropertyValue(property).replace(/url\(["']?[^)"']*#([^)"']+)["']?\)/g,
            (match, id) => ids.has(id) ? 'url(#' + id + ')' : match);
          if (value) target.style.setProperty(property, value);
        }
        for (const attribute of ['tabindex', 'data-cfx-keyboard-component', 'data-cfx-trail-index', 'data-cfx-highlight-state'])
          target.removeAttribute(attribute);
      });
      copy.style.removeProperty('transform'); copy.style.removeProperty('transition');
      copy.querySelectorAll('[data-cfx-browser-hit-area],[data-cfx-highlight-outline],[data-cfx-keyboard-focus-outline],[data-cfx-annotation-editor]').forEach(node => node.remove());
      return { svg: copy, data: new XMLSerializer().serializeToString(copy) };
    });
  };
  const downloadBlob = (root, blob, extension, format) => {
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = (root.dataset.cfxChartId || 'chart') + '.' + extension;
    document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(url);
    emitHostEvent(root, 'cfxexport', { format });
  };
  const svgSize = (svg) => {
    const viewBox = svg.viewBox && svg.viewBox.baseVal ? svg.viewBox.baseVal : null;
    const rect = svg.getBoundingClientRect ? svg.getBoundingClientRect() : { width: 0, height: 0 };
    const width = Math.max(1, Math.round((viewBox && viewBox.width) || (svg.width && svg.width.baseVal ? svg.width.baseVal.value : 0) || rect.width || 800));
    const height = Math.max(1, Math.round((viewBox && viewBox.height) || (svg.height && svg.height.baseVal ? svg.height.baseVal.value : 0) || rect.height || 450));
    return { width, height };
  };
  const exportSvg = (root) => {
    if (!hasFeature(root, 'Export')) return;
    const serialized = serializeSvg(root);
    if (!serialized) return;
    downloadBlob(root, new Blob([serialized.data], { type: 'image/svg+xml;charset=utf-8' }), 'svg', 'svg');
  };
  const exportPng = (root) => {
    if (!hasFeature(root, 'Export')) return;
    const serialized = serializeSvg(root);
    if (!serialized) return;
    const source = new Blob([serialized.data], { type: 'image/svg+xml;charset=utf-8' });
    const sourceUrl = URL.createObjectURL(source);
    const image = new Image();
    let canceled = false;
    const release = ownChartWork(root, () => {
      canceled = true; image.onload = null; image.onerror = null; image.src = ''; URL.revokeObjectURL(sourceUrl);
    });
    image.onload = () => {
      if (canceled) return;
      const size = svgSize(serialized.svg);
      const scale = Math.max(1, Math.ceil(window.devicePixelRatio || 1));
      const canvas = document.createElement('canvas');
      canvas.width = size.width * scale;
      canvas.height = size.height * scale;
      const context = canvas.getContext('2d');
      if (!context) {
        release();
        return;
      }
      context.setTransform(scale, 0, 0, scale, 0, 0);
      context.clearRect(0, 0, size.width, size.height);
      context.drawImage(image, 0, 0, size.width, size.height);
      canvas.toBlob((blob) => {
        if (canceled) return;
        release();
        if (blob && root.isConnected) downloadBlob(root, blob, 'png', 'png');
      }, 'image/png');
    };
    image.onerror = release;
    image.src = sourceUrl;
  };
