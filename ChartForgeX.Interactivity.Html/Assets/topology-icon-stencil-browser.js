(() => {
  const root = document.querySelector('[data-cfx-icon-browser="true"]');
  if (!root) return;
  const search = root.querySelector('[data-cfx-icon-search]');
  const count = root.querySelector('[data-cfx-icon-count]');
  const nodes = Array.from(root.querySelectorAll('[data-cfx-role="topology-node"]'));
  const badges = Array.from(root.querySelectorAll('[data-cfx-role="topology-node-status"]'));
  const groups = Array.from(root.querySelectorAll('[data-cfx-role="topology-group"]'));
  const state = { vendor: '', pack: '', category: '', search: search ? search.value.toLowerCase().trim() : '' };
  const attr = (element, name) => element.getAttribute(name) || '';
  const meta = (element, key) => attr(element, 'data-cfx-meta-' + key);
  const detail = name => root.querySelector('[data-cfx-icon-detail="' + name + '"]');
  const setText = (name, value) => { const target = detail(name); if (target) target.textContent = value || '-'; };
  const textFor = element => [
    attr(element, 'data-node-icon-id'),
    attr(element, 'data-node-icon-label'),
    attr(element, 'data-node-icon-pack'),
    attr(element, 'data-node-icon-shape'),
    attr(element, 'data-node-kind'),
    meta(element, 'category'),
    meta(element, 'vendor'),
    meta(element, 'pack-label'),
    meta(element, 'icon-tags'),
    meta(element, 'icon-source-path'),
    meta(element, 'pack-source-url'),
    meta(element, 'pack-source-license')
  ].join(' ').toLowerCase();
  const nodeVisible = node => {
    if (state.vendor && meta(node, 'vendor') !== state.vendor) return false;
    if (state.pack && attr(node, 'data-node-icon-pack') !== state.pack) return false;
    if (state.category && meta(node, 'category') !== state.category) return false;
    return !state.search || textFor(node).includes(state.search);
  };
  const syncButtons = kind => {
    root.querySelectorAll('[data-cfx-icon-filter="' + kind + '"]').forEach(button => {
      button.setAttribute('aria-pressed', attr(button, 'data-cfx-icon-filter-value') === state[kind] ? 'true' : 'false');
    });
  };
  const apply = () => {
    let visible = 0;
    let firstVisible = null;
    const visibleNodeIds = new Set();
    const visibleByPack = new Set();
    for (const node of nodes) {
      const show = nodeVisible(node);
      node.classList.toggle('cfx-icon-browser-hidden', !show);
      if (show) {
        visible++;
        if (!firstVisible) firstVisible = node;
        visibleNodeIds.add(attr(node, 'data-node-id'));
        visibleByPack.add(attr(node, 'data-node-icon-pack'));
      }
    }
    for (const badge of badges) badge.classList.toggle('cfx-icon-browser-hidden', !visibleNodeIds.has(attr(badge, 'data-node-id')));
    for (const group of groups) {
      const pack = attr(group, 'data-group-icon-id').split(':')[0] || meta(group, 'packid');
      group.classList.toggle('cfx-icon-browser-hidden', pack && !visibleByPack.has(pack));
    }
    if (count) count.textContent = visible + ' icons';
    if (firstVisible && (state.search || state.vendor || state.pack || state.category)) {
      window.requestAnimationFrame(() => firstVisible.scrollIntoView({ block: 'nearest', inline: 'center' }));
    }
  };
  const select = node => {
    nodes.forEach(item => item.classList.toggle('cfx-icon-browser-selected', item === node));
    const payload = {
      iconId: attr(node, 'data-node-icon-id'),
      iconPack: attr(node, 'data-node-icon-pack'),
      iconLabel: attr(node, 'data-node-icon-label'),
      iconShape: attr(node, 'data-node-icon-shape'),
      iconArtwork: attr(node, 'data-node-icon-artwork') || meta(node, 'icon-artwork'),
      nodeKind: attr(node, 'data-node-kind'),
      category: meta(node, 'category'),
      vendor: meta(node, 'vendor'),
      packLabel: meta(node, 'pack-label'),
      tags: meta(node, 'icon-tags'),
      sourcePath: meta(node, 'icon-source-path'),
      sourceUrl: meta(node, 'pack-source-url'),
      sourceRevision: meta(node, 'icon-source-revision') || meta(node, 'pack-source-revision'),
      sourceLicense: meta(node, 'pack-source-license'),
      sourceLicenseUrl: meta(node, 'pack-source-licenseurl')
    };
    setText('label', payload.iconLabel);
    setText('id', payload.iconId);
    setText('pack', payload.packLabel || payload.iconPack);
    setText('vendor', payload.vendor);
    setText('category', payload.category);
    setText('shape', payload.iconShape);
    setText('artwork', payload.iconArtwork || 'fallback shape');
    setText('source', payload.sourcePath || payload.sourceUrl);
    setText('revision', payload.sourceRevision);
    setText('license', payload.sourceLicense);
    root.dispatchEvent(new CustomEvent('cfx-icon-browser-select', { bubbles: true, detail: payload }));
  };
  root.querySelectorAll('[data-cfx-icon-filter]').forEach(button => {
    button.addEventListener('click', () => {
      const kind = attr(button, 'data-cfx-icon-filter');
      state[kind] = attr(button, 'data-cfx-icon-filter-value');
      syncButtons(kind);
      apply();
    });
  });
  if (search) search.addEventListener('input', () => {
    state.search = search.value.toLowerCase().trim();
    apply();
  });
  nodes.forEach(node => {
    node.setAttribute('tabindex', '0');
    node.addEventListener('click', () => select(node));
    node.addEventListener('keydown', event => {
      if (event.key === 'Enter' || event.key === ' ') {
        event.preventDefault();
        select(node);
      }
    });
  });
  apply();
})();
