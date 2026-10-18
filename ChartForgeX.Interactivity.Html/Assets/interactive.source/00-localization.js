  const uiText = (root, message, fallback, values) => {
    let messages;
    const raw = root?.dataset.cfxUi || '';
    if (root && root._cfxUiSource === raw) messages = root._cfxUiMessages;
    else {
      try { messages = raw ? JSON.parse(raw) : {}; } catch { messages = {}; }
      if (root) { root._cfxUiSource = raw; root._cfxUiMessages = messages; }
    }
    let text = typeof messages?.[message] === 'string' ? messages[message] : fallback;
    if (values) Object.entries(values).forEach(([name, value]) => { text = text.split('{' + name + '}').join(String(value)); });
    return text;
  };
