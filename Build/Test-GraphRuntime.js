const { spawnSync } = require('node:child_process');
const path = require('node:path');

const tests = [
  'tests/graph-explorer-bundles/runtime.test.js',
  'tests/graph-explorer-runtime/physics.test.js',
  'tests/graph-explorer-runtime/picking.test.js',
  'tests/graph-explorer-runtime/surface.test.js',
  'tests/graph-explorer-runtime/state.test.js',
  'tests/graph-explorer-runtime/webgl.test.js',
  'tests/graph-explorer-runtime/images.test.js',
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
