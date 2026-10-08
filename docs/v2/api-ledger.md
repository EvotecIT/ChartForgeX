# Public API ownership ledger

The post-extraction inventory covers eight assemblies, including the shared visual-default contracts:

| Artifact | Purpose |
| --- | --- |
| [api-ledger-phase4.csv](api-ledger-phase4.csv) | Current exported source symbols and their assembly owners. |
| [api-ledger-phase4-diff.csv](api-ledger-phase4-diff.csv) | Assembly moves, changed signatures and removed/replaced APIs relative to the merged Phase 3 source. |
| [api-ledger-phase4-manifest.json](api-ledger-phase4-manifest.json) | Pinned source commits, source counts and disposition totals. |

Regenerate these files with `dotnet run --project tools/v2-api-ledger/ApiLedger.csproj -- --phase4 . docs/v2 <candidate-commit>`. Assembly moves are breaking changes for compiled callers even when their namespaces and signatures stay the same. The inventory records source symbols; four-framework packed assets, dependencies, resources and isolated execution are qualified separately through `Build/Test-PackageQualification.ps1`.

`assembly-move-and-signature-change` records both migration requirements for the same symbol. Rebuild against its new assembly and inspect the old/new signatures for changed base types, interfaces or call defaults. Move-only and signature-only rows retain their separate classifications.

The baseline inventories below record the six runtime assemblies before extraction at pinned source commits. They cover externally visible types and declared public, protected and protected-internal members, including overloads, optional parameter defaults, generic constraints, properties and their accessor visibility, indexers, events, operators, implicit public constructors, delegate invocation signatures and enum numeric values. Partial declarations are combined through compiler symbols. Inherited members remain represented by their declaring type and base/interface relationships rather than copied onto every subclass.

The generator uses the .NET SDK's bundled Roslyn assemblies, without adding a NuGet package or a production dependency. It reads immutable Git objects into memory, follows the existing project-reference graph and rejects compiler errors. It does not build or execute ChartForgeX. Its miniature self-test protects API inventory fidelity: nested accessibility, partial-type deduplication, overloads, optional defaults, protected members, implicit constructors, enum constants, delegates and accessor visibility. A synthetic Git consumer also verifies anonymous evidence counts, exclusion of private paths and commits, external mapping validation, and UTF-8 CSV quoting with LF separators. An optional path after `--self-test` selects its scratch directory.

Run from the repository root with a .NET 10 SDK:

```powershell
dotnet run --project tools/v2-api-ledger/ApiLedger.csproj -- --self-test
dotnet run --project tools/v2-api-ledger/ApiLedger.csproj -- . docs/v2 <integrated-baseline-commit>
```

The optional fourth argument specifies the consumer repository root. Otherwise the tool uses `EVOTEC_GITHUB_ROOT`, falling back to the parent of the repository's common Git directory. This also works from a linked worktree. Baseline commits for main and the label-placement candidate are pinned in the tool; the integrated baseline is a required argument.

An optional fifth argument supplies a private consumer mapping JSON file kept outside the repository. Each entry contains `neutralName` and `path`, for example `[{"neutralName":"private-consumer-reporting","path":"/external/private-consumer"}]`. Use a neutral role beginning with `private-consumer-`. The generator exposes only that role and file counts for these consumers, with no repository identities, commits or paths. Full private evidence stays in the external audit. The public default indexes only PowerBGInfo, ImagePlayground and OfficeIMO; reproducing private evidence counts also requires the corresponding external mapping and source snapshots.

| Artifact | Purpose |
| --- | --- |
| `api-ledger-main.csv` (external evidence) | Exact exported surface on main before integration, reproducible from its pinned commit. |
| `api-ledger-label.csv` (external evidence) | Exact exported surface on the label-placement candidate, reproducible from its pinned commit. |
| `api-ledger-integrated.csv` | Exact exported surface of the integrated baseline commit. |
| `api-ledger-baseline-diff.csv` | Changed or absent original APIs, with explicit integration review, replacement and source evidence. Unchanged totals remain in the manifest. |
| `api-ledger-manifest.json` | Source commits, package/type/member totals, conditional-source flags, analysis runtime and evidence limits. |

The complete main/label inventories are retained outside the checkout under the resolved consumer root's `_worktrees/_codex-evidence/ChartForgeX-v2-phase01/api-ledger/`. Their paths, byte sizes and SHA-256 hashes are recorded in the manifest. The integrated inventory remains checked in; the generator recreates the pinned originals when external evidence is unavailable.

`proposed_owner`, `planned_disposition`, `replacement_plan` and `migration_phase` record the reviewed migration direction. They do not claim that package extraction or the replacement API has already happened. `implementation_state` deliberately identifies a baseline inventory. A retained capability can still adopt the common contracts later; retaining a capability does not promise binary compatibility or identical renderer internals.

Ownership rules distinguish genuine diagrams from factual composition despite their current shared VisualBlocks namespace. Mixed block contracts/renderers and artifact dispatch require decomposition; their common contract belongs in core while optional producers/rendering belong in their packages. Member-level overrides identify topology animation, GIF/APNG output, watermark coupling and static decorative controls. Meaningful checked/completed-state markers are retained. Static GIF decoding remains a core input capability; GIF encoding moves to Stories, including ImageComposition's current instance export, which needs a Stories-owned replacement extension. Shared format enums may retain input identifiers without allowing static packages to encode animation formats.

Consumer and existing-test columns contain **text leads**, not semantic call analysis or executed proof. The generator indexes tracked C#/PowerShell files containing `ChartForgeX` in the three public consumers, recording each local head and dirty state. Optional private consumers contribute neutral roles and counts. It requires receiver/type and member token co-occurrence in a file and caps displayed leads at five before grouping private matches as `private-consumer-<role>#displayed-text-leads=<count>`. These counts describe displayed matches, not all matching files. This can miss aliases/global imports, dynamically bound PowerShell calls and generic helpers, and can include comments or unrelated same-named members. An empty column means no qualifying lead was found; it never authorizes deletion. Specific consumer worktrees and scripts identified by the consumer audit remain required evidence, even if the root checkout differs.

`required_proof` is an acceptance obligation, while `proof_state` remains unqualified by this inventory. Family rules supply a shared obligation to every member; exact consumer migration examples and output fixtures close that obligation during the relevant phase. Do not convert a token hit into a passing test or a proposed replacement into an implemented API.

The symbol pass resolves against the tool runtime's BCL. Source conditionals cause a failing exit and require target-specific symbol passes. The current source inventory has no such conditionals, but all four compiled framework assets still require the repository build/package gate; this tool alone does not qualify net472, netstandard2.0, net8.0 or net10.0. SDK-generated assembly metadata, private/internal implementation, compiler backing fields, CLI argument surfaces and consumer cmdlet parameters are outside this runtime source ledger. Explicit interface implementations remain represented by the exported interface contract. The separate package/consumer qualification records provide compiled and installed-package evidence.

The integrated baseline review identifies two signature changes and 31 replaced candidate APIs. `Chart.Options` gains a private setter, preserving its external getter contract. `MermaidFlowchartDocument` becomes inheritable to support Mermaid domain document types. Main's canonical `ChartForgeX.Mermaid.MermaidRenderOptions` replaces the candidate's `MermaidVisualMarkupRenderOptions`, preserving its named per-kind options while adding the later diagram families; both markup parser constructors accept the canonical type. Those replacements are distinct from the future v2 package moves and have explicit rows in the diff. Source review and the existing per-kind parser test are evidence pointers; executed integration validation is recorded by the coordinator.
