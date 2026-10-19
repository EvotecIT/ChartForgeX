  // Native legend density remains bounded. Optional HTML chrome exposes retained omitted facts and commands.
  const legendOverflowPageSize = 6;
  const legendOverflowIdentity = source => {
    const target = seriesTarget(source);
    return JSON.stringify([target.seriesKey, target.targetKind, target.targetId]);
  };
  const syncLegendOverflow = root => {
    const state = root._cfxLegendOverflow;
    if (!state) return;
    for (const control of state.controls) {
      for (const [button, attribute] of [[control.mute, 'cfxMuted'], [control.isolate, 'cfxIsolated']]) {
        const active = control.source.dataset[attribute] === 'true';
        button.setAttribute('aria-pressed', String(active));
      }
    }
  };
  const renderLegendOverflow = (root, requestedPage, direction = 0) => {
    const state = root._cfxLegendOverflow;
    if (!state) return;
    state.pageCleanups.splice(0).forEach(release => release());
    state.controls = [];
    state.body.replaceChildren();
    const page = chartControlPage(state.sources.length, requestedPage ?? state.page, legendOverflowPageSize);
    state.page = page.page;
    const navigation = document.createElement('div');
    navigation.className = 'cfx-legend-overflow__navigation';
    const range = document.createElement('span');
    range.className = 'cfx-legend-overflow__range';
    range.setAttribute('role', 'status'); range.setAttribute('aria-atomic', 'true');
    range.tabIndex = -1;
    range.textContent = uiText(root, 'LegendRange', '{first}–{last} of {count}',
      { first: page.first, last: page.end, count: state.sources.length });
    navigation.appendChild(range);
    if (page.last) [-1, 1].forEach(step => {
      const button = document.createElement('button');
      button.type = 'button'; button.className = 'cfx-tool';
      button.dataset.cfxLegendPage = step < 0 ? 'previous' : 'next';
      button.textContent = uiText(root, step < 0 ? 'Previous' : 'Next', step < 0 ? 'Prev' : 'Next');
      button.setAttribute('aria-label', uiText(root, step < 0 ? 'LegendPrevious' : 'LegendNext',
        step < 0 ? 'Previous legend entries' : 'Next legend entries'));
      button.disabled = step < 0 ? page.page === 0 : page.page === page.last;
      state.pageCleanups.push(listenChart(root, button, 'click', event => {
        event.stopPropagation(); renderLegendOverflow(root, page.page + step, step);
      }));
      navigation.appendChild(button);
    });
    state.body.appendChild(navigation);
    const list = document.createElement('div');
    list.className = 'cfx-legend-overflow__entries';
    state.sources.slice(page.offset, page.end).forEach((source, index) => {
      const row = document.createElement('div');
      row.className = 'cfx-legend-overflow__entry';
      row.dataset.cfxLegendIndex = String(page.offset + index);
      const label = source.getAttribute('aria-label') || source.dataset.cfxLabel || '';
      const actionable = hasFeature(root, 'LegendToggles') && source.dataset.cfxSeries !== undefined;
      if (!actionable) {
        row.textContent = label;
        list.appendChild(row); return;
      }
      const mute = document.createElement('button');
      mute.type = 'button'; mute.className = 'cfx-tool cfx-legend-overflow__toggle';
      mute.dataset.cfxLegendAction = 'mute';
      mute.setAttribute('aria-label', uiText(root, 'LegendMute', 'Mute {label}', { label }));
      const caption = document.createElement('span'); caption.textContent = label; mute.appendChild(caption);
      const isolate = document.createElement('button');
      isolate.type = 'button'; isolate.className = 'cfx-tool';
      isolate.dataset.cfxLegendAction = 'isolate';
      isolate.textContent = uiText(root, 'LegendIsolateButton', 'Isolate');
      isolate.setAttribute('aria-label', uiText(root, 'LegendIsolate', 'Isolate {label}', { label }));
      for (const [button, action] of [[mute, () => toggleSeries(root, source)], [isolate, () => toggleSeriesFocus(root, source, true, true)]])
        state.pageCleanups.push(listenChart(root, button, 'click', event => {
          event.stopPropagation();
          if (root._cfxLegendOverflow === state && root.contains(source)) action();
        }));
      state.controls.push({ source, mute, isolate });
      row.append(mute, isolate); list.appendChild(row);
    });
    state.body.appendChild(list);
    syncLegendOverflow(root);
    if (direction) {
      const control = direction < 0 ? state.controls[state.controls.length - 1] : state.controls[0];
      (control?.mute || range).focus({ preventScroll: true });
    }
  };
  const bindLegendOverflow = root => {
    const stage = root.querySelector('.cfx-stage');
    const sources = Array.from(stage?.querySelectorAll('svg [data-cfx-role="legend-entry-omitted"]') || []);
    if (!stage || !sources.length) return;
    const details = document.createElement('details');
    details.className = 'cfx-legend-overflow';
    const summary = document.createElement('summary');
    summary.textContent = uiText(root, 'LegendMore', 'More legend entries ({count})', { count: sources.length });
    const body = document.createElement('div'); body.className = 'cfx-legend-overflow__body';
    details.append(summary, body); stage.after(details);
    const state = { sources, details, summary, body, controls: [], pageCleanups: [], page: 0 };
    root._cfxLegendOverflow = state;
    ownChartWork(root, () => {
      state.pageCleanups.splice(0).forEach(release => release());
      state.controls = []; state.sources = []; details.remove();
      if (root._cfxLegendOverflow === state) delete root._cfxLegendOverflow;
    });
    renderLegendOverflow(root);
  };
  const captureLegendOverflow = (root, sameSource) => {
    const state = root._cfxLegendOverflow;
    if (!state) return root._cfxLegendOverflowPreferences || null;
    const active = root.getRootNode().activeElement;
    const control = state.controls.find(item => item.mute === active || item.isolate === active);
    const target = control && seriesTarget(control.source);
    return { open: state.details.open, page: state.page, ownedFocus: state.details.contains(active),
      focused: target && (sameSource || durableChartTarget(target))
        ? { target, action: active === control.isolate ? 'isolate' : 'mute' } : null };
  };
  const restoreLegendOverflow = (root, captured) => {
    if (!captured) return;
    root._cfxLegendOverflowPreferences = { open: captured.open, page: captured.page, ownedFocus: false, focused: null };
    const state = root._cfxLegendOverflow;
    if (!state) return;
    state.details.open = captured.open;
    const identity = captured.focused && JSON.stringify([captured.focused.target.seriesKey,
      captured.focused.target.targetKind, captured.focused.target.targetId]);
    const focusedIndex = identity ? state.sources.findIndex(source => legendOverflowIdentity(source) === identity) : -1;
    renderLegendOverflow(root, focusedIndex < 0 ? captured.page : Math.floor(focusedIndex / legendOverflowPageSize));
    if (!captured.ownedFocus) return;
    const control = focusedIndex >= 0 && state.controls.find(item => legendOverflowIdentity(item.source) === identity);
    (control && state.details.open ? control[captured.focused.action] : state.summary).focus({ preventScroll: true });
  };
