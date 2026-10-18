import assert from 'node:assert/strict';
import test from 'node:test';
import {isSyntaxRejection} from './reference-errors.mjs';

class UnknownDiagramError extends Error {
  constructor(message) { super(message); this.name = 'UnknownDiagramError'; }
}

test('Qualified diagram-selection, lexical and grammar errors remain documented syntax rejections', () => {
  const unknown = new UnknownDiagramError('No diagram type detected matching given configuration for text: newer-beta');
  // Shapes captured from the actual pinned Mermaid 10 modern-flowchart/requirement probes.
  const lexical = Object.assign(new Error('Lexical error on line 2. Unrecognized text.'), {hash: {text: '', token: null, line: 1}});
  const grammar = Object.assign(new Error('Parse error on line 2:\nExpecting LINE, got NEWLINE'), {
    hash: {text: '\n', token: 'NEWLINE', line: 2, loc: {first_line: 2}, expected: ["'LINE'"]}
  });
  // Captured from all three pinned references for invalid/flowchart-unclosed-subgraph.mmd.
  const grammarEof = Object.assign(new Error("Parse error on line 5:\nExpecting 'end', got '1'"), {
    hash: {text: '', token: 1, line: 4, loc: {first_line: 5}, expected: ["'end'"]}
  });
  for (const version of ['10.9.8', '11.17.2', '12.2.0']) {
    for (const error of [unknown, lexical, grammar, grammarEof]) assert.equal(Boolean(isSyntaxRejection(error, version)), true);
  }
});

test('Runtime, initialization and unqualified errors cannot count as expected syntax rejection', () => {
  for (const error of [new TypeError('Cannot read properties of undefined'), new ReferenceError('parser is not defined'),
    new Error('Parse error on line 2:'), new UnknownDiagramError('Diagram renderer not found.'), null, 'Internal failure']) {
    assert.equal(Boolean(isSyntaxRejection(error, '10.9.8')), false);
  }
  assert.equal(Boolean(isSyntaxRejection(new UnknownDiagramError('No diagram type detected matching given configuration for text: x'), '13.0.0')), false);
  for (const [token, text, message] of [[2, '', "Parse error on line 5:\ngot '2'"],
    [1, 'unexpected', "Parse error on line 5:\ngot '1'"], [1, '', 'Internal parser failure']]) {
    const error = Object.assign(new Error(message), {hash: {text, token, line: 4, loc: {first_line: 5}, expected: ["'end'"]}});
    assert.equal(Boolean(isSyntaxRejection(error, '12.2.0')), false);
  }
});
