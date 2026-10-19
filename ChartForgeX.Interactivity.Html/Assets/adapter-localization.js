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
