// ChartForgeX drill-down feature: configured bar and line categories open child charts, with breadcrumbs back.
(() => {
  const registry = window[Symbol.for('ChartForgeX.chartFeatures')] ||= {};
  if (registry.drilldown) return;
  const format = (template, values) => String(template).replace(/\{(\w+)\}/g, (match, name) => values[name] ?? match);
  const text = (config, key, fallback, values) => format(config?.text?.[key] || fallback, values || {});
  // One trail per top-level chart. Level 0 is the parent; each later level is one opened child section.
  const trails = new WeakMap();
  const pathOf = trail => trail.levels.map(level => ({ id: level.target?.id ?? null, label: level.label }));
  const announce = (trail, message) => {
    const status = trail.status;
    status.textContent = '';
    requestAnimationFrame(() => { if (!trail.disposed) status.textContent = message; });
  };
  const emit = (trail, k, name, detail) => k.emitHostEvent(trail.top, name, { level: trail.levels.length - 1, path: pathOf(trail), ...detail });
  const current = trail => trail.levels[trail.levels.length - 1];
  // Breadcrumbs live in the visible section so they take its tokens; the parent level shows none.
  const renderBreadcrumbs = (trail, k) => {
    trail.nav?.remove(); trail.nav = null;
    if (trail.levels.length < 2) return;
    const root = current(trail).root, frame = root.querySelector('.cfx-frame'), stage = root.querySelector('.cfx-stage');
    if (!frame || !stage) return;
    const nav = document.createElement('nav'), list = document.createElement('ol');
    nav.className = 'cfx-hierarchy-breadcrumb cfx-drill-breadcrumb'; nav.dataset.cfxDrillBreadcrumbs = 'true';
    nav.setAttribute('aria-label', text(trail.config, 'DrilldownNavigation', 'Drill-down path'));
    trail.levels.forEach((level, index) => {
      const item = document.createElement('li'), button = document.createElement('button');
      button.type = 'button'; button.className = 'cfx-tool'; button.textContent = level.label; button.dataset.cfxDrillLevel = String(index);
      if (index === trail.levels.length - 1) button.setAttribute('aria-current', 'page');
      else button.addEventListener('click', () => back(trail, k, index));
      item.appendChild(button); list.appendChild(item);
    });
    nav.appendChild(list); stage.before(nav); trail.nav = nav;
  };
  const release = (trail, k, level) => { trails.delete(level.root); k.releaseChart(level.root); level.root.remove(); };
  const createTrail = (root, k) => {
    const config = k.drillConfig(root), status = document.createElement('div');
    status.className = 'cfx-drill-status'; status.dataset.cfxDrillStatus = 'true';
    status.setAttribute('role', 'status'); status.setAttribute('aria-live', 'polite');
    root.before(status);
    const trail = { top: root, k, config, status, nav: null, pending: null, disposed: false,
      levels: [{ root, label: config.label, target: null, view: null, focus: null }] };
    trails.set(root, trail);
    k.ownChartWork(root, () => {
      trail.disposed = true; trail.pending = null;
      while (trail.levels.length > 1) release(trail, k, trail.levels.pop());
      delete root.dataset.cfxDrillHidden; status.remove(); trails.delete(root);
    });
    return trail;
  };
  const show = (trail, k, section, target) => {
    const parent = current(trail).root;
    section.dataset.cfxDrillLevel = String(trail.levels.length);
    parent.dataset.cfxDrillHidden = 'true'; parent.after(section);
    try { k.mountChart(section); } catch (error) {
      section.remove(); delete parent.dataset.cfxDrillHidden;
      announce(trail, text(trail.config, 'DrilldownFailed', '{label} could not be loaded.', { label: target.label }));
      throw error;
    }
    trail.levels.push({ root: section, label: target.label, target, view: null, focus: null });
    trails.set(section, trail);
    // An adaptive child swaps its drawing in place; its breadcrumbs follow it.
    section.addEventListener('cfxlayoutchange', event => { if (event.target === section && current(trail).root === section) renderBreadcrumbs(trail, k); });
    renderBreadcrumbs(trail, k);
    trail.nav?.querySelector('[aria-current]')?.focus({ preventScroll: true });
    announce(trail, text(trail.config, 'DrilldownOpened', 'Showing {label}, level {level} of {count}',
      { label: target.label, level: trail.levels.length, count: trail.levels.length }));
    emit(trail, k, 'cfxdrill', { action: 'open', targetId: target.id, category: target.category });
  };
  const back = (trail, k, index) => {
    if (trail.disposed || index < 0 || index >= trail.levels.length - 1) return false;
    trail.pending = null;
    while (trail.levels.length - 1 > index) release(trail, k, trail.levels.pop());
    const level = current(trail);
    delete level.root.dataset.cfxDrillHidden;
    k.restoreView(level.root, level.view);
    renderBreadcrumbs(trail, k);
    if (!k.focusTarget(level.root, level.focus)) trail.nav?.querySelector('[aria-current]')?.focus({ preventScroll: true });
    announce(trail, text(trail.config, 'DrilldownReturned', 'Back to {label}', { label: level.label }));
    emit(trail, k, 'cfxdrill', { action: 'return', targetId: level.target?.id ?? null, category: level.target?.category ?? null });
    return true;
  };
  const open = (root, k, node, target) => {
    const trail = trails.get(root) || createTrail(root, k);
    if (trail.disposed || current(trail).root !== root) return false;
    const level = current(trail);
    level.view = k.captureView(root); level.focus = k.targetIdentity(node);
    k.clearHover(root, true, true); k.hideTip(root, root.querySelector('.cfx-tooltip'), true);
    const config = k.drillConfig(root);
    if (target.embedded) {
      const section = k.drillTemplate(root, target.id)?.content.firstElementChild?.cloneNode(true);
      if (section) show(trail, k, section, target);
      else announce(trail, text(config, 'DrilldownFailed', '{label} could not be loaded.', { label: target.label }));
      return true;
    }
    // The host supplies an on-demand child. A later drill or return supersedes an unanswered request.
    const token = {}; trail.pending = token;
    announce(trail, text(config, 'DrilldownLoading', 'Loading {label}…', { label: target.label }));
    const detail = { targetId: target.id, category: target.category, label: target.label };
    const settled = () => trail.pending === token && !trail.disposed && current(trail).root === root;
    const reject = reason => {
      if (!settled()) return false;
      trail.pending = null;
      announce(trail, text(config, 'DrilldownFailed', '{label} could not be loaded.', { label: target.label }));
      emit(trail, k, 'cfxdrillerror', { ...detail, reason: reason == null ? '' : String(reason) });
      return true;
    };
    const respond = value => {
      if (!settled()) return false;
      let section;
      try { section = k.readSection(value); } catch (error) { reject(error.message); throw error; }
      trail.pending = null; show(trail, k, section, target);
      return true;
    };
    emit(trail, k, 'cfxdrillrequest', { ...detail, respond, reject });
    return true;
  };
  // Drillable categories describe where they lead; the description goes when the drawing does.
  const bind = (root, k) => {
    const config = k.drillConfig(root);
    if (!config) return;
    const described = [];
    root.querySelectorAll('.cfx-stage [data-cfx-role="point"][data-cfx-x]').forEach(node => {
      const target = k.drillTargetFor(root, node);
      if (!target || node.hasAttribute('aria-description')) return;
      node.setAttribute('aria-description', text(config, 'DrilldownAvailable', 'Opens {label}', { label: target.label }));
      node.dataset.cfxDrillable = 'true'; described.push(node);
    });
    k.ownChartWork(root, () => described.forEach(node => { node.removeAttribute('aria-description'); delete node.dataset.cfxDrillable; }));
  };
  registry.drilldown = Object.freeze({ open, bind, back: (root, level = 0) => { const trail = trails.get(root); return !!trail && back(trail, trail.k, level); } });
})();
