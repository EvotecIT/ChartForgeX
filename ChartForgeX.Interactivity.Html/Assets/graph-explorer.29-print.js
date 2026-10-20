  // The live graph keeps its camera and selection; print uses the existing SVG export materialization.
  let graphPrintSequence = 0;
  const scopeGraphPrintIds = (svg, prefix) => {
    const nodes = [svg, ...svg.querySelectorAll('*')], ids = new Map();
    nodes.forEach(node => { if (node.id) ids.set(node.id, prefix + node.id); });
    const urls = value => value.replace(/url\(\s*(['"]?)#([^)'"\s]+)\1\s*\)/g, (match, quote, id) => ids.has(id) ? `url(#${ids.get(id)})` : match);
    nodes.forEach(node => {
      for (const attribute of Array.from(node.attributes)) {
        const name = attribute.name, value = attribute.value;
        if (name === 'id') node.setAttribute(name, ids.get(value));
        else if ((name === 'href' || name === 'xlink:href') && value.startsWith('#') && ids.has(value.slice(1))) node.setAttribute(name, '#' + ids.get(value.slice(1)));
        else if (name === 'aria-labelledby' || name === 'aria-describedby') {
          const references = value.split(/\s+/).filter(id => ids.has(id)).map(id => ids.get(id));
          if (references.length) node.setAttribute(name, references.join(' ')); else node.removeAttribute(name);
        } else if (value.includes('url(')) node.setAttribute(name, urls(value));
      }
      if (node.localName === 'style') node.textContent = urls(node.textContent || '');
      node.removeAttribute('tabindex');
    });
  };
  // Measure the materialized SVG, including its actual font metrics and badge surfaces.
  // Existing scene bounds retain the renderer's mark/curve/shadow clearance.
  const graphPrintBounds = (root, svg) => {
    let bounds = contentBounds(root, root.__cfxGraphState || graphState(root));
    // A nested graphics group consistently includes the viewBox mapping; the
    // outer SVG's getCTM can instead describe its CSS viewport coordinate frame.
    const reference = svg.querySelector('[data-cfx-role="graph-viewport"]');
    const matrix = reference?.getCTM();
    if (!matrix) return bounds;
    const inverse = matrix.inverse();
    const screenInverse = reference.getScreenCTM()?.inverse();
    const include = point => {
      if (!Number.isFinite(point.x) || !Number.isFinite(point.y)) return;
      if (!bounds) bounds = { minX: point.x, minY: point.y, maxX: point.x, maxY: point.y };
      else {
        bounds.minX = Math.min(bounds.minX, point.x); bounds.maxX = Math.max(bounds.maxX, point.x);
        bounds.minY = Math.min(bounds.minY, point.y); bounds.maxY = Math.max(bounds.maxY, point.y);
      }
    };
    svg.querySelectorAll('circle,ellipse,rect,path,polygon,polyline,line,text,image,use').forEach(element => {
      if (element.classList.contains('cfx-graph-bg') || element.closest('defs,clipPath,mask,marker,pattern,symbol') || !element.getClientRects().length) return;
      const style = root.ownerDocument.defaultView.getComputedStyle(element);
      if (style.display === 'none' || style.visibility === 'hidden' || style.visibility === 'collapse') return;
      const transform = element.getCTM();
      if (!transform) return;
      const box = element.getBBox(), toScene = inverse.multiply(transform);
      const stroke = style.stroke === 'none' ? 0 : (parseFloat(style.strokeWidth) || 0) / 2;
      const join = style.strokeLinejoin === 'miter' ? Math.max(1, parseFloat(style.strokeMiterlimit) || 4) : 1;
      const pad = stroke * join;
      for (const x of [box.x - pad, box.x + box.width + pad]) for (const y of [box.y - pad, box.y + box.height + pad]) {
        const point = svg.createSVGPoint(); point.x = x; point.y = y;
        include(point.matrixTransform(toScene));
      }
      // Browser text raster bounds can exceed the local font box. Keep that
      // measured extent too, without adding a separate font estimator or margin.
      if (screenInverse) {
        const rect = element.getBoundingClientRect();
        for (const x of [rect.left, rect.right]) for (const y of [rect.top, rect.bottom]) {
          const point = svg.createSVGPoint(); point.x = x; point.y = y;
          include(point.matrixTransform(screenInverse));
        }
      }
    });
    return bounds;
  };
  const bindGraphPrint = root => {
    const lifetime = graphLifetime(root), view = root.ownerDocument.defaultView;
    if (!lifetime || !view) return;
    const media = view.matchMedia('print');
    let surface = null;
    const clear = () => {
      surface?.remove(); surface = null;
      root.removeAttribute('data-cfx-graph-print-ready');
    };
    const prepare = () => {
      clear();
      if (!lifetime.active || !root.isConnected || root.hidden || root.closest('[hidden]') || !root.getClientRects().length || view.getComputedStyle(root).visibility === 'hidden') return;
      const content = exportSvgContent(root);
      if (!content) return;
      const parsed = new DOMParser().parseFromString(content, 'image/svg+xml').documentElement;
      if (parsed.localName !== 'svg') return;
      const svg = root.ownerDocument.importNode(parsed, true);
      scopeGraphPrintIds(svg, `cfx-graph-print-${++graphPrintSequence}-`);
      const camera = svg.querySelector('[data-cfx-role="graph-viewport"]');
      camera?.removeAttribute('transform'); camera?.style.removeProperty('transform');
      svg.setAttribute('role', 'img'); svg.setAttribute('aria-hidden', 'false'); svg.setAttribute('focusable', 'false');
      // Print prepares a full scene at its design aspect, independently of a
      // portrait screen snapshot's physical dimensions and current camera.
      const design = sceneSize(root);
      svg.setAttribute('width', String(design.width)); svg.setAttribute('height', String(design.height));
      ['width', 'height', 'aspect-ratio'].forEach(name => svg.style.removeProperty(name));
      surface = root.ownerDocument.createElement('div');
      surface.className = 'cfx-graph-print-surface';
      surface.setAttribute('data-cfx-role', 'graph-print-surface');
      surface.appendChild(svg);
      root.after(surface);
      root.setAttribute('data-cfx-graph-print-ready', 'true');
      const heading = root.querySelector('.cfx-graph-header');
      surface.style.setProperty('--cfx-graph-print-reserved-height', `${Math.ceil(heading?.getBoundingClientRect().height || 0) + 16}px`);
      // beforeprint may precede the print media change. A synchronous visible measurement
      // also covers that path without keeping an extra on-screen surface afterward.
      surface.style.display = 'block';
      // Font raster bounds can change with scale; remeasure once after fitting.
      for (let pass = 0; pass < 2; pass++) {
        const bounds = graphPrintBounds(root, svg);
        if (!bounds) break;
        const size = sceneSize(root), aspect = size.width / size.height;
        let width = Math.max(1, bounds.maxX - bounds.minX), height = Math.max(1, bounds.maxY - bounds.minY);
        width = Math.max(width, height * aspect); height = width / aspect;
        const x = (bounds.minX + bounds.maxX - width) / 2, y = (bounds.minY + bounds.maxY - height) / 2;
        svg.setAttribute('viewBox', `${x} ${y} ${width} ${height}`);
        const background = svg.querySelector('.cfx-graph-bg');
        if (background) for (const [name, value] of Object.entries({ x, y, width, height })) background.setAttribute(name, String(value));
      }
      surface.style.removeProperty('display');
    };
    listenGraph(root, view, 'beforeprint', prepare);
    listenGraph(root, view, 'afterprint', clear);
    listenGraph(root, media, 'change', () => media.matches ? prepare() : clear());
    lifetime.own(clear);
    if (media.matches) prepare();
  };
