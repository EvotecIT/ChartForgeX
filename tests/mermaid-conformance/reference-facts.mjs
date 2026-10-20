// Semantic facts qualified against the pinned Mermaid 10/11/12 packages.
export function gitGraphFacts(db, expected) {
  const source = db.getCommits();
  const commits = (source instanceof Map ? [...source.values()] : Object.values(source)).sort((a, b) => a.seq - b.seq);
  const displayedTypes = ['Normal', 'Reverse', 'Highlight', 'Merge', 'CherryPick'];
  return {
    branches: db.getBranchesAsObjArray().map(branch => branch.name),
    commits: commits.map((commit, index) => [
      expected.commits[index][0] === null ? null : commit.id,
      commit.branch,
      commit.parents,
      displayedTypes[commit.customType ?? commit.type],
      (commit.tags ?? (commit.tag ? [commit.tag] : [])).join(', ')
    ]),
    selectedParent: commits.filter(commit => commit.type === 4)
      .flatMap(commit => commit.tags ?? (commit.tag ? [commit.tag] : []))
      .map(tag => /\|parent:(.+)$/.exec(tag)?.[1]).find(parent => parent !== undefined) ?? ''
  };
}

// Test-only semantic facts. Qualified against official Mermaid 10.9.8, 11.17.2 and 12.2.0.
// FlowDB appends repeated class declarations, then compiles default/node/assigned classes
// in that order; direct vertex declarations override compiled class properties.
export function flowchartStyleFacts(db) {
  const values = collection => collection instanceof Map ? [...collection.values()] : Object.values(collection);
  const definitions = values(db.getClasses());
  const vertices = values(db.getVertices());
  const styles = declarations => Object.fromEntries((declarations ?? []).filter(text => text.trim()).map(text => {
    const colon = text.indexOf(':');
    return [text.slice(0, colon).trim().toLowerCase(), text.slice(colon + 1).trim()];
  }));
  const facts = { nodeClasses: {}, nodeStyles: {}, edgeStyles: [] };
  for (const vertex of vertices) {
    facts.nodeClasses[vertex.id] = vertex.classes;
    const declarations = [];
    for (const name of ['default', 'node', ...vertex.classes]) {
      const definition = definitions.find(item => item.id === name);
      // textStyles target labels, not node surfaces. Mermaid 10 converts source color
      // to text fill; merging it here would incorrectly recolor the node background.
      declarations.push(...(definition?.styles ?? []));
    }
    declarations.push(...(vertex.styles ?? []));
    facts.nodeStyles[vertex.id] = styles(declarations);
  }
  const edges = db.getEdges();
  for (const edge of edges) {
    const declarations = [...(edges.defaultStyle ?? []), ...(edge.style ?? [])];
    // FlowDB injects fill:none for an explicitly styled link. It is an internal
    // open-path paint default, not an authored declaration in this shared contract.
    facts.edgeStyles.push(Object.fromEntries(Object.entries(styles(declarations))
      .filter(([property, value]) => property !== 'fill' || value !== 'none')));
  }
  return facts;
}
