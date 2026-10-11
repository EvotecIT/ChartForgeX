  // SVG coordinates have three decimals. Bound screen-space comparisons by one logical
  // quantization step, projected through the live transform, plus a small screen rounding allowance.
  const svgCoordinatePrecision = matrix => ({
    x: .001 * (Math.abs(matrix.a) + Math.abs(matrix.c)) + .0001,
    y: .001 * (Math.abs(matrix.b) + Math.abs(matrix.d)) + .0001
  });
