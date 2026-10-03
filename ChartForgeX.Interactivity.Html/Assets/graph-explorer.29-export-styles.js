  const applyGraphExportStyles = (root, svg, clone) => {
    // The standalone SVG no longer inherits typography from its HTML host.
    const inherited = root.ownerDocument.defaultView?.getComputedStyle(svg);
    ['font-family', 'font-size', 'font-weight', 'font-style', 'color'].forEach(property => {
      const value = inherited?.getPropertyValue(property);
      if (value) clone.style.setProperty(property, value);
    });
    const styleSource = root.ownerDocument.querySelector('style[data-cfx-graph-assets="true"]')
      || Array.from(root.ownerDocument.querySelectorAll('style')).find(style => (style.textContent || '').includes('.cfx-graph-explorer'));
    const externalStyle = root.ownerDocument.querySelector('link[data-cfx-graph-assets="true"]');
    let exportCss = styleSource?.textContent || '';
    if (!exportCss && externalStyle?.sheet) {
      try {
        exportCss = Array.from(externalStyle.sheet.cssRules, rule => rule.cssText).join('\n');
      } catch (_) {
        // A cross-origin stylesheet can render normally while its rules are inaccessible to script.
      }
    }
    if (exportCss) {
      const style = root.ownerDocument.createElementNS('http:' + '//www.w3.org/2000/svg', 'style');
      style.setAttribute('data-cfx-export-style', 'true');
      style.textContent = exportCss;
      const defs = clone.querySelector('defs');
      if (defs) defs.insertBefore(style, defs.firstChild);
      else clone.insertBefore(style, clone.firstChild);
      return;
    }
    if (!externalStyle) return;

    // A cross-origin stylesheet can paint the page without granting CSSOM access. Materialize
    // the SVG's computed paints while the clone is briefly in the same styled graph stage.
    const stage = svg.parentNode;
    const properties = ['fill', 'fill-opacity', 'stroke', 'stroke-opacity', 'stroke-width',
      'stroke-dasharray', 'stroke-linecap', 'stroke-linejoin', 'clip-path', 'filter', 'opacity', 'display',
      'visibility', 'font-family', 'font-size', 'font-weight', 'font-style', 'color',
      'text-anchor', 'dominant-baseline', 'paint-order', 'vector-effect', 'marker-start',
      'marker-mid', 'marker-end'];
    clone.style.position = 'absolute';
    clone.style.left = '-100000px';
    clone.style.top = '0';
    clone.style.display = 'block';
    clone.style.opacity = '1';
    stage.appendChild(clone);
    try {
      [clone, ...clone.querySelectorAll('*')].forEach(element => {
        if (element.localName === 'style') return;
        const computed = root.ownerDocument.defaultView?.getComputedStyle(element);
        if (!computed) return;
        properties.forEach(property => {
          if (element === clone && (property === 'opacity' || property === 'display')) return;
          const value = computed.getPropertyValue(property);
          if (value) element.style.setProperty(property, value);
        });
      });
    } finally {
      clone.remove();
      clone.style.removeProperty('position');
      clone.style.removeProperty('left');
      clone.style.removeProperty('top');
    }
  };
