  const graphThemeMedia = typeof window.matchMedia === 'function' ? window.matchMedia('(prefers-color-scheme: dark)') : null;
  const graphReducedMotionMedia = typeof window.matchMedia === 'function' ? window.matchMedia('(prefers-reduced-motion: reduce)') : null;
  const graphForcedColorsMedia = typeof window.matchMedia === 'function' ? window.matchMedia('(forced-colors: active)') : null;
  const graphThemeModes = ['system', 'light', 'dark'];
  const graphThemeStorageKey = 'cfx-graph-explorer-theme';
  const graphThemeMode = (root) => graphThemeModes.includes(attr(root, 'data-cfx-graph-theme')) ? attr(root, 'data-cfx-graph-theme') : 'system';
  const graphThemeActive = (mode) => mode === 'system' ? (graphThemeMedia?.matches ? 'dark' : 'light') : mode;
  const graphThemeStored = () => {
    try {
      const value = window.localStorage?.getItem(graphThemeStorageKey) || '';
      return graphThemeModes.includes(value) ? value : '';
    } catch {
      return '';
    }
  };
  const persistGraphTheme = (mode) => {
    try { window.localStorage?.setItem(graphThemeStorageKey, mode); } catch { /* Storage can be unavailable in sandboxed or private documents. */ }
  };
  const graphCssColor = (root, name, fallback) => {
    const value = typeof getComputedStyle === 'function' ? getComputedStyle(root).getPropertyValue(name).trim() : '';
    return value || fallback;
  };
  const graphThemePalette = (root) => {
    const dark = attr(root, 'data-cfx-graph-theme-active') === 'dark';
    return {
      dark,
      paper: graphCssColor(root, '--cfx-color-paper', dark ? '#0b1220' : '#ffffff'),
      paperSoft: graphCssColor(root, '--cfx-color-paper-soft', dark ? '#0d1728' : '#f8fbff'),
      halo: graphCssColor(root, '--cfx-color-graph-halo', dark ? '#0b1220' : '#ffffff'),
      text: graphCssColor(root, '--cfx-color-graph-text', dark ? '#e5edf8' : '#334155'),
      muted: graphCssColor(root, '--cfx-color-graph-muted', dark ? '#a9b7ca' : '#64748b'),
      edge: graphCssColor(root, '--cfx-color-edge', dark ? '#63738b' : '#8796aa'),
      edgeLabel: graphCssColor(root, '--cfx-color-edge-label', dark ? '#d2dbea' : '#44546a'),
      clusterFill: graphCssColor(root, '--cfx-color-cluster-fill', dark ? 'rgba(7,89,133,.42)' : 'rgba(224,242,254,.84)'),
      clusterStroke: graphCssColor(root, '--cfx-color-cluster-stroke', dark ? '#38bdf8' : '#0284c7'),
      clusterText: graphCssColor(root, '--cfx-color-cluster-text', dark ? '#bae6fd' : '#075985'),
      selected: graphCssColor(root, '--cfx-color-selected', dark ? '#fbbf24' : '#d97706')
    };
  };
  const graphColorRgb = (value) => {
    const source = String(value || '').trim();
    if (/^#[0-9a-f]{3}$/i.test(source)) return source.slice(1).split('').map(part => parseInt(part + part, 16));
    if (/^#[0-9a-f]{6}$/i.test(source)) return [parseInt(source.slice(1, 3), 16), parseInt(source.slice(3, 5), 16), parseInt(source.slice(5, 7), 16)];
    const match = source.match(/^rgba?\(\s*([\d.]+)[, ]+([\d.]+)[, ]+([\d.]+)/i);
    return match ? [Number(match[1]), Number(match[2]), Number(match[3])] : null;
  };
  const graphColorLuminance = (rgb) => {
    const linear = rgb.map(channel => {
      const value = Math.max(0, Math.min(255, channel)) / 255;
      return value <= .04045 ? value / 12.92 : Math.pow((value + .055) / 1.055, 2.4);
    });
    return linear[0] * .2126 + linear[1] * .7152 + linear[2] * .0722;
  };
  const graphColorContrast = (first, second) => {
    const a = graphColorRgb(first), b = graphColorRgb(second);
    if (!a || !b) return Number.POSITIVE_INFINITY;
    const firstLuminance = graphColorLuminance(a), secondLuminance = graphColorLuminance(b);
    return (Math.max(firstLuminance, secondLuminance) + .05) / (Math.min(firstLuminance, secondLuminance) + .05);
  };
  const graphAdaptiveTextColor = (root, requested, fallback) => {
    const value = String(requested || '').trim();
    if (!value) return fallback;
    return graphColorContrast(value, graphThemePalette(root).paper) >= 4.5 ? value : fallback;
  };
  const syncSvgThemeColors = (root) => {
    const palette = graphThemePalette(root);
    const physical = (selector) => Array.from(root.querySelectorAll(selector));
    const nodeDetails = new Map(physical('[data-cfx-role="graph-node-details"]').map(details => [attr(details, 'data-node-details-for'), details]));
    physical('[data-cfx-role="graph-node"]').forEach(node => {
      const color = graphAdaptiveTextColor(root, attr(node, 'data-node-label-color'), palette.text);
      node.style.setProperty('--cfx-node-label-adaptive', color);
      nodeDetails.get(attr(node, 'data-node-id'))?.style.setProperty('--cfx-node-label-adaptive', color);
    });
    const edgeLabels = new Map(physical('[data-cfx-role="graph-edge-label"]').map(label => [attr(label, 'data-edge-label-for'), label]));
    physical('[data-cfx-role="graph-edge"]').forEach(edge => edgeLabels.get(attr(edge, 'data-edge-id'))?.style.setProperty('--cfx-edge-label-adaptive', graphAdaptiveTextColor(root, attr(edge, 'data-edge-label-color'), palette.edgeLabel)));
  };
  const graphPrefersReducedMotion = (root) => attr(root, 'data-cfx-graph-reduced-motion') === 'true' || graphReducedMotionMedia?.matches === true;
  const syncGraphThemeControl = (root, mode, active) => {
    const button = root.querySelector("[data-cfx-graph-action='theme']");
    if (!button) return;
    const label = button.querySelector('.cfx-graph-tool-label');
    const visible = mode[0].toUpperCase() + mode.slice(1);
    const next = graphThemeModes[(graphThemeModes.indexOf(mode) + 1) % graphThemeModes.length];
    if (label) label.textContent = visible;
    button.setAttribute('aria-label', `Color theme: ${visible}. Activate to use ${next} theme.`);
    button.setAttribute('data-cfx-tooltip', `Color theme: ${visible} (${active} colors)`);
    button.setAttribute('data-cfx-graph-theme-mode', mode);
  };
  const redrawGraphTheme = (root) => {
    syncSvgThemeColors(root);
    const state = root.__cfxGraphState;
    if (!state) return;
    drawCanvas(root, state);
    if (typeof updateOverview === 'function') updateOverview(root, state);
  };
  const setGraphTheme = (root, requested, options) => {
    const mode = graphThemeModes.includes(String(requested || '').toLowerCase()) ? String(requested).toLowerCase() : 'system';
    const active = graphThemeActive(mode);
    root.setAttribute('data-cfx-graph-theme', mode);
    root.setAttribute('data-cfx-graph-theme-active', active);
    const shell = root.closest('.cfx-graph-shell');
    if (shell) shell.classList.toggle('cfx-graph-page-dark', active === 'dark');
    syncGraphThemeControl(root, mode, active);
    if (options?.persist !== false && attr(root, 'data-cfx-graph-theme-persist') !== 'false') persistGraphTheme(mode);
    redrawGraphTheme(root);
    const detail = { graphId: attr(root, 'data-cfx-graph-id'), mode, active };
    if (options?.announce !== false) {
      const announcer = root.querySelector('[data-cfx-role="graph-announcer"]');
      if (announcer) announcer.textContent = `${active[0].toUpperCase() + active.slice(1)} color theme enabled.`;
    }
    emit(root, 'cfxgraphthemechange', detail);
    return detail;
  };
  const syncGraphAccessibilityPreferences = (root) => {
    root.setAttribute('data-cfx-graph-reduced-motion', graphReducedMotionMedia?.matches ? 'true' : 'false');
    root.setAttribute('data-cfx-graph-forced-colors', graphForcedColorsMedia?.matches ? 'true' : 'false');
  };
  const bindGraphTheme = (root) => {
    const configured = graphThemeMode(root);
    const stored = configured === 'system' && attr(root, 'data-cfx-graph-theme-persist') !== 'false' ? graphThemeStored() : '';
    syncGraphAccessibilityPreferences(root);
    setGraphTheme(root, stored || configured, { persist: false, announce: false });
    root.querySelector("[data-cfx-graph-action='theme']")?.addEventListener('click', () => {
      const current = graphThemeMode(root);
      setGraphTheme(root, graphThemeModes[(graphThemeModes.indexOf(current) + 1) % graphThemeModes.length]);
    });
    const syncSystemTheme = () => {
      syncGraphAccessibilityPreferences(root);
      if (graphThemeMode(root) === 'system') setGraphTheme(root, 'system', { persist: false, announce: false });
      else redrawGraphTheme(root);
    };
    graphThemeMedia?.addEventListener?.('change', syncSystemTheme);
    graphReducedMotionMedia?.addEventListener?.('change', syncSystemTheme);
    graphForcedColorsMedia?.addEventListener?.('change', syncSystemTheme);
  };
