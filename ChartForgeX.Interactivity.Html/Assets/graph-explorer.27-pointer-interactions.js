  const bindPointerInteractions = (root) => {
    const stage = root.querySelector('.cfx-graph-stage');
    if (!stage) return;
    const chromeTarget = (event) => event.target.closest?.('.cfx-graph-command-rail,.cfx-graph-breadcrumbs');
    let active = null;
    stage.addEventListener('pointerdown', event => {
      if (event.button !== 0 || chromeTarget(event)) return;
      const point = scenePoint(root, event);
      const targetNode = event.target.closest ? event.target.closest('[data-cfx-role="graph-node"]') : null;
      const node = targetNode ? (root.__cfxGraphState || graphState(root)).nodes.find(item => item.el === targetNode) : hitNodeAt(root, point);
      const graphItem = event.target.closest ? event.target.closest('[data-cfx-role="graph-node"],[data-cfx-role="graph-edge"],[data-cfx-role="graph-edge-label"],[data-cfx-role="graph-cluster"]') : null;
      const hitItem = node || graphItem || hitGraphItemAt(root, point);
      const hitCanSelect = hasFeature(root, 'Selection') && !!hitItem;
      const hitBlocksPan = (node && hasFeature(root, 'DragNodes')) || hitCanSelect;
      root.dataset.cfxGraphLastPointerX = point.x.toFixed(3); root.dataset.cfxGraphLastPointerY = point.y.toFixed(3);
      root.dataset.cfxGraphLastPointerHit = node?.id || ''; root.__cfxGraphLastPointerHitTick = Date.now();
      if (node && hasFeature(root, 'DragNodes')) {
        event.preventDefault(); stage.setPointerCapture?.(event.pointerId); root.dataset.cfxGraphLastPointerMode = 'node';
        active = { mode: 'node', pointerId: event.pointerId, nodeId: node.id, startX: point.x, startY: point.y, lastX: point.x, lastY: point.y, lastAt: performanceClock(), vx: 0, vy: 0, fixed: attr(node.el, 'data-node-fixed'), movingFixed: node.fixed, moved: false };
        select(root, node.el, { additive: event.ctrlKey || event.metaKey || event.shiftKey, toggle: event.ctrlKey || event.metaKey || event.shiftKey });
        root.__cfxGraphPointerSelectionTick = Date.now(); root.__cfxGraphPointerSelectionId = node.id; root.__cfxGraphSuppressClickId = node.id;
      } else if (hasFeature(root, 'Viewport') && !hitBlocksPan) {
        event.preventDefault(); stage.setPointerCapture?.(event.pointerId); root.classList.add('cfx-graph-panning'); root.dataset.cfxGraphLastPointerMode = 'pan';
        active = { mode: 'pan', pointerId: event.pointerId, screenX: point.screenX, screenY: point.screenY, view: viewport(root) };
      } else root.dataset.cfxGraphLastPointerMode = 'none';
    });
    stage.addEventListener('pointermove', event => {
      if (!active || active.pointerId !== event.pointerId) return;
      const point = scenePoint(root, event);
      if (active.mode === 'node') {
        const state = root.__cfxGraphState || graphState(root), node = state.nodes.find(item => item.id === active.nodeId);
        if (!node) return;
        const dragThreshold = 3;
        if (!active.moved && Math.hypot(point.x - active.startX, point.y - active.startY) < dragThreshold) return;
        if (!active.moved) {
          active.moved = true; root.__cfxGraphDragNodeId = node.id; node.fixed = true; node.vx = 0; node.vy = 0; node.el.setAttribute('data-node-fixed', 'true');
          const livePhysics = !graphPrefersReducedMotion(root) && attr(root, 'data-cfx-graph-drag-live-physics') !== 'false' && hasFeature(root, 'RuntimePhysics');
          if (livePhysics) reheatPhysics(root, 'drag-start', { fit: false }); else if (hasFeature(root, 'RuntimePhysics')) pausePhysics(root);
          root.classList.add('cfx-graph-dragging-node'); emit(root, 'cfxgraphdragstart', { graphId: attr(root, 'data-cfx-graph-id'), nodeId: node.id, x: node.x, y: node.y });
        }
        const now = performanceClock(), elapsed = Math.max(4, now - active.lastAt);
        const nextVx = (point.x - active.lastX) * 16 / elapsed, nextVy = (point.y - active.lastY) * 16 / elapsed;
        active.vx = active.vx * 0.55 + nextVx * 0.45; active.vy = active.vy * 0.55 + nextVy * 0.45;
        active.lastX = point.x; active.lastY = point.y; active.lastAt = now; node.x = point.x; node.y = point.y;
        updateDraggedPhysicsNode(root, node); applyLayout(root, state);
        emit(root, 'cfxgraphdrag', { graphId: attr(root, 'data-cfx-graph-id'), nodeId: node.id, x: node.x, y: node.y });
      } else if (active.mode === 'pan') {
        root.__cfxGraphViewportTouched = true; setViewport(root, { x: active.view.x + point.screenX - active.screenX, y: active.view.y + point.screenY - active.screenY, scale: active.view.scale });
      }
    });
    const finish = (event) => {
      if (!active || active.pointerId !== event.pointerId) return;
      stage.releasePointerCapture?.(event.pointerId);
      if (active.mode === 'node' && active.moved) {
        root.__cfxGraphSuppressClickId = active.nodeId;
        const node = (root.__cfxGraphState || graphState(root)).nodes.find(item => item.id === active.nodeId);
        const release = attr(root, 'data-cfx-graph-drag-behavior') !== 'pin-on-drop', canceled = event.type === 'pointercancel';
        if (node) {
          const momentum = canceled || !release || active.movingFixed || graphPrefersReducedMotion(root) ? 0 : Math.max(0, num(root, 'data-cfx-graph-drag-momentum', 0.18));
          node.fixed = release ? active.movingFixed : true; node.vx = active.vx * momentum; node.vy = active.vy * momentum;
          node.el.setAttribute('data-node-fixed', release ? active.fixed || 'false' : 'true'); root.__cfxGraphDragNodeId = '';
          releaseDraggedPhysicsNode(root, node);
          if (!graphPrefersReducedMotion(root) && attr(root, 'data-cfx-graph-reheat-drag') !== 'false' && hasFeature(root, 'RuntimePhysics')) reheatPhysics(root, 'drag-end', { fit: false });
          emit(root, 'cfxgraphdragend', { graphId: attr(root, 'data-cfx-graph-id'), nodeId: active.nodeId, fixed: node.fixed, behavior: release ? 'release-and-reheat' : 'pin-on-drop', vx: node.vx, vy: node.vy });
        }
      }
      if (active.mode === 'node' && !active.moved) {
        const node = (root.__cfxGraphState || graphState(root)).nodes.find(item => item.id === active.nodeId);
        if (node) { node.fixed = active.movingFixed; node.el.setAttribute('data-node-fixed', active.fixed || 'false'); }
      }
      root.__cfxGraphDragNodeId = ''; root.classList.remove('cfx-graph-dragging-node', 'cfx-graph-panning'); active = null;
    };
    stage.addEventListener('pointerup', finish); stage.addEventListener('pointercancel', finish);
    stage.addEventListener('wheel', event => {
      if (!hasFeature(root, 'Viewport') || chromeTarget(event)) return;
      event.preventDefault(); root.__cfxGraphViewportTouched = true;
      const point = scenePoint(root, event); zoomViewport(root, event.deltaY < 0 ? 1.12 : 0.88, { x: point.screenX, y: point.screenY });
    }, { passive: false });
  };
