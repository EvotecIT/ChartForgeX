import assert from 'node:assert/strict';
import test from 'node:test';
import { validateExternalPluginDescriptor } from './external-plugin-corpus.mjs';

const path = 'external-plugins/zenuml/occurrences.mmd';
const hash = 'b'.repeat(64);
function descriptor() {
  return {
    schemaVersion: 1, corpus: 'external-plugins',
    references: { plugin: { package: '@mermaid-js/mermaid-zenuml', version: '1.0.1', license: 'MIT', integrity: 'sha512-test' } },
    fixtures: { [path]: {
      reference: 'plugin', family: 'ZenUML', sha256: hash,
      provenance: { kind: 'authored-regression', description: 'Original fixture.', sources: ['https://mermaid.js.org/syntax/zenuml.html'] },
      qualification: { status: 'pending-render', method: 'external-plugin-browser-render', parseIsSemanticEvidence: false, receipt: null },
      requiredVisibleTextTokens: ['A', 'B'], requiredObservedContracts: ['creation order']
    } }
  };
}

test('the named external corpus requires exhaustive current-source metadata', () => {
  validateExternalPluginDescriptor(descriptor(), { [path]: hash });
  assert.throws(() => validateExternalPluginDescriptor(descriptor(), {}), /Every external-plugin source/);
  assert.throws(() => validateExternalPluginDescriptor(descriptor(), { [path]: hash, 'external-plugins/zenuml/new.mmd': hash }), /Every external-plugin source/);
  assert.throws(() => validateExternalPluginDescriptor(descriptor(), { [path]: 'c'.repeat(64) }), /source hash changed/);
  const missing = descriptor(); missing.fixtures[path].provenance.sources = [];
  assert.throws(() => validateExternalPluginDescriptor(missing, { [path]: hash }), /source provenance/);
});

test('a plugin parse success cannot become observed semantic qualification', () => {
  const parseOnly = descriptor(); parseOnly.fixtures[path].qualification.parseIsSemanticEvidence = true;
  assert.throws(() => validateExternalPluginDescriptor(parseOnly, { [path]: hash }), /dummy parse/);
  const qualified = descriptor(); qualified.fixtures[path].qualification.status = 'qualified-render';
  assert.throws(() => validateExternalPluginDescriptor(qualified, { [path]: hash }), /bind the observed source/);
  qualified.fixtures[path].qualification.receipt = { sourceSha256: hash, packageVersion: '1.0.1', path: 'retained/render-receipt.json' };
  validateExternalPluginDescriptor(qualified, { [path]: hash });
});
