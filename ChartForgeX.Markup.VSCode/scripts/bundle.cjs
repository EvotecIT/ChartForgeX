const path = require('path');
const esbuild = require('esbuild');

const args = new Set(process.argv.slice(2));
const watch = args.has('--watch');

const buildOptions = {
  entryPoints: [path.join(__dirname, '..', 'src', 'extension.ts')],
  outfile: path.join(__dirname, '..', 'out', 'extension.js'),
  bundle: true,
  platform: 'node',
  format: 'cjs',
  target: 'node20',
  sourcemap: true,
  external: ['vscode'],
  logLevel: 'info'
};

async function run() {
  if (watch) {
    const context = await esbuild.context(buildOptions);
    await context.watch();
    return;
  }

  await esbuild.build(buildOptions);
}

run().catch((error) => {
  console.error(error);
  process.exit(1);
});
