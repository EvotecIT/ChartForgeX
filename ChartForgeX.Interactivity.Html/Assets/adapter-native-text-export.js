(() => {
  const key = Symbol.for('ChartForgeX.nativeTextExport');
  if (window[key]) return;
  const properties = ['color', 'display', 'visibility', 'opacity', 'fill', 'fill-opacity', 'fill-rule',
    'stroke', 'stroke-width', 'stroke-opacity', 'stroke-dasharray', 'stroke-dashoffset', 'stroke-linecap',
    'stroke-linejoin', 'stroke-miterlimit', 'paint-order', 'vector-effect', 'clip-path', 'clip-rule', 'filter',
    'marker-start', 'marker-mid', 'marker-end', 'stop-color', 'stop-opacity', 'flood-color', 'flood-opacity',
    'font-family', 'font-size', 'font-weight', 'font-style', 'font-stretch', 'font-variant', 'letter-spacing',
    'word-spacing', 'text-anchor', 'dominant-baseline', 'text-decoration-line', 'text-decoration-color',
    'text-decoration-style', 'text-decoration-thickness'];
  // The source remains editable text. Only the detached PNG composition consumes native contours.
  const prepare = (source, copy, png) => {
    const sources = [source, ...source.querySelectorAll('*')], copies = [copy, ...copy.querySelectorAll('*')];
    const ids = new Set(Array.from(source.querySelectorAll('[id]'), node => node.id));
    const resources = new Set(Array.from(copy.querySelectorAll('[data-cfx-png-text-resources] path[id], [data-cfx-native-text-resources] path[id]'), node => node.id));
    const decorations = new Map();
    sources.forEach((node, index) => {
      let target = copies[index];
      let decorated = decorations.get(node.parentElement) === true;
      // Resource paths intentionally inherit the accepted text paint from their eventual <use>.
      if (!node.closest('[data-cfx-png-text-resources], [data-cfx-native-text-resources]')) {
        const style = node.ownerDocument.defaultView.getComputedStyle(node);
        for (const property of properties) {
          const value = style.getPropertyValue(property).replace(/url\(["']?[^)"']*#([^)"']+)["']?\)/g,
            (match, id) => ids.has(id) ? 'url(#' + id + ')' : match);
          if (value) target.style.setProperty(property, value);
          if (property === 'text-decoration-line' && value && value !== 'none') decorated = true;
        }
      }
      // SVG decorations propagate from ancestors even when the text's computed line is none.
      decorations.set(node, decorated);
      const captured = target.getAttribute('data-cfx-png-glyphs');
      // Detached SVG images may use a fallback face. Keep the accepted live advance for plain decorated text.
      if (png && target.localName === 'text' && decorated && !node.firstElementChild
        && !node.hasAttribute('textLength') && !node.hasAttribute('lengthAdjust')) {
        const advance = node.getComputedTextLength?.();
        if (Number.isFinite(advance) && advance > 0) {
          target.setAttribute('textLength', String(advance)); target.setAttribute('lengthAdjust', 'spacingAndGlyphs');
        }
      }
      if (png && target.localName === 'text' && captured !== null && !decorated) {
        const glyphs = captured ? captured.split('|').map(value => value.split(';')) : [];
        if (glyphs.every(([id, transform, extra]) => resources.has(id) && !extra
          && /^matrix\([-+0-9.eE ]+\)$/.test(transform))) {
          const group = target.ownerDocument.createElementNS('http://www.w3.org/2000/svg', 'g');
          for (const attribute of target.attributes) group.setAttributeNS(attribute.namespaceURI, attribute.name, attribute.value);
          for (const name of ['x', 'y', 'dx', 'dy', 'textLength', 'lengthAdjust']) group.removeAttribute(name);
          group.style.setProperty('fill-rule', 'nonzero');
          for (const [id, transform] of glyphs) {
            const use = target.ownerDocument.createElementNS('http://www.w3.org/2000/svg', 'use');
            use.setAttribute('href', '#' + id); use.setAttribute('transform', transform); group.appendChild(use);
          }
          target.replaceWith(group); target = group;
        }
      }
      target.removeAttribute('data-cfx-png-glyphs'); target.removeAttribute('data-cfx-png-font-fallback');
    });
    for (const defs of copy.querySelectorAll('[data-cfx-png-text-resources]')) {
      if (png) defs.removeAttribute('data-cfx-png-text-resources'); else defs.remove();
    }
  };
  window[key] = Object.freeze({ prepare });
})();
