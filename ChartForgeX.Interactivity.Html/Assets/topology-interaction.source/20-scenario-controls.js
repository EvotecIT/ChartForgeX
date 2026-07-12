    const scenarioRoute = scenarioId => {
      if (!scenarioId) return null;
      const scenarioButton = findScenarioButton(scenarioId);
      const scenarioSummary = scenarioButton ? null : findScenarioSummary(scenarioId);
      if (!scenarioButton && !scenarioSummary) return null;
      const steps = scenarioButton ? scenarioSteps(scenarioButton) : (Array.isArray(scenarioSummary.steps) ? scenarioSummary.steps : []);
      const metadata = scenarioButton ? scenarioMetadata(scenarioButton) : (scenarioSummary.metadata && typeof scenarioSummary.metadata === 'object' && !Array.isArray(scenarioSummary.metadata) ? scenarioSummary.metadata : {});
      const nodeIds = new Set();
      const edgeIds = new Set();
      const groupIds = new Set();
      wrapper.querySelectorAll('[data-cfx-role="topology-node"][data-scenario-ids]').forEach(node => {
        if (!scenarioIds(node).includes(scenarioId)) return;
        nodeIds.add(attr(node, 'data-node-id'));
        groupIds.add(attr(node, 'data-group-id'));
      });
      wrapper.querySelectorAll('[data-cfx-role="topology-edge"][data-scenario-ids]').forEach(edge => {
        if (!scenarioIds(edge).includes(scenarioId)) return;
        edgeIds.add(attr(edge, 'data-edge-id'));
        nodeIds.add(attr(edge, 'data-source-node-id'));
        nodeIds.add(attr(edge, 'data-target-node-id'));
        groupIds.add(attr(edge, 'data-source-group-id'));
        groupIds.add(attr(edge, 'data-target-group-id'));
      });
      return {
        scenarioId,
        button: scenarioButton,
        label: scenarioButton ? attr(scenarioButton, 'data-cfx-scenario-label') || scenarioButton.textContent.trim() : scenarioSummary.label || scenarioId,
        description: scenarioButton ? attr(scenarioButton, 'data-cfx-scenario-description') : scenarioSummary.description || '',
        color: scenarioButton ? attr(scenarioButton, 'data-cfx-scenario-color') : scenarioSummary.color || '',
        stepCount: scenarioButton ? Number(attr(scenarioButton, 'data-cfx-scenario-step-count') || '0') : Number(scenarioSummary.stepCount || steps.length || 0),
        metadata,
        steps,
        nodeIds,
        edgeIds,
        groupIds
      };
    };
    const renderScenarioPanel = route => {
      if (!scenarioPanel) return;
      const title = scenarioPanel.querySelector('[data-cfx-scenario-panel-title]');
      const meta = scenarioPanel.querySelector('[data-cfx-scenario-panel-meta]');
      const stepsList = scenarioPanel.querySelector('[data-cfx-scenario-panel-steps]');
      if (!route) {
        scenarioPanel.removeAttribute('data-cfx-panel-active-scenario');
        scenarioPanel.style.removeProperty('--cfx-topology-scenario-color');
        if (title) title.textContent = 'All';
        if (meta) meta.textContent = 'All topology routes visible';
        if (stepsList) stepsList.replaceChildren();
        return;
      }

      scenarioPanel.setAttribute('data-cfx-panel-active-scenario', route.scenarioId);
      if (route.color) scenarioPanel.style.setProperty('--cfx-topology-scenario-color', route.color);
      else scenarioPanel.style.removeProperty('--cfx-topology-scenario-color');
      if (title) title.textContent = route.label || route.scenarioId;
      if (meta) {
        const metadata = Object.entries(route.metadata).slice(0, 2).map(([key, value]) => key + ': ' + value);
        const summary = [route.description, route.stepCount ? route.stepCount + ' steps' : '', ...metadata].filter(value => value);
        meta.textContent = summary.join(' / ');
      }
      if (stepsList) {
        stepsList.replaceChildren();
        route.steps.forEach(step => {
          const item = document.createElement('li');
          item.setAttribute('data-cfx-scenario-step-kind', step.kind || '');
          item.setAttribute('data-cfx-scenario-step-id', step.id || '');
          item.setAttribute('data-cfx-scenario-step-index', String((Number(step.index) || 0) + 1));
          item.setAttribute('role', 'button');
          item.setAttribute('tabindex', '0');
          item.textContent = step.label || step.id || '';
          stepsList.appendChild(item);
        });
      }
    };
    const clearScenarioPreview = () => {
      wrapper.style.removeProperty('--cfx-topology-preview-scenario-color');
      wrapper.querySelectorAll('.cfx-topology-html-scenario-preview,.cfx-topology-html-scenario-preview-muted').forEach(item => {
        item.classList.remove('cfx-topology-html-scenario-preview', 'cfx-topology-html-scenario-preview-muted');
      });
    };
    const previewScenario = scenarioId => {
      clearScenarioPreview();
      if (!scenarioId) return;
      const route = scenarioRoute(scenarioId);
      if (!route) return;
      if (route.color) wrapper.style.setProperty('--cfx-topology-preview-scenario-color', route.color);
      wrapper.querySelectorAll('[data-cfx-role="topology-edge"]').forEach(edge => {
        const active = route.edgeIds.has(attr(edge, 'data-edge-id'));
        edge.classList.toggle('cfx-topology-html-scenario-preview', active);
        edge.classList.toggle('cfx-topology-html-scenario-preview-muted', !active);
      });
      wrapper.querySelectorAll('[data-cfx-role="topology-edge-label"]').forEach(label => {
        const active = route.edgeIds.has(attr(label, 'data-edge-id'));
        label.classList.toggle('cfx-topology-html-scenario-preview', active);
        label.classList.toggle('cfx-topology-html-scenario-preview-muted', !active);
      });
      wrapper.querySelectorAll('[data-cfx-role="topology-node"]').forEach(node => {
        const active = route.nodeIds.has(attr(node, 'data-node-id'));
        node.classList.toggle('cfx-topology-html-scenario-preview', active);
        node.classList.toggle('cfx-topology-html-scenario-preview-muted', !active);
      });
      wrapper.querySelectorAll('[data-cfx-role="topology-node-status"]').forEach(status => {
        const active = route.nodeIds.has(attr(status, 'data-node-id'));
        status.classList.toggle('cfx-topology-html-scenario-preview', active);
        status.classList.toggle('cfx-topology-html-scenario-preview-muted', !active);
      });
      wrapper.querySelectorAll('[data-cfx-role="topology-group"]').forEach(group => {
        const active = route.groupIds.has(attr(group, 'data-group-id'));
        group.classList.toggle('cfx-topology-html-scenario-preview', active);
        group.classList.toggle('cfx-topology-html-scenario-preview-muted', !active);
      });
      wrapper.dispatchEvent(new CustomEvent('cfx-topology-scenario-preview', { bubbles: true, detail: { chartId: attr(wrapper, 'data-chart-id'), scenarioId, label: route.label, description: route.description, color: route.color, stepCount: route.stepCount, metadata: route.metadata, steps: route.steps, nodeIds: Array.from(route.nodeIds), edgeIds: Array.from(route.edgeIds), groupIds: Array.from(route.groupIds) } }));
    };
    const updateScenarioStepControls = playing => {
      const hasRoute = !!scenarioRoute(attr(wrapper, 'data-cfx-active-scenario'));
      wrapper.querySelectorAll('[data-cfx-scenario-step-control]').forEach(button => {
        button.disabled = !hasRoute;
        if (attr(button, 'data-cfx-scenario-step-control') === 'play') button.setAttribute('aria-pressed', playing ? 'true' : 'false');
      });
    };
    const clearScenarioStep = () => {
      wrapper.removeAttribute('data-cfx-active-scenario-step');
      wrapper.querySelectorAll('.cfx-topology-html-scenario-step-active,.cfx-topology-html-scenario-step-muted').forEach(item => item.classList.remove('cfx-topology-html-scenario-step-active', 'cfx-topology-html-scenario-step-muted'));
      if (scenarioPanel) scenarioPanel.querySelectorAll('[data-cfx-scenario-step-id]').forEach(item => item.removeAttribute('aria-current'));
      updateScenarioStepControls(false);
    };
    const stopScenarioPlayback = () => {
      if (scenarioPlayback) window.clearInterval(scenarioPlayback);
      scenarioPlayback = null;
      updateScenarioStepControls(false);
    };
    const setScenarioStep = (stepIndex, keepPlaying, emit = true, sync = true) => {
      const route = scenarioRoute(attr(wrapper, 'data-cfx-active-scenario'));
      if (!route || !route.steps.length) return;
      const requested = Number(stepIndex);
      const index = clamp(Number.isFinite(requested) ? requested : 0, 0, route.steps.length - 1);
      wrapper.setAttribute('data-cfx-active-scenario-step', String(index));
      wrapper.querySelectorAll('[data-cfx-role="topology-edge"],[data-cfx-role="topology-node"]').forEach(item => {
        const routeMember = item.classList.contains('cfx-topology-html-scenario-active');
        const current = routeMember && scenarioStepIndices(item, route.scenarioId).includes(String(index));
        item.classList.toggle('cfx-topology-html-scenario-step-active', current);
        item.classList.toggle('cfx-topology-html-scenario-step-muted', routeMember && !current);
      });
      if (scenarioPanel) scenarioPanel.querySelectorAll('[data-cfx-scenario-step-id]').forEach(item => {
        if (attr(item, 'data-cfx-scenario-step-index') === String(index + 1)) item.setAttribute('aria-current', 'step');
        else item.removeAttribute('aria-current');
      });
      if (!keepPlaying) stopScenarioPlayback();
      if (emit) wrapper.dispatchEvent(new CustomEvent('cfx-topology-scenario-step', { bubbles: true, detail: { chartId: attr(wrapper, 'data-chart-id'), scenarioId: route.scenarioId, index, step: route.steps[index] } }));
      syncScenarioUrl(route.scenarioId, index);
      if (sync) emitSync('scenario-step', { scenarioId: route.scenarioId, index });
    };
    const stepScenario = delta => {
      const route = scenarioRoute(attr(wrapper, 'data-cfx-active-scenario'));
      if (!route) return;
      const current = Number(wrapper.getAttribute('data-cfx-active-scenario-step') || (delta > 0 ? '-1' : route.steps.length));
      setScenarioStep(current + delta, false);
    };
    const playScenario = () => {
      const route = scenarioRoute(attr(wrapper, 'data-cfx-active-scenario'));
      if (!route || !route.steps.length) return;
      if (scenarioPlayback) {
        stopScenarioPlayback();
        return;
      }
      updateScenarioStepControls(true);
      let index = Number(wrapper.getAttribute('data-cfx-active-scenario-step') || '-1') + 1;
      if (!Number.isFinite(index) || index >= route.steps.length) index = 0;
      scenarioPlayback = window.setInterval(() => {
        if (index >= route.steps.length) {
          stopScenarioPlayback();
          return;
        }
        setScenarioStep(index++, true);
      }, 900);
      setScenarioStep(index++, true);
    };
    const copyScenarioLink = () => { syncScenarioUrl(attr(wrapper, 'data-cfx-active-scenario') || attr(wrapper, 'data-cfx-active-scenarios'), wrapper.getAttribute('data-cfx-active-scenario-step') || ''); const url = window.location.href; if (navigator.clipboard && navigator.clipboard.writeText) navigator.clipboard.writeText(url).catch(() => {}); wrapper.dispatchEvent(new CustomEvent('cfx-topology-scenario-link', { bubbles: true, detail: { chartId: attr(wrapper, 'data-chart-id'), url } })); };
    const emitFullscreenState = () => wrapper.dispatchEvent(new CustomEvent('cfx-topology-fullscreen', { bubbles: true, detail: { chartId: attr(wrapper, 'data-chart-id'), fullscreen: document.fullscreenElement === wrapper } }));
    const setFullscreenState = () => { wrapper.setAttribute('data-cfx-topology-fullscreen', document.fullscreenElement === wrapper ? 'true' : 'false'); emitFullscreenState(); };
    const toggleFullscreen = () => {
      if (!document.fullscreenElement && wrapper.requestFullscreen) wrapper.requestFullscreen().catch(() => {});
      else if (document.fullscreenElement === wrapper && document.exitFullscreen) document.exitFullscreen().catch(() => {});
    };
