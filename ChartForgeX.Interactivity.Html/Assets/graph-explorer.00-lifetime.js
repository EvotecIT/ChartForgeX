  const graphLifetime = root => window[Symbol.for('ChartForgeX.adapterLifetimes')].get(root, 'graph');
  const listenGraph = (root, target, name, listener, options) => target?.__cfxVirtual
    ? () => {} : graphLifetime(root)?.listen(target, name, listener, options) || (() => {});
  const beginGraphLifetime = root => {
    const lifetime = window[Symbol.for('ChartForgeX.adapterLifetimes')].create(root, 'graph');
    lifetime.own(() => {
      stopWorkerPhysics(root); stopMainPhysics(root);
      if (root.dataset.cfxGraphPhysicsState === 'running') root.dataset.cfxGraphPhysicsState = 'paused';
      if (root.__cfxGraphState) syncSvgLayout(root, root.__cfxGraphState);
      graphHover(root, null);
      const runtime = root.__cfxGraphWebGl;
      if (runtime) {
        for (const name of ['positionBuffer', 'colorBuffer', 'pointPositionBuffer', 'pointColorBuffer', 'strokeBuffer', 'pointBuffer'])
          if (runtime[name]) runtime.gl.deleteBuffer(runtime[name]);
        runtime.gl.deleteProgram(runtime.program);
      }
      delete root.__cfxGraphWebGl; delete root.__cfxGraphEdgeMesh; delete root.__cfxGraphBadgeMeasurements;
      delete root.__cfxGraphHoverKey; delete root.__cfxGraphHoverDetail;
      delete root.__cfxGraphSuppressClickId; delete root.__cfxGraphPointerSelectionId;
      delete root.__cfxGraphPointerSelectionTick; delete root.__cfxGraphLastPointerHitTick;
      root.classList.remove('cfx-graph-panning', 'cfx-graph-dragging-node', 'cfx-graph-box-selecting');
      const box = root.querySelector('[data-cfx-role="graph-selection-box"]'); if (box) box.hidden = true;
      items(root, '[data-cfx-graph-selection-bound]').forEach(item => item.removeAttribute('data-cfx-graph-selection-bound'));
      root.querySelector('[data-cfx-role="graph-physics-configurator"]')?.removeAttribute('data-cfx-physics-bound');
      root.removeAttribute('data-cfx-graph-bound');
    });
    const invalidateText = () => {
      root.__cfxGraphBadgeMeasurements?.clear();
      delete root.__cfxGraphEdgeMesh;
      if (lifetime.active && root.isConnected) applyLayout(root, root.__cfxGraphState || graphState(root));
    };
    listenGraph(root, root.ownerDocument.fonts, 'loadingdone', invalidateText);
    return lifetime;
  };
