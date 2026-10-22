const { spawnSync } = require('node:child_process');
const path = require('node:path');

const tests = [
  'tests/adapter-localization/runtime.test.js',
  'tests/chart-interactivity/boxplot-distributions.test.js',
  'tests/graph-explorer-bundles/runtime.test.js',
  'tests/graph-explorer-runtime/physics.test.js',
  'tests/graph-explorer-runtime/picking.test.js',
  'tests/graph-explorer-runtime/surface.test.js',
  'tests/graph-explorer-runtime/fit-bounds.test.js',
  'tests/graph-explorer-runtime/state.test.js',
  'tests/graph-explorer-runtime/webgl.test.js',
  'tests/graph-explorer-runtime/svg-arrows.test.js',
  'tests/graph-explorer-runtime/images.test.js',
  'tests/graph-explorer-runtime/lifetime.test.js',
  'tests/graph-explorer-runtime/linked-commands.test.js',
  'tests/graph-explorer-runtime/selection-policy.test.js',
  'tests/interactive-runtime/linked-paint.test.js',
  'tests/interactive-runtime/target-resolution.test.js',
  'tests/interactive-runtime/annotation-identity.test.js',
  'tests/interactive-runtime/peer-hover-ownership.test.js',
  'tests/interactive-runtime/highlight-intent.test.js',
  'tests/chart-runtime/readable-values.test.js',
  'tests/chart-runtime/brush-scale.test.js',
  'tests/chart-runtime/time-spacing.test.js',
  'tests/chart-runtime/native-physical-axes.test.js',
  'tests/chart-runtime/native-hierarchy.test.js',
  'tests/chart-runtime/calendar-window-boundary.test.js',
  'tests/graph-neighborhood/planner.test.js',
  'tests/graph-neighborhood/navigation.test.js',
  'tests/graph-quality/overlaps.test.js',
  'tests/topology-report/navigation.test.js',
  'tests/topology-filters/composition.test.js',
  'tests/topology-filters/shared-trunks.test.js'
];

const result = spawnSync(process.execPath, ['--test', ...tests], {
  cwd: path.resolve(__dirname, '..'),
  stdio: 'inherit'
});
if (result.error) console.error(result.error.message);
process.exitCode = result.status ?? 1;
