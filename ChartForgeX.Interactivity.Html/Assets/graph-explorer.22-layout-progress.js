  // Reports only the first layout of a large graph; later reheats stay quiet.
  const graphLayoutProgressElement = root => root.querySelector('[data-cfx-role="graph-layout-progress"]');
  // A detached root no longer bubbles to the document, so report to it directly.
  const publishLayoutProgress = (root, progress, done) =>
    emit(root.isConnected === false && root.ownerDocument ? root.ownerDocument : root, 'cfxgraphlayoutprogress', { graphId: attr(root, 'data-cfx-graph-id'), progress, done });
  const beginLayoutProgress = (root, iterations) => {
    const element = graphLayoutProgressElement(root);
    if (!element || root.dataset.cfxGraphLayoutProgress) return;
    root.dataset.cfxGraphLayoutProgress = 'running';
    root.__cfxGraphLayoutProgressIterations = Math.max(1, iterations || 1);
    root.__cfxGraphLayoutProgressPercent = -1;
    element.hidden = false;
    syncLayoutProgress(root, 0);
  };
  const syncLayoutProgress = (root, tick) => {
    if (root.dataset.cfxGraphLayoutProgress !== 'running') return;
    const element = graphLayoutProgressElement(root);
    if (!element) return;
    // Physics may settle early, so the bar stays below 100 until it does.
    const percent = Math.max(0, Math.min(99, Math.floor((Number(tick) || 0) / root.__cfxGraphLayoutProgressIterations * 100)));
    // A worker fallback restarts its ticks; hold instead of going backwards.
    if (percent <= root.__cfxGraphLayoutProgressPercent) return;
    root.__cfxGraphLayoutProgressPercent = percent;
    element.setAttribute('aria-valuenow', String(percent));
    element.style.setProperty('--cfx-graph-layout-progress', String(percent / 100));
    const value = element.querySelector('[data-cfx-role="graph-layout-progress-value"]');
    if (value) value.textContent = percent + '%';
    publishLayoutProgress(root, percent / 100, false);
  };
  const finishLayoutProgress = root => {
    if (root.dataset.cfxGraphLayoutProgress !== 'running') return;
    root.dataset.cfxGraphLayoutProgress = 'done';
    const element = graphLayoutProgressElement(root);
    if (element) {
      element.setAttribute('aria-valuenow', '100');
      element.style.setProperty('--cfx-graph-layout-progress', '1');
      element.hidden = true;
    }
    publishLayoutProgress(root, 1, true);
  };
