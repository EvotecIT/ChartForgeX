
  const bindChartContextMenu = root => {
    if (root.dataset.cfxContextMenu !== 'true') return;
    const stage = root.querySelector('.cfx-stage'), view = root.ownerDocument.defaultView;
    if (!stage || !view) return;
    let menu = null, returnFocus = null, cleanups = [];
    const close = (restore = false) => {
      for (const cleanup of cleanups.splice(0)) cleanup();
      menu?.remove(); menu = null;
      delete root._cfxContextMenuSourceFocus;
      if (restore && returnFocus?.isConnected) returnFocus.focus?.({ preventScroll: true });
      returnFocus = null;
    };
    const open = (x, y) => {
      const actions = Array.from(root.querySelectorAll('[data-cfx-reset],.cfx-toolbar [data-cfx-export]'));
      if (!actions.some(action => !action.disabled && !action.hidden)) return false;
      close();
      const tree = root.getRootNode(), style = view.getComputedStyle(root);
      returnFocus = tree.activeElement || root;
      // Portals retain owner/focus.

      root._cfxContextMenuSourceFocus = root.contains(returnFocus) ? returnFocus : stage;
      menu = root.ownerDocument.createElement('div');
      menu.className = 'cfx-chart-context-menu'; menu.setAttribute('role', 'menu');
      menu.setAttribute('aria-label', uiText(root, 'ChartControls', 'Chart controls'));
      menu.dir = style.direction; menu.lang = root.lang;
      for (const name of ['--cfx-tooltip-surface', '--cfx-tooltip-text', '--cfx-tooltip-line', '--cfx-tooltip-font'])
        menu.style.setProperty(name, style.getPropertyValue(name));
      menu.style.visibility = 'hidden';
      const items = actions.map(action => {
        const item = root.ownerDocument.createElement('button'); item.type = 'button'; item.setAttribute('role', 'menuitem');
        item.textContent = action.title || action.textContent; item.disabled = action.disabled || action.hidden;
        item.dataset.cfxContextAction = action.dataset.cfxExport || 'reset';
        menu.appendChild(item);
        cleanups.push(listenChart(root, item, 'click', () => {
          const enabled = action.isConnected && root.contains(action) && !action.disabled && !action.hidden;
          close(true); if (enabled) action.click();
        }));
        return item;
      });
      (tree instanceof ShadowRoot ? tree : root.ownerDocument.body).appendChild(menu);
      const bounds = menu.getBoundingClientRect(), gap = 8;
      menu.style.left = Math.max(gap, Math.min(x, view.innerWidth - bounds.width - gap)) + 'px';
      menu.style.top = Math.max(gap, Math.min(y, view.innerHeight - bounds.height - gap)) + 'px';
      menu.style.visibility = '';
      const enabled = items.filter(item => !item.disabled);
      cleanups.push(listenChart(root, menu, 'keydown', event => {
        if (event.key === 'Escape') { event.preventDefault(); event.stopPropagation(); close(true); return; }
        if (event.key === 'Tab') { close(true); return; }
        if (!['ArrowDown', 'ArrowUp', 'Home', 'End'].includes(event.key)) return;
        event.preventDefault();
        const active = root.getRootNode().activeElement, current = enabled.indexOf(active);
        const index = event.key === 'Home' ? 0 : event.key === 'End' ? enabled.length - 1
          : (current + (event.key === 'ArrowDown' ? 1 : -1) + enabled.length) % enabled.length;
        enabled[index]?.focus();
      }));
      cleanups.push(listenChart(root, root.ownerDocument, 'pointerdown', event => {
        if (!event.composedPath().includes(menu)) close();
      }, true));
      cleanups.push(listenChart(root, view, 'resize', () => close(true)));
      cleanups.push(listenChart(root, root.ownerDocument, 'scroll', event => {
        if (!event.composedPath().includes(menu)) close(true);
      }, true));
      enabled[0]?.focus({ preventScroll: true });
      return true;
    };
    listenChart(root, stage, 'contextmenu', event => {
      if (open(event.clientX, event.clientY)) event.preventDefault();
    });
    listenChart(root, root, 'keydown', event => {
      if (event.key !== 'ContextMenu' && !(event.key === 'F10' && event.shiftKey)) return;
      const active = root.getRootNode().activeElement;
      if (active !== root && !stage.contains(active)) return;
      const box = active.getBoundingClientRect();
      if (open(box.left + box.width / 2, box.top + box.height / 2)) { event.preventDefault(); event.stopPropagation(); }
    });
    ownChartWork(root, close);
  };
