  const webGlShader = (gl, type, source) => {
    const shader = gl.createShader(type);
    if (!shader) return null;
    gl.shaderSource(shader, source);
    gl.compileShader(shader);
    if (gl.getShaderParameter(shader, gl.COMPILE_STATUS)) return shader;
    gl.deleteShader(shader);
    return null;
  };
  const webGlRuntime = (root) => {
    if (root.__cfxGraphWebGl === false) return null;
    if (root.__cfxGraphWebGl) return root.__cfxGraphWebGl;
    const canvas = root.querySelector('[data-cfx-role="graph-webgl"]');
    const gl = canvas?.getContext('webgl2', { alpha: false, antialias: true, depth: false, preserveDrawingBuffer: true, powerPreference: 'high-performance' });
    if (!gl) {
      root.__cfxGraphWebGl = false;
      return null;
    }
    const vertex = webGlShader(gl, gl.VERTEX_SHADER, `#version 300 es
      in vec2 a_position;
      in vec4 a_color;
      in float a_size;
      uniform vec2 u_sceneSize;
      uniform vec3 u_view;
      uniform vec3 u_surface;
      out vec4 v_color;
      void main() {
        vec2 screen = (a_position * u_view.z + u_view.xy) * u_surface.x + u_surface.yz;
        vec2 clip = screen / u_sceneSize * 2.0 - 1.0;
        gl_Position = vec4(clip.x, -clip.y, 0.0, 1.0);
        gl_PointSize = clamp(a_size * max(0.72, u_view.z), 3.0, 72.0) * u_surface.x;
        v_color = a_color;
      }`);
    const fragment = webGlShader(gl, gl.FRAGMENT_SHADER, `#version 300 es
      precision mediump float;
      in vec4 v_color;
      uniform bool u_points;
      out vec4 outColor;
      void main() {
        if (u_points) {
          vec2 point = gl_PointCoord * 2.0 - 1.0;
          float radius = dot(point, point);
          if (radius > 1.0) discard;
          float edge = smoothstep(1.0, 0.78, radius);
          outColor = vec4(v_color.rgb, v_color.a * edge);
        } else outColor = v_color;
      }`);
    if (!vertex || !fragment) {
      root.__cfxGraphWebGl = false;
      return null;
    }
    const program = gl.createProgram();
    gl.attachShader(program, vertex);
    gl.attachShader(program, fragment);
    gl.linkProgram(program);
    gl.deleteShader(vertex);
    gl.deleteShader(fragment);
    if (!gl.getProgramParameter(program, gl.LINK_STATUS)) {
      gl.deleteProgram(program);
      root.__cfxGraphWebGl = false;
      return null;
    }
    const runtime = {
      canvas, gl, program,
      position: gl.getAttribLocation(program, 'a_position'),
      color: gl.getAttribLocation(program, 'a_color'),
      size: gl.getAttribLocation(program, 'a_size'),
      sceneSize: gl.getUniformLocation(program, 'u_sceneSize'),
      view: gl.getUniformLocation(program, 'u_view'),
      surface: gl.getUniformLocation(program, 'u_surface'),
      points: gl.getUniformLocation(program, 'u_points'),
      positionBuffer: gl.createBuffer(), colorBuffer: gl.createBuffer(), sizeBuffer: gl.createBuffer()
    };
    root.__cfxGraphWebGl = runtime;
    return runtime;
  };
  const webGlAvailable = (root) => !!webGlRuntime(root);
  const webGlColor = (value, alpha, fallback) => {
    let rgb = fallback || [37, 99, 235];
    const source = (value || '').trim();
    if (/^#[0-9a-f]{3}$/i.test(source)) rgb = source.slice(1).split('').map(value => parseInt(value + value, 16));
    else if (/^#[0-9a-f]{6}$/i.test(source)) rgb = [parseInt(source.slice(1, 3), 16), parseInt(source.slice(3, 5), 16), parseInt(source.slice(5, 7), 16)];
    else {
      const match = source.match(/^rgba?\(\s*([\d.]+)[, ]+([\d.]+)[, ]+([\d.]+)/i);
      if (match) rgb = [Number(match[1]), Number(match[2]), Number(match[3])];
    }
    return [rgb[0] / 255, rgb[1] / 255, rgb[2] / 255, alpha];
  };
  const webGlStatusColor = (node) => {
    const status = attr(node.el, 'data-cfx-status').toLowerCase();
    if (status === 'critical') return '#ef4444';
    if (status === 'warning') return '#f59e0b';
    if (status === 'healthy') return '#22c55e';
    if (status === 'disabled' || status === 'muted') return '#94a3b8';
    return node.backgroundColor || '#2563eb';
  };
  const webGlResize = (runtime, size) => {
    const rect = runtime.canvas.getBoundingClientRect();
    const ratio = Math.max(1, window.devicePixelRatio || 1);
    const width = Math.max(1, Math.round((rect.width || size.width) * ratio));
    const height = Math.max(1, Math.round((rect.height || size.height) * ratio));
    if (runtime.canvas.width !== width || runtime.canvas.height !== height) {
      runtime.canvas.width = width;
      runtime.canvas.height = height;
    }
    runtime.gl.viewport(0, 0, width, height);
  };
  // Keep CPU staging arrays and GPU storage until a larger scene needs capacity.
  const webGlUploadAttribute = (runtime, name, values, components) => {
    const { gl } = runtime;
    const uploads = runtime.uploads || (runtime.uploads = {});
    let upload = uploads[name];
    gl.bindBuffer(gl.ARRAY_BUFFER, runtime[name + 'Buffer']);
    if (!upload || upload.data.length < values.length) {
      const capacity = Math.max(64, 2 ** Math.ceil(Math.log2(Math.max(1, values.length))));
      upload = uploads[name] = { data: new Float32Array(capacity) };
      gl.bufferData(gl.ARRAY_BUFFER, upload.data.byteLength, gl.DYNAMIC_DRAW);
    }
    upload.data.set(values);
    gl.bufferSubData(gl.ARRAY_BUFFER, 0, upload.data.subarray(0, values.length));
    gl.enableVertexAttribArray(runtime[name]);
    gl.vertexAttribPointer(runtime[name], components, gl.FLOAT, false, 0, 0);
  };
  const webGlUpload = (runtime, positions, colors, sizes, points) => {
    webGlUploadAttribute(runtime, 'position', positions, 2);
    webGlUploadAttribute(runtime, 'color', colors, 4);
    webGlUploadAttribute(runtime, 'size', sizes, 1);
    runtime.gl.uniform1i(runtime.points, points ? 1 : 0);
  };
  const drawWebGl = (root, state) => {
    const runtime = webGlRuntime(root);
    if (!runtime) return false;
    const { gl } = runtime;
    const size = sceneSize(root);
    const view = viewport(root);
    const palette = graphThemePalette(root);
    webGlResize(runtime, size);
    const clear = webGlColor(palette.paper, 1, palette.dark ? [11, 18, 32] : [255, 255, 255]);
    gl.clearColor(clear[0], clear[1], clear[2], 1);
    gl.clear(gl.COLOR_BUFFER_BIT);
    gl.useProgram(runtime.program);
    const fit = graphSurfaceFit(size, runtime.canvas.width, runtime.canvas.height);
    gl.uniform2f(runtime.sceneSize, runtime.canvas.width, runtime.canvas.height);
    gl.uniform3f(runtime.surface, fit.scale, fit.offsetX, fit.offsetY);
    gl.uniform3f(runtime.view, view.x, view.y, view.scale);
    gl.enable(gl.BLEND);
    gl.blendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA);

    const linePositions = [], lineColors = [], lineSizes = [];
    state.edges.forEach(edge => {
      if (!visible(edge.el) || !edgeHasVisibleEndpoints(edge, state.byId)) return;
      const rendered = visualEdge(edge, state.byId);
      const selected = edge.el.classList.contains('cfx-graph-selected');
      const related = edge.el.classList.contains('cfx-graph-neighborhood-related');
      const dimmed = edge.el.classList.contains('cfx-graph-neighborhood-dim');
      const color = webGlColor(selected ? palette.selected : related ? '#14b8a6' : edge.strokeColor || palette.edge, dimmed ? .08 : selected ? .95 : related ? .8 : .42);
      const control = edgeControl(rendered);
      const points = edgeHasRoute(rendered) ? routeRenderPoints(rendered) : [rendered.source, rendered.target];
      for (let index = 1; index < points.length; index++) {
        linePositions.push(points[index - 1].x, points[index - 1].y, points[index].x, points[index].y);
        lineColors.push(...color, ...color);
        lineSizes.push(1, 1);
      }
      const appendArrow = side => {
        const arrow = edgeArrowGeometry(rendered, control, side);
        linePositions.push(arrow.tip.x, arrow.tip.y, arrow.left.x, arrow.left.y, arrow.tip.x, arrow.tip.y, arrow.right.x, arrow.right.y);
        lineColors.push(...color, ...color, ...color, ...color);
        lineSizes.push(1, 1, 1, 1);
      };
      if (rendered.sourceArrow) appendArrow('source');
      if (rendered.targetArrow || rendered.directed) appendArrow('target');
    });
    if (linePositions.length) {
      webGlUpload(runtime, linePositions, lineColors, lineSizes, false);
      gl.drawArrays(gl.LINES, 0, linePositions.length / 2);
    }

    const pointPositions = [], pointColors = [], pointSizes = [];
    state.clusters.forEach(cluster => {
      if (!visible(cluster.el) || attr(cluster.el, 'data-cluster-collapsed') !== 'true') return;
      const metrics = clusterMetrics(cluster, state.byId);
      if (!metrics) return;
      const clusterColors = graphClusterColors(root, cluster, palette);
      pointPositions.push(metrics.x, metrics.y);
      pointColors.push(...webGlColor(cluster.el.classList.contains('cfx-graph-selected') ? palette.selected : clusterColors.stroke, .9));
      pointSizes.push(Math.max(16, metrics.radius * 1.5));
    });
    state.nodes.forEach(node => {
      if (!visible(node.el)) return;
      const selected = node.el.classList.contains('cfx-graph-selected');
      const primary = node.el.classList.contains('cfx-graph-neighborhood-primary');
      const dimmed = node.el.classList.contains('cfx-graph-neighborhood-dim');
      pointPositions.push(node.x, node.y);
      pointColors.push(...webGlColor(selected ? palette.selected : primary ? '#14b8a6' : webGlStatusColor(node), dimmed ? .15 : 1));
      pointSizes.push(Math.max(5, node.size * 2 + (selected || primary ? 7 : 0)));
    });
    if (pointPositions.length) {
      webGlUpload(runtime, pointPositions, pointColors, pointSizes, true);
      gl.drawArrays(gl.POINTS, 0, pointPositions.length / 2);
    }
    gl.disable(gl.BLEND);
    return true;
  };
  const bindWebGlHitTesting = (root) => {
    const canvas = root.querySelector('[data-cfx-role="graph-webgl"]');
    if (!canvas) return;
    canvas.addEventListener('click', event => {
      if (root.dataset.cfxGraphRendererActive !== 'webgl') return;
      const best = hitGraphItemAt(root, scenePoint(root, event));
      if (!best) return;
      const bestId = attr(best.el, 'data-node-id') || attr(best.el, 'data-edge-id') || attr(best.el, 'data-cluster-id');
      if (bestId && root.__cfxGraphSuppressClickId === bestId) {
        root.__cfxGraphSuppressClickId = '';
        event.preventDefault();
        return;
      }
      if (Date.now() - (root.__cfxGraphPointerSelectionTick || 0) < 250) return;
      select(root, best.el, { additive: event.ctrlKey || event.metaKey || event.shiftKey, toggle: event.ctrlKey || event.metaKey || event.shiftKey });
    });
    canvas.addEventListener('keydown', event => {
      if (root.dataset.cfxGraphRendererActive !== 'webgl') return;
      if (moveAcceleratedGraphSelection(root, event)) return;
      if (event.key !== 'Enter' && event.key !== ' ') return;
        const selected = acceleratedGraphSelectedItem(root);
      if (!selected) return;
      event.preventDefault();
      select(root, selected.el, { additive: event.ctrlKey || event.metaKey || event.shiftKey, toggle: event.ctrlKey || event.metaKey || event.shiftKey });
    });
  };
