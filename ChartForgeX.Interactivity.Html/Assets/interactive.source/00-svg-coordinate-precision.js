
  const svgScreenProjection = (svg, styles) => {
    const projections = styles && (styles._cfxScreenProjections ||= new WeakMap());
    if (projections?.has(svg)) return projections.get(svg);
    const matrix = svg?.getScreenCTM(), view = svg?.viewBox.baseVal;
    if (!matrix || !view?.width || !view.height || matrix.is2D === false
      || matrix.b !== 0 || matrix.c !== 0 || matrix.a <= 0 || matrix.d <= 0) return null;
    const style = getComputedStyle(svg);
    const viewport = (dimension, sides) => {
      const inset = sides.reduce((sum, side) => sum + parseFloat(style['padding' + side]) + parseFloat(style['border' + side + 'Width']), 0);
      const size = parseFloat(style[dimension]) - (style.boxSizing === 'border-box' ? inset : 0);
      return { size, outer: size + inset };
    };
    const horizontal = viewport('width', ['Left', 'Right']), vertical = viewport('height', ['Top', 'Bottom']);
    const width = horizontal.size, height = vertical.size;
    if (!(width > 0 && height > 0)) return null;
    const box = svg.getBoundingClientRect(), aspect = svg.preserveAspectRatio.baseVal;
    const scale = (aspect.meetOrSlice === 2 ? Math.max : Math.min)(width / view.width, height / view.height);
    const x = (aspect.align === 1 ? width / view.width : scale) * box.width / horizontal.outer / matrix.a;
    const y = (aspect.align === 1 ? height / view.height : scale) * box.height / vertical.outer / matrix.d;
    const projection = Math.abs(x - 1) + Math.abs(y - 1) >= .000001
      && Number.isFinite(x) && Number.isFinite(y) && x > 0 && y > 0
      ? new DOMMatrix([x, 0, 0, y, matrix.e * (1 - x), matrix.f * (1 - y)]) : null;
    projections?.set(svg, projection);
    return projection;
  };
  const svgScreenMatrix = (node, projection = svgScreenProjection(node?.ownerSVGElement || node)) => {
    const matrix = node?.getScreenCTM();
    return matrix && projection ? projection.multiply(matrix) : matrix;
  };

  const svgCoordinatePrecision = matrix => ({
    x: .001 * (Math.abs(matrix.a) + Math.abs(matrix.c)) + .0001,
    y: .001 * (Math.abs(matrix.b) + Math.abs(matrix.d)) + .0001
  });

  // A 2D matrix maps a point as x * a + y * c + e and x * b + y * d + f, exactly as DOMPoint.matrixTransform does;
  // the arithmetic spares four point objects per box, which dense charts measure for every observation.
  const screenBounds = ({ x, y, width, height }, m) => {
    const xs = [], ys = [];
    for (const [px, py] of [[x, y], [x + width, y], [x, y + height], [x + width, y + height]]) { xs.push(px * m.a + py * m.c + m.e); ys.push(px * m.b + py * m.d + m.f); }
    return { left: Math.min(...xs), right: Math.max(...xs), top: Math.min(...ys), bottom: Math.max(...ys) };
  };
  const nativeRegionScreenBox = (node, region = node._cfxNativeRegion, projection) => {
    const matrix = region && svgScreenMatrix(node, projection);
    return matrix ? screenBounds(region, matrix) : null;
  };
