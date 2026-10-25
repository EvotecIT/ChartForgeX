  const editableProgressRows = root => interactiveTargets(root, '[data-cfx-role="progress-row"]').filter(node => {
    const identity = targetIdentity(node);
    return identity.seriesKey && identity.observationId;
  });
  const progressDomain = (root, row) => {
    const minimum = Number(row.dataset.cfxMin ?? 0);
    const maximum = Number(row.dataset.cfxMax ?? row.closest('[data-cfx-role="progress-bar-chart"]')?.dataset.cfxMaximum);
    if (![minimum, maximum].every(Number.isFinite) || maximum <= minimum || !Number.isFinite(maximum - minimum))
      throw new TypeError('Progress: finite positive row domain required.');
    return { minimum, maximum };
  };
  const progressEditingConnected = root => root.dataset.cfxProgressEditing === 'true' && !!nativePreparationBinding(root, 'progress');
  const syncProgressEditing = root => {
    const connected = progressEditingConnected(root);
    if (connected) root.dataset.cfxProgressConnected = "true"; else delete root.dataset.cfxProgressConnected;
    editableProgressRows(root).forEach(row => {
      const focus = targetFocusNode(row), domain = progressDomain(root, row);
      if (connected) {
        focus.setAttribute('role', 'slider');
        focus.setAttribute('aria-valuemin', domain.minimum); focus.setAttribute('aria-valuemax', domain.maximum);
        focus.setAttribute('aria-valuenow', Math.max(domain.minimum, Math.min(domain.maximum, Number(row.dataset.cfxValue))));
        focus.setAttribute('aria-valuetext', row.dataset.cfxFullLabel ?? row.dataset.cfxValue);
      } else if (focus.getAttribute('role') === 'slider') {
        focus.setAttribute('role', 'button');
        ['aria-valuemin', 'aria-valuemax', 'aria-valuenow', 'aria-valuetext'].forEach(key => focus.removeAttribute(key));
      }
    });
  };
  const requestProgressValue = (root, match, value) => {
    if (!progressEditingConnected(root)) throw new TypeError('Progress: enable editing and connect native preparation.');
    if (!match || typeof match.seriesKey !== 'string' || typeof match.observationId !== 'string' || !Number.isFinite(value))
      throw new TypeError('Progress: authored seriesKey/observationId and finite value required.');
    const row = editableProgressRows(root).find(node => {
      const identity = targetIdentity(node); return identity.seriesKey === match.seriesKey && identity.observationId === match.observationId;
    });
    if (!row) throw new TypeError('Progress: existing identified row required.');
    const domain = progressDomain(root, row);
    if (value < domain.minimum || value > domain.maximum) throw new RangeError('Progress: edited value must belong to the row domain.');
    return requestNativePreparation(root, 'progress', { kind: 'replace-value', seriesKey: match.seriesKey, observationId: match.observationId, value }, {
      eventPrefix: 'cfxprogress', preserveViewport: true,
      validate: next => {
        const replacement = editableProgressRows(next).find(node => {
          const identity = targetIdentity(node); return identity.seriesKey === match.seriesKey && identity.observationId === match.observationId;
        });
        const after = replacement && progressDomain(next, replacement);
        if (!replacement || Number(replacement.dataset.cfxValue) !== value || after.minimum !== domain.minimum || after.maximum !== domain.maximum)
          throw new TypeError('Progress reply: source identity/value/domain mismatch.');
      }
    });
  };
  const bindProgressEditing = root => {
    syncProgressEditing(root);
    const stepRatio = Number(root.dataset.cfxProgressStepRatio ?? .01);
    editableProgressRows(root).forEach(row => {
      const focus = targetFocusNode(row);
      const propose = ratio => {
        const { minimum, maximum } = progressDomain(root, row);
        const bounded = Math.max(0, Math.min(1, ratio));
        const snapped = bounded === 0 || bounded === 1 ? bounded : Math.max(0, Math.min(1, Math.round(bounded / stepRatio) * stepRatio));
        requestProgressValue(root, targetIdentity(row), minimum + (maximum - minimum) * snapped);
      };
      listenChart(root, focus, 'keydown', event => {
        if (event.key === 'Escape') { pointer = null; return; }
        if (!progressEditingConnected(root) || !['ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown', 'PageUp', 'PageDown', 'Home', 'End'].includes(event.key)) return;
        event.preventDefault(); event.stopImmediatePropagation();
        const { minimum, maximum } = progressDomain(root, row);
        const current = Math.max(0, Math.min(1, (Number(row.dataset.cfxValue) - minimum) / (maximum - minimum)));
        const direction = ['ArrowRight', 'ArrowUp', 'PageUp'].includes(event.key) ? 1 : -1;
        propose(event.key === 'Home' ? 0 : event.key === 'End' ? 1 : current + direction * stepRatio * (event.key.startsWith('Page') ? 10 : 1));
      }, true);
      let pointer = null;
      const handle = row.querySelector('[data-cfx-role="progress-handle"]');
      if (!handle) return;
      listenChart(root, handle, 'pointerdown', event => {
        if (!progressEditingConnected(root) || event.button !== 0) return;
        event.preventDefault(); event.stopPropagation(); pointer = event.pointerId;
        handle.setPointerCapture?.(pointer); focus.focus({ preventScroll: true });
      });
      listenChart(root, handle, 'pointerup', event => {
        if (pointer !== event.pointerId) return;
        pointer = null; event.preventDefault(); event.stopPropagation();
        const track = row.querySelector('[data-cfx-role="progress-track"]');
        const matrix = track?.getScreenCTM();
        if (!track || !matrix) return;
        const point = new DOMPoint(event.clientX, event.clientY).matrixTransform(matrix.inverse()), box = track.getBBox();
        if (box.width > 0) propose((point.x - box.x) / box.width);
      });
      listenChart(root, handle, 'pointercancel', () => { pointer = null; });
      listenChart(root, handle, 'click', event => { if (progressEditingConnected(root)) { event.preventDefault(); event.stopImmediatePropagation(); } }, true);
    });
  };
