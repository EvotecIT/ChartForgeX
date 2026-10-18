    const tip = root.querySelector('.cfx-tooltip');
    const stage = root.querySelector('.cfx-stage');
    const brush = root.querySelector('.cfx-brush-box');
    const crosshair = root.querySelector('.cfx-crosshair');
    const compareTray = root.querySelector('[data-cfx-compare-tray]');
    if (!tip) return;
    listenChart(root, root, 'keydown', event => {
      if (event.key === 'Escape') cancelXViewportRequest(chartLifetimes.get(root));
    }, true);
    applyRenderedTargetContract(root);
    bindHostCommands(root);
    prepareRootBranchHighlights(root);
    if (root._cfxBranchHighlights) listenChart(root, root, 'keydown', event => {
      if (event.key !== 'Escape') return;
      event.preventDefault();
      event.stopPropagation();
      clearHover(root, true, true);
      hideCrosshair(root, crosshair);
      hideTip(root, tip, true);
    });
    applyViewport(root, getState(root));
    renderCompare(root);
    syncSelectionAppearance(root);
    listenChart(root, root, 'keydown', event => {
      if (event.key !== 'Escape') return;
      clearBrushCommand(root, { emit: true, sync: true });
      if (!hasFeature(root, 'Selection') || !selectedTargets(root).length) return;
      clearSelections(root);
      publishSelectionChange(root, 'clear', true);
      publishCompare(root, false);
    }, true);
    if (compareTray) {
      let pointerFocus = false;
      listenChart(root, compareTray, 'pointerdown', (event) => { pointerFocus = event.button === 0; event.stopPropagation(); });
      ['pointerup', 'pointercancel'].forEach(type => listenChart(root, compareTray, type, () => { pointerFocus = false; }));
      listenChart(root, compareTray, 'pointerleave', () => {
        pointerFocus = false;
        const active = root.getRootNode().activeElement;
        if (!compareTray.contains(active) || !active.matches(':focus-visible')) hideTip(root, tip, false);
      });
      listenChart(root, compareTray, 'focusout', event => {
        if (!compareTray.contains(event.relatedTarget)) hideTip(root, tip, false);
      });
      listenChart(root, compareTray, 'focusin', (event) => {
        if (!pointerFocus && event.target instanceof Element && event.target.matches(':focus-visible')) scrollKeyboardTargetIntoView(root, event.target);
      });
    }
    if (hasFeature(root, 'StateBookmarks')) {
      listenChart(root, root, 'cfx-capture-state', (event) => {
        const detail = event.detail || {};
        publishInteractionState(root, detail.source || 'host', detail.sync);
      });
      listenChart(root, root, 'cfx-apply-state', (event) => {
        const detail = event.detail || {};
        applyInteractionState(root, detail.snapshot || detail.state || detail, true, detail.sync);
      });
    }
    prepareKeyboardNavigation(root);
    bindStageLayout(root, stage, crosshair);
    const targets = interactiveTargets(root);
    targets.forEach((node) => {
      const focusNode = targetFocusNode(node);
      const labels = pointLabelSurfaces(root, node);
      [node, ...labels].forEach(surface => {
        listenChart(root, surface, 'pointerenter', (event) => {
          if (pointerReadoutSuspended(root) || keyboardReadoutOwnsPointer(root) || !pointerTargetPaint(node)) return;
          if (!ownsTooltipPointer(root, event)) hideCrosshair(root, crosshair);
          setHover(root, node, true, true);
          showTip(root, tip, node, event);
        });
        listenChart(root, surface, 'pointermove', (event) => {
          if (ownsTooltipPointer(root, event)) moveTip(tip, event, node);
        });
        listenChart(root, surface, 'pointerleave', (event) => {
          if (!ownsTooltipPointer(root, event)) return;
          clearHover(root, true, true);
          if (!retainPointerTip(root, event)) hideTip(root, tip, false);
        });
      });
      // Preserve native link focus; disabled adapter navigation must not create implicit SVG tab stops.
      if (hasFeature(root, 'KeyboardNavigation') || focusNode.matches('a[href]')) {
        let pointerFocus = false;
        // Touch compatibility mouse events can focus the mark after pointerup has already cleared the press.
        ['pointerdown', 'mousedown'].forEach(type => listenChart(root, focusNode, type, (event) => { pointerFocus = event.button === 0; }));
        ['pointerup', 'pointercancel', 'pointerleave', 'mouseup'].forEach(type => listenChart(root, focusNode, type, () => { pointerFocus = false; }));
        listenChart(root, focusNode, 'focus', (event) => {
          refreshKeyboardNavigation(root, node);
          // Revealing a partly visible mark between pointer down and up retargets its native click.
          const keyboardFocus = focusNode.matches(':focus-visible');
          if (keyboardFocus && !pointerFocus) delete root._cfxPointerFocus;
          refreshKeyboardFocusPaint(root);
          if (hasFeature(root, 'KeyboardNavigation') && !pointerFocus && keyboardFocus) scrollKeyboardTargetIntoView(root, focusNode);
          pointerFocus = false;
          if (!keyboardFocus || root._cfxRecoveringFocus) return;
          resumePointerReadout(root);
          root._cfxKeyboardReadout = node;
          if (!tooltipReadoutAvailable(node, event)) { clearHover(root, true, true); hideTip(root, tip, false); return; }
          setHover(root, node, true, true);
          showTip(root, tip, node, event);
        });
        listenChart(root, focusNode, 'blur', () => {
          pointerFocus = false;
          clearKeyboardFocusPaint(root);
          if (root._cfxKeyboardReadout === node) delete root._cfxKeyboardReadout;
          clearHover(root, true, true);
          hideTip(root, tip, false);
        });
      }
      const activateTarget = (event) => {
        event.stopPropagation();
        if (event instanceof KeyboardEvent) resumePointerReadout(root);
        const activated = pointerActivationTarget(root, node, event);
        if (!tooltipReadoutAvailable(activated, event)) return;
        if ((activated.dataset ? activated.dataset.cfxRole : '') === 'legend-item') {
          if (event.shiftKey) toggleSeriesFocus(root, activated, true, true);
          else toggleSeries(root, activated);
        }
        else {
          toggleSelection(root, activated, event);
          pinTip(root, tip, activated, event);
        }
      };
      listenChart(root, focusNode, 'click', activateTarget);
      labels.forEach(label => listenChart(root, label, 'click', activateTarget));
      if (hasFeature(root, 'KeyboardNavigation')) listenChart(root, focusNode, 'keydown', (event) => {
        if (!hasFeature(root, 'KeyboardNavigation') || event.defaultPrevented || event.target !== focusNode) return;
        delete root._cfxPointerFocus;
        refreshKeyboardFocusPaint(root);
        root._cfxKeyboardReadout = node;
        if ((node.dataset ? node.dataset.cfxRole : '') === 'legend-item' && event.key.toLowerCase() === 'i') {
          event.preventDefault();
          event.stopPropagation();
          toggleSeriesFocus(root, node, true, true);
          return;
        }
        if (focusAdjacentTarget(root, node, event.key)) {
          event.preventDefault();
          event.stopPropagation();
          return;
        }
        if (event.key !== 'Enter' && event.key !== ' ') return;
        // Enter on a link must retain native navigation. Space selects the cell without following the link.
        if (event.key === 'Enter' && focusNode.matches('a[href]')) return;
        event.preventDefault();
        event.stopPropagation();
        // Keep the keyboard event: a synthetic click's finite (0,0) would replace the focused mark's position.
        activateTarget(event);
      });
    });
    root.querySelectorAll('[data-cfx-zoom]').forEach((button) => {
      listenChart(root, button, 'click', () => zoomBy(root, button.dataset.cfxZoom === 'in' ? 1.25 : 0.8));
    });
    root.querySelectorAll('[data-cfx-mode-button]').forEach((button) => {
      listenChart(root, button, 'click', () => setMode(root, button.dataset.cfxModeButton || ''));
    });
    root.querySelectorAll('[data-cfx-export]').forEach((button) => {
      listenChart(root, button, 'click', () => {
        if (button.dataset.cfxExport === 'png') exportPng(root);
        else exportSvg(root);
      });
    });
    root.querySelectorAll('[data-cfx-scenario]').forEach((button) => {
      listenChart(root, button, 'click', () => setScenario(root, button.dataset.cfxScenario || '', true));
    });
    if (hasFeature(root, 'Scenarios')) {
      listenChart(root, root, 'cfx-set-scenario', (event) => {
        const detail = event.detail || {};
        setScenario(root, detail.scenarioId || '', true, true);
      });
      listenChart(root, root, 'cfx-clear-scenario', () => setScenario(root, '', true, true));
    }
    if (hasFeature(root, 'StepPlayback')) {
      root.querySelectorAll('[data-cfx-scenario-step-control]').forEach((button) => {
        listenChart(root, button, 'click', () => {
          const action = button.dataset.cfxScenarioStepControl || '';
          if (action === 'previous') stepScenario(root, -1);
          else if (action === 'next') stepScenario(root, 1);
          else if (action === 'play') playScenario(root);
          else if (action === 'reset') { stopScenarioPlayback(root, 'paused'); clearScenarioStep(root); clearReveals(root); }
          else if (action === 'link') copyScenarioLink(root);
        });
      });
      listenChart(root, root, 'cfx-set-scenario-step', (event) => {
        const detail = event.detail || {};
        if (detail.scenarioId && root.dataset.cfxActiveScenario !== detail.scenarioId) setScenario(root, detail.scenarioId, true, true);
        const index = detail.index !== undefined ? detail.index : detail.stepIndex;
        setScenarioStep(root, index, true, true);
      });
      listenChart(root, root, 'cfx-clear-scenario-step', () => {
        stopScenarioPlayback(root, 'paused');
        clearScenarioStep(root);
        clearReveals(root);
      });
      listenChart(root, root, 'cfx-play-scenario', (event) => {
        const detail = event.detail || {};
        if (detail.scenarioId && root.dataset.cfxActiveScenario !== detail.scenarioId) setScenario(root, detail.scenarioId, true, true);
        playScenario(root);
      });
      listenChart(root, root, 'cfx-pause-scenario', () => stopScenarioPlayback(root, 'paused'));
      const scrubber = root.querySelector('[data-cfx-scenario-scrubber]');
      if (scrubber) listenChart(root, scrubber, 'input', () => {
        stopScenarioPlayback(root, 'paused');
        setScenarioStep(root, Number(scrubber.value) - 1, true, true);
      });
      const stepsList = root.querySelector('[data-cfx-scenario-panel-steps]');
      if (stepsList) {
        const focusStep = (target) => {
          const item = target instanceof Element ? target.closest('[data-cfx-scenario-step-index]') : null;
          if (item && stepsList.contains(item)) setScenarioStep(root, Number(item.getAttribute('data-cfx-scenario-step-index')) - 1, true, true);
        };
        listenChart(root, stepsList, 'click', (event) => focusStep(event.target));
        listenChart(root, stepsList, 'keydown', (event) => {
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
      const initialRoute = scenarioRoute(root, root.dataset.cfxActiveScenario || '');
      const reducedMotion = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
      if (hasFeature(root, 'StepPlayback') && initialRoute && initialRoute.autoPlay && !initialScenarioStep && !reducedMotion) startScenarioPlayback(root, initialRoute, 0, false, true);
    }
    if (stage) {
      listenChart(root, stage, 'pointerenter', (event) => {
        // A new contact can acquire sparse plot space before any native mark receives its own entry.
        if (keyboardReadoutOwnsPointer(root)) return;
        if (!ownsTooltipPointer(root, event)) updateNearestPoint(root, crosshair, tip, event);
      });
      listenChart(root, stage, 'wheel', (event) => {
        if (!hasFeature(root, 'Zoom')) return;
        if (connectedXViewport(root) && !chartXProjection(root)) return;
        event.preventDefault();
        zoomBy(root, event.deltaY < 0 ? 1.08 : 0.92, logicalXPointer(root, event));
      }, { passive: false });
      let drag = null;
      let pointerFrame = 0, pendingPointerMove = null;
      const cancelPointerMove = () => {
        if (pointerFrame) cancelAnimationFrame(pointerFrame);
        pointerFrame = 0; pendingPointerMove = null;
      };
      // Tooltip cancellation owns queued hover work; an active gesture keeps its
      // provisional movement and still commits its final sample on pointerup.
      root._cfxCancelPointerHover = () => { if (!drag) cancelPointerMove(); };
      const applyPointerMove = (event, nativeTarget = event.target) => {
        if (!drag || drag.id !== event.pointerId) {
          if (!ownsTooltipPointer(root, event)) return;
          updateNearestPoint(root, crosshair, tip, event, nativeTarget);
          return;
        }
        if (drag.mode === 'native-pan') {
          const x = logicalXPointer(root, event);
          if (x !== null) drag.delta = x - drag.x;
        } else if (drag.mode === 'pan') {
          const point = stagePointer(stage, event);
          applyViewport(root, { zoom: getState(root).zoom, panX: drag.panX + point.x - drag.x, panY: drag.panY + point.y - drag.y });
        } else if (drag.mode === 'brush' && brush) {
          setBrushBox(root, stage, brush, drag, brushPointer(root, stage, event));
        }
      };
      // Keep the last sample in each display frame; pointerup commits its final position
      // synchronously, so coalescing cannot omit the last drag segment or selection.
      root._cfxCancelBrushDrag = () => {
        if (drag?.mode !== 'brush') return false;
        cancelPointerMove();
        if (stage.hasPointerCapture(drag.id)) stage.releasePointerCapture(drag.id);
        drag = null; return true;
      };
      ownChartWork(root, () => {
        cancelPointerMove();
        if (drag && stage.hasPointerCapture(drag.id)) stage.releasePointerCapture(drag.id);
        drag = null;
        delete root._cfxCancelBrushDrag;
        delete root._cfxCancelPointerHover;
      });
      listenChart(root, stage, 'pointerdown', (event) => {
        if (event.button !== 0) return;
        root._cfxPointerFocus = true;
        clearKeyboardFocusPaint(root);
        resumePointerHover(root);
        cancelPointerMove();
        if (root.dataset.cfxMode === 'pan' && hasFeature(root, 'Pan')) {
          if (connectedXViewport(root)) {
            const x = logicalXPointer(root, event);
            if (x === null || !chartXProjection(root)) return;
            drag = { mode: 'native-pan', id: event.pointerId, x, delta: 0 };
            event.preventDefault(); stage.setPointerCapture(event.pointerId);
            return;
          }
          const state = getState(root);
          const point = stagePointer(stage, event);
          drag = { mode: 'pan', id: event.pointerId, x: point.x, y: point.y, panX: state.panX, panY: state.panY };
          event.preventDefault();
          stage.setPointerCapture(event.pointerId);
        } else if (root.dataset.cfxMode === 'brush' && hasFeature(root, 'Brush') && brush) {
          const point = brushPointer(root, stage, event);
          const previousBrush = { hidden: brush.hidden, left: brush.style.left, top: brush.style.top,
            width: brush.style.width, height: brush.style.height };
          drag = { mode: 'brush', id: event.pointerId, left: point.x, top: point.y, previousBrush };
          brush.hidden = false;
          brush.style.left = drag.left + 'px';
          setBrushBox(root, stage, brush, drag, point);
          event.preventDefault();
          stage.setPointerCapture(event.pointerId);
        }
      });
      listenChart(root, stage, 'pointermove', (event) => {
        resumePointerHover(root);
        if (drag ? drag.id !== event.pointerId : !ownsTooltipPointer(root, event)) return;
        // A shadow event is retargeted as dispatch leaves the chart. Capture its
        // native surface now, while retaining the PointerEvent for timing and ownership.
        const nativeTarget = event.composedPath().find(node => node instanceof Element && stage.contains(node)) || event.target;
        pendingPointerMove = { event, nativeTarget };
        if (pointerFrame) return;
        pointerFrame = requestAnimationFrame(() => {
          pointerFrame = 0;
          const pending = pendingPointerMove; pendingPointerMove = null;
          if (pending && root.isConnected && chartLifetimes.get(root)?.disposed === false) applyPointerMove(pending.event, pending.nativeTarget);
        });
      });
      listenChart(root, stage, 'pointerup', (event) => {
        if (!drag || drag.id !== event.pointerId) return;
        cancelPointerMove();
        applyPointerMove(event);
        if (drag.mode === 'native-pan') {
          const plot = chartXProjection(root)?.plotBounds;
          if (plot && Math.abs(drag.delta) > 0.5) requestXViewport(root, 'pan', plot.x - drag.delta, plot.x + plot.width - drag.delta);
        } else if (drag.mode === 'brush' && brush) {
          const dataRange = finishBrushSelection(root, stage, brush);
          root.dataset.cfxBrush = [brush.style.left, brush.style.top, brush.style.width, brush.style.height].join(' ');
          syncResetControl(root);
          const previousTargets = selectedTargets(root), previousSelection = previousTargets.map(targetKey).join('\n');
          const boxTargets = selectTargetsInBox(root, brush.getBoundingClientRect(), event.shiftKey, dataRange);
          const replaceSelection = !event.shiftKey;
          emitHostEvent(root, 'cfxbrush', { bounds: root.dataset.cfxBrush, ...dataRange });
          if (boxTargets.length || replaceSelection) emitHostEvent(root, 'cfxlasso', { bounds: root.dataset.cfxBrush, count: boxTargets.length, targets: boxTargets, ...dataRange });
          if (previousSelection !== selectionSignature(root)) publishSelectionChange(root, 'lasso');
          emitSync(root, { action: 'brush', bounds: root.dataset.cfxBrush });
          if (boxTargets.length || replaceSelection) emitSync(root, { action: 'lasso', bounds: root.dataset.cfxBrush,
            targets: selectedTargets(root), previousTargets, replace: replaceSelection });
          publishCompare(root, false);
        } else if (drag.mode === 'pan') {
          emitHostEvent(root, 'cfxviewport', { state: getState(root) });
          emitSync(root, { action: 'viewport', state: getState(root) });
        }
        stage.releasePointerCapture(event.pointerId);
        drag = null;
      });
      listenChart(root, stage, 'pointerleave', (event) => {
        if (!ownsTooltipPointer(root, event)) return;
        if (!drag) cancelPointerMove();
        hideCrosshair(root, crosshair);
        clearHover(root, true, true);
        hideTip(root, tip, false);
      });
      listenChart(root, stage, 'pointercancel', (event) => {
        if (drag?.id === event.pointerId || !drag && ownsTooltipPointer(root, event)) cancelPointerMove();
        if (drag && drag.id === event.pointerId) {
          // Cancel only the provisional gesture; a previously committed selection and box remain current.
          if (drag.mode === 'brush' && brush) {
            brush.hidden = drag.previousBrush.hidden;
            for (const name of ['left', 'top', 'width', 'height']) brush.style[name] = drag.previousBrush[name];
          }
          drag = null;
        }
        if (!ownsTooltipPointer(root, event)) return;
        hideCrosshair(root, crosshair);
        clearHover(root, true, true);
        hideTip(root, tip, false);
      });
    }
    const reset = root.querySelector('[data-cfx-reset]');
    if (reset) listenChart(root, window, 'resize', () => syncResetControl(root));
    if (reset) listenChart(root, reset, 'click', () => {
      if (connectedXViewport(root) && chartXProjection(root) && (nativeXWindowChanged(root) || chartLifetimes.get(root)?.xViewportPending)) {
        requestXViewport(root, 'reset');
        return;
      }
      resetViewport(root);
      emitHostEvent(root, 'cfxreset', {});
      emitHostEvent(root, 'cfxviewport', { state: getState(root) });
      emitSync(root, { action: 'viewport', state: getState(root) });
      const previousSelection = selectionSignature(root);
      clearSelections(root);
      root.querySelectorAll('.cfx-series-muted').forEach((node) => node.classList.remove('cfx-series-muted'));
      root.querySelectorAll('[data-cfx-muted]').forEach((node) => node.removeAttribute('data-cfx-muted'));
      setSeriesIsolation(root, null, false);
      refreshKeyboardNavigation(root);
      clearFocusTrail(root);
      clearReveals(root);
      hideCrosshair(root, crosshair);
      hideTip(root, tip, true);
      if (root._cfxBranchHighlights) clearHover(root, true, true);
      publishResetSelection(root, previousSelection);
      syncResetControl(root);
    });
  };
