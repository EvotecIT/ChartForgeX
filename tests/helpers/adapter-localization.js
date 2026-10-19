const fs = require('node:fs');
const path = require('node:path');
const source = fs.readFileSync(path.resolve(__dirname, '../../ChartForgeX.Interactivity.Html/Assets/adapter-localization.js'), 'utf8');

// Use the shipped prelude, including its parsing, fallback and per-root cache.
function installUiText(window) {
  new Function('window', source)(window);
  return window[Symbol.for('ChartForgeX.uiText')];
}
module.exports = { installUiText, localizationSource: source };
