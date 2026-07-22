  const readJson = (value, fallback) => {
    try {
      const parsed = JSON.parse(value || '');
      return parsed === null || parsed === undefined ? fallback : parsed;
    } catch {
      return fallback;
    }
  };
  const scenarioUrlParam = (root, name) => {
    if (root.dataset.cfxDeepLinkState !== 'true') return '';
    try {
      const params = new URL(window.location.href).searchParams;
      return params.get(scenarioUrlKey(root, name)) || params.get(name) || '';
    } catch { return ''; }
  };
  const scenarioUrlKey = (root, name) => {
    const chartId = (root.dataset.cfxChartId || 'chart').replace(/[^a-z0-9_.-]+/gi, '-').replace(/^-+|-+$/g, '') || 'chart';
    return 'cfx-' + chartId + '-' + name;
  };
  const syncScenarioUrl = (root, scenarioId, stepIndex) => {
    if (root.dataset.cfxDeepLinkState !== 'true' || !window.history || !window.history.replaceState) return;
    try {
      const url = new URL(window.location.href);
      const scenarioKey = scenarioUrlKey(root, 'scenario');
      const stepKey = scenarioUrlKey(root, 'scenarioStep');
      scenarioId ? url.searchParams.set(scenarioKey, scenarioId) : url.searchParams.delete(scenarioKey);
      stepIndex === undefined || stepIndex === null || stepIndex === '' ? url.searchParams.delete(stepKey) : url.searchParams.set(stepKey, stepIndex);
      url.searchParams.delete('scenario');
      url.searchParams.delete('scenarioStep');
      window.history.replaceState(window.history.state, '', url);
    } catch { }
  };
  const scenarioButton = (root, scenarioId) => Array.from(root.querySelectorAll('[data-cfx-scenario]')).find((button) => (button.dataset.cfxScenario || '') === scenarioId) || null;
  const scenarioRoute = (root, scenarioId) => {
    const button = scenarioButton(root, scenarioId);
    if (!button || !scenarioId) return null;
    return {
      id: scenarioId,
      label: button.dataset.cfxScenarioLabel || button.textContent.trim(),
      description: button.dataset.cfxScenarioDescription || '',
      color: button.dataset.cfxScenarioColor || '',
      steps: readJson(button.dataset.cfxScenarioSteps || '[]', []),
      metadata: readJson(button.dataset.cfxScenarioMetadata || '{}', {}),
      playbackDelayMilliseconds: Number(button.dataset.cfxScenarioPlaybackDelay || '900'),
      loopPlayback: button.dataset.cfxScenarioLoop === 'true',
      autoPlay: button.dataset.cfxScenarioAutoplay === 'true',
      focusMode: button.dataset.cfxScenarioFocusMode || 'highlight'
    };
  };
  const scenarioTargetMatches = (node, step) => {
    const data = node.dataset || {};
    const kind = step.targetKind || '';
    const target = step.targetId || '';
    if (renderedTargetKind(node) === kind && renderedTargetId(node, kind) === target) return true;
    const seriesOrdinalTarget = /^(0|[1-9]\d*)$/.test(target);
    if (kind === 'series' && (seriesOrdinalTarget ? data.cfxSeries === target : seriesKey(node) === target)) return true;
    if (kind === 'point' && data.cfxPoint === target) return true;
    if (kind === 'point' && data.cfxSeries !== undefined && data.cfxPoint !== undefined && (data.cfxSeries + ':' + data.cfxPoint === target || data.cfxSeries + '.' + data.cfxPoint === target)) return true;
    if (kind === 'annotation' && (data.cfxRole || '').indexOf('annotation') === 0 && (data.cfxLabel === target || data.cfxKind === target || data.cfxValue === target || data.cfxId === target || node.id === target)) return true;
    if (kind === 'element' && (data.cfxId === target || node.id === target)) return true;
    if (data.cfxRole === kind && (data.cfxLabel === target || data.cfxText === target || data.cfxValue === target || data.cfxY === target)) return true;
    return false;
  };
  const scenarioTargetCandidates = (root, route) => {
    const visualKinds = new Set(['series', 'point', 'annotation', 'region', 'node', 'link', 'legend']);
    const roleKinds = new Set((route ? route.steps : [])
      .map((step) => step.targetKind || '')
      .filter((kind) => kind && kind !== 'element' && !visualKinds.has(kind)));
    const candidates = new Set(Array.from(root.querySelectorAll('.cfx-interactive-region,[data-cfx-target-kind],[data-cfx-label],[data-cfx-role],[data-cfx-series],[data-cfx-point],[data-cfx-id]'))
      .filter((node) => visualKinds.has(renderedTargetKind(node)) || roleKinds.has((node.dataset || {}).cfxRole || '')));
    const elementIds = new Set((route ? route.steps : [])
      .filter((step) => (step.targetKind || '') === 'element' && step.targetId)
      .map((step) => step.targetId));
    if (elementIds.size) {
      root.querySelectorAll('[id]').forEach((node) => {
        if (!elementIds.has(node.id) || node.closest('defs')) return;
        if (node.querySelector('.cfx-interactive-region,[data-cfx-target-kind],[data-cfx-label],[data-cfx-role],[data-cfx-series],[data-cfx-point],[data-cfx-id]')) return;
        candidates.add(node);
      });
    }
    return Array.from(candidates);
  };
  const scenarioTargets = (root, route, stepIndex) => {
    const matches = new Set();
    if (!route) return matches;
    const steps = stepIndex === undefined || stepIndex === null ? route.steps : route.steps.filter((_, index) => index === Number(stepIndex));
    scenarioTargetCandidates(root, route).forEach((node) => {
      if (steps.some((step) => scenarioTargetMatches(node, step))) matches.add(node);
    });
    return matches;
  };
  const renderScenarioPanel = (root, route) => {
    const panel = root.querySelector('[data-cfx-scenario-panel]');
    if (!panel) return;
    const title = panel.querySelector('[data-cfx-scenario-panel-title]');
    const meta = panel.querySelector('[data-cfx-scenario-panel-meta]');
    const steps = panel.querySelector('[data-cfx-scenario-panel-steps]');
    if (!route) {
      panel.removeAttribute('data-cfx-panel-active-scenario');
      panel.style.removeProperty('--cfx-scenario-color');
      if (title) title.textContent = 'All';
      if (meta) meta.textContent = 'All chart data visible';
      if (steps) steps.replaceChildren();
      return;
    }
    panel.setAttribute('data-cfx-panel-active-scenario', route.id);
    if (route.color) panel.style.setProperty('--cfx-scenario-color', route.color);
    else panel.style.removeProperty('--cfx-scenario-color');
    if (title) title.textContent = route.label || route.id;
    if (meta) {
      const pairs = Object.entries(route.metadata || {}).slice(0, 2).map(([key, value]) => key + ': ' + value);
      meta.textContent = [route.description, route.steps.length ? route.steps.length + ' steps' : '', ...pairs].filter(Boolean).join(' / ');
    }
    if (steps) {
      steps.replaceChildren();
      route.steps.forEach((step, index) => {
        const item = document.createElement('li');
        item.setAttribute('data-cfx-scenario-step-index', String(index + 1));
        item.setAttribute('data-cfx-scenario-target-kind', step.targetKind || '');
        item.setAttribute('data-cfx-scenario-target-id', step.targetId || '');
        if (step.durationMilliseconds) item.setAttribute('data-cfx-scenario-step-duration', String(step.durationMilliseconds));
        item.setAttribute('role', 'button');
        item.setAttribute('tabindex', '0');
        item.textContent = step.label || step.targetId || '';
        steps.appendChild(item);
      });
    }
  };
  const updateScenarioProgress = (root, route, stepIndex) => {
    const progress = root.querySelector('[data-cfx-scenario-progress]');
    const count = route && route.steps ? route.steps.length : 0;
    const hasStep = stepIndex !== undefined && stepIndex !== null && stepIndex !== '';
    const current = count && hasStep ? clamp(Number(stepIndex) + 1, 0, count) : 0;
    const percent = count ? Math.round((current / count) * 100) : 0;
    const label = 'Step ' + current + ' / ' + count;
    if (progress) {
      progress.hidden = !count;
      progress.style.setProperty('--cfx-scenario-progress', percent + '%');
      progress.setAttribute('aria-valuemax', String(count));
      progress.setAttribute('aria-valuenow', String(current));
      progress.setAttribute('aria-valuetext', label);
      const textNode = progress.querySelector('[data-cfx-scenario-progress-text]');
      if (textNode) textNode.textContent = label;
    }
    const scrubber = root.querySelector('[data-cfx-scenario-scrubber]');
    if (scrubber) {
      scrubber.disabled = !count;
      scrubber.max = String(Math.max(1, count));
      scrubber.value = String(Math.max(1, current));
      scrubber.setAttribute('aria-valuetext', label);
    }
    if (count) root.dataset.cfxScenarioProgress = current + '/' + count;
    else root.removeAttribute('data-cfx-scenario-progress');
    return { current, count, percent, label };
  };
  const clearScenarioStep = (root) => {
    const route = scenarioRoute(root, root.dataset.cfxActiveScenario || '');
    root.removeAttribute('data-cfx-active-scenario-step');
    root.querySelectorAll('.cfx-scenario-step-active,.cfx-scenario-step-muted').forEach((node) => node.classList.remove('cfx-scenario-step-active', 'cfx-scenario-step-muted'));
    root.querySelectorAll('[data-cfx-scenario-step-index]').forEach((node) => node.removeAttribute('aria-current'));
    updateScenarioProgress(root, route, null);
  };
  const setScenarioStep = (root, stepIndex, emit, sync) => {
    const route = scenarioRoute(root, root.dataset.cfxActiveScenario || '');
    if (!route || !route.steps.length) return;
    const index = clamp(Number(stepIndex) || 0, 0, route.steps.length - 1);
    const targets = scenarioTargets(root, route, index);
    root.dataset.cfxActiveScenarioStep = String(index);
    root.querySelectorAll('.cfx-scenario-active').forEach((node) => {
      const active = targets.has(node);
      node.classList.toggle('cfx-scenario-step-active', active);
      node.classList.toggle('cfx-scenario-step-muted', !active && route.focusMode === 'spotlight');
    });
    root.querySelectorAll('[data-cfx-scenario-step-index]').forEach((node) => {
      node.getAttribute('data-cfx-scenario-step-index') === String(index + 1) ? node.setAttribute('aria-current', 'step') : node.removeAttribute('aria-current');
    });
    const progress = updateScenarioProgress(root, route, index);
    const trail = Array.from(targets).map((node) => targetIdentity(node)).slice(0, 5);
    if (trail.length && hasFeature(root, 'FocusTrail')) {
      applyFocusTrail(root, trail);
      if (emit !== false) emitHostEvent(root, 'cfxtrail', { target: trail[0], trail, count: trail.length, source: 'scenario-step' });
      if (sync !== false) emitSync(root, { action: 'trail', target: trail[0], trail, count: trail.length, source: 'scenario-step' });
    }
    revealNodes(root, Array.from(targets), emit, sync, 'scenario-step');
    syncScenarioUrl(root, route.id, index);
    if (emit !== false) emitHostEvent(root, 'cfxscenariostep', { scenarioId: route.id, index, step: route.steps[index], progress });
    if (sync !== false) emitSync(root, { action: 'scenario-step', scenarioId: route.id, index, progress });
  };
  const setScenario = (root, scenarioId, emit, sync) => {
    const route = scenarioRoute(root, scenarioId);
    const previousPlayback = root.dataset.cfxScenarioPlayback || '';
    stopScenarioPlayback(root, 'idle', emit !== false && previousPlayback && previousPlayback !== 'idle');
    clearScenarioStep(root);
    clearReveals(root);
    root.querySelectorAll('.cfx-scenario-active,.cfx-scenario-muted').forEach((node) => node.classList.remove('cfx-scenario-active', 'cfx-scenario-muted'));
    root.querySelectorAll('[data-cfx-scenario]').forEach((button) => button.setAttribute('aria-pressed', (button.dataset.cfxScenario || '') === (route ? route.id : '') ? 'true' : 'false'));
    if (!route) {
      root.removeAttribute('data-cfx-active-scenario');
      renderScenarioPanel(root, null);
      updateScenarioProgress(root, null, null);
      syncScenarioUrl(root, '', '');
      if (emit !== false) emitHostEvent(root, 'cfxscenarioclear', {});
      if (sync !== false) emitSync(root, { action: 'scenario', scenarioId: '' });
      return;
    }
    root.dataset.cfxActiveScenario = route.id;
    if (route.color) root.style.setProperty('--cfx-scenario-color', route.color);
    else root.style.removeProperty('--cfx-scenario-color');
    const targets = scenarioTargets(root, route, null);
    scenarioTargetCandidates(root, route).forEach((node) => {
      const active = targets.has(node);
      node.classList.toggle('cfx-scenario-active', active);
      node.classList.toggle('cfx-scenario-muted', !active && route.focusMode === 'spotlight');
    });
    renderScenarioPanel(root, route);
    updateScenarioProgress(root, route, null);
    syncScenarioUrl(root, route.id, '');
    if (emit !== false) emitHostEvent(root, 'cfxscenario', { scenarioId: route.id, label: route.label, description: route.description, color: route.color, steps: route.steps, metadata: route.metadata });
    if (sync !== false) emitSync(root, { action: 'scenario', scenarioId: route.id });
  };
  const stepScenario = (root, delta) => {
    const route = scenarioRoute(root, root.dataset.cfxActiveScenario || '');
    if (!route || !route.steps.length) return;
    const current = Number(root.dataset.cfxActiveScenarioStep || (delta > 0 ? '-1' : route.steps.length));
    setScenarioStep(root, current + delta, true, true);
  };
  const scenarioPlaybackDelay = (root, route, stepIndex) => {
    const step = route && route.steps && Number.isFinite(Number(stepIndex)) ? route.steps[Number(stepIndex)] : null;
    const delay = Number((step && step.durationMilliseconds) || (route && route.playbackDelayMilliseconds) || root.dataset.cfxScenarioPlaybackDelay || '900');
    return Number.isFinite(delay) && delay >= 200 ? delay : 900;
  };
  const setScenarioPlaybackState = (root, state, route, emit) => {
    const normalized = state || 'idle';
    root.dataset.cfxScenarioPlayback = normalized;
    root.querySelectorAll('[data-cfx-scenario-step-control="play"]').forEach((button) => {
      const playing = normalized === 'playing';
      button.setAttribute('aria-pressed', playing ? 'true' : 'false');
      button.textContent = playing ? (button.dataset.cfxScenarioPauseLabel || 'Pause') : (button.dataset.cfxScenarioPlayLabel || 'Play');
      button.title = playing ? 'Pause scenario' : 'Play scenario';
    });
    if (emit !== false) {
      emitHostEvent(root, 'cfxscenarioplayback', {
        state: normalized,
        scenarioId: route ? route.id : (root.dataset.cfxActiveScenario || ''),
        stepIndex: root.dataset.cfxActiveScenarioStep || '',
        progress: root.dataset.cfxScenarioProgress || '',
        delay: scenarioPlaybackDelay(root, route, root.dataset.cfxActiveScenarioStep)
      });
    }
  };
  const stopScenarioPlayback = (root, state, emit) => {
    if (root._cfxScenarioPlayback) window.clearTimeout(root._cfxScenarioPlayback);
    root._cfxScenarioPlayback = null;
    setScenarioPlaybackState(root, state || 'idle', scenarioRoute(root, root.dataset.cfxActiveScenario || ''), emit);
  };
  const playScenario = (root, emit) => {
    const route = scenarioRoute(root, root.dataset.cfxActiveScenario || '');
    if (!route || !route.steps.length) return;
    if (root._cfxScenarioPlayback) {
      stopScenarioPlayback(root, 'paused', emit);
      return;
    }
    let index = Number(root.dataset.cfxActiveScenarioStep || '-1') + 1;
    if (!Number.isFinite(index) || index >= route.steps.length) index = 0;
    startScenarioPlayback(root, route, index, emit, true);
  };
  const startScenarioPlayback = (root, route, stepIndex, emit, advanceNow) => {
    if (!route || !route.steps.length) return;
    setScenarioPlaybackState(root, 'playing', route, emit);
    let index = Number(stepIndex);
    if (!Number.isFinite(index) || index >= route.steps.length) index = 0;
    const advance = () => {
      if (index >= route.steps.length) {
        if (route.loopPlayback) index = 0;
        else {
          stopScenarioPlayback(root, 'finished', emit);
          return;
        }
      }
      const current = index++;
      setScenarioStep(root, current, true, true);
      root._cfxScenarioPlayback = window.setTimeout(advance, scenarioPlaybackDelay(root, route, current));
    };
    if (advanceNow !== false) advance();
    else root._cfxScenarioPlayback = window.setTimeout(advance, scenarioPlaybackDelay(root, route, Number(root.dataset.cfxActiveScenarioStep || '0')));
  };
  const copyScenarioLink = (root) => {
    syncScenarioUrl(root, root.dataset.cfxActiveScenario || '', root.dataset.cfxActiveScenarioStep || '');
    const url = window.location.href;
    if (navigator.clipboard && navigator.clipboard.writeText) navigator.clipboard.writeText(url).catch(() => {});
    emitHostEvent(root, 'cfxscenariolink', { url });
  };
  document.querySelectorAll('.cfx-interactive-chart').forEach((root) => {
    if (root.dataset.cfxRuntimeBound === 'true') return;
    root.dataset.cfxRuntimeBound = 'true';
