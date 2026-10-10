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
  for (const version of ['10.9.8', '11.17.2', '12.1.0']) {
    for (const error of [unknown, lexical, grammar]) assert.equal(Boolean(isSyntaxRejection(error, version)), true);
  }
});

test('Runtime, initialization and unqualified errors cannot count as expected syntax rejection', () => {
  for (const error of [new TypeError('Cannot read properties of undefined'), new ReferenceError('parser is not defined'),
    new Error('Parse error on line 2:'), new UnknownDiagramError('Diagram renderer not found.'), null, 'Internal failure']) {
    assert.equal(Boolean(isSyntaxRejection(error, '10.9.8')), false);
  }
  assert.equal(Boolean(isSyntaxRejection(new UnknownDiagramError('No diagram type detected matching given configuration for text: x'), '13.0.0')), false);
});
