// Contract-shaped DOM geometry for tests whose viewport transform is identity.
// CSS zoom and transformed hosts are exercised by the real-browser projection suite.
class Point {
  constructor(x, y) { this.x = x; this.y = y; }
  matrixTransform(matrix) {
    return new Point(matrix.a * this.x + matrix.c * this.y + matrix.e,
      matrix.b * this.x + matrix.d * this.y + matrix.f);
  }
}

const identitySvg = () => ({
  viewBox: { baseVal: { width: 100, height: 100 } },
  preserveAspectRatio: { baseVal: { align: 6, meetOrSlice: 1 } },
  getScreenCTM: () => ({ a: 1, b: 0, c: 0, d: 1, e: 0, f: 0, is2D: true, inverse() { return this; } }),
  getBoundingClientRect: () => ({ width: 100, height: 100 })
});
const getComputedStyle = () => ({ width: '100px', height: '100px', boxSizing: 'content-box',
  paddingLeft: '0', paddingRight: '0', paddingTop: '0', paddingBottom: '0',
  borderLeftWidth: '0', borderRightWidth: '0', borderTopWidth: '0', borderBottomWidth: '0' });

module.exports = { Point, identitySvg, getComputedStyle };
