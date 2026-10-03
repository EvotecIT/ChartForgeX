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
    const gl = canvas?.getContext('webgl2', { alpha: true, premultipliedAlpha: true, antialias: true, depth: false, preserveDrawingBuffer: true, powerPreference: 'high-performance' });
    if (!gl) {
      root.__cfxGraphWebGl = false;
      return null;
    }
    const vertex = webGlShader(gl, gl.VERTEX_SHADER, `#version 300 es
      in vec2 a_position;
      in vec4 a_color;
      in vec4 a_stroke;
      in vec2 a_point;
      uniform vec2 u_sceneSize;
      uniform vec3 u_view;
      uniform vec3 u_surface;
      out vec4 v_color;
      out vec4 v_stroke;
      out vec2 v_point;
      void main() {
        vec2 screen = (a_position * u_view.z + u_view.xy) * u_surface.x + u_surface.yz;
        vec2 clip = screen / u_sceneSize * 2.0 - 1.0;
        gl_Position = vec4(clip.x, -clip.y, 0.0, 1.0);
        v_color = a_color;
        v_stroke = a_stroke; v_point = a_point;
        gl_PointSize = a_point.x * u_view.z * u_surface.x;
      }`);
    const fragment = webGlShader(gl, gl.FRAGMENT_SHADER, `#version 300 es
      precision mediump float;
      in vec4 v_color;
      in vec4 v_stroke;
      in vec2 v_point;
      uniform bool u_points;
      out vec4 outColor;
      void main() {
        if (u_points) {
          float radius = length(gl_PointCoord * 2.0 - 1.0);
          float smoothing = max(fwidth(radius), 0.001);
          float coverage = 1.0 - smoothstep(1.0 - smoothing, 1.0, radius);
          float border = smoothstep(v_point.y - smoothing, v_point.y + smoothing, radius);
          outColor = mix(v_color, v_stroke, border);
          outColor.a *= coverage;
        } else outColor = v_color;
      }`);
    if (!vertex || !fragment) {
      if (vertex) gl.deleteShader(vertex);
      if (fragment) gl.deleteShader(fragment);
      root.__cfxGraphWebGl = false;
      return null;
    }
    const program = gl.createProgram();
    if (!program) {
      gl.deleteShader(vertex); gl.deleteShader(fragment);
      root.__cfxGraphWebGl = false;
      return null;
    }
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
      stroke: gl.getAttribLocation(program, 'a_stroke'),
      point: gl.getAttribLocation(program, 'a_point'),
      points: gl.getUniformLocation(program, 'u_points'),
      pointLimit: gl.getParameter(gl.ALIASED_POINT_SIZE_RANGE)[1],
      sceneSize: gl.getUniformLocation(program, 'u_sceneSize'),
      view: gl.getUniformLocation(program, 'u_view'),
      surface: gl.getUniformLocation(program, 'u_surface'),
      positionBuffer: gl.createBuffer(), colorBuffer: gl.createBuffer(),
      pointPositionBuffer: gl.createBuffer(), pointColorBuffer: gl.createBuffer(), strokeBuffer: gl.createBuffer(), pointBuffer: gl.createBuffer()
    };
    runtime.pointPosition = runtime.position;
    runtime.pointColor = runtime.color;
    const buffers = [runtime.positionBuffer, runtime.colorBuffer, runtime.pointPositionBuffer, runtime.pointColorBuffer, runtime.strokeBuffer, runtime.pointBuffer];
    if (buffers.some(buffer => !buffer)) {
      for (const buffer of buffers) if (buffer) gl.deleteBuffer(buffer);
      gl.deleteProgram(program);
      root.__cfxGraphWebGl = false;
      return null;
    }
    root.__cfxGraphWebGl = runtime;
    return runtime;
  };
  const webGlAvailable = (root) => !!webGlRuntime(root);
  const webGlColor = (value, alpha, fallback) => {
    const rgba = graphColorRgba(value) || [...(fallback || [37, 99, 235]), 1];
    return [rgba[0] / 255, rgba[1] / 255, rgba[2] / 255, alpha * rgba[3]];
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
  const webGlUpload = (runtime, positions, colors) => {
    webGlUploadAttribute(runtime, 'position', positions, 2);
    webGlUploadAttribute(runtime, 'color', colors, 4);
  };
  const drawWebGl = (root, state) => {
    const runtime = webGlRuntime(root);
    if (!runtime || runtime.gl.isContextLost?.()) return false;
    const { gl } = runtime, size = sceneSize(root), view = viewport(root), palette = graphThemePalette(root);
    webGlResize(runtime, size);
    gl.clearColor(0, 0, 0, 0);
    gl.clear(gl.COLOR_BUFFER_BIT);
    gl.useProgram(runtime.program);
    const fit = graphSurfaceFit(size, runtime.canvas.width, runtime.canvas.height);
    gl.uniform2f(runtime.sceneSize, runtime.canvas.width, runtime.canvas.height);
    gl.uniform3f(runtime.surface, fit.scale, fit.offsetX, fit.offsetY);
    gl.uniform3f(runtime.view, view.x, view.y, view.scale);
    gl.enable(gl.BLEND);
    gl.blendFuncSeparate(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA, gl.ONE, gl.ONE_MINUS_SRC_ALPHA);
    const byId = state.byId || new Map(state.nodes.map(node => [node.id, node]));
    const compact = root.classList.contains('cfx-graph-lod-compact') || root.classList.contains('cfx-graph-semantic-overview');
    const dense = compact || state.edges.length > 250, moving = root.dataset.cfxGraphPhysicsState === 'running';
    const mesh = webGlEdgeMesh(root, state, byId, palette, dense, moving, fit.scale, compact, runtime.pointLimit);
    if (!drawWebGlUnderlay(root, state, { byId, palette, moving, mesh })) {
      setGraphRenderer(root, 'canvas');
      return false;
    }
    if (mesh.positions.length) {
      if (runtime.uploadedMesh !== mesh) { webGlUpload(runtime, mesh.positions, mesh.colors); runtime.uploadedMesh = mesh; }
      else { webGlBindAttribute(runtime, 'position', 2); webGlBindAttribute(runtime, 'color', 4); }
      gl.disableVertexAttribArray(runtime.stroke); gl.vertexAttrib4f(runtime.stroke, 0, 0, 0, 0);
      gl.disableVertexAttribArray(runtime.point); gl.vertexAttrib2f(runtime.point, 1, 1);
      gl.uniform1i(runtime.points, 0);
      if (mesh.lineVertices) {
        gl.lineWidth(1);
        gl.drawArrays(gl.LINES, mesh.triangleVertices, mesh.lineVertices);
      }
      if (mesh.triangleVertices) gl.drawArrays(gl.TRIANGLES, 0, mesh.triangleVertices);
    }
    // Native fallback strokes and node bodies must share their paint order.
    if (!mesh.fallbackEdges.length) webGlDrawNodes(runtime, mesh.nodePoints);
    gl.disable(gl.BLEND);
    return drawWebGlDetails(root, state, { byId, palette, compact, dense, moving, mesh });
  };
  const bindWebGlHitTesting = (root) => {
    const canvas = root.querySelector('[data-cfx-role="graph-webgl"]');
    if (!canvas) return;
    canvas.addEventListener('webglcontextlost', event => {
      event.preventDefault();
      root.__cfxGraphWebGl = false;
      if (root.dataset.cfxGraphRendererActive !== 'webgl') return;
      setGraphRenderer(root, 'canvas');
      drawCanvas(root, root.__cfxGraphState || graphState(root));
    });
    canvas.addEventListener('webglcontextrestored', () => {
      root.__cfxGraphWebGl = null;
      if (hasFeature(root, 'LevelOfDetail')) applyLod(root);
      else if (attr(root, 'data-cfx-graph-renderer') === 'webgl') setGraphRenderer(root, webGlAvailable(root) ? 'webgl' : 'canvas');
      drawCanvas(root, root.__cfxGraphState || graphState(root));
    });
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
