  // Navigation chrome reuses the captured axis math and requests a fresh native scene.
  const fullXNavigationAxis = root => {
    const projection = chartXProjection(root), axis = root._cfxDataProjection?.x;
    return projection && axis ? { ...axis, minimum: projection.fullMinimum, maximum: projection.fullMaximum,
      transformedMinimum: projection.transformedFullMinimum, transformedMaximum: projection.transformedFullMaximum,
      timeSessions: projection.timeSessions, maximumInclusive: true, reversed: false } : null;
  };
  const xNavigationWindow = (root, axis, minimum, maximum, maximumInclusive) => {
    // An authored full edge inside a unit stays inclusive after native confinement.
    // Otherwise navigation emits exact canonical calendar edges, never centered marks.
    const inclusive = maximumInclusive ?? (axis.timeSpacing?.kind === 'unit'
      ? !validSourceAxisMaximum({ maximum, maximumInclusive: false }, axis) : chartXProjection(root)?.maximumInclusive !== false);
    return { minimum, maximum, ...(inclusive ? {} : { maximumInclusive: false }) };
  };
  const xNavigationCaption = (root, value) => {
    const axis = root._cfxDataProjection?.x;
    const native = [...(axis?.displayBounds || []), ...(axis?.displayTicks || [])].find(tick => tick.value === value);
    return native ? native.text : String(dataValue(value, axis?.scale === 'time' && value >= 0));
  };
  const paintXNavigationRange = (root, first, last, rawWindow) => {
    const panel = root.querySelector('[data-cfx-x-navigation]'), projection = chartXProjection(root), axis = fullXNavigationAxis(root);
    if (!panel || !projection || !axis) return;
    const start = panel.querySelector('[data-cfx-x-range="start"]'), end = panel.querySelector('[data-cfx-x-range="end"]');
    start.value = String(first); end.value = String(last);
    const lower = rawWindow?.minimum ?? projectedAxisValue(axis, first, false), upper = rawWindow?.maximum ?? projectedAxisValue(axis, last, true);
    const captions = [xNavigationCaption(root, lower), xNavigationCaption(root, upper)];
    [start, end].forEach((input, index) => { input.setAttribute('aria-valuetext', captions[index]); });
    panel.querySelector('[data-cfx-x-value="start"]').textContent = captions[0];
    panel.querySelector('[data-cfx-x-value="end"]').textContent = captions[1];
    const selected = panel.querySelector('[data-cfx-x-window]');
    if (selected) {
      selected.style.left = ((projection.reversed ? 1 - last : first) * 100) + '%';
      selected.style.width = ((last - first) * 100) + '%';
    }
    const position = panel.querySelector('[data-cfx-x-range="position"]');
    if (position) {
      const available = 1 - (last - first);
      position.value = String(available > 0 ? first / available : 0);
      position.disabled = !connectedXViewport(root) || available <= Number.EPSILON;
      position.setAttribute('aria-valuetext', captions.join(' – '));
    }
    panel.querySelectorAll('[data-cfx-x-range]').forEach(input => { input.dir = projection.reversed ? 'rtl' : 'ltr'; });
  };
  const syncXNavigation = root => {
    const panel = root.querySelector('[data-cfx-x-navigation]');
    if (!panel) return;
    const projection = chartXProjection(root), axis = fullXNavigationAxis(root);
    const enabled = connectedXViewport(root) && !!projection && !!axis;
    panel.hidden = !enabled;
    panel.querySelectorAll('button,input').forEach(control => { control.disabled = !enabled; });
    if (!enabled) return;
    const first = projectedAxisRatio(axis, projection.minimum, false), last = projectedAxisRatio(axis, projection.maximum, true, projection.maximumInclusive !== false);
    paintXNavigationRange(root, first, last, { minimum: projection.minimum, maximum: projection.maximum });
    const pending = chartLifetimes.get(root)?.preparationPending?.channel === 'viewport';
    panel.setAttribute('aria-busy', String(pending));
    panel.querySelector('[data-cfx-x-status]').textContent = pending
      ? uiText(root, 'XRangeUpdating', 'Updating range…') : '';
    panel.querySelector('[data-cfx-x-full]').setAttribute('aria-pressed', String(!nativeXWindowChanged(root)));
    panel.querySelectorAll('[data-cfx-x-preset]').forEach(button => {
      const maximum = projection.fullMaximum, minimum = Math.max(projection.fullMinimum, maximum - Number(button.dataset.cfxXPreset));
      button.disabled = !(minimum < maximum);
      button.setAttribute('aria-pressed', String(projection.minimum === minimum && projection.maximum === maximum && projection.maximumInclusive !== false));
    });
  };
  const bindXNavigation = root => {
    const panel = root.querySelector('[data-cfx-x-navigation]');
    if (!panel) return;
    const commit = (first, last, rawWindow) => {
      const axis = fullXNavigationAxis(root);
      if (!axis || !requestNativeXWindow(root, rawWindow || xNavigationWindow(root, axis, projectedAxisValue(axis, first, false), projectedAxisValue(axis, last, true))))
        syncXNavigation(root);
    };
    const ratios = () => {
      const projection = chartXProjection(root), axis = fullXNavigationAxis(root);
      return projection && axis ? [projectedAxisRatio(axis, projection.minimum, false), projectedAxisRatio(axis, projection.maximum, true, projection.maximumInclusive !== false)] : null;
    };
    const overview = panel.querySelector('.cfx-x-navigation__overview');
    if (overview) {
      let drag = null;
      const pointerRatio = event => {
        const box = overview.getBoundingClientRect();
        const ratio = clamp((event.clientX - box.left) / box.width, 0, 1);
        return chartXProjection(root)?.reversed ? 1 - ratio : ratio;
      };
      const cancel = () => { drag = null; syncXNavigation(root); };
      listenChart(root, overview, 'pointerdown', event => {
        if (event.button !== 0 || !connectedXViewport(root)) return;
        event.preventDefault();
        drag = { id: event.pointerId, first: pointerRatio(event), clientX: event.clientX };
        overview.setPointerCapture(event.pointerId);
      });
      listenChart(root, overview, 'pointermove', event => {
        if (!drag || drag.id !== event.pointerId) return;
        const last = pointerRatio(event);
        if (last !== drag.first) paintXNavigationRange(root, Math.min(drag.first, last), Math.max(drag.first, last));
      });
      listenChart(root, overview, 'pointerup', event => {
        if (!drag || drag.id !== event.pointerId) return;
        const first = drag.first, last = pointerRatio(event), moved = Math.abs(event.clientX - drag.clientX) >= 4;
        drag = null;
        if (overview.hasPointerCapture(event.pointerId)) overview.releasePointerCapture(event.pointerId);
        if (moved && first !== last) commit(Math.min(first, last), Math.max(first, last));
        else syncXNavigation(root);
      });
      listenChart(root, overview, 'pointercancel', cancel);
      listenChart(root, root, 'keydown', event => { if (event.key === 'Escape') cancel(); });
    }
    panel.querySelectorAll('[data-cfx-x-range]').forEach(input => {
      let draft = null;
      const updateDraft = () => {
        const committed = ratios();
        if (!committed || !connectedXViewport(root)) return;
        const value = clamp(Number(input.value), 0, 1), kind = input.dataset.cfxXRange;
        if (kind === 'position') {
          const span = committed[1] - committed[0], first = value * (1 - span);
          draft = [first, first + span];
        } else {
          draft = committed;
          if (kind === 'start') draft[0] = Math.min(value, draft[1] - Number.EPSILON);
          else draft[1] = Math.max(value, draft[0] + Number.EPSILON);
          const projection = chartXProjection(root), axis = fullXNavigationAxis(root);
          draft.push(xNavigationWindow(root, axis,
            kind === 'start' ? projectedAxisValue(axis, draft[0], false) : projection.minimum,
            kind === 'end' ? projectedAxisValue(axis, draft[1], true) : projection.maximum,
            kind === 'start' ? projection.maximumInclusive !== false : undefined));
        }
        paintXNavigationRange(root, draft[0], draft[1], draft[2]);
      };
      const commitDraft = () => { if (draft) commit(...draft); draft = null; };
      listenChart(root, input, 'input', updateDraft);
      listenChart(root, input, 'change', commitDraft);
      listenChart(root, input, 'keydown', event => {
        if (event.key === 'Escape') { draft = null; syncXNavigation(root); }
        if (event.defaultPrevented || event.altKey || event.ctrlKey || event.metaKey || input.disabled || !connectedXViewport(root)) return;
        const current = Number(input.value), horizontal = input.dir === 'rtl' ? -1 : 1;
        let next;
        switch (event.key) {
          case 'ArrowLeft': next = current - .01 * horizontal; break;
          case 'ArrowRight': next = current + .01 * horizontal; break;
          case 'ArrowDown': next = current - .01; break;
          case 'ArrowUp': next = current + .01; break;
          case 'PageDown': next = current - .1; break;
          case 'PageUp': next = current + .1; break;
          case 'Home': next = 0; break;
          case 'End': next = 1; break;
          default: return;
        }
        event.preventDefault();
        next = clamp(next, 0, 1);
        if (next === current) return;
        input.value = String(next);
        updateDraft();
        commitDraft();
      });
      listenChart(root, input, 'pointercancel', () => { draft = null; syncXNavigation(root); });
    });
    panel.querySelectorAll('[data-cfx-x-preset]').forEach(button => listenChart(root, button, 'click', () => {
      const projection = chartXProjection(root);
      if (projection) requestNativeXWindow(root, { minimum: Math.max(projection.fullMinimum, projection.fullMaximum - Number(button.dataset.cfxXPreset)),
        maximum: projection.fullMaximum });
    }));
    listenChart(root, panel.querySelector('[data-cfx-x-full]'), 'click', () => {
      const projection = chartXProjection(root);
      if (projection) requestNativeXWindow(root, { minimum: projection.fullMinimum, maximum: projection.fullMaximum });
    });
    listenChart(root, root, 'cfxviewporterror', () => {
      syncXNavigation(root);
      panel.querySelector('[data-cfx-x-status]').textContent = uiText(root, 'XRangeFailed', 'The range could not be updated.');
    });
    syncXNavigation(root);
  };
