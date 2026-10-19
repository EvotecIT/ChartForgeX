// These signatures are qualified against the pinned packages, rather than treating any
// rejection from mermaid.parse as unavailable syntax. New error shapes fail closed.
const qualifiedVersions = new Set(['10.9.8', '11.17.2', '12.2.0']);

export function isSyntaxRejection(error, version) {
  if (!qualifiedVersions.has(version)) return false;
  if (error?.name === 'UnknownDiagramError' && error?.constructor?.name === 'UnknownDiagramError') {
    return typeof error.message === 'string' && error.message.startsWith('No diagram type detected matching given configuration for text:');
  }
  if (error?.name !== 'Error' || typeof error.message !== 'string') return false;
  const hash = error.hash;
  if (!hash || !Number.isInteger(hash.line) || hash.line < 0 || typeof hash.text !== 'string') return false;
  if (/^Lexical error on line \d+\./.test(error.message)) return hash.token === null;
  // The pinned Jison flowchart parsers use numeric EOF (1) after an unclosed subgraph.
  const grammarToken = typeof hash.token === 'string' ||
    (hash.token === 1 && hash.text === '' && /got '1'$/.test(error.message));
  return Boolean(/^Parse error on line \d+:/.test(error.message) && grammarToken &&
    hash.loc && Number.isInteger(hash.loc.first_line) && Array.isArray(hash.expected) && hash.expected.every(value => typeof value === 'string'));
}
