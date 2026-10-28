const fs = require('node:fs');
const path = require('node:path');
const source = fs.readFileSync(path.resolve(__dirname,
  '../../ChartForgeX.Interactivity.Html/Assets/adapter-native-text-export.js'), 'utf8');

// Use the same prelude that the chart, topology and report producers include.
function installNativeText(window, document) {
  new Function('window', 'document', source)(window, document);
  return window[Symbol.for('ChartForgeX.nativeTextExport')];
}

module.exports = { installNativeText, nativeTextSource: source };
