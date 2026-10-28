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
  // Browser mode retains editable text; adapter-selected native ink stays authoritative in detached exports.
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
    // Automatic text can switch to an owned browser face live. Detached exports retain the same native glyph placement.
    for (const ink of copy.querySelectorAll('[data-cfx-owned-font-native]')) { ink.removeAttribute('display'); ink.style.removeProperty('display'); }
    for (const text of copy.querySelectorAll('[data-cfx-owned-font-text]')) text.remove();
    for (const defs of copy.querySelectorAll('[data-cfx-png-text-resources]')) {
      if (png) defs.removeAttribute('data-cfx-png-text-resources'); else defs.remove();
    }
  };
  const maximumRootBytes = 2 * 1024 * 1024, maximumDistinctBytes = 4 * 1024 * 1024, maximumRoots = 32;
  const registrations = new Map(), mounted = new WeakMap();
  const sameBytes = (a, b) => a.length === b.length && a.every((value, index) => value === b[index]);
  // The Website and generated sections share this binary-only registration primitive. It never fetches a face.
  const registerBinary = (family, borrowed, descriptors = {}) => {
    const weight = String(descriptors.weight || '400'), style = descriptors.style || 'normal';
    if (typeof family !== 'string' || !/^[A-Za-z][A-Za-z0-9 _.-]{0,127}$/.test(family)
      || !(borrowed instanceof Uint8Array) || !borrowed.length || borrowed.length > maximumRootBytes
      || !/^\d{1,4}$/.test(weight) || Number(weight) < 1 || Number(weight) > 1000 || !['normal', 'italic'].includes(style))
      throw new TypeError('Owned font registration requires bounded binary bytes and a finite family/weight/style.');
    if (typeof FontFace !== 'function' || !document.fonts) throw new TypeError('Binary FontFace registration is unavailable.');
    const identity = JSON.stringify([family, weight, style]);
    let entry = registrations.get(identity);
    if (entry && !sameBytes(entry.bytes, borrowed)) throw new TypeError('An owned font alias cannot change its captured bytes.');
    if (!entry) {
      const bytes = new Uint8Array(borrowed); // Detach the caller view before any asynchronous operation.
      const face = new FontFace(family, bytes, { weight, style });
      entry = { bytes, face, references: 0, ready: null }; registrations.set(identity, entry);
      entry.ready = face.load().then(loaded => { if (entry.references > 0) document.fonts.add(loaded); return loaded; });
      entry.ready.catch(() => {});
    }
    entry.references++;
    let active = true;
    return Object.freeze({ ready: entry.ready, release: () => {
      if (!active) return; active = false;
      if (--entry.references === 0) { document.fonts.delete(entry.face); if (registrations.get(identity) === entry) registrations.delete(identity); }
    } });
  };
  const records = root => Array.from(root.querySelectorAll('template[data-cfx-font-root],template[data-cfx-font-face]'));
  const copyBindings = (source, destination) => {
    for (const record of records(destination)) record.remove();
    for (const record of records(source)) destination.appendChild(record.cloneNode(true));
  };
  const attributesAre = (node, expected) => node.attributes.length === expected.length
    && expected.every(name => node.hasAttribute(name));
  const bindingsFor = (root, allowEmpty) => {
    const bindings = records(root), roots = new Map(), faces = [], aliases = new Set();
    let total = 0;
    for (const record of bindings.filter(node => node.hasAttribute('data-cfx-font-root'))) {
      const id = record.dataset.cfxFontRoot, length = Number(record.dataset.cfxFontByteLength), payload = record.content.textContent;
      if (!attributesAre(record, ['data-cfx-font-root', 'data-cfx-font-byte-length']) || !/^[a-f0-9]{64}$/.test(id || '')
        || !Number.isSafeInteger(length) || length < 1 || length > maximumRootBytes || roots.has(id)
        || roots.size >= maximumRoots || (total += length) > maximumDistinctBytes
        || Array.from(record.content.childNodes).some(node => node.nodeType !== 3))
        throw new TypeError('Owned HTML font roots require exact bounded identity/length records.');
      if (!payload && !allowEmpty) throw new TypeError('A normal owned HTML font root requires its captured binary bytes.');
      if (payload) {
        const alphabet = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/';
        if (payload.length > Math.ceil(maximumRootBytes / 3) * 4 || payload.length % 4
          || !/^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==|[A-Za-z0-9+/]{3}=)?$/.test(payload)
          || payload.length / 4 * 3 - (payload.endsWith('==') ? 2 : payload.endsWith('=') ? 1 : 0) !== length
          || payload.endsWith('==') && alphabet.indexOf(payload[payload.length - 3]) % 16 !== 0
          || !payload.endsWith('==') && payload.endsWith('=') && alphabet.indexOf(payload[payload.length - 2]) % 4 !== 0)
          throw new TypeError('Owned HTML font Base64 must retain its exact finite length and canonical padding.');
        const header = atob(payload.slice(0, 8));
        if (!(header.startsWith('OTTO') || header.startsWith('true') || header.charCodeAt(0) === 0 && header.charCodeAt(1) === 1 && header.charCodeAt(2) === 0 && header.charCodeAt(3) === 0))
          throw new TypeError('Owned HTML delivery requires an actual single sfnt face.');
      }
      roots.set(id, { id, length, payload, bytes: null, ready: null, referenced: false });
    }
    for (const record of bindings.filter(node => node.hasAttribute('data-cfx-font-face'))) {
      const alias = record.dataset.cfxFontFace, resource = roots.get(record.dataset.cfxFontResource);
      const weight = record.dataset.cfxFontWeight, style = record.dataset.cfxFontStyle;
      if (!attributesAre(record, ['data-cfx-font-face', 'data-cfx-font-resource', 'data-cfx-font-weight', 'data-cfx-font-style'])
        || record.content.childNodes.length || !/^cfx-owned-[a-f0-9]{64}$/.test(alias || '') || !resource || aliases.has(alias)
        || !/^\d{1,4}$/.test(weight || '') || Number(weight) < 1 || Number(weight) > 1000 || !['normal', 'italic'].includes(style)
        || faces.length >= maximumRoots * 2)
        throw new TypeError('Owned HTML font views require exact finite aliases, descriptors and admitted roots.');
      aliases.add(alias); resource.referenced = true; faces.push({ alias, resource, weight, style });
    }
    if ([...roots.values()].some(resource => !resource.referenced)) throw new TypeError('Foreign unreferenced HTML font root.');
    return { roots, faces };
  };
  const observation = (root, allowEmpty = false) => {
    bindingsFor(root, allowEmpty); // Never erase malformed/foreign records or their identity/descriptor differences.
    const copy = root.cloneNode(true);
    for (const record of records(copy)) if (record.hasAttribute('data-cfx-font-root')) record.content.replaceChildren();
    return copy;
  };
  const rootBytes = resource => resource.ready ||= (async () => {
    if (!resource.payload || !crypto?.subtle) throw new TypeError('Captured binary validation is unavailable.');
    const binary = atob(resource.payload), bytes = Uint8Array.from(binary, character => character.charCodeAt(0));
    const digest = await crypto.subtle.digest('SHA-256', bytes);
    const identity = Array.from(new Uint8Array(digest), value => value.toString(16).padStart(2, '0')).join('');
    if (identity !== resource.id) throw new TypeError('Owned HTML font bytes do not match their captured root identity.');
    resource.bytes = bytes; return bytes;
  })();
  const mount = (root, own) => {
    const previous = mounted.get(root);
    if (root.ownerDocument !== document) { previous?.(); return Promise.resolve(); }
    const releases = [], pending = [];
    const svg = root.querySelector('.cfx-stage svg') || root.querySelector('.cfx-topology-viewport svg') || root.querySelector('svg');
    let active = true;
    for (const ink of svg?.querySelectorAll('[data-cfx-owned-font-native]') || []) ink.removeAttribute('display');
    for (const text of svg?.querySelectorAll('[data-cfx-owned-font-text]') || []) text.setAttribute('display', 'none');
    const release = () => { if (!active) return; active = false; for (const handle of releases) handle.release(); };
    const cleanup = own ? own(release) : window[Symbol.for('ChartForgeX.adapterLifetimes')]?.create(root, 'owned-fonts').own(release) || release;
    mounted.set(root, cleanup);
    const current = () => active && root.isConnected && svg?.isConnected && root.contains(svg);
    const accept = (binding, handle) => {
      releases.push(handle);
      return handle.ready.then(() => {
        if (!current()) return;
        for (const text of svg.querySelectorAll('[data-cfx-owned-font-text]')) if (text.dataset.cfxOwnedFontText === binding.alias) {
          text.removeAttribute('display');
          for (const ink of text.parentElement.querySelectorAll('[data-cfx-owned-font-native]'))
            if (ink.dataset.cfxOwnedFontNative === binding.alias) ink.setAttribute('display', 'none');
        }
      });
    };
    try {
      const bindings = bindingsFor(root, true);
      for (const binding of bindings.faces) {
        const { alias, resource, weight, style } = binding, identity = JSON.stringify([alias, weight, style]);
        const known = registrations.get(identity);
        if (known && known.rootId === resource.id && known.bytes.length === resource.length
          && (!resource.payload || resource.payload === known.payload)) {
          // Acquire the exact current owned view before its predecessor releases it. No registry/path lookup occurs.
          pending.push(accept(binding, registerBinary(alias, known.bytes, { weight, style })).catch(() => {}));
        } else if (resource.payload) pending.push(rootBytes(resource).then(bytes => {
          if (!current()) return;
          const handle = registerBinary(alias, bytes, { weight, style });
          const entry = registrations.get(identity); entry.rootId = resource.id; entry.payload = resource.payload;
          return accept(binding, handle);
        }).catch(() => {}));
        // Missing bytes or an unavailable new face leave the canonical native ink visible.
      }
    } catch { release(); }
    previous?.();
    return Promise.all(pending);
  };
  window[key] = Object.freeze({ prepare, registerBinary, mount, copyBindings, observation });
})();
