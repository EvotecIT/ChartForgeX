// Test-only native paint facts from the pinned Mermaid 11/12 class and ER databases.
// Source declarations are resolved semantically; browser CSS is not a production dependency.
export function nodeStyleFacts(db, family) {
  const values = collection => collection instanceof Map ? [...collection.values()] : Object.values(collection ?? {});
  const nodes = family === 'class' ? values(db.getClasses()) : values(db.getEntities());
  const definitions = family === 'class' ? values(db.styleClasses) : values(db.getClasses());
  const parse = declarations => Object.fromEntries((declarations ?? []).filter(text => text.trim()).map(text => {
    const separator = text.indexOf(':');
    return [text.slice(0, separator).trim().toLowerCase(), text.slice(separator + 1).trim()];
  }));
  const facts = { nodeClasses: {}, nodeStyles: {} };
  for (const node of nodes) {
    const id = family === 'class' ? node.id : node.label;
    const classes = node.cssClasses.split(/\s+/).filter(name => name && name !== 'default');
    facts.nodeClasses[id] = classes;
    const declarations = [];
    for (const name of ['default', ...classes]) {
      const definition = definitions.find(item => item.id === name);
      declarations.push(...(definition?.styles ?? []));
    }
    declarations.push(...(family === 'class' ? node.styles : node.cssStyles));
    facts.nodeStyles[id] = parse(declarations);
  }
  return facts;
}
