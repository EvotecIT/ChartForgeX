// Test-only projections of public pinned Mermaid database facts, independent of layout pixels.
const entries = collection => collection instanceof Map ? [...collection.entries()] : Object.entries(collection ?? {});
const values = collection => Array.isArray(collection) ? collection : entries(collection).map(([, value]) => value);
const noteText = text => (text ?? '').replaceAll('\\n', '\n').split('\n').map(line => line.trim()).join('\n');
const genericText = text => {
  let depth = 0;
  const result = [...text].map((character, index, source) => {
    if (character !== '~') return character;
    if (index > 0 && /[\p{L}\p{N}]/u.test(source[index - 1]) && /[\p{L}\p{N}_]/u.test(source[index + 1] ?? '')) { depth++; return '<'; }
    if (depth > 0) { depth--; return '>'; }
    return '~';
  }).join('');
  return depth === 0 ? result : text;
};

export function classFacts(db) {
  const marker = value => ({ none: 'None', 0: 'OpenDiamond', 1: 'OpenTriangle', 2: 'Diamond', 3: 'Arrow', 4: 'Lollipop' })[value];
  const multiplicity = value => value === 'none' || value === undefined ? '' : value;
  return {
    nodes: values(db.getClasses()).map(node => [node.id, node.label + (node.type ? `<${node.type}>` : ''),
      node.members.map(member => genericText((member.visibility ?? '') + member.id)),
      node.methods.map(member => genericText((member.visibility ?? '') + member.id + '(' + (member.parameters ?? '') + ')' + (member.returnType ? ' ' + member.returnType : '')))]),
    relationships: db.getRelations().map(item => [item.relation.type1 === 4 ? '@interface' : item.id1,
      item.relation.type2 === 4 ? '@interface' : item.id2, marker(item.relation.type1), marker(item.relation.type2),
      item.relation.lineType === 0 ? 'Solid' : 'Dashed', item.title ?? '', multiplicity(item.relationTitle1), multiplicity(item.relationTitle2)]),
    notes: values(db.getNotes()).map(note => [note.class ?? null, noteText(note.text)])
  };
}

export function stateFacts(db) {
  const states = new Map(); const edges = []; const notes = new Map(); const directions = {};
  const special = state => state.start || state.id === 'root_start' || state.id === 'root_end';
  const add = (state, parent) => {
    if (special(state)) return;
    const previous = states.get(state.id);
    if (!previous || state.type && state.type !== 'default') states.set(state.id, [state.id, state.type && state.type !== 'default' ? state.type : 'state', parent]);
  };
  const walk = (document, parent = null) => {
    for (const statement of document ?? []) {
      if (statement.stmt === 'dir') directions[parent ?? 'root'] = statement.value;
      else if (statement.stmt === 'relation') {
        add(statement.state1, parent); add(statement.state2, parent);
        edges.push([special(statement.state1) ? '[*]' : statement.state1.id,
          special(statement.state2) ? '[*]' : statement.state2.id, statement.description ?? '']);
      } else if (statement.stmt === 'state') {
        add(statement, parent);
        if (statement.note) notes.set(statement.id, [statement.id, statement.note.position.startsWith('left') ? 'Left' : 'Right', noteText(statement.note.text)]);
        if (statement.doc) walk(statement.doc, statement.id);
      }
    }
  };
  walk(db.getRootDocV2().doc);
  return { states: [...states.values()], edges, notes: [...notes.values()], directions };
}

export function erFacts(db) {
  const entities = entries(db.getEntities());
  const ids = new Map(entities.map(([id, entity]) => [entity.id ?? id, id]));
  const cardinality = value => ({ ONLY_ONE: 'ExactlyOne', ZERO_OR_ONE: 'ZeroOrOne', ONE_OR_MORE: 'OneOrMany', ZERO_OR_MORE: 'ZeroOrMany' })[value];
  return {
    entities: entities.map(([id, entity]) => [id, entity.alias ?? entity.label ?? id,
      entity.attributes.map(attribute => [attribute.type ?? attribute.attributeType, attribute.name ?? attribute.attributeName,
        attribute.keys ?? attribute.attributeKeyTypeList ?? [], attribute.comment ?? attribute.attributeComment ?? ''])]),
    relationships: db.getRelationships().map(item => [ids.get(item.entityA), ids.get(item.entityB),
      cardinality(item.relSpec.cardB), cardinality(item.relSpec.cardA), item.relSpec.relType === 'IDENTIFYING' ? 'Solid' : 'Dashed', item.roleA])
  };
}

export function sequenceLifetimeFacts(db) {
  const actors = entries(db.getActors());
  return {
    participants: actors.map(([id, actor]) => [id, actor.description, actor.type]),
    boxes: db.getBoxes().map(box => [box.name, box.fill, box.actorKeys]),
    created: entries(db.getCreatedActors()), destroyed: entries(db.getDestroyedActors())
  };
}
