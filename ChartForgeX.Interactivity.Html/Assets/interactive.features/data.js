// ChartForgeX data feature: CSV/JSON download and copy, and the chart context menu.
(() => {
  const registry = window[Symbol.for('ChartForgeX.chartFeatures')] ||= {};
  if (registry.data) return;
  const finite = value => value !== undefined && value !== null && String(value).trim() !== '' && Number.isFinite(Number(value));
  const rows = (root, k) => {
    const seen = new Set();
    // Viewport visibility controls hit testing, not the saved report's data.
    return Array.from(root.querySelectorAll('[data-cfx-target-kind][data-cfx-target-id]'))
      .filter(node => !k.isPassiveTarget(node) && !node.closest('defs,[data-cfx-role="legend-item"]')
        && k.renderedTargetKind(node) !== 'legend' && (k.renderedTargetKind(node) !== 'series' || node.dataset.cfxValue !== undefined))
      .flatMap(node => {
        const target = k.targetIdentity(node), key = JSON.stringify([target.targetKind, target.targetId, target.seriesKey]);
        if (seen.has(key)) return [];
        seen.add(key);
        const data = node.dataset, measure = name => data['cfx' + name[0].toUpperCase() + name.slice(1)];
        return [{ ...target, seriesIndex: target.series, series: k.seriesLabel(node), category: target.category ?? target.label, x: target.x,
          nodeId: data.cfxNode, parentId: data.cfxParent, sourceId: data.cfxSource, destinationId: data.cfxTarget,
          hierarchyPath: k.tooltipHierarchy(node)?.path,
          value: target.numericValue ?? target.value, ...(target.sourcePoint !== undefined ? { sourcePoint: Number(target.sourcePoint) } : {}),
          ...Object.fromEntries(['open', 'high', 'low', 'close', 'size', 'colorValue'].filter(name => finite(measure(name)))
            .map(name => [name, Number(measure(name))])) }];
      });
  };
  const text = (root, k, format) => {
    const data = rows(root, k);
    if (format === 'json') return JSON.stringify(data, null, 2);
    const fields = Array.from(new Set(data.flatMap(row => Object.keys(row))));
    // Spreadsheet readers execute formulas even inside quoted CSV cells. Only
    // text receives the literal prefix; numeric facts and raw JSON stay exact.
    const cell = value => {
      let content = value && typeof value === 'object' ? JSON.stringify(value) : String(value ?? '');
      if (typeof value !== 'number' && /^[\s\u0000-\u001f]*[=+@-]|^[\t\r\n]/.test(content)) content = "'" + content;
      return '"' + content.replace(/"/g, '""') + '"';
    };
    return [fields.map(cell).join(','), ...data.map(row => fields.map(field => cell(row[field])).join(','))].join('\r\n');
  };
  const copy = async (root, k, format) => {
    const value = text(root, k, format);
    try {
      if (navigator.clipboard?.writeText) await navigator.clipboard.writeText(value);
      else {
        const active = root.getRootNode().activeElement, input = document.createElement('textarea');
        input.value = value; input.style.cssText = 'position:fixed;left:-10000px;top:0';
        document.body.appendChild(input);
        try { input.select(); if (!document.execCommand('copy')) throw new Error('Clipboard copy is unavailable.'); }
        finally { input.remove(); active?.focus?.({ preventScroll: true }); }
      }
      k.emitHostEvent(root, 'cfxexport', { format, action: 'copy' }); return true;
    } catch (error) {
      k.emitHostEvent(root, 'cfxexporterror', { format, action: 'copy', message: String(error.message || error) }); return false;
    }
  };
  const exportData = (root, k, format, copied) => {
    if (!k.hasFeature(root, 'Export')) return Promise.resolve(false);
    if (copied) return copy(root, k, format);
    k.downloadBlob(root, new Blob([text(root, k, format)], { type: format === 'json' ? 'application/json;charset=utf-8' : 'text/csv;charset=utf-8' }), format, format);
    return Promise.resolve(true);
  };
  // Host items come from C# (dispatched as cfxcontextaction), the controller, or a
  // saved page's cfxContextMenuItems property. Invalid page-provided items are ignored.
  const hostItems = (root, k) => {
    let declared = [];
    try { declared = JSON.parse(root.dataset.cfxContextItems || '[]'); } catch (_) { declared = []; }
    const page = Array.isArray(root.cfxContextMenuItems) ? root.cfxContextMenuItems.filter(item => item && typeof item.id === 'string'
      && typeof item.label === 'string' && item.label.trim() && typeof item.action === 'function') : [];
    const items = [...declared.filter(item => item && item.id && item.label).map(item => ({ id: item.id, label: item.label, disabled: item.disabled === true,
      action: context => root.dispatchEvent(new CustomEvent('cfxcontextaction', { bubbles: true, composed: true,
        detail: { id: item.id, chartId: context.chartId, selection: context.selection, data: context.data } })) })),
      ...k.contextMenuItems(root), ...page];
    const ids = new Set();
    return items.filter(item => !ids.has(item.id) && ids.add(item.id));
  };
  const menus = new WeakMap();
  const closeMenu = (root, restore = false) => {
    const state = menus.get(root); if (!state) return;
    menus.delete(root);
    for (const cleanup of state.cleanups.splice(0)) cleanup?.();
    state.menu.remove();
    if (restore && state.returnFocus?.isConnected) state.returnFocus.focus?.({ preventScroll: true });
  };
  const openMenu = (root, k, x, y) => {
    const view = root.ownerDocument.defaultView, stage = root.querySelector('.cfx-stage');
    if (!view || !stage) return false;
    const label = (key, fallback) => k.uiText(root, key, fallback);
    const actions = Array.from(root.querySelectorAll('[data-cfx-reset],.cfx-toolbar [data-cfx-export]'));
    const extras = [...(k.hasFeature(root, 'Export') ? ['csv', 'json'].flatMap(format => [
      { id: format, label: label('Download' + format.toUpperCase(), 'Download ' + format.toUpperCase()), action: () => exportData(root, k, format, false) },
      { id: 'copy-' + format, label: label('Copy' + format.toUpperCase(), 'Copy ' + format.toUpperCase()), action: () => exportData(root, k, format, true) }
    ]) : []), ...hostItems(root, k),
    ...(k.annotationsDisconnected(root) ? [{ id: 'annotations', label: label('AnnotationDisconnected', 'Annotations need a connected host'), disabled: true }] : [])];
    if (!actions.some(action => !action.disabled && !action.hidden) && !extras.some(item => !item.disabled)) return false;
    closeMenu(root);
    // The menu replaces any transient readout under the pointer.
    k.clearHover(root, true, true); k.hideTip(root, root.querySelector('.cfx-tooltip'), true); k.hideCrosshair(root, root.querySelector('.cfx-crosshair'));
    const tree = root.getRootNode(), style = view.getComputedStyle(root);
    const returnFocus = tree.activeElement || root;
    k.rememberMenuFocus(root, root.contains(returnFocus) ? returnFocus : stage);
    const menu = root.ownerDocument.createElement('div');
    const state = { menu, returnFocus, cleanups: [() => k.rememberMenuFocus(root, null)] };
    menus.set(root, state);
    menu.className = 'cfx-chart-context-menu'; menu.setAttribute('role', 'menu');
    menu.setAttribute('aria-label', label('ChartControls', 'Chart controls'));
    menu.dir = style.direction; menu.lang = root.lang;
    for (const name of ['--cfx-tooltip-surface', '--cfx-tooltip-text', '--cfx-tooltip-muted', '--cfx-tooltip-line', '--cfx-tooltip-font',
      '--cfx-tooltip-shadow', '--cfx-tooltip-radius', '--cfx-control-radius', '--cfx-focus-ring', '--cfx-default-host-accent', '--cfx-host-accent'])
      menu.style.setProperty(name, style.getPropertyValue(name));
    menu.style.visibility = 'hidden';
    const button = (text, id, disabled) => {
      const item = root.ownerDocument.createElement('button'); item.type = 'button'; item.setAttribute('role', 'menuitem');
      item.textContent = text; item.disabled = disabled; item.dataset.cfxContextAction = id;
      menu.appendChild(item); return item;
    };
    const items = actions.map(action => {
      const item = button(action.title || action.textContent, action.dataset.cfxExport || 'reset', action.disabled || action.hidden);
      state.cleanups.push(k.listenChart(root, item, 'click', () => {
        const enabled = action.isConnected && root.contains(action) && !action.disabled && !action.hidden;
        closeMenu(root, true); if (enabled) action.click();
      }));
      return item;
    });
    extras.forEach((action, index) => {
      if (index === 0 && items.length) menu.appendChild(root.ownerDocument.createElement('hr')).setAttribute('role', 'separator');
      const item = button(action.label, action.id, action.disabled === true);
      items.push(item);
      state.cleanups.push(k.listenChart(root, item, 'click', () => {
        closeMenu(root, true);
        action.action({ root, chartId: root.dataset.cfxChartId, selection: k.selectedTargets(root), data: rows(root, k) });
      }));
    });
    (tree instanceof ShadowRoot ? tree : root.ownerDocument.body).appendChild(menu);
    const bounds = menu.getBoundingClientRect(), gap = 8;
    menu.style.left = Math.max(gap, Math.min(x, view.innerWidth - bounds.width - gap)) + 'px';
    menu.style.top = Math.max(gap, Math.min(y, view.innerHeight - bounds.height - gap)) + 'px';
    menu.style.visibility = '';
    const enabled = items.filter(item => !item.disabled);
    state.cleanups.push(k.listenChart(root, menu, 'keydown', event => {
      if (event.key === 'Escape') { event.preventDefault(); event.stopPropagation(); closeMenu(root, true); return; }
      if (event.key === 'Tab') { closeMenu(root, true); return; }
      if (!['ArrowDown', 'ArrowUp', 'Home', 'End'].includes(event.key)) return;
      event.preventDefault();
      const current = enabled.indexOf(root.getRootNode().activeElement);
      const index = event.key === 'Home' ? 0 : event.key === 'End' ? enabled.length - 1
        : (current + (event.key === 'ArrowDown' ? 1 : -1) + enabled.length) % enabled.length;
      enabled[index]?.focus();
    }));
    state.cleanups.push(k.listenChart(root, root.ownerDocument, 'pointerdown', event => {
      if (!event.composedPath().includes(menu)) closeMenu(root);
    }, true));
    state.cleanups.push(k.listenChart(root, view, 'resize', () => closeMenu(root, true)));
    state.cleanups.push(k.listenChart(root, root.ownerDocument, 'scroll', event => {
      if (!event.composedPath().includes(menu)) closeMenu(root, true);
    }, true));
    enabled[0]?.focus({ preventScroll: true });
    return true;
  };
  registry.data = Object.freeze({ exportData, openMenu, closeMenu, rows });
})();
