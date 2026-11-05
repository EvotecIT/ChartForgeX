---
description: "Check which Mermaid diagram types ChartForgeX renders, what syntax each one supports, and where the output differs from Mermaid.js."
---

# Mermaid Support Matrix

Use this page to check whether ChartForgeX can render your Mermaid diagram. Each family lists the syntax ChartForgeX draws, what it keeps without drawing, and what is not supported yet. ChartForgeX renders Mermaid natively, without a browser, so layouts differ from Mermaid.js even when the content matches.

I test against **Mermaid.js 12.2.0** and also check the same examples against Mermaid 10.9.8 and 11.17.2, so older spellings such as `graph`, `stateDiagram-v2` and `xychart-beta` keep working. That covers the tested examples, not every syntax form in those releases. Mermaid 12.2.0's mindmap branch and depth colours are supported; its other new syntax and layout changes are not covered yet.

## What support means

- **Renders:** the source is parsed into a typed model and drawn as SVG, PNG or HTML.
- **Kept but not drawn:** the source is read and stays available on the parsed document, but it doesn't change the picture yet. You get a `CFXM007` warning.
- **Approximation:** the diagram is drawn with a ChartForgeX chart or topology model and some details differ. C4 layouts and three-circle Venn diagrams whose areas can't be matched exactly report `CFXM009` and still render.
- **Recognised only:** the header is known, but nothing is drawn yet.
- **Unknown:** the header isn't recognised and parsing returns an error.

Diagnostics carry stable codes and point at the exact source location. Invalid notation reports `CFXM008`. Unsupported HTML, math, CSS, remote images and scripts are never executed; they stay literal text or are ignored with a warning.

## Families that render

| Family | Header | What renders | Drawn as | Not supported yet |
| --- | --- | --- | --- | --- |
| Flowchart | `flowchart`, `graph`, `flowchart-elk` | Nodes, edges with circle, cross and two-way endpoints, inline and pipe labels, nested subgraphs with their own direction, multiline and **bold**/*italic* captions, classes, direct styles, `linkStyle`, clicks and link lengths. The `@{ shape: ... }` catalog, caller-supplied icons and images, and edge IDs are drawn. Long edges route around the nodes they skip. `flowchart-elk` uses the standard layout and warns. | `TopologyChart` | HTML and math in captions stay literal. Some compound ports and very dense nested layouts. Unknown shapes or assets warn; source text never downloads artwork. |
| Sequence | `sequenceDiagram` | Participants and all eight participant kinds, boxes, creation and destruction, every arrow type including Mermaid 12 half-arrows, notes, activations, nested blocks and branches including `par_over`, autonumber, links, background rectangles, spacing, wrapping, participant typography and preset appearance. | `SequenceArtifact` | Physical multiline participant configuration. Very dense nested fragments may differ from Mermaid's spacing. |
| Class | `classDiagram` | Classes with generics and members, abstract and static typography, nested namespaces, notes, relationships with multiplicities, lollipop interfaces, links, and `fill`/`stroke`/`color` styles. Comma-separated generics are a ChartForgeX extension and warn. | `TopologyChart` | Callbacks, HTML, math and unsupported CSS. |
| State | `stateDiagram`, `stateDiagram-v2` | States, nested composites with local directions, concurrent regions with dashed separators, attached and floating notes, choices, forks and joins, classes and direct styles. | `TopologyChart` | Unsupported note forms and styles. Dense routing can differ from Mermaid. |
| Entity relationship | `erDiagram` | Entities with aliases, attribute tables with types, keys and comments, identifying relationships, cardinalities, nested groups with their own direction, rich captions, classes and direct styles. | `TopologyChart` | Unsupported CSS. Dense group routing can differ from Mermaid. |
| Requirement | `requirementDiagram` | Requirements of every kind, elements, relationships, rich names and text, risk and verification values, direction, classes, direct styles and literal theme colours. | `TopologyChart` | Code, links, images, HTML, math and unmapped theme variables. |
| Architecture | `architecture-beta` | Groups, services and junctions with optional captions, endpoint sides, row and column alignment, nested parents, ports and the database, server and cloud icons. | `TopologyChart` | The disk and internet icons, external artwork and quoted icon text. Mixed manual and automatic positioning. |
| C4 | `C4Context`, `C4Container`, `C4Component`, `C4Dynamic`, `C4Deployment` | Elements, nested boundaries and deployment nodes, dynamic order, style updates, label offsets, shapes, row limits, legend labels and source typography, palette and spacing. | `TopologyChart` (approximation) | Mermaid's specialised C4 layout and wrapping. External sprites and unsupported tags. |
| Git graph | `gitGraph` | Branches, commits, merges, tags and cherry-picks, main branch name and order, lane and caption visibility, caption rotation and `LR`/`TB`/`BT` directions. | `GitGraphBlock` | Appearance settings beyond the six documented Git graph options. |
| Block | `block-beta` | Items and edges, nested blocks, fixed and automatic columns, spaces and spans, every block shape and arrow, classes, direct styles and caption styles. | `BlockLayoutBlock` | Rich HTML or Markdown captions, unsupported CSS and obstacle-avoiding connectors. |
| Packet | `packet-beta` | Bit fields with explicit and relative ranges, title, `bitsPerRow`, `showBits`, `bitOrder`, `bitWidth`, `rowHeight` and padding. | `PacketLayoutBlock` | Very dense rows can crowd their labels. |
| Venn | `venn`, `venn-beta` | One to three sets with overlaps, text, colours, opacity and outline width. Circle areas follow the values. | `VennDiagramBlock` | Three sets can't always be matched exactly with circles; the result warns when the error exceeds 0.5%. |
| Ishikawa | `ishikawa`, `ishikawa-beta` | The effect and its cause tree, padding, sizing and theme colours. Large trees grow the canvas instead of shrinking text. | `FishboneDiagramBlock` | Node classes and styles, CSS and HTML. |
| Wardley | `wardley-beta` | Stages and boundaries, anchors, components, pipelines, links, evolve trends, notes, numbered annotations, label offsets, sourcing and inertia marks. | `WardleyMapBlock` | Raw CSS, assets and custom decorators. |
| Mindmap | `mindmap` | The hierarchy with **bold**/*italic* and multiline captions, every node shape, classes, built-in icons and branch and depth colours. | `TopologyChart` | Code, links, images, HTML, math, external CSS and external icon packs. |
| Tree view | `treeView-beta`, `treeView` | Indented and box-drawing trees, labels, descriptions, directory markers, classes, icons and `showIcons`. | `TreeViewBlock` | External classes, icon packs and file-extension icon maps. |
| Event modeling | `eventmodeling` | Frames and resets, sources, namespaces, JSON and schema facts and data references, laid out in swimlanes. `ToDataTable` gives a table view. | `TopologyChart` | Additional entity, note and given-when-then forms; theme variables. |
| Kanban | `kanban` | Columns and tasks with assignee, ticket, priority, label, ticket links, padding and section width. | `TopologyChart` | Arbitrary CSS and external icons. A column can grow wider than requested to fit its cards. |
| Pie | `pie` | Title, `showData` and slices. | `Chart` pie | Source styling and label placement options. |
| Journey | `journey` | Sections, tasks, one-to-five scores and actors. | `JourneyDiagramBlock` | Very dense journeys can crowd the fixed frame. |
| Timeline | `timeline` | Sections, periods, multiline events and direction. Date-like labels stay text. | `TimelineDiagramBlock` | Custom theming. Long captions are shortened, with the full text kept in the SVG title. |
| Quadrant | `quadrantChart` | Title, axis labels, quadrant labels, points, point and class styles, dimensions and theme colours. Render options add borders, caption anchors and full axes. | `Chart` scatter with quadrants | Some compact layouts and remaining settings. |
| Gantt | `gantt` | Title, sections, date formats, tasks, tags, `after` and `until` references, milestones, durations, excludes, includes and weekends, dependency connectors, axis formats, tick intervals, vertical markers and today marker. | `Chart` Gantt | Unsupported CSS. |
| XY chart | `xychart`, `xychart-beta` | Axes, category and numeric ranges, bar and line series, data labels, orientation and source configuration. | `Chart` | Per-axis typography and tick styling; HTML and math. |
| Sankey | `sankey`, `sankey-beta` | CSV links, title, value labels, source, target, gradient and literal link colours, node colours and label styles. | `Chart` Sankey | Some CSV edge cases. |
| Radar | `radar-beta` | Axes, curves, values, scale bounds and ticks, circle and polygon grids, curve tension, dimensions, margins and source colours and sizes. | `Chart` radar | Some remaining source settings. |
| Treemap | `treemap-beta` | Hierarchy, values, classes, value visibility and number formats. | `Chart` treemap | Arbitrary D3 formats and unsupported CSS. |
| Swimlane | `swimlane-beta` | Lanes, nested responsibilities, spacing, padding and fit, lane ordering and line hops at crossings. | `TopologyChart` | Some styles and very dense routes. |
| Use case | `usecase-beta` | Actors, use cases, boundaries, include, extend and generalisation, notes, stereotypes, actor styles, tables and named associations. | `TopologyChart` | Nested system boundaries. |
| Cynefin | `cynefin-beta` | Domains, items, title, transitions, the fold and cliff shapes and confusion region, fonts and text colours. | `TopologyChart` | Mermaid's badge and arrow layout. |
| Agentflow | `agentflow-beta` | Nodes, nested flows, collapsed containers, sequence, reference and failure edges and the task, tool, input, decision, document and action shapes. Metadata stays data; nothing in it runs. | `TopologyChart` | Unsupported shapes, layouts, animation and rich labels. |
| ZenUML | `zenuml` | Participants, aliases and groups, calls, assignments, async and reply messages, returns, creation, control fragments, references, dividers, comments, stereotypes and colours. | `SequenceArtifact` | Unknown symbol shortcodes. Expressions are drawn, never evaluated. |
| Railroad | `railroad-beta`, `railroad-ebnf-beta`, `railroad-abnf-beta`, `railroad-peg-beta` | Rules for IR, EBNF, ABNF and PEG grammars: references, choices, sequences, optional and repeated paths, predicates, exceptions and character ranges. | `RailroadDiagramBlock` | Documented subsets of each grammar, not complete standards. |

Agentflow instructions, models and connector data never run. ZenUML has its own grammar and draws through the native sequence renderer; no Mermaid JavaScript plugin is shipped.

## Across every family

| Area | What works | Not supported yet |
| --- | --- | --- |
| Titles | An explicit title wins, then the diagram's `title` statement, then the front-matter title. Subtitles are opt-in. | Some additional title syntax. |
| Versions | Examples are checked against Mermaid 10.9.8, 11.17.2 and 12.2.0, including older Gantt calendars and Mermaid 10's ER directions. | Complete compatibility with every release. |
| Configuration and themes | Front matter and `%%{init}%%` directives, with directives winning. `dark` and `default` themes, font stacks, `themeVariables.fontFamily` and `fontSize` apply to charts, topology, sequence and blocks; Radar also maps its colours. | Broader YAML, more theme variables and family options. |
| Mermaid 12 appearance | ChartForgeX draws its own layout. It doesn't run ELK or copy Mermaid's default browser look. | Mapping `layout`, `look` and `defaultRenderer`. |
| Labels and styles | Flowchart, Class, State, ER and Requirement share `fill`, `stroke` and `color` styles and classes, and draw **bold**, *italic* and line breaks. Repeated `linkStyle` replaces the earlier one, as in Mermaid. | Rich text in the other families. HTML, math and unsupported CSS stay literal or are ignored with a warning. |
| Layout | Shared topology layout draws markers, groups and routing for the graph families. | Some ports, loops, parallel edges and dense label routing. |
| Safety | Static output contains no script. `MermaidRenderOptions` sets presentation defaults, warning handling, size and depth limits, and cancellation for direct and Markdown-fence rendering. | Cancellation can't stop a layout that is already running. |
| Outputs | Every rendering family produces SVG, PNG, Markdown fences and portable JSON, and works with Native AOT on every supported framework. | |

## Shared Owners

Native hierarchy options, authored identities, chart orientation and relationship styling use the shared chart model. Series replace complete hierarchy or flow data through `ReplaceHierarchyItems` and `ReplaceFlowData`; the [API conventions](v2/api-conventions.md) document configuration and identity rules.

## Upstream References

Use the [Mermaid syntax catalog](https://mermaid.js.org/intro/) and [versioned documentation source](https://github.com/mermaid-js/mermaid/tree/mermaid%4012.1.0/packages/mermaid/src/docs/syntax) together; the navigation list alone omits some registered families.

- [Mermaid 12.0.0 changes](https://github.com/mermaid-js/mermaid/releases/tag/mermaid%4012.0.0) and [12.1.0 changes](https://github.com/mermaid-js/mermaid/releases/tag/mermaid%4012.1.0): compatibility changes, new families and regressions.
- [Flowchart grammar and shapes](https://mermaid.js.org/syntax/flowchart.html), [sequence](https://mermaid.js.org/syntax/sequenceDiagram.html), [class](https://mermaid.js.org/syntax/classDiagram.html), [state](https://mermaid.js.org/syntax/stateDiagram.html), [ER](https://mermaid.js.org/syntax/entityRelationshipDiagram.html).
- [Gantt](https://mermaid.js.org/syntax/gantt.html), [radar](https://mermaid.js.org/syntax/radar.html), [treemap](https://mermaid.js.org/syntax/treemap.html), [packet](https://mermaid.js.org/syntax/packet.html).
- [Configuration](https://mermaid.js.org/config/configuration.html), [themes](https://mermaid.js.org/config/theming.html), [directives](https://mermaid.js.org/config/directives.html), [math](https://mermaid.js.org/config/math.html).
