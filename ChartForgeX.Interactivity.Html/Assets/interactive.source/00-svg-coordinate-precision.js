
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
  // Allow native .001/screen rounding.
  const svgCoordinatePrecision = matrix => ({
    x: .001 * (Math.abs(matrix.a) + Math.abs(matrix.c)) + .0001,
    y: .001 * (Math.abs(matrix.b) + Math.abs(matrix.d)) + .0001
  });

  const nativeRegionScreenBox = (node, region = node._cfxNativeRegion, projection) => {
    const matrix = region && svgScreenMatrix(node, projection);
    if (!matrix) return null;
    const corners = [[region.x, region.y], [region.x + region.width, region.y],
      [region.x, region.y + region.height], [region.x + region.width, region.y + region.height]]
      .map(([x, y]) => new DOMPoint(x, y).matrixTransform(matrix));
    return { left: Math.min(...corners.map(point => point.x)), right: Math.max(...corners.map(point => point.x)),
      top: Math.min(...corners.map(point => point.y)), bottom: Math.max(...corners.map(point => point.y)) };
  };
