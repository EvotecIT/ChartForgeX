    const tip = root.querySelector('.cfx-tooltip');
    const stage = root.querySelector('.cfx-stage');
    const brush = root.querySelector('.cfx-brush-box');
    const crosshair = root.querySelector('.cfx-crosshair');
    const compareTray = root.querySelector('[data-cfx-compare-tray]');
    if (!tip) return;
    listenChart(root, root, 'keydown', event => {
      if (event.key === 'Escape') cancelNativePreparation(chartLifetimes.get(root));
    }, true);
    applyRenderedTargetContract(root);
    bindLegendTargets(root);
    bindStandaloneGestures(root);
    bindLegendOverflow(root);
    bindXNavigation(root);
    bindHierarchyNavigation(root);
    bindProgressEditing(root);
    bindChartContextMenu(root);
    bindChartFeatures(root);
    bindHostCommands(root);
    bindAnnotationEditor(root);
    prepareRootBranchHighlights(root);
    bindLegendHover(root);
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
      // Escape dismisses a pinned readout as well as the selection it came from.
      if (root.dataset.cfxTooltipPinned === 'true') unpinTooltip(root);
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
    // The readout settles the card and toolbar first, so the keyboard watch starts from the laid-out chart rather than
    // refreshing every stop for those writes in the next frame.
    bindStageLayout(root, stage, crosshair);
    prepareKeyboardNavigation(root);
    const targets = Array.from(new Set([...chartTargets(root),
      ...root.querySelectorAll('[data-cfx-role="legend-native"],[data-cfx-role="legend-proxy"]')]));
    // A pointer moving straight onto another target hands the hover over: leaving keeps the chart's shared hover
    // state for the entering target to settle, because clearing and re-adding it restyles every mark.
    const hoverSurfaces = new WeakSet();
    // The stage readout reports a pending frame; a hover handed over settles there, or in a later frame if none ran.
    let stageFrame = () => false;
    const settleHandover = () => root._cfxHoverPending && (stageFrame() ? requestAnimationFrame(settleHandover) : clearHover(root, false, false));
    const handsOver = (from, to) => {
      for (let node = to; node instanceof Element && root.contains(node); node = node.parentElement)
        if (hoverSurfaces.has(node) && !node.contains(from)) return true;
      return false;
    };
    targets.forEach((node) => {
      const focusNode = targetFocusNode(node);
      const labels = pointLabelSurfaces(root, node);
      [node, ...labels].forEach(surface => {
        hoverSurfaces.add(surface);
        const enter = (event) => {
          if (pointerReadoutSuspended(root) || keyboardReadoutOwnsPointer(root)) return;
          if (!pointerTargetPaint(node)) { clearUnavailableHover(root, node); return; }
          if (renderedTargetKind(node) === 'legend' && !legendEntryShown(root, node)) {
            clearLegendReadout(root); showTip(root, tip, node, event); return;
          }
          if (!ownsTooltipPointer(root, event)) hideCrosshair(root, crosshair);
          setHover(root, node, true, true);
          showTip(root, tip, node, event);
        };
        // Unless only exact hits read out, the stage readout resolves a pointer on a line, area or bar series to its
        // observation in its next frame. Hovering the whole series first restyled every mark of a dense series twice.
        const staged = stage?.contains(surface) && renderedTargetKind(node) === 'series' && categoricalPointerKind(node) && !node.matches('[data-cfx-point],[data-cfx-value]')
          && root.dataset.cfxTooltipRange !== 'exact' && (hasFeature(root, 'Tooltips') || hasFeature(root, 'Crosshair'));
        listenChart(root, surface, 'pointerenter', (event) => {
          if (staged && event instanceof PointerEvent) return;
          enter(event);
          if (root._cfxHoverPending) clearHover(root, false, false);
        });
        listenChart(root, surface, 'pointermove', (event) => {
          if (renderedTargetKind(node) === 'legend' && !node.closest('svg') && ownsTooltipPointer(root, event)) {
            resumePointerHover(root);
            if (legendEntryShown(root, node)) setHover(root, node, true, true);
            else clearLegendReadout(root);
            showTip(root, tip, node, event);
            return;
          }
          if (ownsTooltipPointer(root, event)) moveTip(tip, event, node);
        });
        listenChart(root, surface, 'pointerleave', (event) => {
          if (!ownsTooltipPointer(root, event)) return;
          // Moving off a mark onto the drawing hands the hover to the stage readout, which settles it in its next frame:
          // clearing the chart's hover state here and setting it again there restyles every marker of a dense chart.
          const handover = handsOver(surface, event.relatedTarget), staged = !handover && stage?.contains(event.relatedTarget);
          clearHover(root, true, true, handover || staged);
          if (handover || staged) requestAnimationFrame(settleHandover);
          if (!retainPointerTip(root, event)) hideTip(root, tip, false);
        });
      });

      if (hasFeature(root, 'KeyboardNavigation') || focusNode.matches('a[href]')) {
        let pointerFocus = false;

        ['pointerdown', 'mousedown'].forEach(type => listenChart(root, focusNode, type, (event) => { pointerFocus = event.button === 0; }));
        ['pointerup', 'pointercancel', 'pointerleave', 'mouseup'].forEach(type => listenChart(root, focusNode, type, () => { pointerFocus = false; }));
        listenChart(root, focusNode, 'focus', (event) => {
          refreshKeyboardNavigation(root, node);

          const keyboardFocus = focusNode.matches(':focus-visible');
          if (keyboardFocus && !pointerFocus) delete root._cfxPointerFocus;
          refreshKeyboardFocusPaint(root);
          if (hasFeature(root, 'KeyboardNavigation') && !pointerFocus && keyboardFocus) scrollKeyboardTargetIntoView(root, focusNode);
          pointerFocus = false;
          if (!keyboardFocus || root._cfxRecoveringFocus) return;
          resumePointerReadout(root);
          root._cfxKeyboardReadout = node;
          announceChartTarget(root, node);
          if (renderedTargetKind(node) === 'legend' && !legendEntryShown(root, node)) {
            clearLegendReadout(root); showTip(root, tip, node, event); return;
          }
          if (!tooltipReadoutAvailable(node, event)) { clearHover(root, true, true); hideTip(root, tip, false); return; }
          setHover(root, node, true, true);
          // The guide's value pills exist before the readout chooses a side clear of them.
          showFocusCrosshair(root, crosshair, node);
          showTip(root, tip, node, event);
        });
        listenChart(root, focusNode, 'blur', () => {
          pointerFocus = false;
          clearKeyboardFocusPaint(root);
          if (root._cfxKeyboardReadout === node) delete root._cfxKeyboardReadout;
          clearHover(root, true, true);
          hideCrosshair(root, crosshair);
          hideTip(root, tip, false);
        });
      }
      const activateTarget = (event) => {
        if (event.defaultPrevented && !(event instanceof KeyboardEvent)
          || event.target instanceof Element && event.target.closest('[data-cfx-standalone-hierarchy]')) return;
        if (renderedTargetKind(node) !== 'legend' && event instanceof MouseEvent && event.detail > 0 && root._cfxSuppressPlotActivation) {
          event.stopPropagation(); return;
        }
        event.stopPropagation();
        if (event instanceof KeyboardEvent) resumePointerReadout(root);
        const activated = pointerActivationTarget(root, node, event);
        if (!tooltipReadoutAvailable(activated, event)) return;
        if ((activated.dataset ? activated.dataset.cfxRole : '') === 'legend-item') {
          activateLegendItem(root, activated, event);
        }
        else {
          if (!(event instanceof KeyboardEvent && event.key === ' ') && activateHierarchyTarget(root, activated)) return;
          toggleSelection(root, activated, event);
          pinTip(root, tip, activated, event);
        }
      };
      listenChart(root, focusNode, 'click', activateTarget);
      if (renderedTargetKind(node) === 'legend') {
        listenChart(root, focusNode, 'pointerdown', event => { root._cfxLegendTouch = event.pointerType === 'touch'; });
        listenChart(root, focusNode, 'dblclick', event => { event.preventDefault(); event.stopPropagation(); });
      }
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

        if (event.key === 'Enter' && focusNode.matches('a[href]')) return;
        event.preventDefault();
        event.stopPropagation();

        activateTarget(event);
      });
    });
    root.querySelectorAll('[data-cfx-zoom]').forEach((button) => {
      listenChart(root, button, 'click', () => zoomBy(root, button.dataset.cfxZoom === 'in' ? 1.25 : 0.8, undefined, undefined, true));
    });
    const zoomReset = root.querySelector('[data-cfx-zoom-reset]');
    if (zoomReset) listenChart(root, zoomReset, 'click', () => resetZoom(root));
    root.querySelectorAll('[data-cfx-mode-button]').forEach((button) => {
      listenChart(root, button, 'click', () => setMode(root, button.dataset.cfxModeButton || ''));
    });
    root.querySelectorAll('[data-cfx-export]').forEach((button) => {
      listenChart(root, button, 'click', () => {
        if (nativePreparationBackpressureBusy(root)) return;
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
      const updateStageReadout = (event, nativeTarget = event.target) => {
        // The drawing's margin outside its card is page, not chart: the readout clears there as it does off the stage.
        const card = (nativeTarget.closest?.('svg') || stage).querySelector('[data-cfx-role="frame-card"]')?.getBoundingClientRect();
        if (card && (event.clientX < card.left || event.clientX > card.right || event.clientY < card.top || event.clientY > card.bottom)) {
          hideCrosshair(root, crosshair); clearHover(root, true, true); hideTip(root, tip, false); return;
        }
        const legend = nativeTarget instanceof Element && nativeTarget.closest('[data-cfx-role="legend-item"]');
        if (legend && !legendEntryShown(root, legend)) { clearLegendReadout(root); return; }
        updateNearestPoint(root, crosshair, tip, event, nativeTarget);
      };
      // Time a tap with the events' own timestamps: a busy main thread can deliver a quick
      // press's pointerup long after its pointerdown, and that delay is not the user's.
      let tap = null, completedTap = null;
      const tapPointers = new Set();
      // Observe at the root capture boundary: the pinch owner intentionally
      // stops the second pointer before ordinary stage listeners receive it.
      listenChart(root, root, 'pointerdown', event => {
        if (!stage.contains(event.target) || event.button !== 0) return;
        completedTap = null; delete root._cfxSuppressPlotActivation;
        tapPointers.add(event.pointerId);
        if (tapPointers.size > 1) { if (tap) tap.blocked = true; return; }
        tap = { id: event.pointerId, x: event.clientX, y: event.clientY, time: event.timeStamp,
          blocked: ['pan', 'brush'].includes(root.dataset.cfxMode) };
      }, true);
      listenChart(root, root, 'pointermove', event => {
        if (tap?.id === event.pointerId && Math.hypot(event.clientX - tap.x, event.clientY - tap.y) > 8) tap.blocked = true;
      }, true);
      listenChart(root, root.ownerDocument, 'pointerup', event => {
        if (!tapPointers.has(event.pointerId)) return;
        const valid = tap?.id === event.pointerId && !tap.blocked && tapPointers.size === 1 && event.timeStamp - tap.time < 700
          && Math.hypot(event.clientX - tap.x, event.clientY - tap.y) <= 8;
        root._cfxSuppressPlotActivation = !valid;
        completedTap = valid ? { x: event.clientX, y: event.clientY, time: event.timeStamp } : null;
        tapPointers.delete(event.pointerId);
        if (!tapPointers.size) tap = null;
      }, true);
      listenChart(root, root.ownerDocument, 'pointercancel', event => {
        if (!tapPointers.has(event.pointerId)) return;
        tapPointers.delete(event.pointerId); completedTap = null;
        if (tap) tap.blocked = true;
        root._cfxSuppressPlotActivation = true;
      }, true);
      listenChart(root, stage, 'click', event => {
        const tap = completedTap; completedTap = null;
        if (!tap || event.defaultPrevented || root._cfxSuppressPlotActivation || event.timeStamp - tap.time > 800
          || Math.hypot(event.clientX - tap.x, event.clientY - tap.y) > 8
          || event.target instanceof Element && event.target.closest('[data-cfx-standalone-hierarchy]')) return;
        const plot = root._cfxCartesianPlot || root._cfxDataProjection?.plotBounds, point = logicalChartPointer(root, event);
        if (!plot || !point || point.x < plot.x || point.x > plot.x + plot.width || point.y < plot.y || point.y > plot.y + plot.height) return;
        const nearest = acquiredTooltipPoint(root, pointerCandidates(root, event, true));
        if (!nearest || !isDataTarget(nearest.node)) return;
        setHover(root, nearest.node, true, true);
        toggleSelection(root, nearest.node, event);
        pinTip(root, tip, nearest.node, event);
      });
      ownChartWork(root, () => { tapPointers.clear(); tap = null; completedTap = null; delete root._cfxSuppressPlotActivation; });
      listenChart(root, stage, 'keydown', event => {
        if (event.defaultPrevented || !hasFeature(root, 'KeyboardNavigation') || !(event.target instanceof Element)
          || !event.target.closest('[data-cfx-standalone-hierarchy]')) return;
        const node = event.target.closest('[data-cfx-node]');
        if (node && focusAdjacentTarget(root, node, event.key)) { event.preventDefault(); event.stopPropagation(); }
      });
      listenChart(root, stage, 'focusin', event => {
        if (!(event.target instanceof Element) || !event.target.closest('[data-cfx-standalone-hierarchy]')) return;
        const node = event.target.closest('[data-cfx-node]');
        if (node) { refreshKeyboardNavigation(root, node); announceChartTarget(root, node); }
      });
      listenChart(root, stage, 'pointerenter', (event) => {

        if (keyboardReadoutOwnsPointer(root)) return;
        if (!ownsTooltipPointer(root, event)) updateStageReadout(event);
      });
      listenChart(root, stage, 'wheel', (event) => {
        if (!hasFeature(root, 'Zoom') || standaloneZoomDeclined(root)) return;
        const point = logicalChartPointer(root, event);
        if (standaloneViewport(root)) {
          const plot = root._cfxDataProjection.plotBounds;
          if (!point || point.x < plot.x || point.x > plot.x + plot.width || point.y < plot.y || point.y > plot.y + plot.height || !event.deltaY) return;
          event.preventDefault();
          zoomStandaloneViewport(root, event.deltaY < 0 ? 1.08 : 1 / 1.08, point.x, point.y, event.shiftKey ? ['y'] : ['x']);
          return;
        }
        if (polarViewportConnected(root) ? !event.deltaY || event.ctrlKey || event.metaKey || event.altKey || event.shiftKey || !polarPointer(root, point) : connectedXViewport(root) && !chartXProjection(root)) return;
        event.preventDefault();
        zoomBy(root, event.deltaY < 0 ? 1.08 : 0.92, point?.x, point?.y);
      }, { passive: false });
      let drag = null;
      root._cfxCancelStandaloneDrag = () => {
        cancelPointerMove();
        if (drag && stage.hasPointerCapture(drag.id)) stage.releasePointerCapture(drag.id);
        drag = null;
      };
      let pointerFrame = 0, pendingPointerMove = null;
      const cancelPointerMove = () => {
        if (pointerFrame) cancelAnimationFrame(pointerFrame);
        pointerFrame = 0; pendingPointerMove = null;
      };
      stageFrame = () => pointerFrame !== 0;


      root._cfxCancelPointerHover = () => { if (!drag) cancelPointerMove(); };
      const applyPointerMove = (event, nativeTarget = event.target) => {
        if (!drag || drag.id !== event.pointerId) {
          if (!ownsTooltipPointer(root, event)) return;
          updateStageReadout(event, nativeTarget);
          return;
        }
        if (drag.mode === 'polar-pan') {
          const point = polarPointer(root, logicalChartPointer(root, event), false);
          if (point) {
            drag.angle += Math.atan2(Math.sin(point.bearing - drag.lastBearing), Math.cos(point.bearing - drag.lastBearing));
            drag.lastBearing = point.bearing; drag.radial = point.radius - drag.radius;
          } else drag.invalid = true;
        } else if (drag.mode === 'native-pan') {
          const point = logicalChartPointer(root, event);
          if (point) { drag.deltaX = point.x - drag.x; drag.deltaY = point.y - drag.y; }
        } else if (drag.mode === 'pan') {
          const point = stagePointer(stage, event);
          applyViewport(root, { zoom: getState(root).zoom, panX: drag.panX + point.x - drag.x, panY: drag.panY + point.y - drag.y });
        } else if (drag.mode === 'brush' && brush) {
          setBrushBox(root, stage, brush, drag, brushPointer(root, stage, event));
        }
      };


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
        delete root._cfxCancelStandaloneDrag;
      });
      listenChart(root, capturedPolarProjection(root) ? root.ownerDocument : root, 'keydown', event => {
        if (event.key !== 'Escape' || drag?.mode !== 'polar-pan') return;
        cancelPointerMove();
        if (stage.hasPointerCapture(drag.id)) stage.releasePointerCapture(drag.id);
        drag = null;
      });
      listenChart(root, stage, 'lostpointercapture', () => { if (drag?.mode === 'polar-pan') { cancelPointerMove(); drag = null; } });
      listenChart(root, stage, 'pointerdown', (event) => {
        if (event.button !== 0) return;
        root._cfxPointerFocus = true;
        clearKeyboardFocusPaint(root);
        resumePointerHover(root);
        cancelPointerMove();
        if (root.dataset.cfxMode === 'pan' && hasFeature(root, 'Pan')) {
          if (polarViewportConnected(root)) {
            const point = polarPointer(root, logicalChartPointer(root, event));
            if (!point) return;
            drag = { mode: 'polar-pan', id: event.pointerId, lastBearing: point.bearing, radius: point.radius, angle: 0, radial: 0 };
            event.preventDefault(); stage.setPointerCapture(event.pointerId); return;
          }
          if (connectedXViewport(root)) {
            const point = logicalChartPointer(root, event);
            if (!point || !chartXProjection(root)) return;
            drag = { mode: 'native-pan', id: event.pointerId, x: point.x, y: point.y, deltaX: 0, deltaY: 0 };
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


        if (drag?.mode === 'polar-pan') { applyPointerMove(event); return; }
        const nativeTarget = event.composedPath().find(node => node instanceof Element && stage.contains(node)) || event.target;
        pendingPointerMove = { event, nativeTarget };
        if (pointerFrame) return;
        pointerFrame = requestAnimationFrame(() => {
          pointerFrame = 0;
          const pending = pendingPointerMove; pendingPointerMove = null;
          if (pending && root.isConnected && chartLifetimes.get(root)?.disposed === false) applyPointerMove(pending.event, pending.nativeTarget);
          if (root._cfxHoverPending) clearHover(root, false, false);
        });
      });
      listenChart(root, stage, 'pointerup', (event) => {
        if (!drag || drag.id !== event.pointerId) return;
        cancelPointerMove();
        applyPointerMove(event);
        if (drag.mode === 'polar-pan') {
          requestPolarViewportPan(root, drag);
        } else if (drag.mode === 'native-pan') {
          requestNativeViewportPan(root, drag.deltaX, drag.deltaY);
        } else if (drag.mode === 'brush' && brush) {
          const zoomRange = standaloneViewport(root) && hasFeature(root, 'Zoom') ? standaloneBrushWindow(root, brush.getBoundingClientRect()) : null;
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
          if (zoomRange) { requestStandaloneWindow(root, zoomRange, true, undefined, true, true); brush.hidden = true; }
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

          if (drag.mode === 'polar-pan' && stage.hasPointerCapture(drag.id)) stage.releasePointerCapture(drag.id);
          if (drag?.mode === 'brush' && brush) {
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
      if (root._cfxStandaloneHierarchy?.rootId != null) { navigateStandaloneHierarchy(root, null); return; }
      if (hierarchyConnected(root) && (hierarchyViewChanged(root) || nativeSeriesVisibilityChanged(root) || chartLifetimes.get(root)?.preparationPending || nativePreparationBackpressureBusy(root))) {
        requestNativeHierarchy(root, null); return;
      }
      if ((polarViewportConnected(root) && (nativePolarViewportChanged(root) || nativeSeriesVisibilityChanged(root) || chartLifetimes.get(root)?.preparationPending || nativePreparationBackpressureBusy(root)))
        || nativePreparationBinding(root, 'viewport') && chartXProjection(root) && (nativeViewportChanged(root) || nativeSeriesVisibilityChanged(root) || chartLifetimes.get(root)?.preparationPending || nativePreparationBackpressureBusy(root))) {
        requestViewportReset(root);
        return;
      }
      if (nativeLegendConnected(root) && (nativeSeriesVisibilityChanged(root) || chartLifetimes.get(root)?.preparationPending || nativePreparationBackpressureBusy(root))) {
        requestNativeVisibilityReset(root); return;
      }
      settleChartUpdateMotion(root); cancelNativePreparation(chartLifetimes.get(root));
      resetViewport(root);
      emitHostEvent(root, 'cfxreset', {});
      emitViewportReset(root);
      const previousSelection = selectionSignature(root);
      clearSelections(root);
      setSeriesIsolation(root, null, false);
      root.querySelectorAll('.cfx-series-muted').forEach((node) => node.classList.remove('cfx-series-muted'));
      root.querySelectorAll('[data-cfx-muted]').forEach((node) => node.removeAttribute('data-cfx-muted'));
      syncLegendButtons(root);
      syncLegendOverflow(root);
      rescaleStandaloneSeries(root);
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
