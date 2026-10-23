// Test-only projections of public pinned Mermaid namespace/subgraph data.
// Fixtures are authored locally from the documented Class and ER syntax; no renderer is shipped.
const values = collection => collection instanceof Map ? [...collection.values()] : Object.values(collection ?? {});
const declarations = list => Object.fromEntries((list ?? []).map(item => {
  const separator = item.indexOf(':');
  return [item.slice(0, separator).trim().toLowerCase(), item.slice(separator + 1).trim()];
}));

export function classNamespaceFacts(db) {
  return {
    namespaces: values(db.getNamespaces()).map(item => [item.id, item.label, item.parent ?? null]),
    classes: values(db.getClasses()).map(item => [item.id, item.parent ?? null])
  };
}

export function entityGroupFacts(db) {
  // The canonical data projection excludes implicit entities replaced by group shells
  // and resolves first-completed-group ownership exactly as the reference renderer does.
  const nodes = db.getData().nodes;
  return {
    groups: nodes.filter(item => item.isGroup).map(item => [item.id, item.label, item.parentId ?? null, item.dir ?? null]),
    entities: nodes.filter(item => !item.isGroup).map(item => [item.label, item.parentId ?? null]),
    groupStyles: Object.fromEntries(nodes.filter(item => item.isGroup).map(item =>
      [item.id, declarations([...(item.cssCompiledStyles ?? []), ...(item.cssStyles ?? [])])]))
  };
}
