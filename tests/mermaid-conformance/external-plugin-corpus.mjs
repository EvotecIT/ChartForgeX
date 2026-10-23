import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { readFile, readdir } from 'node:fs/promises';
import { join } from 'node:path';

// This inventory is deliberately separate from the official built-in parser corpus.
// An external plugin may expose a dummy parser and perform real parsing only while rendering.
export function validateExternalPluginDescriptor(manifest, files) {
  assert.equal(manifest.schemaVersion, 1, 'Unsupported external-plugin corpus schema.');
  assert.equal(manifest.corpus, 'external-plugins');
  assert.ok(Object.keys(manifest.references ?? {}).length > 0, 'Declare the external reference.');
  assert.deepEqual(Object.keys(manifest.fixtures ?? {}).sort(), Object.keys(files).sort(),
    'Every external-plugin source needs metadata; remove stale entries.');
  for (const [path, fixture] of Object.entries(manifest.fixtures)) {
    assert.match(path, /^external-plugins\/[a-z0-9-]+\/[a-z0-9-]+\.mmd$/, 'Keep plugin sources in their named corpus.');
    const reference = manifest.references[fixture.reference];
    assert.ok(reference?.package && reference.version && reference.license && reference.integrity,
      `${path}: pin the package, version, license and archive integrity.`);
    assert.ok(fixture.family && fixture.provenance?.description && fixture.provenance?.sources?.length > 0,
      `${path}: record family and source provenance.`);
    assert.equal(fixture.provenance.kind, 'authored-regression');
    assert.equal(fixture.sha256, files[path], `${path}: source hash changed; review its metadata.`);
    assert.ok(fixture.requiredVisibleTextTokens?.length > 0 && fixture.requiredObservedContracts?.length > 0,
      `${path}: declare the visible and semantic observations needed.`);
    const qualification = fixture.qualification;
    assert.equal(qualification?.method, 'external-plugin-browser-render', `${path}: parsing alone is insufficient.`);
    assert.equal(qualification.parseIsSemanticEvidence, false, `${path}: a dummy parse is not semantic proof.`);
    assert.ok(['pending-render', 'qualified-render'].includes(qualification.status), `${path}: invalid qualification status.`);
    if (qualification.status === 'pending-render') assert.equal(qualification.receipt, null, `${path}: pending means unobserved.`);
    else {
      assert.equal(qualification.receipt?.sourceSha256, fixture.sha256, `${path}: bind the observed source.`);
      assert.equal(qualification.receipt?.packageVersion, reference.version, `${path}: bind the observed plugin.`);
      assert.ok(qualification.receipt?.path, `${path}: retain the actual DOM/paint qualification receipt.`);
    }
  }
}

export async function assertExternalPluginCorpus(root) {
  const manifest = JSON.parse(await readFile(join(root, 'external-plugin-corpus.json'), 'utf8'));
  const files = {};
  async function visit(relative) {
    for (const entry of await readdir(join(root, relative), { withFileTypes: true })) {
      assert.ok(!entry.isSymbolicLink(), `${relative}/${entry.name}: external corpora cannot follow links.`);
      const path = `${relative}/${entry.name}`;
      if (entry.isDirectory()) await visit(path);
      else if (entry.name.endsWith('.mmd')) files[path] = createHash('sha256').update(await readFile(join(root, path))).digest('hex');
    }
  }
  await visit('external-plugins');
  validateExternalPluginDescriptor(manifest, files);
}
