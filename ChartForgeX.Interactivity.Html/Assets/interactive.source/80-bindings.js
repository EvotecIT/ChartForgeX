    const tip = root.querySelector('.cfx-tooltip');
    const stage = root.querySelector('.cfx-stage');
    const brush = root.querySelector('.cfx-brush-box');
    const crosshair = root.querySelector('.cfx-crosshair');
    const compareTray = root.querySelector('[data-cfx-compare-tray]');
    if (!tip) return;
    applyRenderedTargetContract(root);
    applyViewport(root, getState(root));
    renderCompare(root);
    if (compareTray) compareTray.addEventListener('pointerdown', (event) => event.stopPropagation());
    if (hasFeature(root, 'StateBookmarks')) {
      root.addEventListener('cfx-capture-state', (event) => {
        const detail = event.detail || {};
        publishInteractionState(root, detail.source || 'host', detail.sync);
      });
      root.addEventListener('cfx-apply-state', (event) => {
        const detail = event.detail || {};
        applyInteractionState(root, detail.snapshot || detail.state || detail, true, detail.sync);
      });
    }
    const targets = interactiveTargets(root);
    targets.forEach((node) => {
      if (!node.hasAttribute('tabindex')) node.setAttribute('tabindex', '0');
      node.addEventListener('pointerenter', (event) => {
        setHover(root, node, true, true);
        showTip(root, tip, node, event);
      });
      node.addEventListener('pointermove', (event) => moveTip(tip, event, node));
      node.addEventListener('pointerleave', () => {
        clearHover(root, true, true);
        hideTip(root, tip, false);
      });
      node.addEventListener('focus', (event) => {
        setHover(root, node, true, true);
        showTip(root, tip, node, event);
      });
      node.addEventListener('blur', () => {
        clearHover(root, true, true);
        hideTip(root, tip, false);
      });
      node.addEventListener('click', (event) => {
        if ((node.dataset ? node.dataset.cfxRole : '') === 'legend-item') {
          if (event.shiftKey) toggleSeriesFocus(root, node, true, true);
          else toggleSeries(root, node);
        }
        else {
          toggleSelection(root, node);
          pinTip(root, tip, node, event);
        }
      });
      node.addEventListener('keydown', (event) => {
        if (!hasFeature(root, 'KeyboardNavigation')) return;
        if ((node.dataset ? node.dataset.cfxRole : '') === 'legend-item' && event.key.toLowerCase() === 'i') {
          event.preventDefault();
          toggleSeriesFocus(root, node, true, true);
          return;
        }
        if (focusAdjacentTarget(root, node, event.key)) {
          event.preventDefault();
          return;
        }
        if (event.key !== 'Enter' && event.key !== ' ') return;
        event.preventDefault();
        if ((node.dataset ? node.dataset.cfxRole : '') === 'legend-item' && event.shiftKey) toggleSeriesFocus(root, node, true, true);
        else node.dispatchEvent(new MouseEvent('click', { bubbles: true }));
      });
    });
    root.querySelectorAll('[data-cfx-zoom]').forEach((button) => {
      button.addEventListener('click', () => zoomBy(root, button.dataset.cfxZoom === 'in' ? 1.25 : 0.8));
    });
    root.querySelectorAll('[data-cfx-mode-button]').forEach((button) => {
      button.addEventListener('click', () => setMode(root, button.dataset.cfxModeButton || ''));
    });
    root.querySelectorAll('[data-cfx-export]').forEach((button) => {
      button.addEventListener('click', () => {
        if (button.dataset.cfxExport === 'png') exportPng(root);
        else exportSvg(root);
      });
    });
    root.querySelectorAll('[data-cfx-scenario]').forEach((button) => {
      button.addEventListener('click', () => setScenario(root, button.dataset.cfxScenario || '', true));
    });
    if (hasFeature(root, 'Scenarios')) {
      root.addEventListener('cfx-set-scenario', (event) => {
        const detail = event.detail || {};
        setScenario(root, detail.scenarioId || '', true, true);
      });
      root.addEventListener('cfx-clear-scenario', () => setScenario(root, '', true, true));
    }
    if (hasFeature(root, 'StepPlayback')) {
      root.querySelectorAll('[data-cfx-scenario-step-control]').forEach((button) => {
        button.addEventListener('click', () => {
          const action = button.dataset.cfxScenarioStepControl || '';
          if (action === 'previous') stepScenario(root, -1);
          else if (action === 'next') stepScenario(root, 1);
          else if (action === 'play') playScenario(root);
          else if (action === 'reset') { stopScenarioPlayback(root, 'paused'); clearScenarioStep(root); clearReveals(root); }
          else if (action === 'link') copyScenarioLink(root);
        });
      });
      root.addEventListener('cfx-set-scenario-step', (event) => {
        const detail = event.detail || {};
        if (detail.scenarioId && root.dataset.cfxActiveScenario !== detail.scenarioId) setScenario(root, detail.scenarioId, true, true);
        const index = detail.index !== undefined ? detail.index : detail.stepIndex;
        setScenarioStep(root, index, true, true);
      });
      root.addEventListener('cfx-clear-scenario-step', () => {
        stopScenarioPlayback(root, 'paused');
        clearScenarioStep(root);
        clearReveals(root);
      });
      root.addEventListener('cfx-play-scenario', (event) => {
        const detail = event.detail || {};
        if (detail.scenarioId && root.dataset.cfxActiveScenario !== detail.scenarioId) setScenario(root, detail.scenarioId, true, true);
        playScenario(root);
      });
      root.addEventListener('cfx-pause-scenario', () => stopScenarioPlayback(root, 'paused'));
      const stepsList = root.querySelector('[data-cfx-scenario-panel-steps]');
      if (stepsList) {
        const focusStep = (target) => {
          const item = target instanceof Element ? target.closest('[data-cfx-scenario-step-index]') : null;
          if (item && stepsList.contains(item)) setScenarioStep(root, Number(item.getAttribute('data-cfx-scenario-step-index')) - 1, true, true);
        };
        stepsList.addEventListener('click', (event) => focusStep(event.target));
        stepsList.addEventListener('keydown', (event) => {
          if (event.key !== 'Enter' && event.key !== ' ') return;
          event.preventDefault();
          focusStep(event.target);
        });
      }
    }
    if (hasFeature(root, 'Scenarios')) {
      const initialScenario = scenarioUrlParam(root, 'scenario') || root.dataset.cfxActiveScenario || '';
      const initialScenarioStep = scenarioUrlParam(root, 'scenarioStep');
      setScenario(root, initialScenario, false, false);
      if (hasFeature(root, 'StepPlayback') && initialScenarioStep) setScenarioStep(root, initialScenarioStep, false, false);
    }
    if (stage) {
      stage.addEventListener('wheel', (event) => {
        if (!hasFeature(root, 'Zoom')) return;
        event.preventDefault();
        zoomBy(root, event.deltaY < 0 ? 1.08 : 0.92);
      }, { passive: false });
      let drag = null;
      stage.addEventListener('pointerdown', (event) => {
        if (event.button !== 0) return;
        if (root.dataset.cfxMode === 'pan' && hasFeature(root, 'Pan')) {
          const state = getState(root);
          drag = { mode: 'pan', id: event.pointerId, x: event.clientX, y: event.clientY, panX: state.panX, panY: state.panY };
          stage.setPointerCapture(event.pointerId);
        } else if (root.dataset.cfxMode === 'brush' && hasFeature(root, 'Brush') && brush) {
          const rect = stage.getBoundingClientRect();
          drag = { mode: 'brush', id: event.pointerId, left: event.clientX - rect.left, top: event.clientY - rect.top };
          brush.hidden = false;
          brush.style.left = drag.left + 'px';
          brush.style.top = drag.top + 'px';
          brush.style.width = '0px';
          brush.style.height = '0px';
          stage.setPointerCapture(event.pointerId);
        }
      });
      stage.addEventListener('pointermove', (event) => {
        if (!drag || drag.id !== event.pointerId) {
          updateNearestPoint(root, crosshair, tip, event);
          return;
        }
        if (drag.mode === 'pan') {
          applyViewport(root, { zoom: getState(root).zoom, panX: drag.panX + event.clientX - drag.x, panY: drag.panY + event.clientY - drag.y });
        } else if (drag.mode === 'brush' && brush) {
          const rect = stage.getBoundingClientRect();
          const x = clamp(event.clientX - rect.left, 0, rect.width);
          const y = clamp(event.clientY - rect.top, 0, rect.height);
          const left = Math.min(drag.left, x);
          const top = Math.min(drag.top, y);
          brush.style.left = left + 'px';
          brush.style.top = top + 'px';
          brush.style.width = Math.abs(x - drag.left) + 'px';
          brush.style.height = Math.abs(y - drag.top) + 'px';
        }
      });
      stage.addEventListener('pointerup', (event) => {
        if (!drag || drag.id !== event.pointerId) return;
        if (drag.mode === 'brush' && brush) {
          root.dataset.cfxBrush = [brush.style.left, brush.style.top, brush.style.width, brush.style.height].join(' ');
          const selectedTargets = selectTargetsInBox(root, brush.getBoundingClientRect(), event.shiftKey);
          const replaceSelection = !event.shiftKey;
          emitHostEvent(root, 'cfxbrush', { bounds: root.dataset.cfxBrush });
          if (selectedTargets.length || replaceSelection) emitHostEvent(root, 'cfxlasso', { bounds: root.dataset.cfxBrush, count: selectedTargets.length, targets: selectedTargets });
          emitSync(root, { action: 'brush', bounds: root.dataset.cfxBrush });
          if (selectedTargets.length || replaceSelection) emitSync(root, { action: 'lasso', bounds: root.dataset.cfxBrush, targets: selectedTargets, replace: replaceSelection });
          publishCompare(root, true);
        } else if (drag.mode === 'pan') {
          emitHostEvent(root, 'cfxviewport', { state: getState(root) });
          emitSync(root, { action: 'viewport', state: getState(root) });
        }
        stage.releasePointerCapture(event.pointerId);
        drag = null;
      });
      stage.addEventListener('pointerleave', () => {
        hideCrosshair(root, crosshair);
        clearHover(root, true, true);
        hideTip(root, tip, false);
      });
      stage.addEventListener('pointercancel', () => {
        drag = null;
        hideCrosshair(root, crosshair);
        clearHover(root, true, true);
        hideTip(root, tip, false);
      });
    }
    const reset = root.querySelector('[data-cfx-reset]');
    if (reset) reset.addEventListener('click', () => {
      resetViewport(root);
      emitHostEvent(root, 'cfxreset', {});
      emitHostEvent(root, 'cfxviewport', { state: getState(root) });
      emitSync(root, { action: 'viewport', state: getState(root) });
      clearSelections(root);
      root.querySelectorAll('.cfx-series-muted').forEach((node) => node.classList.remove('cfx-series-muted'));
      root.querySelectorAll('[data-cfx-muted]').forEach((node) => node.removeAttribute('data-cfx-muted'));
      setSeriesIsolation(root, null, false);
      clearFocusTrail(root);
      clearReveals(root);
      if (brush) brush.hidden = true;
      hideCrosshair(root, crosshair);
      hideTip(root, tip, true);
      publishCompare(root, true);
    });
  });
})();
