    const forceSearchText = element => [
      attr(element, 'data-node-id'), attr(element, 'data-node-label'), attr(element, 'data-node-kind'), attr(element, 'data-group-id'),
      attr(element, 'data-edge-id'), attr(element, 'data-edge-label'), attr(element, 'data-edge-secondary-label'), attr(element, 'data-edge-tertiary-label'), attr(element, 'data-edge-kind'), attr(element, 'data-source-node-id'), attr(element, 'data-target-node-id'),
      attr(element, 'data-group-label'), attr(element, 'data-group-symbol'), element.textContent || ''
    ].join(' ').toLowerCase();
    const cssEscape = value => window.CSS && CSS.escape ? CSS.escape(value) : String(value).replace(/["\\]/g, '\\$&');
    const forceGraphState = () => ({
      query: ((forceGraphPanel && forceGraphPanel.querySelector('[data-cfx-force-search]')) || {}).value || '',
      status: ((forceGraphPanel && forceGraphPanel.querySelector('[data-cfx-force-status]')) || {}).value || '',
      group: ((forceGraphPanel && forceGraphPanel.querySelector('[data-cfx-force-group]')) || {}).value || '',
      edges: !!(forceGraphPanel && forceGraphPanel.querySelector('[data-cfx-force-toggle="edges"]:checked')),
      focus: !!(forceGraphPanel && forceGraphPanel.querySelector('[data-cfx-force-toggle="focus"]:checked')),
      labels: !!(forceGraphPanel && forceGraphPanel.querySelector('[data-cfx-force-toggle="edge-labels"]:checked')),
      groups: !!(forceGraphPanel && forceGraphPanel.querySelector('[data-cfx-force-toggle="groups"]:checked')),
      hideMovingEdges: !!(forceGraphPanel && forceGraphPanel.querySelector('[data-cfx-force-toggle="hide-moving-edges"]:checked'))
    });
    const setForceHidden = (selector, predicate) => {
      wrapper.querySelectorAll(selector).forEach(element => element.classList.toggle('cfx-topology-html-force-hidden', predicate(element)));
    };
    const applyForceGraphFilters = () => {
      if (!forceGraphControls || !forceGraphPanel) return;
      const state = forceGraphState();
      const query = state.query.trim().toLowerCase();
      const visibleNodes = new Set();
      const queryNodes = new Set();
      const statusNodes = new Set();
      if (query) {
        wrapper.querySelectorAll('[data-cfx-role="topology-edge"]').forEach(edge => {
          if (!forceSearchText(edge).includes(query)) return;
          queryNodes.add(attr(edge, 'data-source-node-id'));
          queryNodes.add(attr(edge, 'data-target-node-id'));
        });
        wrapper.querySelectorAll('[data-cfx-role="topology-group"]').forEach(group => {
          if (!forceSearchText(group).includes(query)) return;
          const groupId = attr(group, 'data-group-id');
          wrapper.querySelectorAll('[data-cfx-role="topology-node"][data-group-id="' + cssEscape(groupId) + '"]').forEach(node => queryNodes.add(attr(node, 'data-node-id')));
        });
      }
      if (state.status) {
        wrapper.querySelectorAll('[data-cfx-role="topology-edge"]').forEach(edge => {
          if (attr(edge, 'data-cfx-status') !== state.status) return;
          statusNodes.add(attr(edge, 'data-source-node-id'));
          statusNodes.add(attr(edge, 'data-target-node-id'));
        });
      }
      wrapper.setAttribute('data-cfx-force-edges-visible', state.edges ? 'true' : 'false');
      wrapper.setAttribute('data-cfx-force-focus', state.focus ? 'true' : 'false');
      wrapper.setAttribute('data-cfx-force-edge-labels-visible', state.labels ? 'true' : 'false');
      wrapper.setAttribute('data-cfx-force-groups-visible', state.groups ? 'true' : 'false');
      wrapper.setAttribute('data-cfx-force-hide-moving-edges', state.hideMovingEdges ? 'true' : 'false');
      wrapper.querySelectorAll('[data-cfx-role="topology-node"]').forEach(node => {
        const groupOk = !state.group || attr(node, 'data-group-id') === state.group;
        const statusOk = !state.status || attr(node, 'data-cfx-status') === state.status || statusNodes.has(attr(node, 'data-node-id'));
        const queryOk = !query || forceSearchText(node).includes(query) || queryNodes.has(attr(node, 'data-node-id'));
        const visible = groupOk && statusOk && queryOk;
        node.classList.toggle('cfx-topology-html-force-hidden', !visible);
        wrapper.querySelectorAll('[data-cfx-role="topology-node-status"][data-node-id="' + cssEscape(attr(node, 'data-node-id')) + '"]').forEach(status => status.classList.toggle('cfx-topology-html-force-hidden', !visible));
        if (visible) visibleNodes.add(attr(node, 'data-node-id'));
      });
      const nodeById = id => wrapper.querySelector('[data-cfx-role="topology-node"][data-node-id="' + cssEscape(id) + '"]');
      setForceHidden('[data-cfx-role="topology-edge"]', edge => {
        const sourceId = attr(edge, 'data-source-node-id');
        const targetId = attr(edge, 'data-target-node-id');
        const source = nodeById(sourceId);
        const target = nodeById(targetId);
        const endpointsVisible = visibleNodes.has(sourceId) && visibleNodes.has(targetId);
        const edgeQueryOk = !query || forceSearchText(edge).includes(query) || (source && forceSearchText(source).includes(query)) || (target && forceSearchText(target).includes(query));
        const edgeStatusOk = !state.status || attr(edge, 'data-cfx-status') === state.status || (source && attr(source, 'data-cfx-status') === state.status) || (target && attr(target, 'data-cfx-status') === state.status);
        return !state.edges || !endpointsVisible || !edgeQueryOk || !edgeStatusOk;
      });
      setForceHidden('[data-cfx-role="topology-edge-label"]', label => !state.labels || !state.edges || !wrapper.querySelector('[data-cfx-role="topology-edge"][data-edge-id="' + cssEscape(attr(label, 'data-edge-id')) + '"]:not(.cfx-topology-html-force-hidden)'));
      syncTopologyGroupVisibility();
      syncTopologyHighlightAppearance();
      const detail = publishTopologyFilterSummary();
      wrapper.dispatchEvent(new CustomEvent('cfx-topology-force-filter', { bubbles: true, composed: true, detail: { ...detail, query: state.query, status: state.status, group: state.group, kind: '' } }));
      restoreForceFocusLabels();
    };
    const clearForceFocusLabels = () => {
      if (!forceGraphControls) return;
      const state = forceGraphPanel ? forceGraphState() : { labels: false };
      wrapper.querySelectorAll('.cfx-topology-html-force-focus-label').forEach(label => {
        label.classList.remove('cfx-topology-html-force-focus-label');
        if (!state.labels) label.classList.add('cfx-topology-html-force-hidden');
      });
    };
    const selectedFocusElement = () => {
      const kind = wrapper.getAttribute('data-cfx-selection-kind') || '';
      const id = wrapper.getAttribute('data-cfx-selection-id') || '';
      return kind && id ? findSelection({ kind, id }) : null;
    };
    const restoreForceFocusLabels = () => {
      if (!forceGraphControls || !forceGraphPanel) return;
      const selected = selectedFocusElement();
      if (selected && isViewportVisible(selected)) applyForceFocusLabels(identity(selected));
      else clearForceFocusLabels();
    };
    const applyForceFocusLabels = detail => {
      if (!forceGraphControls || !forceGraphPanel) return;
      const state = forceGraphState();
      if (!state.focus || !state.edges) {
        clearForceFocusLabels();
        return;
      }
      detail.related = detail.related || related(detail);
      const edgeIds = new Set(detail.related.edgeIds || []);
      if (detail.kind === 'edge') edgeIds.add(detail.id);
      wrapper.querySelectorAll('[data-cfx-role="topology-edge-label"]').forEach(label => {
        const edgeId = attr(label, 'data-edge-id');
        const edge = wrapper.querySelector('[data-cfx-role="topology-edge"][data-edge-id="' + cssEscape(edgeId) + '"]:not(.cfx-topology-html-force-hidden)');
        const active = edgeIds.has(edgeId) && !!edge;
        label.classList.toggle('cfx-topology-html-force-focus-label', active);
        if (active) label.classList.remove('cfx-topology-html-force-hidden');
        else if (!state.labels) label.classList.add('cfx-topology-html-force-hidden');
      });
    };
    wrapper.querySelectorAll(selectables).forEach(element => {
      if (!element.hasAttribute('tabindex')) element.setAttribute('tabindex', '0');
      if (!element.hasAttribute('role')) element.setAttribute('role', 'button');
      listen(element, 'pointerenter', () => hover(element));
      listen(element, 'pointerleave', () => {
        clearHover();
        wrapper.dispatchEvent(new CustomEvent('cfx-topology-hover-clear', { bubbles: true, composed: true }));
      });
      listen(element, 'focus', () => hover(element));
      listen(element, 'blur', () => {
        clearHover();
        wrapper.dispatchEvent(new CustomEvent('cfx-topology-hover-clear', { bubbles: true, composed: true }));
      });
    });
    const initiallySelected = Array.from(wrapper.querySelectorAll(selectables)).find(element => wrapper.__cfxTopologySelectionHydrated
      ? element.getAttribute('aria-selected') === 'true' : attr(element, 'data-cfx-selected') === 'true');
    wrapper.__cfxTopologySelectionHydrated = true;
    if (initiallySelected) hydrateSelected(initiallySelected);
    else renderSelectionPanel(null);
    if (viewportControls) {
      applyViewport(viewportState());
      wrapper.querySelectorAll('[data-cfx-topology-zoom]').forEach(button => {
        listen(button, 'click', () => zoomBy(attr(button, 'data-cfx-topology-zoom') === 'in' ? 1.2 : 0.8333333333));
      });
      wrapper.querySelectorAll('[data-cfx-topology-mode]').forEach(button => {
        listen(button, 'click', () => setViewportMode(attr(button, 'data-cfx-topology-mode')));
      });
      wrapper.querySelectorAll('[data-cfx-topology-fit]').forEach(button => {
        listen(button, 'click', () => fitViewport());
      });
      wrapper.querySelectorAll('[data-cfx-topology-reset]').forEach(button => {
        listen(button, 'click', () => resetViewport());
      });
      let drag = null;
      let suppressClick = false;
      const endViewportDrag = (event, canceled = false, emit = true) => {
        if (!drag || (event && drag.id !== event.pointerId)) return;
        const ended = drag;
        drag = null;
        suppressClick = ended.moved && !canceled;
        try { if (viewport.releasePointerCapture) viewport.releasePointerCapture(ended.id); } catch { }
        if (forceGraphMotionTimer) lifetime.clearTimeout(forceGraphMotionTimer);
        forceGraphMotionTimer = null;
        wrapper.removeAttribute('data-cfx-force-moving-edges');
        wrapper.removeAttribute('data-cfx-topology-dragging');
        wrapper.querySelectorAll('.cfx-topology-html-force-moving').forEach(edge => edge.classList.remove('cfx-topology-html-force-moving'));
        if (ended.moved && emit) emitViewport();
      };
      lifetime.own(() => endViewportDrag(null, true, false));
      const isViewportChrome = target => target instanceof Element && target.closest('.cfx-topology-controls,.cfx-topology-scenarios,.cfx-topology-scenario-panel,.cfx-topology-selection-panel,.cfx-topology-force-controls');
      listen(viewport, 'wheel', event => {
        if (isViewportChrome(event.target)) return;
        event.preventDefault();
        zoomBy(event.deltaY < 0 ? 1.08 : 0.92, event);
      }, { passive: false });
      listen(viewport, 'pointerdown', event => {
        if (event.button !== 0) return;
        if (isViewportChrome(event.target)) return;
        if (!(event.target instanceof Element && event.target.closest(selectables))) event.preventDefault();
        const state = viewportState();
        suppressClick = false;
        drag = { id: event.pointerId, x: event.clientX, y: event.clientY, panX: state.panX, panY: state.panY, moved: false };
        wrapper.setAttribute('data-cfx-topology-dragging', 'true');
      });
      listen(viewport, 'pointermove', event => {
        if (!drag || drag.id !== event.pointerId) return;
        const dx = event.clientX - drag.x;
        const dy = event.clientY - drag.y;
        if (Math.abs(dx) > 2 || Math.abs(dy) > 2) {
          // Capturing a simple press retargets its click away from the semantic node or link.
          if (!drag.moved) {
            try { if (viewport.setPointerCapture) viewport.setPointerCapture(event.pointerId); } catch { }
          }
          drag.moved = true;
          suppressClick = true;
          markForceGraphMoving();
        }
        applyViewport({ zoom: viewportState().zoom, panX: drag.panX + dx, panY: drag.panY + dy });
      });
      // Before movement starts there is no capture, so release can occur outside this viewport.
      listen(window, 'pointerup', event => endViewportDrag(event), true);
      listen(window, 'pointercancel', event => endViewportDrag(event, true), true);
      listen(window, 'blur', () => endViewportDrag(null, true));
      listen(viewport, 'lostpointercapture', event => {
        // A touch node's implicit capture can end when the viewport acquires the moving gesture.
        if (event.target === viewport) endViewportDrag(event, true);
      });
      listen(wrapper, 'cfx-topology-set-viewport', event => {
        applyViewport(event.detail || {});
        emitViewport();
      });
      listen(wrapper, 'cfx-topology-reset-viewport', () => resetViewport());
      listen(wrapper, 'cfx-topology-fit-visible', event => {
        const detail = event.detail || {};
        if (detail.kind && detail.id) {
          const element = findSelection(detail);
          if (element) {
            fitViewportToElements([element]);
            return;
          }
        }

        fitVisibleTopology();
      });
      listen(wrapper, 'click', event => {
        if (isViewportChrome(event.target)) return;
        if (!suppressClick) return;
        suppressClick = false;
        event.preventDefault();
        event.stopPropagation();
      }, true);
    }
    if (exportControls) {
      wrapper.querySelectorAll('[data-cfx-topology-export]').forEach(button => {
        listen(button, 'click', () => {
          if (attr(button, 'data-cfx-topology-export') === 'png') exportPng();
          else exportSvg();
        });
      });
    }
    if (fullscreenControl) {
      wrapper.querySelectorAll('[data-cfx-topology-fullscreen]').forEach(button => {
        listen(button, 'click', () => toggleFullscreen());
      });
      listen(document, 'fullscreenchange', setFullscreenState);
      setFullscreenState();
    }
    if (forceGraphControls && forceGraphPanel) {
      forceGraphPanel.querySelectorAll('input,select').forEach(control => {
        const filterAndReveal = () => {
          applyForceGraphFilters();
          // Query changes reveal results; appearance toggles preserve the reader's viewport.
          if (control.matches('[data-cfx-force-search],[data-cfx-force-status],[data-cfx-force-group]')) fitVisibleTopology();
        };
        listen(control, control.tagName === 'SELECT' ? 'change' : 'input', filterAndReveal);
      });
      listen(wrapper, 'cfx-topology-force-filter-set', event => {
        const detail = event.detail || {};
        if (forceGraphPanel.querySelector('[data-cfx-force-search]') && detail.query !== undefined) forceGraphPanel.querySelector('[data-cfx-force-search]').value = detail.query || '';
        if (forceGraphPanel.querySelector('[data-cfx-force-status]') && detail.status !== undefined) forceGraphPanel.querySelector('[data-cfx-force-status]').value = detail.status || '';
        if (forceGraphPanel.querySelector('[data-cfx-force-group]') && detail.group !== undefined) forceGraphPanel.querySelector('[data-cfx-force-group]').value = detail.group || '';
        applyForceGraphFilters();
      });
      applyForceGraphFilters();
    }
    listen(wrapper, 'cfx-topology-set-filter', event => applyTopologyFilter(event.detail || {}));
    listen(wrapper, 'cfx-topology-clear-filter', () => clearTopologyFilter());
    if (scenarioControls) {
      if (scenarioControlMode === 'checkboxes') {
        wrapper.querySelectorAll('[data-cfx-topology-scenario-toggle]').forEach(input => {
          const scenarioId = attr(input, 'data-cfx-topology-scenario-toggle');
          listen(input, 'change', () => setScenarioFilters(Array.from(wrapper.querySelectorAll('[data-cfx-topology-scenario-toggle]:checked')).map(item => attr(item, 'data-cfx-topology-scenario-toggle'))));
          listen(input, 'pointerenter', () => previewScenario(scenarioId));
          listen(input, 'pointerleave', clearScenarioPreview);
          listen(input, 'focus', () => previewScenario(scenarioId));
          listen(input, 'blur', clearScenarioPreview);
        });
      } else {
        wrapper.querySelectorAll('[data-cfx-topology-scenario]').forEach(button => {
          const scenarioId = attr(button, 'data-cfx-topology-scenario');
          listen(button, 'click', () => setScenario(scenarioId));
          listen(button, 'pointerenter', () => previewScenario(scenarioId));
          listen(button, 'pointerleave', clearScenarioPreview);
          listen(button, 'focus', () => previewScenario(scenarioId));
          listen(button, 'blur', clearScenarioPreview);
        });
      }
      listen(wrapper, 'cfx-topology-set-scenario', event => setScenario((event.detail || {}).scenarioId || ''));
      listen(wrapper, 'cfx-topology-set-scenario-filters', event => setScenarioFilters((event.detail || {}).scenarioIds || []));
      listen(wrapper, 'cfx-topology-clear-scenario', () => setScenario(''));
      listen(wrapper, 'cfx-topology-set-scenario-step', event => setScenarioStep((event.detail || {}).index, false));
      wrapper.querySelectorAll('[data-cfx-scenario-step-control]').forEach(button => {
        listen(button, 'click', () => {
          const action = attr(button, 'data-cfx-scenario-step-control');
          if (action === 'previous') stepScenario(-1);
          else if (action === 'next') stepScenario(1);
          else if (action === 'play') playScenario();
          else if (action === 'reset') { stopScenarioPlayback(); clearScenarioStep(); }
          else if (action === 'link') copyScenarioLink();
        });
      });
      const scrubber = scenarioPanel ? scenarioPanel.querySelector('[data-cfx-scenario-scrubber]') : null;
      if (scrubber) listen(scrubber, 'input', () => { stopScenarioPlayback(); setScenarioStep(Number(scrubber.value) - 1, false); });
      if (scenarioPanel) listen(scenarioPanel, 'keydown', event => {
        if (event.altKey || event.ctrlKey || event.metaKey) return;
        if (event.target instanceof Element && event.target.matches('input,textarea,select')) return;
        const route = scenarioRoute(attr(wrapper, 'data-cfx-active-scenario'));
        if (!route || !route.steps.length) return;
        const key = String(event.key || '').toLowerCase();
        if (key === 'n' || key === 'arrowright') stepScenario(1);
        else if (key === 'b' || key === 'arrowleft') stepScenario(-1);
        else if (key === 'p') playScenario();
        else if (key === 'r' || key === 'home') { stopScenarioPlayback(); setScenarioStep(0, false); }
        else if (key === 'e' || key === 'end') { stopScenarioPlayback(); setScenarioStep(route.steps.length - 1, false); }
        else return;
        event.preventDefault();
      });
      const stepsList = scenarioPanel ? scenarioPanel.querySelector('[data-cfx-scenario-panel-steps]') : null;
      if (stepsList) {
        const focusStep = target => { const item = target instanceof Element ? target.closest('[data-cfx-scenario-step-index]') : null; if (item && stepsList.contains(item)) setScenarioStep(Number(attr(item, 'data-cfx-scenario-step-index')) - 1, false); };
        listen(stepsList, 'click', event => focusStep(event.target));
        listen(stepsList, 'keydown', event => { if (event.key !== 'Enter' && event.key !== ' ') return; event.preventDefault(); focusStep(event.target); });
      }
      const initialScenario = scenarioUrlParam('scenario') || attr(wrapper, 'data-cfx-active-scenario');
      const initialScenarioStep = scenarioUrlParam('scenarioStep');
      if (scenarioControlMode === 'checkboxes') setScenarioFilters(initialScenario ? scenarioIdTokens(initialScenario) : Array.from(wrapper.querySelectorAll('[data-cfx-topology-scenario-toggle]:checked')).map(item => attr(item, 'data-cfx-topology-scenario-toggle')), false, false);
      else setScenario(initialScenario, false, false);
      if (initialScenarioStep && (scenarioControlMode !== 'checkboxes' || attr(wrapper, 'data-cfx-active-scenario'))) setScenarioStep(initialScenarioStep, false, false, false);
      const initialRoute = scenarioRoute(attr(wrapper, 'data-cfx-active-scenario'));
      const reducedMotion = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
      if (initialRoute && initialRoute.autoPlay && !initialScenarioStep && !reducedMotion) playScenario();
    }
    listen(wrapper, 'click', event => {
      const element = event.target instanceof Element ? event.target.closest(selectables) : null;
      if (!element || !wrapper.contains(element)) return;
      event.preventDefault();
      select(element);
    });
    listen(wrapper, 'keydown', event => {
      if (event.key === 'Escape') {
        clear();
        wrapper.dispatchEvent(new CustomEvent('cfx-topology-clear', { bubbles: true, composed: true }));
        emitSync('clear-selection', {});
        return;
      }
      if (event.key === 'ArrowRight' || event.key === 'ArrowDown' || event.key === 'ArrowLeft' || event.key === 'ArrowUp') {
        const element = event.target instanceof Element ? event.target.closest(selectables) : null;
        if (!element || !wrapper.contains(element)) return;
        if (focusRelated(element, event.key === 'ArrowRight' || event.key === 'ArrowDown' ? 1 : -1)) event.preventDefault();
        return;
      }
      if (event.key !== 'Enter' && event.key !== ' ') return;
      const element = event.target instanceof Element ? event.target.closest(selectables) : null;
      if (!element || !wrapper.contains(element)) return;
      event.preventDefault();
      select(element);
    });
    listen(wrapper, 'cfx-topology-set-selection', event => {
      setSelection(event.detail);
    });
    listen(wrapper, 'cfx-topology-clear-selection', event => {
      clear();
      if (event.detail?.emit === true) wrapper.dispatchEvent(new CustomEvent('cfx-topology-clear', { bubbles: true, composed: true }));
      if (event.detail?.sync === true) emitSync('clear-selection', {});
    });
    listen(wrapper, 'cfx-topology-highlight', event => highlight(event.detail));
    listen(wrapper, 'cfx-topology-clear-highlight', () => highlight({}));
    listen(wrapper, 'cfx-topology-apply-sync', event => {
      const detail = event.detail || {};
      if (!syncEnabled || !syncGroup || detail.group !== syncGroup) return;
      applyingSync = true;
      try {
        if (detail.action === 'selection' && detail.selection) {
          setSelection({ ...detail.selection, emit: true });
        } else if (detail.action === 'clear-selection') {
          clear();
          wrapper.dispatchEvent(new CustomEvent('cfx-topology-clear', { bubbles: true, composed: true }));
        } else if (detail.action === 'viewport' && detail.state) {
          applyViewport(detail.state);
          wrapper.dispatchEvent(new CustomEvent('cfx-topology-viewport', { bubbles: true, composed: true, detail: { chartId: attr(wrapper, 'data-chart-id'), state: viewportState(), sourceChartId: detail.chartId } }));
        } else if (detail.action === 'scenario') {
          setScenario(detail.scenarioId || '');
        } else if (detail.action === 'scenario-filters') {
          setScenarioFilters(detail.scenarioIds || []);
        } else if (detail.action === 'scenario-step') {
          if (detail.scenarioId && attr(wrapper, 'data-cfx-active-scenario') !== detail.scenarioId) setScenario(detail.scenarioId);
          setScenarioStep(detail.index, false);
        }
      } finally {
        applyingSync = false;
      }
    });
    }
    return wrappers;
  };
  // Prefix-specific assets share one dispatcher. Re-registering an asset replaces its
  // prefix entry without adding recursive wrappers or invalidating retained mount references.
  const mountsKey = Symbol.for('ChartForgeXTopology.mounts');
  const previous = window.ChartForgeXTopology?.mount;
  const prefixMounts = previous?.[mountsKey] instanceof Map ? previous[mountsKey] : new Map();
  mountForPrefix.dispose = root => {
    const wrappers = root?.matches?.(selector) ? [root] : Array.from((root || document).querySelectorAll(selector));
    return wrappers.filter(wrapper => {
      const lifetime = window[Symbol.for('ChartForgeX.adapterLifetimes')].get(wrapper, 'topology:' + selector);
      if (!lifetime) return false;
      lifetime.dispose(); return true;
    });
  };
  prefixMounts.set(selector, mountForPrefix);
  const mount = previous?.[mountsKey] === prefixMounts ? previous : root => {
    const wrappers = new Set();
    for (const bind of prefixMounts.values()) for (const wrapper of bind(root)) wrappers.add(wrapper);
    return Array.from(wrappers);
  };
  if (!mount[mountsKey]) Object.defineProperty(mount, mountsKey, { value: prefixMounts });
  const dispose = root => {
    const wrappers = new Set();
    for (const bind of prefixMounts.values()) for (const wrapper of bind.dispose?.(root) || []) wrappers.add(wrapper);
    return Array.from(wrappers);
  };
  window.ChartForgeXTopology = Object.freeze({ mount, dispose });
  mount(document);
})();
