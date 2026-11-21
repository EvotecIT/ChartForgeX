window[Symbol.for('ChartForgeX.uiText')] ||= (root, key, text, values) => {
  const source = root?.dataset.cfxUi || '';
  let bundle = root?._cfxUi;
  if (!bundle || bundle.source !== source) {
    let messages;
    try { messages = source ? JSON.parse(source) : {}; } catch { messages = {}; }
    bundle = { source, messages };
    if (root) root._cfxUi = bundle;
  }
  const value = bundle.messages?.[key];
  if (typeof value === 'string') text = value;
  return values ? text.replace(/\{([^{}]+)\}/g, (token, name) => Object.prototype.hasOwnProperty.call(values, name) ? String(values[name]) : token) : text;
};
// Strict CSP: runtimes turn HtmlStrictStyles' cfx-s-* classes back into inline styles through the CSSOM when they mount.
window[Symbol.for('ChartForgeX.strictStyles')] ||= (() => {
  const pattern = /^\.(cfx-s-[0-9a-f]{12})\.\1\.\1$/, cache = new WeakMap();
  const rulesOf = sheet => {
    if (!cache.has(sheet)) {
      const rules = new Map();
      try { for (const rule of sheet.cssRules) { const match = pattern.exec(rule.selectorText || ''); if (match) rules.set(match[1], rule.style.cssText); } } catch { }
      cache.set(sheet, rules);
    }
    return cache.get(sheet);
  };
  return (root, css) => {
    if (!root?.querySelectorAll) return;
    // Adaptive drawings wait in templates, which querySelectorAll does not enter.
    const nodes = root.matches?.('[class*="cfx-s-"]') ? [root] : [];
    const collect = scope => {
      nodes.push(...scope.querySelectorAll('[class*="cfx-s-"]'));
      for (const template of scope.querySelectorAll('template')) collect(template.content);
    };
    collect(root);
    if (!nodes.length) return;
    const sources = [];
    if (css) try { const sheet = new CSSStyleSheet(); sheet.replaceSync(css); sources.push(rulesOf(sheet)); } catch { }
    for (const owner of new Set([root.getRootNode(), root.ownerDocument, document])) for (const sheet of owner?.styleSheets || []) sources.push(rulesOf(sheet));
    for (const node of nodes) {
      for (const name of [...node.classList]) {
        const text = name.startsWith('cfx-s-') && sources.find(rules => rules.has(name))?.get(name);
        if (text === undefined || text === false) continue;
        node.style.cssText = node.style.cssText ? node.style.cssText + ';' + text : text;
        node.classList.remove(name);
      }
      if (!node.classList.length) node.removeAttribute('class');
    }
  };
})();
