  const webGlNodePoints = (state, palette, compact, moving, scale, limit) => {
    const points = { positions: [], colors: [], strokes: [], sizes: [], nodes: new Set() }, statusPoints = [];
    if (!compact && !moving) return points;
    const eligible = (node) => {
      if (node.shape !== 'circle' || node.card || node.shadow && !moving || node.icon && !moving) return false;
      const paint = graphNodeMarkPaint(node, node.el.classList.contains('cfx-graph-selected'), compact);
      // Include the driver's point limit at maximum semantic zoom and DPR.
      return (node.size * 2 + paint.width) * scale * 4 <= limit && 11 * scale * 4 <= limit;
    };
    const visibleNodes = state.nodes.filter(node => visible(node.el));
    const gpuNodes = new Set(visibleNodes.filter(eligible));
    if (gpuNodes.size && gpuNodes.size !== visibleNodes.length) {
      // Overlapping mixed marks share Canvas, including transitive overlaps and
      // status dots. Spatially separate circles can retain the GPU batch.
      const margin = .5 / Math.max(.001, scale * .2);
      const entries = visibleNodes.map(node => graphNodeMarkBounds(node, compact, moving, margin));
      const fallback = graphBoundsFallback(entries, entries.filter(entry => !gpuNodes.has(entry.node)));
      fallback.forEach(entry => gpuNodes.delete(entry.node));
    }
    const point = (x, y, radius, width, fill, stroke, alpha) => {
      points.positions.push(x, y);
      points.colors.push(...webGlColor(fill, alpha));
      points.strokes.push(...webGlColor(stroke, alpha));
      points.sizes.push(2 * radius + width, Math.max(0, (radius - width / 2) / (radius + width / 2)));
    };
    state.nodes.forEach(node => {
      if (!gpuNodes.has(node)) return;
      const paint = graphNodeMarkPaint(node, node.el.classList.contains('cfx-graph-selected'), compact);
      const alpha = node.el.classList.contains('cfx-graph-neighborhood-dim') ? .18 : 1;
      points.nodes.add(node);
      point(node.x, node.y, node.size, paint.width, paint.fill, paint.stroke, alpha);
      const status = attr(node.el, 'data-cfx-status').toLowerCase();
      if (status && status !== 'unknown') {
        const color = status === 'healthy' ? '#22c55e' : status === 'warning' ? '#f59e0b' : status === 'critical' ? '#ef4444' : '#94a3b8';
        statusPoints.push([node.x - node.size * .8, node.y - node.size * .8, 4.5, 2, color, palette.paper, alpha]);
      }
    });
    statusPoints.forEach(values => point(...values));
    return points;
  };
  const webGlBindAttribute = (runtime, name, components) => {
    const { gl } = runtime;
    gl.bindBuffer(gl.ARRAY_BUFFER, runtime[name + 'Buffer']);
    gl.enableVertexAttribArray(runtime[name]);
    gl.vertexAttribPointer(runtime[name], components, gl.FLOAT, false, 0, 0);
  };
  const webGlDrawNodes = (runtime, points) => {
    if (!points.positions.length) return;
    const attributes = [['pointPosition', points.positions, 2], ['pointColor', points.colors, 4], ['stroke', points.strokes, 4], ['point', points.sizes, 2]];
    attributes.forEach(([name, values, components]) => {
      if (runtime.uploadedPoints !== points) webGlUploadAttribute(runtime, name, values, components);
      else webGlBindAttribute(runtime, name, components);
    });
    runtime.uploadedPoints = points;
    runtime.gl.uniform1i(runtime.points, 1);
    runtime.gl.drawArrays(runtime.gl.POINTS, 0, points.positions.length / 2);
  };
