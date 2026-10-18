  // SVG coordinates have three decimals. Bound screen-space comparisons by one logical
  // quantization step, projected through the live transform, plus a small screen rounding allowance.
  const svgCoordinatePrecision = matrix => ({
    x: .001 * (Math.abs(matrix.a) + Math.abs(matrix.c)) + .0001,
    y: .001 * (Math.abs(matrix.b) + Math.abs(matrix.d)) + .0001
  });
  // Captured native geometry avoids browser hit-area inflation and screen-layout box quantization.
  const nativeRegionScreenBox = (node, region = node._cfxNativeRegion) => {
    const matrix = region && node.getScreenCTM();
    if (!matrix) return null;
    const corners = [[region.x, region.y], [region.x + region.width, region.y],
      [region.x, region.y + region.height], [region.x + region.width, region.y + region.height]]
      .map(([x, y]) => new DOMPoint(x, y).matrixTransform(matrix));
    return { left: Math.min(...corners.map(point => point.x)), right: Math.max(...corners.map(point => point.x)),
      top: Math.min(...corners.map(point => point.y)), bottom: Math.max(...corners.map(point => point.y)) };
  };
