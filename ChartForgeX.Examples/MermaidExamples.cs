using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;

internal static partial class MermaidExamples {
    internal static void Write(string output) {
        ExampleTypography.Register();
        WriteGitGraphDepthExamples(output);
        WriteNativeBlocks(output);
        WriteKanbanSourceGeometryPrograms(output);
        WriteIshikawaDepthExamples(output);
        WriteWardleyDepthExamples(output);
        WriteMixedCaptionExamples(output);
        WriteZenUmlPresentationExamples(output);
        WriteFlowchartCurveExamples(output);
        WriteFlowchartAssetExamples(output);
        WriteTopologyPaletteExamples(output);
        WriteSequenceLayoutTypographyExamples(output);
        WriteSequenceParticipantViewsExamples(output);
        WriteSequenceMessagePresentationExamples(output);
        WriteSequenceSelfRoutesExamples(output);
        WriteSequenceNavigationExamples(output);
        WriteSequenceSourceDefaultsExamples(output);
        WriteSequencePresetAppearanceExamples(output);
        WriteRequirementThemeExamples(output);
        WriteSequenceSurfaceShadowExamples(output);
        WriteXYSourceDepthExamples(output);
        WriteUseCaseSourceDepthExamples(output);
        WriteAdvancedActorExamples(output);
        WriteNativeShapeCatalogExamples(output);
        WriteGraphStyleSettingsExamples(output);
        WriteNativeSurfacePaintExamples(output);
        WriteUseCaseJsonExamples(output);
        const string mindMap = "mindmap\n    root((Roadmap))\n          plan[Planning]\n            tasks[Tasks]\n                review[Review]\n          delivery[Delivery]\n              releases[Releases]\n                support[Support]\n";
        var agentflow = MermaidFixtureSources.Read("agentflow-native-workflow");
        var zenuml = MermaidFixtureSources.Read("zenuml-native-occurrences");
        var stateStyles = MermaidFixtureSources.Read("state-native-safe-styles");
        var stateRegions = MermaidFixtureSources.Read("state-concurrent-regions");
        var quadrantSource = MermaidFixtureSources.Read("quadrant-native-source-depth");
        var quadrantDark = quadrantSource.Replace("theme: default", "theme: dark", StringComparison.Ordinal)
            .Replace("quadrantXAxisTextFill: \"#475569\"", "quadrantXAxisTextFill: \"#CBD5E1\"", StringComparison.Ordinal)
            .Replace("quadrantTitleFill: \"#0F172A\"", "quadrantTitleFill: \"#F1F5F9\"", StringComparison.Ordinal);
        var examples = new Dictionary<string, string> {
            ["mermaid-block-depth-compact-light"] = MermaidFixtureSources.Read("block-native-depth"),
            ["mermaid-block-depth-compact-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidFixtureSources.Read("block-native-depth"),
            ["mermaid-block-depth-wide-light"] = MermaidFixtureSources.Read("block-native-depth"),
            ["mermaid-block-depth-wide-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidFixtureSources.Read("block-native-depth"),
            ["mermaid-kanban-source-compact-light"] = MermaidFixtureSources.Read("kanban-source-geometry"),
            ["mermaid-kanban-source-compact-dark"] = MermaidFixtureSources.Read("kanban-source-geometry").Replace("\r\n", "\n", StringComparison.Ordinal).Replace("config:\n", "config:\n  theme: dark\n", StringComparison.Ordinal),
            ["mermaid-kanban-source-wide-light"] = MermaidFixtureSources.Read("kanban-source-geometry"),
            ["mermaid-kanban-source-wide-dark"] = MermaidFixtureSources.Read("kanban-source-geometry").Replace("\r\n", "\n", StringComparison.Ordinal).Replace("config:\n", "config:\n  theme: dark\n", StringComparison.Ordinal),
            ["mermaid-ishikawa-depth-compact-light"] = MermaidIshikawaDepthExamples.Source(false),
            ["mermaid-ishikawa-depth-compact-dark"] = MermaidIshikawaDepthExamples.Source(true),
            ["mermaid-ishikawa-depth-wide-light"] = MermaidIshikawaDepthExamples.Source(false),
            ["mermaid-ishikawa-depth-wide-dark"] = MermaidIshikawaDepthExamples.Source(true),
            ["mermaid-c4-config-compact-light"] = MermaidFixtureSources.Read("c4-config-typography-native"),
            ["mermaid-c4-config-compact-dark"] = MermaidFixtureSources.Read("c4-config-typography-native").Replace("config:\n", "config:\n  theme: dark\n", StringComparison.Ordinal),
            ["mermaid-c4-config-wide-light"] = MermaidFixtureSources.Read("c4-config-typography-native"),
            ["mermaid-c4-config-wide-dark"] = MermaidFixtureSources.Read("c4-config-typography-native").Replace("config:\n", "config:\n  theme: dark\n", StringComparison.Ordinal),
            ["mermaid-c4-source-shapes-compact-light"] = MermaidFixtureSources.Read("c4-style-shapes-native"),
            ["mermaid-c4-source-shapes-compact-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidFixtureSources.Read("c4-style-shapes-native"),
            ["mermaid-c4-source-shapes-wide-light"] = MermaidFixtureSources.Read("c4-style-shapes-native"),
            ["mermaid-c4-source-shapes-wide-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidFixtureSources.Read("c4-style-shapes-native"),
            ["mermaid-c4-source-rows-compact-light"] = MermaidFixtureSources.Read("c4-layout-config-native"),
            ["mermaid-c4-source-rows-compact-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidFixtureSources.Read("c4-layout-config-native"),
            ["mermaid-c4-source-rows-wide-light"] = MermaidFixtureSources.Read("c4-layout-config-native"),
            ["mermaid-c4-source-rows-wide-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidFixtureSources.Read("c4-layout-config-native"),
            ["mermaid-quadrant-source-compact-light"] = quadrantSource,
            ["mermaid-quadrant-source-compact-dark"] = quadrantDark,
            ["mermaid-quadrant-source-wide-light"] = quadrantSource,
            ["mermaid-quadrant-source-wide-dark"] = quadrantDark,
            ["mermaid-gitgraph-depth-lr-compact-light"] = MermaidFixtureSources.Read("gitgraph-native-depth"),
            ["mermaid-gitgraph-depth-lr-compact-dark"] = MermaidFixtureSources.Read("gitgraph-native-depth").Replace("config:\n", "config:\n  theme: dark\n", StringComparison.Ordinal),
            ["mermaid-gitgraph-depth-lr-wide-light"] = MermaidFixtureSources.Read("gitgraph-native-depth"),
            ["mermaid-gitgraph-depth-lr-wide-dark"] = MermaidFixtureSources.Read("gitgraph-native-depth").Replace("config:\n", "config:\n  theme: dark\n", StringComparison.Ordinal),
            ["mermaid-gitgraph-depth-tb-compact-light"] = MermaidFixtureSources.Read("gitgraph-native-depth").Replace("gitGraph LR", "gitGraph TB", StringComparison.Ordinal),
            ["mermaid-gitgraph-depth-tb-compact-dark"] = MermaidFixtureSources.Read("gitgraph-native-depth").Replace("gitGraph LR", "gitGraph TB", StringComparison.Ordinal).Replace("config:\n", "config:\n  theme: dark\n", StringComparison.Ordinal),
            ["mermaid-gitgraph-depth-tb-wide-light"] = MermaidFixtureSources.Read("gitgraph-native-depth").Replace("gitGraph LR", "gitGraph TB", StringComparison.Ordinal),
            ["mermaid-gitgraph-depth-tb-wide-dark"] = MermaidFixtureSources.Read("gitgraph-native-depth").Replace("gitGraph LR", "gitGraph TB", StringComparison.Ordinal).Replace("config:\n", "config:\n  theme: dark\n", StringComparison.Ordinal),
            ["mermaid-gitgraph-depth-bt-compact-light"] = MermaidFixtureSources.Read("gitgraph-native-depth").Replace("gitGraph LR", "gitGraph BT", StringComparison.Ordinal),
            ["mermaid-gitgraph-depth-bt-compact-dark"] = MermaidFixtureSources.Read("gitgraph-native-depth").Replace("gitGraph LR", "gitGraph BT", StringComparison.Ordinal).Replace("config:\n", "config:\n  theme: dark\n", StringComparison.Ordinal),
            ["mermaid-gitgraph-depth-bt-wide-light"] = MermaidFixtureSources.Read("gitgraph-native-depth").Replace("gitGraph LR", "gitGraph BT", StringComparison.Ordinal),
            ["mermaid-gitgraph-depth-bt-wide-dark"] = MermaidFixtureSources.Read("gitgraph-native-depth").Replace("gitGraph LR", "gitGraph BT", StringComparison.Ordinal).Replace("config:\n", "config:\n  theme: dark\n", StringComparison.Ordinal),
            ["mermaid-treemap-styles-compact-light"] = MermaidFixtureSources.Read("treemap-native-styles"),
            ["mermaid-treemap-styles-compact-dark"] = MermaidFixtureSources.Read("treemap-native-styles").Replace("config:\n", "config:\n  theme: dark\n", StringComparison.Ordinal),
            ["mermaid-treemap-styles-wide-light"] = MermaidFixtureSources.Read("treemap-native-styles"),
            ["mermaid-treemap-styles-wide-dark"] = MermaidFixtureSources.Read("treemap-native-styles").Replace("config:\n", "config:\n  theme: dark\n", StringComparison.Ordinal),
            ["mermaid-eventmodeling-depth-compact-light"] = MermaidFixtureSources.Read("eventmodeling-native-depth"),
            ["mermaid-eventmodeling-depth-compact-dark"] = MermaidFixtureSources.Read("eventmodeling-native-depth").Replace("title: Cart information flow\n", "title: Cart information flow\nconfig:\n  theme: dark\n", StringComparison.Ordinal),
            ["mermaid-eventmodeling-depth-wide-light"] = MermaidFixtureSources.Read("eventmodeling-native-depth"),
            ["mermaid-eventmodeling-depth-wide-dark"] = MermaidFixtureSources.Read("eventmodeling-native-depth").Replace("title: Cart information flow\n", "title: Cart information flow\nconfig:\n  theme: dark\n", StringComparison.Ordinal),
            ["mermaid-wardley-overlays-compact-light"] = MermaidFixtureSources.Read("wardley-native-overlays"),
            ["mermaid-wardley-overlays-compact-dark"] = MermaidWardleyDepthExamples.Source(true),
            ["mermaid-wardley-overlays-wide-light"] = MermaidFixtureSources.Read("wardley-native-overlays"),
            ["mermaid-wardley-overlays-wide-dark"] = MermaidWardleyDepthExamples.Source(true),
            ["mermaid-kanban-board-compact-light"] = MermaidFixtureSources.Read("kanban-native-board"),
            ["mermaid-kanban-board-compact-dark"] = MermaidFixtureSources.Read("kanban-native-board").Replace("config:\n", "config:\n  theme: dark\n", StringComparison.Ordinal),
            ["mermaid-kanban-board-wide-light"] = MermaidFixtureSources.Read("kanban-native-board"),
            ["mermaid-kanban-board-wide-dark"] = MermaidFixtureSources.Read("kanban-native-board").Replace("config:\n", "config:\n  theme: dark\n", StringComparison.Ordinal),
            ["mermaid-class-captions-compact-light"] = MermaidFixtureSources.Read("class-native-captions"),
            ["mermaid-class-captions-compact-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidFixtureSources.Read("class-native-captions"),
            ["mermaid-class-captions-wide-light"] = MermaidFixtureSources.Read("class-native-captions"),
            ["mermaid-class-captions-wide-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidFixtureSources.Read("class-native-captions"),
            ["mermaid-er-captions-compact-light"] = MermaidFixtureSources.Read("er-native-captions"),
            ["mermaid-er-captions-compact-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidFixtureSources.Read("er-native-captions"),
            ["mermaid-er-captions-wide-light"] = MermaidFixtureSources.Read("er-native-captions"),
            ["mermaid-er-captions-wide-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidFixtureSources.Read("er-native-captions"),
            ["mermaid-treeview-rows-compact-light"] = MermaidTreeViewExamples.Source,
            ["mermaid-treeview-rows-compact-dark"] = MermaidTreeViewExamples.Source.Replace("config:\n", "config:\n  theme: dark\n", StringComparison.Ordinal),
            ["mermaid-treeview-rows-wide-light"] = MermaidTreeViewExamples.Source,
            ["mermaid-treeview-rows-wide-dark"] = MermaidTreeViewExamples.Source.Replace("config:\n", "config:\n  theme: dark\n", StringComparison.Ordinal),
            ["mermaid-agentflow-native-light"] = agentflow,
            ["mermaid-agentflow-native-dark"] = "%%{init: {'theme':'dark'}}%%\n" + agentflow,
            ["mermaid-zenuml-native-light"] = zenuml,
            ["mermaid-zenuml-native-dark"] = "%%{init: {'theme':'dark'}}%%\n" + zenuml,
            ["mermaid-state-styles-light"] = stateStyles,
            ["mermaid-state-styles-dark"] = "%%{init: {'theme':'dark'}}%%\n" + stateStyles,
            ["mermaid-state-regions-light"] = stateRegions,
            ["mermaid-state-regions-dark"] = "%%{init: {'theme':'dark'}}%%\n" + stateRegions,
            ["mermaid-state-floating-compact-light"] = MermaidFixtureSources.Read("state-floating-notes"),
            ["mermaid-state-floating-compact-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidFixtureSources.Read("state-floating-notes"),
            ["mermaid-state-floating-wide-light"] = MermaidFixtureSources.Read("state-floating-notes"),
            ["mermaid-state-floating-wide-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidFixtureSources.Read("state-floating-notes"),
            ["mermaid-railroad-ebnf-light"] = MermaidRailroadExamples.Ebnf,
            ["mermaid-railroad-ebnf-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidRailroadExamples.Ebnf,
            ["mermaid-railroad-abnf-light"] = MermaidRailroadExamples.Abnf,
            ["mermaid-railroad-abnf-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidRailroadExamples.Abnf,
            ["mermaid-railroad-peg-light"] = MermaidRailroadExamples.Peg,
            ["mermaid-railroad-peg-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidRailroadExamples.Peg,
            ["mermaid-railroad-ir-light"] = MermaidRailroadExamples.IntermediateRepresentation,
            ["mermaid-railroad-ir-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidRailroadExamples.IntermediateRepresentation,
            ["mermaid-native-journey-light"] = MermaidMappedFamilyExamples.Journey,
            ["mermaid-native-journey-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidMappedFamilyExamples.Journey,
            ["mermaid-native-timeline-light"] = MermaidMappedFamilyExamples.Timeline,
            ["mermaid-native-timeline-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidMappedFamilyExamples.Timeline,
            ["mermaid-native-c4-light"] = MermaidMappedFamilyExamples.C4,
            ["mermaid-native-c4-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidMappedFamilyExamples.C4,
            ["mermaid-native-venn-light"] = MermaidMappedFamilyExamples.Venn,
            ["mermaid-native-venn-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidMappedFamilyExamples.Venn,
            ["mermaid-architecture-alignments-compact-light"] = MermaidFixtureSources.Read("architecture-ordered-alignments"),
            ["mermaid-architecture-alignments-compact-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidFixtureSources.Read("architecture-ordered-alignments"),
            ["mermaid-architecture-alignments-wide-light"] = MermaidFixtureSources.Read("architecture-ordered-alignments"),
            ["mermaid-architecture-alignments-wide-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidFixtureSources.Read("architecture-ordered-alignments"),
            ["mermaid-architecture-captions-compact-light"] = MermaidFixtureSources.Read("architecture-optional-captions"),
            ["mermaid-architecture-captions-compact-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidFixtureSources.Read("architecture-optional-captions"),
            ["mermaid-architecture-captions-wide-light"] = MermaidFixtureSources.Read("architecture-optional-captions"),
            ["mermaid-architecture-captions-wide-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidFixtureSources.Read("architecture-optional-captions"),
            ["mermaid-architecture-boundaries-compact-light"] = MermaidFixtureSources.Read("architecture-native-boundaries"),
            ["mermaid-architecture-boundaries-compact-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidFixtureSources.Read("architecture-native-boundaries"),
            ["mermaid-architecture-boundaries-wide-light"] = MermaidFixtureSources.Read("architecture-native-boundaries"),
            ["mermaid-architecture-boundaries-wide-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidFixtureSources.Read("architecture-native-boundaries"),
            ["mermaid-sequence-participants-compact-light"] = MermaidFixtureSources.Read("sequence-participant-contracts"),
            ["mermaid-sequence-participants-compact-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidFixtureSources.Read("sequence-participant-contracts"),
            ["mermaid-sequence-participants-wide-light"] = MermaidFixtureSources.Read("sequence-participant-contracts"),
            ["mermaid-sequence-participants-wide-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidFixtureSources.Read("sequence-participant-contracts"),
            ["mermaid-sequence-par-over-compact-light"] = MermaidFixtureSources.Read("sequence-par-over"),
            ["mermaid-sequence-par-over-compact-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidFixtureSources.Read("sequence-par-over"),
            ["mermaid-sequence-par-over-wide-light"] = MermaidFixtureSources.Read("sequence-par-over"),
            ["mermaid-sequence-par-over-wide-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidFixtureSources.Read("sequence-par-over"),
            ["mermaid-sequence-rect-numbering-compact-light"] = MermaidFixtureSources.Read("sequence-rect-numbering"),
            ["mermaid-sequence-rect-numbering-compact-dark"] = MermaidFixtureSources.Read("sequence-rect-numbering").Replace("theme: default", "theme: dark", StringComparison.Ordinal),
            ["mermaid-sequence-rect-numbering-wide-light"] = MermaidFixtureSources.Read("sequence-rect-numbering"),
            ["mermaid-sequence-rect-numbering-wide-dark"] = MermaidFixtureSources.Read("sequence-rect-numbering").Replace("theme: default", "theme: dark", StringComparison.Ordinal),
            ["mermaid-sequence-depth-compact-light"] = MermaidFixtureSources.Read("sequence-native-depth"),
            ["mermaid-sequence-depth-compact-dark"] = MermaidFixtureSources.Read("sequence-native-depth").Replace("theme: default", "theme: dark", StringComparison.Ordinal).Replace("signalColor: \"#276749\"", "signalColor: \"#74c69d\"", StringComparison.Ordinal).Replace("signalTextColor: \"#194d35\"", "signalTextColor: \"#b7e4c7\"", StringComparison.Ordinal),
            ["mermaid-sequence-depth-wide-light"] = MermaidFixtureSources.Read("sequence-native-depth"),
            ["mermaid-sequence-depth-wide-dark"] = MermaidFixtureSources.Read("sequence-native-depth").Replace("theme: default", "theme: dark", StringComparison.Ordinal).Replace("signalColor: \"#276749\"", "signalColor: \"#74c69d\"", StringComparison.Ordinal).Replace("signalTextColor: \"#194d35\"", "signalTextColor: \"#b7e4c7\"", StringComparison.Ordinal),
            ["mermaid-sequence-half-arrows-compact-light"] = MermaidFixtureSources.Read("sequence-half-arrows"),
            ["mermaid-sequence-half-arrows-compact-dark"] = MermaidFixtureSources.Read("sequence-half-arrows").Insert(0, "%%{init: {'theme':'dark'}}%%\n"),
            ["mermaid-sequence-half-arrows-wide-light"] = MermaidFixtureSources.Read("sequence-half-arrows"),
            ["mermaid-sequence-half-arrows-wide-dark"] = MermaidFixtureSources.Read("sequence-half-arrows").Insert(0, "%%{init: {'theme':'dark'}}%%\n"),
            ["mermaid-sequence-lifetimes-light"] = MermaidSequenceExamples.Lifecycle,
            ["mermaid-sequence-lifetimes-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidSequenceExamples.Lifecycle,
            ["mermaid-flowchart-nested-directions-light"] = MermaidCompoundExamples.Flowchart,
            ["mermaid-flowchart-nested-directions-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidCompoundExamples.Flowchart,
            ["mermaid-class-native-notes-light"] = MermaidNotationExamples.Class,
            ["mermaid-class-native-notes-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidNotationExamples.Class,
            ["mermaid-state-synchronization-light"] = MermaidNotationExamples.State,
            ["mermaid-state-synchronization-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidNotationExamples.State,
            ["mermaid-er-typed-attributes-light"] = MermaidNotationExamples.EntityRelationship,
            ["mermaid-er-typed-attributes-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidNotationExamples.EntityRelationship,
            ["mermaid-theme-timeline-dark"] = "%%{init: {'theme':'dark'}}%%\ntimeline\ntitle Rendering history\n2024 : Fluent chart API : SVG and PNG output\n2025 : Topology diagrams\n",
            ["mermaid-theme-packet-dark"] = "---\ntitle: Packet fields\nconfig:\n  theme: dark\n---\npacket-beta\n0-7: \"Header\"\n8-15: \"Payload\"\n",
            ["mermaid-radar-theme"] = "---\nconfig:\n  themeVariables:\n    titleColor: '#7C3AED'\n    cScale0: '#2563EB'\n    cScale1: '#EA580C'\n    radar:\n      axisColor: '#16A34A'\n      axisStrokeWidth: 3\n      axisLabelFontSize: 15\n      graticuleColor: '#64748B'\n      graticuleOpacity: 0.12\n      graticuleStrokeWidth: 1.5\n      curveOpacity: 0.25\n      curveStrokeWidth: 4\n      legendFontSize: 16\n---\nradar-beta\ntitle Authored radar paint\naxis a[\"Coverage\"], b[\"Policy\"], c[\"Response\"], d[\"Recovery\"]\ncurve current[\"Current\"]{90,45,80,60}\ncurve target[\"Target\"]{95,85,90,80}\nmin 0\nmax 100\ngraticule circle\n",
            ["mermaid-mindmap-notation-compact-light"] = MermaidFixtureSources.Read("mindmap-native-notation"),
            ["mermaid-mindmap-notation-compact-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidFixtureSources.Read("mindmap-native-notation"),
            ["mermaid-mindmap-notation-wide-light"] = MermaidFixtureSources.Read("mindmap-native-notation"),
            ["mermaid-mindmap-notation-wide-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidFixtureSources.Read("mindmap-native-notation"),
            ["mermaid-mindmap-branch-light"] = "---\nconfig:\n  mindmap:\n    colorBy: branch\n---\n" + mindMap,
            ["mermaid-mindmap-depth-light"] = "---\nconfig:\n  mindmap:\n    colorBy: depth\n---\n" + mindMap,
            ["mermaid-mindmap-branch-dark"] = "---\nconfig:\n  theme: dark\n  mindmap:\n    colorBy: branch\n---\n" + mindMap,
            ["mermaid-mindmap-depth-dark"] = "---\nconfig:\n  theme: dark\n  mindmap:\n    colorBy: depth\n---\n" + mindMap,
            ["mermaid-flowchart-shape-aliases"] = "flowchart LR\nA@{ shape: fr-rect, label: \"Framed process\" } --> B@{ shape: framed-rectangle, label: \"Subprocess\" }\nC@{ shape: question, label: \"Decision\" } --> D@{ shape: odd, label: \"Asymmetric\" }\nE@{ shape: lean-right, label: \"Input\" } --> F@{ shape: lean-left, label: \"Output\" }\nG@{ shape: manual, label: \"Manual task\" } --> H@{ shape: trapezoid-top, label: \"Operation\" }\nI@{ shape: priority, label: \"Priority\" } --> J@{ shape: trapezoid-bottom, label: \"Action\" }\n",
            ["mermaid-flowchart-class-styles"] = "flowchart LR\nclassDef default fill:#dbeafe,stroke:#1e3a8a,color:#172554\nclassDef color,box fill:#fed7aa,color:#7c2d12\nclassDef color stroke:#c2410c\nclassDef color fill:#bbf7d0\nstart[Default] --> work[Colored]:::color-->box[Box]:::box\nwork --> finish[External]:::external-xml\nclassDef external-xml color:#14532d\nstyle work fill:#fef3c7\nstyle work fill:#fef9c3\nclass work box\nlinkStyle 0 stroke:#dc2626\nlinkStyle default stroke:#475569\nlinkStyle 1 stroke:#16a34a\nclassDef read-only stroke:#0f766e\nfinish:::read-only\n",
            ["mermaid-gitgraph-cherry-pick-parent"] = "gitGraph\n  commit id: \"base\"\n  branch work\n  commit id: \"work\"\n  checkout main\n  merge work id: \"merged\" type: HIGHLIGHT\n  branch consumer\n  commit id: \"consumer\"\n  cherry-pick id: \"merged\" parent: \"base\"\n",
            ["mermaid-flowchart-multiline-markdown"] = "flowchart LR\nA[\"`**Review** the\n*request*`\"] -->|\"`Send **now**`\"| B[\"Ready<br>to ship\"]\nC[\"First\nsecond\"] --> D[\"Done\"]\n",
            ["mermaid-flowchart-compound-bypass"] = "flowchart LR\nsubgraph a[Delivery]\none[One] --> middle[Middle] --> last[Last]\none -->|Bypass| last\nend\nsubgraph b[Other lane]\nother[Other]\nend\n",
            ["mermaid-flowchart-shape-captions"] = "flowchart LR\nrequest[Request] --> approved{Approved?} --> done((Done))\nstore[(Audit log)] --> complete(((Complete))) --> manual[/Manual<br/>review/]\n",
            ["mermaid-flowchart-lengths"] = "flowchart LR\nA[Receive] ----> B[Validate]\nB -. accepted ..-> C[Store]\n",
            ["mermaid-quadrant-regions"] = "quadrantChart\ntitle Work priorities\nx-axis Low effort --> High effort\ny-axis Low impact --> High impact\nquadrant-1 Plan\nquadrant-2 Prioritize\nquadrant-3 Consider\nquadrant-4 Reassess\nOpportunity: [0.2, 0.8]\nInvestment: [0.8, 0.8]\nRoutine: [0.2, 0.2]\nReview: [0.8, 0.2]\n",
            ["mermaid-xychart-horizontal-bar"] = "xychart-beta horizontal\ntitle \"Monthly count\"\nx-axis Month [Jan, Feb, Mar]\ny-axis Count 0 --> 100\nbar [20, 50, 80]\n",
            ["mermaid-xychart-horizontal-line"] = "xychart-beta horizontal\ntitle \"Monthly count\"\nx-axis Month [Jan, Feb, Mar]\ny-axis Count 0 --> 100\nline [30, 60, 90]\n",
            ["mermaid-xychart-horizontal-mixed"] = "xychart-beta horizontal\ntitle \"Monthly count\"\nx-axis Month [Jan, Feb, Mar]\ny-axis Count 0 --> 100\nbar [20, 50, 80]\nline [30, 60, 90]\n",
            ["mermaid-packet-geometry"] = "---\nconfig:\n  packet:\n    bitsPerRow: 16\n    bitWidth: 24\n    rowHeight: 40\n    paddingX: 3\n    paddingY: 7\n---\npacket-beta\n0-3: \"Flags\"\n+14: \"Payload\"\n+6: \"Tail\"\n",
            ["mermaid-packet-preferred-row-gap"] = "---\nconfig:\n  packet:\n    bitsPerRow: 8\n    rowHeight: 20\n    paddingY: 100\n    showBits: false\n---\npacket-beta\n0-7: \"Header\"\n8-15: \"Payload\"\n",
            ["mermaid-packet-descending"] = "---\nconfig:\n  packet:\n    bitsPerRow: 16\n    bitOrder: descending\n---\npacket-beta\n0-3: \"Flags\"\n+14: \"Payload\"\n+1: \"Ready\"\n+5: \"Tail\"\n",
            ["mermaid-packet-source-options"] = "---\nconfig:\n  packet:\n    bitsPerRow: 16\n    showBits: false\n---\npacket-beta\n0-3: \"Flags\"\n+14: \"Payload\"\n+1: \"Ready\"\n+5: \"Tail\"\n",
            ["mermaid-source-font-size-wardley-boundaries"] = "%%{init: {'themeVariables':{'fontSize':256}}}%%\nwardley-beta\ncomponent API [0.5, 0.5]\nannotation 1, [1, 1] \"\"\nannotation 2, [1, 0] \"\"\nannotation 3, [0, 1] \"\"\nannotation 4, [0, 0] \"\"\n",
            ["mermaid-source-font-size-icon-fit"] = "%%{init: {'fontFamily':'Arial','themeVariables':{'fontSize':32}}}%%\nflowchart LR\napi[Payments API]\n",
            ["mermaid-source-font-size-icon-maximum-fit"] = "%%{init: {'fontFamily':'Arial','themeVariables':{'fontSize':256}}}%%\nflowchart LR\napi[Payments API]\n",
            ["mermaid-source-font-size-c4-subtitle-fit"] = "%%{init: {'fontFamily':'Arial','themeVariables':{'fontSize':32}}}%%\nC4Container\nContainer(api, \"API\", \"HTTP\", \"Service\")\n",
            ["mermaid-source-font-size-requirement-subtitle-fit"] = "%%{init: {'fontFamily':'Arial','themeVariables':{'fontSize':256}}}%%\nrequirementDiagram\nrequirement API {\n text: HTTP\n}\n",
            ["mermaid-source-font-size-group-header-fit"] = "%%{init: {'fontFamily':'Arial','themeVariables':{'fontSize':256}}}%%\nflowchart LR\nsubgraph Service[Service group]\n A --> B\nend\n",
            ["mermaid-source-font-size-packet-narrow-fit"] = "%%{init: {'fontFamily':'Arial','themeVariables':{'fontSize':256}}}%%\npacket-beta\n0-1: \"ACK\"\n2-31: \"Payload\"\n",
            ["mermaid-source-font-size-axis-rotated-fit"] = "%%{init: {'fontFamily':'Arial','themeVariables':{'fontSize':32}}}%%\nxychart-beta\nx-axis Month [September,October,November]\ny-axis Count 0 --> 100\nline [20,50,80]\n",
            ["mermaid-source-font-size-actor-compact-fit"] = "%%{init: {'fontFamily':'Arial','themeVariables':{'fontSize':32}}}%%\nusecase-beta\ndirection LR\nactor API\nactor UI\nAPI --> UI\n",
            ["mermaid-source-font-size-sankey-fit"] = "%%{init: {'fontFamily':'Arial','themeVariables':{'fontSize':256}}}%%\nsankey-beta\nSource,Target,10\n",
            ["mermaid-source-font-size-treemap-fit"] = "%%{init: {'fontFamily':'Arial','themeVariables':{'fontSize':256}}}%%\ntreemap-beta\n\"Storage\"\n  \"Documents\": 5\n  \"Media\": 5\n",
            ["mermaid-source-font-size-axis-fit"] = "%%{init: {'fontFamily':'Arial','themeVariables':{'fontSize':256}}}%%\nxychart-beta\nx-axis Month [Jan,Feb,Mar]\ny-axis Count 0 --> 100\nbar [20,50,80]\n",
            ["mermaid-source-font-size-actor-fit"] = "%%{init: {'fontFamily':'Arial','themeVariables':{'fontSize':'+2.56e2px'}}}%%\nusecase-beta\ndirection LR\nactor API\nactor UI\n",
            ["mermaid-source-font-size-tile-fit"] = "%%{init: {'fontFamily':'Arial','themeVariables':{'fontSize':256}}}%%\nflowchart LR\nAPI\nUI\n",
            ["mermaid-source-font-size-git-cherry-fit"] = "%%{init: {'fontFamily':'Arial','themeVariables':{'fontSize':256}}}%%\ngitGraph\ncommit id: \"base\"\nbranch dev\ncommit id: \"work\"\ncheckout main\ncherry-pick id: \"work\"\n",
            ["mermaid-source-font-size-wardley-fit"] = "%%{init: {\"themeVariables\":{\"fontSize\":256}}}%%\nwardley-beta\nanchor User [0.9, 0.15]\ncomponent API [0.55, 0.4]\nUser +'uses'> API\nnote \"Data\" [0.25, 0.6]\naccelerator Faster [0.45, 0.65]\nannotation 1, [0.5, 0.5] \"Review\"\npipeline API {\n  component Build [0.3]\n}\n",
            ["mermaid-source-font-size-fishbone-fit"] = "%%{init: {\"themeVariables\":{\"fontSize\":256}}}%%\nishikawa-beta\nAPI\n  People\n    Data\n  Process\n    Review\n",
            ["mermaid-source-font-size-block-fit"] = "%%{init: {\"themeVariables\":{\"fontSize\":256}}}%%\nblock-beta\ncolumns 2\nAPI Store\nAPI --> Store\n",
            ["mermaid-source-font-size-venn-fit"] = "%%{init: {\"themeVariables\":{\"fontSize\":256}}}%%\nvenn-beta\ntitle Venn\nset API [\"API\"] : 60\nset UI [\"UI\"] : 55\nunion API,UI [\"Shared\"] : 18\ntext API note [\"Data\"]\n",
            ["mermaid-source-font-size-pie-fit"] = "%%{init: {\"themeVariables\":{\"fontSize\":256}}}%%\npie\ntitle Service allocation\n\"API\": 2\n\"Store\": 1\n",
            ["mermaid-source-font-size-gitgraph-fit"] = "%%{init: {\"themeVariables\":{\"fontSize\":256}}}%%\ngitGraph\ncommit id: \"base\" tag: \"v1\"\nbranch dev\ncommit id: \"work\"\ncheckout main\nmerge dev id: \"merge\"\n",
            ["mermaid-source-font-size-packet-fit"] = "%%{init: {\"themeVariables\":{\"fontSize\":256}}}%%\npacket-beta\n0-31: \"Header\"\n32-63: \"Payload\"\n",
            ["mermaid-source-font-size-packet"] = "%%{init: {\"themeVariables\":{\"fontSize\":21}}}%%\npacket-beta\n0-15: \"Source Port\"\n16-31: \"Destination Port\"\n32-63: \"Sequence Number\"\n+32: \"Acknowledgment Number\"\n",
            ["mermaid-source-font-size-block"] = "%%{init: {\"themeVariables\":{\"fontSize\":21}}}%%\nblock-beta\ncolumns 3\nfrontend api database\nspace worker:2\nfrontend --> api\napi --> database\n",
            ["mermaid-source-font-size-gitgraph"] = "%%{init: {\"themeVariables\":{\"fontSize\":21}}}%%\ngitGraph LR:\n  commit id: \"base\" tag: \"v1\"\n  branch develop order: 1\n  checkout develop\n  commit id: \"work\" type: HIGHLIGHT\n  checkout main\n  merge develop id: \"merge\" tag: \"v2\"\n  branch hotfix order: 2\n  checkout hotfix\n  cherry-pick id: \"work\"\n  checkout main\n  merge hotfix\n",
            ["mermaid-source-font-size-wardley"] = "%%{init: {\"themeVariables\":{\"fontSize\":21}}}%%\nwardley-beta\ntitle Platform map\nsize [900, 600]\nevolution Genesis -> Custom Built -> Product -> Commodity\nanchor User [0.95, 0.05]\ncomponent Portal [0.80, 0.35] inertia\ncomponent API [0.70, 0.45] (build)\ncomponent Identity [0.55, 0.65] label [-12, 18]\nUser -> Portal\nPortal +'uses'> API\nAPI -.-> Identity;optional\nevolve API 0.75\nnote \"Operational pressure\" [0.35, 0.75]\nannotations [0.12, 0.88]\nannotation 1, [0.60, 0.50] \"Improve automation\"\naccelerator Faster [0.42, 0.62]\ndeaccelerator Risk [0.48, 0.68]\npipeline API {\n  component Build [0.30]\n  component Buy [0.55]\n  component Commodity [0.80]\n}\n",
            ["mermaid-source-font-size-venn"] = "%%{init: {\"themeVariables\":{\"fontSize\":21}}}%%\nvenn-beta\ntitle Capability overlap\nset API [\"API\"] : 60\nset UI [\"UI\"] : 55\nset Ops [\"Operations\"] : 45\nunion API,UI [\"Shared UX\"] : 18\nunion API,Ops [\"Automation\"] : 12\nunion UI,Ops [\"Runbooks\"] : 8\nunion API,UI,Ops [\"Platform\"] : 5\ntext API,UI note [\"Reviewed\"]\n",
            ["mermaid-source-font-size-fishbone"] = "%%{init: {\"themeVariables\":{\"fontSize\":21}}}%%\nishikawa-beta\nDelayed release\n  People\n    Handoffs\n      Timezone gaps\n  Process\n    Late review\n  Platform\n    Slow build\n",
            ["mermaid-source-font-size-sequence"] = "%%{init: {\"themeVariables\":{\"fontSize\":21}}}%%\nsequenceDiagram\nparticipant API\nparticipant Store\nAPI->>Store: Save request\nNote right of Store: Stored record\nAPI->>API: Inspect result\n",
            ["mermaid-source-font-size"] = "---\nconfig:\n  themeVariables:\n    fontSize: 18.5px\n    fontFamily: Georgia, serif\n---\nflowchart LR\nA[API] --> B[Store]\n",
            ["mermaid-source-font-size-quadrant-captions"] = "%%{init: {'fontFamily':'Arial','themeVariables':{'fontSize':96}}}%%\nquadrantChart\nx-axis Low --> High\ny-axis Low --> High\nAlpha: [0.25, 0.25]\nBeta: [0.75, 0.75]\n",
            ["mermaid-source-font-size-journey-captions"] = "%%{init: {'fontFamily':'Arial','themeVariables':{'fontSize':256}}}%%\njourney\nsection Work\nAlpha: 5: Sam\nBeta: 3: Sam\n",
            ["mermaid-flowchart-closing-endpoints"] = "flowchart LR\nA -- label o--o B\nB == label x==x C\nC -. label o-.-o D\nD -- label <--> E\nE == label <==> F\nF -. label <.-> G\n",
            ["mermaid-radar-layout"] = "---\nconfig:\n  radar:\n    width: 460\n    height: 350\n    marginLeft: 12\n    marginTop: 14\n    marginRight: 16\n    marginBottom: 18\n    axisScaleFactor: 0.8\n    axisLabelFactor: 1.2\n    curveTension: 0.25\n---\nradar-beta\ntitle Authored radar layout\naxis a[\"Coverage\"], b[\"Policy\"], c[\"Response\"], d[\"Recovery\"]\ncurve current[\"Current\"]{90,45,80,60}\nmin 0\nmax 100\ngraticule circle\n",
            ["mermaid-radar-curves"] = "---\nconfig:\n  radar:\n    curveTension: 0.25\n---\nradar-beta\ntitle Closed radar curves\naxis a[\"Coverage\"], b[\"Policy\"], c[\"Response\"], d[\"Recovery\"]\ncurve current[\"Current\"]{90, 45, 80, 60}\ncurve target[\"Target\"]{95, 85, 90, 80}\nmin 0\nmax 100\ngraticule circle\n",
            ["mermaid-radar-maximum-tension"] = "---\nconfig:\n  radar:\n    curveTension: 1\n---\nradar-beta\ntitle Full curve fits the viewport\naxis a[\"Coverage\"], b[\"Policy\"], c[\"Response\"], d[\"Recovery\"]\ncurve current[\"Current\"]{100, 100, 100, 100}\nmin 0\nmax 100\n",
            ["mermaid-radar-polygon-curves"] = "---\nconfig:\n  radar:\n    curveTension: 1\n---\nradar-beta\ntitle Polygonal curves retain straight edges\naxis a[\"Coverage\"], b[\"Policy\"], c[\"Response\"], d[\"Recovery\"]\ncurve current[\"Current\"]{90, 45, 80, 60}\nmin 0\nmax 100\ngraticule polygon\n",
            ["mermaid-gantt-ticks-clock"] = "gantt\ntitle Millisecond boundaries\ntickInterval 100millisecond\nTask :2026-01-01, 300ms\n",
            ["mermaid-treemap-labels"] = "---\nconfig:\n  treemap:\n    showValues: false\n---\ntreemap-beta\n\"Storage\"\n  \"Documents\": 1,234.5\n  \"Media\": 765.5\n",
            ["mermaid-treemap-values"] = "treemap-beta\n\"Storage\"\n  \"Documents\": 1,234.5\n  \"Media\": 765.5\n  \"Archive\"\n    \"Documents\": 240\n    \"Media\": 160\n",
            ["mermaid-radar-circle"] = "radar-beta\naxis Reliability, Performance, Security, Usability, Operations\ncurve current[\"Current\"]{75, 60, 85, 65, 80}\ncurve target[\"Target\"]{90, 85, 90, 80, 90}\nmax 100\ngraticule circle\n",
            ["mermaid-radar-polygon"] = "radar-beta\naxis Reliability, Performance, Security, Usability, Operations\ncurve current[\"Current\"]{75, 60, 85, 65, 80}\ncurve target[\"Target\"]{90, 85, 90, 80, 90}\nmax 100\ngraticule polygon\n",
            ["mermaid-radar-zero-guides"] = "radar-beta\naxis Reliability, Performance, Security, Usability, Operations\ncurve current[\"Current\"]{75, 60, 85, 65, 80}\ncurve target[\"Target\"]{90, 85, 90, 80, 90}\nmax 100\nticks 0\n",
            ["mermaid-radar-fractional-guides"] = "radar-beta\naxis Reliability, Performance, Security, Usability, Operations\ncurve current[\"Current\"]{75, 60, 85, 65, 80}\ncurve target[\"Target\"]{90, 85, 90, 80, 90}\nmax 100\nticks 2.5\ngraticule polygon\n",
            ["mermaid-radar-hidden-fractional-guides"] = "radar-beta\naxis Reliability, Performance, Security, Usability, Operations\ncurve current[\"Current\"]{75, 60, 85, 65, 80}\ncurve target[\"Target\"]{90, 85, 90, 80, 90}\nmax 100\nticks 2.5\ngraticule polygon\n",
            ["mermaid-block-circle-routes"] = "block-beta\ncolumns 2\napi[API] worker((Worker))\nstore[(Store)] queue((Queue))\napi --> worker\nworker --> queue\nqueue --> store\n",
            ["mermaid-flowchart-modern"] = "flowchart LR\nA@{ shape: cloud, label: \"API, cloud\" } & B --> C & D --> E\n",
            ["mermaid-flowchart-endpoints"] = "flowchart LR\nA[Publish] ---o B[Event]\nC[Cancel] ---x D[Stop]\nE[Peer] o-- 'til exchanged --o F[Peer]\nG[Blocked] x-. rejects .-x H[Blocked]\n",
            ["mermaid-accessibility-dark"] = "---\nconfig:\n  theme: dark\n---\nflowchart LR\naccTitle: Service path\naccDescr {\n API to storage\n}\nA --> B\n",
            ["mermaid-source-font"] = "---\nconfig:\n  theme: dark\n  themeVariables:\n    fontFamily: Georgia, serif\n---\nflowchart LR\nA[Source typography] --> B[Shared native scene]\n",
            ["mermaid-sankey-configured"] = "---\nconfig:\n  sankey:\n    width: 800\n    height: 480\n    nodeWidth: 18\n    nodePadding: 24\n    nodeAlignment: center\n    showValues: false\n---\nsankey-beta\nIntake,Review,6\nReview,Build,6\nBuild,Delivery,6\nFast,Delivery,4\nIntake,Deferred,2\n",
            ["mermaid-sankey-values"] = "---\nconfig:\n  sankey:\n    width: 800\n    height: 480\n    nodeWidth: 18\n    nodePadding: 24\n    nodeAlignment: right\n    prefix: '$'\n    suffix: ' units'\n---\nsankey-beta\nIntake,Review,6\nReview,Build,6\nBuild,Delivery,6\nFast,Delivery,4\nIntake,Deferred,2\n",
            ["mermaid-sequence-presentation"] = "---\nconfig:\n  theme: dark\n  fontFamily: Georgia, serif\n---\nsequenceDiagram\nparticipant API\nparticipant Store\nAPI->>Store: Save request\nNote right of Store: Stored record\nStore-->>API: Complete\n",
            ["mermaid-class-notation"] = "classDiagram\nnamespace Services {\nclass User {\n+string name\n+save() void\n}\nclass Admin\n}\n<<interface>> User\nUser <|-- Admin\nUser \"1\" o-- \"0..*\" Session : opens\n",
            ["mermaid-er-notation"] = "erDiagram\nCUSTOMER {\n int id PK \"Customer key\"\n string name\n}\nORDER {\n int id PK\n int customerId FK\n}\nCUSTOMER ||--o{ ORDER : places\n",
            ["mermaid-class-direction"] = "classDiagram\ndirection RL\nclass Service {\n+Run() bool\n}\nService --> Store : writes\nnote for Service \"Requests: Service --> Store\"\nclick Service href \"https://example.invalid\" \"Details\"\n",
            ["mermaid-state-direction"] = "stateDiagram-v2\ndirection BT\nIdle --> Processing : submit\nProcessing --> Complete : finish\nnote right of Processing\n Requests: Idle --> Processing\nend note\n",
            ["mermaid-er-direction"] = "erDiagram\ndirection TB\nCUSTOMER ||--o{ ORDER : places\nORDER ||--|{ ITEM : contains\nclassDef marked fill:#eef\nclass CUSTOMER marked\n",
            ["mermaid-gantt-calendar"] = "gantt\ndateFormat YYYY-MM-DD\nexcludes weekends\nsection Delivery\nBuild :active, build, 2026-01-02, 2d\nVerify :after build, 1d\nExplicit :2026-01-02, 2026-01-04\n",
            ["mermaid-xychart-stable"] = "xychart\nx-axis [Jan, Feb, Mar]\ny-axis \"Revenue\" 0 --> 50\nline Revenue [10, 25, 40]\n",
            ["mermaid-gantt-months"] = "gantt\ndateFormat YYYY-MM-DD\naxisFormat %b %d\nsection Calendar durations\nMonth :month, 2026-01-31, 1M\nTwo months :two, after month, 2M\nsection Clock duration\nOne minute :minute, 2026-01-31, 1m\n",
            ["mermaid-gantt-references"] = "gantt\ndateFormat YYYY-MM-DD\naxisFormat %b %d\nsection Delivery\nWindow :window, 2026-01-01, until gate\nGate :milestone, gate, 2026-01-02, 2d\nTail :tail, after gate, 1d\n",
            ["mermaid-gantt-forward"] = "gantt\ndateFormat YYYY-MM-DD\naxisFormat %b %d\nTail :tail, after first other, 1d\nFirst :first, 2026-01-01, 2d\nOther :other, 2026-01-01, 4d\nGate :milestone, gate, after tail, 0d\n",
            ["mermaid-gantt-axis"] = "gantt\naxisFormat %a %d %b\nFirst :first, 2026-01-04, 4d\nSecond :second, 2026-01-05, 2d\n",
            ["mermaid-gantt-markers"] = "gantt\ntodayMarker off\naxisFormat %b %d\nDesign :design,2026-01-01,3d\nDeadline :vert,deadline,2026-01-02,4d\nAfter marker :aftermark,after deadline,2d\nDelivery :delivery,after design,2d\n",
            ["mermaid-gantt-input-dates"] = "gantt\ntitle Calendar input fields\ndateFormat Do-MMMM-YYYY\naxisFormat %a %d %b\nexcludes 3rd-January-2026 4th-January-2026\nincludes 4th-January-2026\nBuild :build,2nd-January-2026,2d\nReview :review,after build,1d\nDeadline :vert,6th-January-2026,0d\n",
            ["mermaid-gantt-ticks"] = "gantt\ntitle Fortnightly delivery\naxisFormat %a %d %b\ntickInterval 2week\nweekday tuesday\nWindow :window, 2026-01-01, 2026-03-01\nBuild :build, 2026-01-13, 14d\nVerify :after build, 14d\n",
            ["mermaid-gantt-today-style"] = "gantt\ntitle Current schedule checkpoint\naxisFormat %a %d %b\ntodayMarker stroke:#16834a,stroke-width:3px,opacity:0.8,stroke-dasharray:6 6\nBuild :build,2026-01-01,4d\nReview :review,after build,2d\n",
            ["mermaid-gantt-date-anchor"] = "gantt\ntitle Anchored leap-year calendar\ndateFormat MM-DD\naxisFormat %a %d %b\nexcludes 02-29\nBuild :build,02-28,2d\nReview :review,after build,1d\nDeadline :vert,03-03,0d\n",
            ["mermaid-membership-flowchart-compact-light"] = MermaidFixtureSources.Read("flowchart-membership"),
            ["mermaid-membership-flowchart-compact-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidFixtureSources.Read("flowchart-membership"),
            ["mermaid-membership-flowchart-wide-light"] = MermaidFixtureSources.Read("flowchart-membership"),
            ["mermaid-membership-flowchart-wide-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidFixtureSources.Read("flowchart-membership"),
            ["mermaid-membership-swimlane-compact-light"] = MermaidFixtureSources.Read("flowchart-membership").Replace("flowchart LR", "swimlane-beta LR", StringComparison.Ordinal),
            ["mermaid-membership-swimlane-compact-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidFixtureSources.Read("flowchart-membership").Replace("flowchart LR", "swimlane-beta LR", StringComparison.Ordinal),
            ["mermaid-membership-swimlane-wide-light"] = MermaidFixtureSources.Read("flowchart-membership").Replace("flowchart LR", "swimlane-beta LR", StringComparison.Ordinal),
            ["mermaid-membership-swimlane-wide-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidFixtureSources.Read("flowchart-membership").Replace("flowchart LR", "swimlane-beta LR", StringComparison.Ordinal),
            ["mermaid-subgraph-merge-flowchart-compact-light"] = MermaidFixtureSources.Read("flowchart-repeated-subgraphs"),
            ["mermaid-subgraph-merge-flowchart-compact-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidFixtureSources.Read("flowchart-repeated-subgraphs"),
            ["mermaid-subgraph-merge-flowchart-wide-light"] = MermaidFixtureSources.Read("flowchart-repeated-subgraphs"),
            ["mermaid-subgraph-merge-flowchart-wide-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidFixtureSources.Read("flowchart-repeated-subgraphs"),
            ["mermaid-subgraph-merge-swimlane-compact-light"] = MermaidFixtureSources.Read("flowchart-repeated-subgraphs").Replace("flowchart LR", "swimlane-beta LR", StringComparison.Ordinal),
            ["mermaid-subgraph-merge-swimlane-compact-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidFixtureSources.Read("flowchart-repeated-subgraphs").Replace("flowchart LR", "swimlane-beta LR", StringComparison.Ordinal),
            ["mermaid-subgraph-merge-swimlane-wide-light"] = MermaidFixtureSources.Read("flowchart-repeated-subgraphs").Replace("flowchart LR", "swimlane-beta LR", StringComparison.Ordinal),
            ["mermaid-subgraph-merge-swimlane-wide-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidFixtureSources.Read("flowchart-repeated-subgraphs").Replace("flowchart LR", "swimlane-beta LR", StringComparison.Ordinal),
            ["mermaid-swimlane-nesting-compact-light"] = MermaidFixtureSources.Read("swimlane-native-nesting"),
            ["mermaid-swimlane-nesting-compact-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidFixtureSources.Read("swimlane-native-nesting"),
            ["mermaid-swimlane-nesting-wide-light"] = MermaidFixtureSources.Read("swimlane-native-nesting"),
            ["mermaid-swimlane-nesting-wide-dark"] = "%%{init: {'theme':'dark'}}%%\n" + MermaidFixtureSources.Read("swimlane-native-nesting"),
            ["mermaid-swimlane-basic"] = "swimlane-beta LR\nsubgraph Requester\n A[Submit] --> B[Review]\nend\nsubgraph Service\n C{Approved?} --> D[Deliver]\nend\nB --> C\n",
            ["mermaid-usecase-basic"] = "usecase-beta\ndirection LR\nactor Customer\nsystemBoundary Store[Online store]\n Browse(\"Browse products\")\n Pay(\"Pay for order\")\n Authenticate(\"Authenticate\")\nend\nCustomer --> Browse\nBrowse ..> : include Authenticate\nPay --|> Browse\n",
            ["mermaid-usecase-vertical"] = "usecase-beta\nactor User\nUser --> Action(Do work)\n",
            ["mermaid-cynefin-basic"] = "cynefin-beta\ntitle Service decisions\ncomplex\n \"Discover demand\"\ncomplicated\n \"Analyze capacity\"\nchaotic\n \"Contain outage\"\nclear\n \"Run checklist\"\nconfusion\n \"Investigate context\"\ncomplex --> complicated : \"Learn\"\n",
        };
        foreach (var item in examples) {
            var options = item.Key switch {
                "mermaid-gantt-today-style" => new MermaidRenderOptions { Gantt = new MermaidGanttRenderOptions { Today = new DateTime(2026, 1, 3) } },
                "mermaid-gantt-date-anchor" => new MermaidRenderOptions { ParseOptions = new MermaidParseOptions { GanttDateAnchor = new DateTime(2028, 1, 1) } },
                _ when (item.Key.StartsWith("mermaid-c4-source-", StringComparison.Ordinal) || item.Key.StartsWith("mermaid-c4-config-", StringComparison.Ordinal)) => new MermaidRenderOptions {
                    C4 = new MermaidTopologyRenderOptions { Width = item.Key.Contains("compact", StringComparison.Ordinal) ? 390 : 1100, Height = 800, Padding = 32 }
                },
                _ when item.Key.StartsWith("mermaid-mindmap-notation-", StringComparison.Ordinal) => new MermaidRenderOptions {
                    MindMap = new MermaidTopologyRenderOptions { Width = item.Key.Contains("compact", StringComparison.Ordinal) ? 390 : 1100, Height = 560 }
                },
                _ when item.Key.StartsWith("mermaid-state-floating-", StringComparison.Ordinal) => new MermaidRenderOptions {
                    State = new MermaidTopologyRenderOptions { Width = item.Key.Contains("compact", StringComparison.Ordinal) ? 390 : 1100, Height = 680 }
                },
                _ => null
            };
            if (item.Key.StartsWith("mermaid-membership-", StringComparison.Ordinal) || item.Key.StartsWith("mermaid-subgraph-merge-", StringComparison.Ordinal)) {
                var width = item.Key.Contains("compact", StringComparison.Ordinal) ? 390 : 1100;
                options = new MermaidRenderOptions { Flowchart = new MermaidFlowchartRenderOptions { Width = width, Height = 700 },
                    Swimlane = new MermaidFlowchartRenderOptions { Width = width, Height = 700 } };
            }
            if (item.Key.StartsWith("mermaid-swimlane-nesting-", StringComparison.Ordinal))
                options = new MermaidRenderOptions { Swimlane = new MermaidFlowchartRenderOptions {
                    Width = item.Key.Contains("compact", StringComparison.Ordinal) ? 390 : 1100, Height = 750 } };
            if (item.Key.StartsWith("mermaid-architecture-boundaries-", StringComparison.Ordinal) || item.Key.StartsWith("mermaid-architecture-captions-", StringComparison.Ordinal))
                options = new MermaidRenderOptions { Architecture = new MermaidTopologyRenderOptions {
                    Width = item.Key.Contains("compact", StringComparison.Ordinal) ? 390 : 1100, Height = 700 } };
            if (item.Key.StartsWith("mermaid-sequence-depth-", StringComparison.Ordinal) || item.Key.StartsWith("mermaid-sequence-half-arrows-", StringComparison.Ordinal) || item.Key.StartsWith("mermaid-sequence-rect-numbering-", StringComparison.Ordinal) || item.Key.StartsWith("mermaid-sequence-par-over-", StringComparison.Ordinal) || item.Key.StartsWith("mermaid-sequence-participants-", StringComparison.Ordinal))
                options = new MermaidRenderOptions { Sequence = new MermaidSequenceRenderOptions {
                    Width = item.Key.Contains("compact", StringComparison.Ordinal) ? 390 : 1100, Height = 620 } };
            if (item.Key.StartsWith("mermaid-architecture-alignments-", StringComparison.Ordinal))
                options = new MermaidRenderOptions { Architecture = new MermaidTopologyRenderOptions {
                    Width = item.Key.Contains("compact", StringComparison.Ordinal) ? 390 : 1100, Height = 700 } };
            if (item.Key.StartsWith("mermaid-ishikawa-depth-", StringComparison.Ordinal))
                options = new MermaidRenderOptions { Ishikawa = new MermaidIshikawaRenderOptions {
                    Width = item.Key.Contains("compact", StringComparison.Ordinal) ? 390 : 1100, Height = 520 } };
            if (item.Key.StartsWith("mermaid-treeview-rows-", StringComparison.Ordinal))
                options = new MermaidRenderOptions { TreeView = new MermaidTreeViewRenderOptions { Width = item.Key.Contains("compact", StringComparison.Ordinal) ? 390 : 1100, Height = 320 } };
            if (item.Key.StartsWith("mermaid-class-captions-", StringComparison.Ordinal) || item.Key.StartsWith("mermaid-er-captions-", StringComparison.Ordinal)) {
                var captionTopology = new MermaidTopologyRenderOptions { Width = item.Key.Contains("compact", StringComparison.Ordinal) ? 390 : 1100, Height = 620 };
                options = new MermaidRenderOptions { Class = captionTopology, EntityRelationship = captionTopology };
            }
            if (item.Key.StartsWith("mermaid-treemap-styles-", StringComparison.Ordinal))
                options = new MermaidRenderOptions { Treemap = new MermaidTreemapRenderOptions {
                    Width = item.Key.Contains("compact", StringComparison.Ordinal) ? 390 : 1100, Height = 620 } };
            if (item.Key.StartsWith("mermaid-eventmodeling-depth-", StringComparison.Ordinal))
                options = new MermaidRenderOptions { EventModeling = new MermaidTopologyRenderOptions {
                    Width = item.Key.Contains("compact", StringComparison.Ordinal) ? 390 : 1200, Height = 700 } };
            if (item.Key.StartsWith("mermaid-wardley-overlays-", StringComparison.Ordinal))
                options = new MermaidRenderOptions { Wardley = new MermaidWardleyRenderOptions { Width = item.Key.Contains("compact", StringComparison.Ordinal) ? 390 : 1100, Height = 600 } };
            if ((item.Key.StartsWith("mermaid-kanban-board-", StringComparison.Ordinal) || item.Key.StartsWith("mermaid-kanban-source-", StringComparison.Ordinal)))
                options = new MermaidRenderOptions { Kanban = new MermaidTopologyRenderOptions {
                    Width = item.Key.Contains("compact", StringComparison.Ordinal) ? 390 : 1200, Height = 700 } };
            if (item.Key.StartsWith("mermaid-gitgraph-depth-", StringComparison.Ordinal))
                options = new MermaidRenderOptions { GitGraph = new MermaidGitGraphRenderOptions {
                    Width = item.Key.Contains("compact", StringComparison.Ordinal) ? 390 : 1100, Height = 620 } };
            if (item.Key.StartsWith("mermaid-block-depth-", StringComparison.Ordinal))
                options = new MermaidRenderOptions { Block = new MermaidBlockRenderOptions {
                    Width = item.Key.Contains("compact", StringComparison.Ordinal) ? 390 : 1100, Height = 620 } };
            if (item.Key.StartsWith("mermaid-quadrant-source-", StringComparison.Ordinal))
                options = new MermaidRenderOptions { Quadrant = new MermaidQuadrantRenderOptions {
                    Width = item.Key.Contains("compact", StringComparison.Ordinal) ? 390 : 800, Height = 500 } };
            var parsed = new MermaidParser().Parse(item.Value, options?.ParseOptions);
            // Ordinary gallery examples keep the native Graphite host palette. Dedicated
            // source-palette recipes and authored source paint retain their own presentation.
            var configuration = parsed.Document?.Configuration;
            var galleryTheme = configuration != null
                && (configuration.Theme == null || configuration.Theme is "default" or "dark")
                && configuration.Settings.All(setting => !setting.Path.StartsWith("themeVariables.", StringComparison.Ordinal)
                    || setting.Path is "themeVariables.fontFamily" or "themeVariables.fontSize");
            // Keep gallery metrics independent of installed fonts without replacing source fonts or sizes.
            var proofTypography = configuration != null && configuration.FontFamily == null;
            if (proofTypography || galleryTheme) {
                options ??= new MermaidRenderOptions();
                options.Presentation ??= new MermaidPresentationOptions();
                if (proofTypography) options.Presentation.FontFamily = ExampleTypography.FontFamily;
                if (galleryTheme) options.Presentation.Theme ??= ChartForgeX.Themes.VisualTheme.Graphite();
            }
            var result = MermaidRenderer.RenderParsed(parsed, options);
            if (result.HasErrors || result.Artifact == null) throw new InvalidOperationException(string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Message)));
            // These incumbent examples retain their authored gallery host presentation.
            if ((item.Key == "mermaid-sequence-lifetimes-light" || item.Key == "mermaid-sequence-lifetimes-dark")
                && result.Artifact.Model is SequenceArtifact lifetimes) {
                lifetimes.Layout = null;
                lifetimes.Typography = null;
            }
            if (item.Key == "mermaid-cynefin-basic") {
                result.Artifact.NaturalSize = new VisualArtifactSize(960, 621);
                result.Artifact.PreserveNaturalSize = true;
            }
            if (galleryTheme && result.Artifact.Model is ChartForgeX.VisualBlocks.IVisualBlock galleryBlock
                && galleryBlock is ChartForgeX.VisualBlocks.JourneyDiagramBlock or ChartForgeX.VisualBlocks.TimelineDiagramBlock)
                galleryBlock.Options.TransparentBackground = true;
            if ((item.Key.StartsWith("mermaid-source-font-size-", StringComparison.Ordinal) || item.Key.StartsWith("mermaid-packet-", StringComparison.Ordinal)) && result.Artifact.Model is ChartForgeX.VisualBlocks.IVisualBlock block
                && block is not ChartForgeX.VisualBlocks.JourneyDiagramBlock && block is not ChartForgeX.VisualBlocks.TimelineDiagramBlock)
                block.Options.ShowCard = false;
            if (item.Key == "mermaid-source-font-size-wardley-boundaries" && result.Artifact.Model is ChartForgeX.VisualBlocks.WardleyMapBlock wardley) {
                wardley.WithSize(420, 320);
            }
            if (item.Key == "mermaid-source-font-size-packet-narrow-fit" && result.Artifact.Model is ChartForgeX.VisualBlocks.PacketLayoutBlock packet)
                packet.WithSize(640, 280);
            if (item.Key == "mermaid-source-font-size-group-header-fit" && result.Artifact.Model is ChartForgeX.Topology.TopologyChart grouped)
                grouped.Groups[0].Subtitle = "Group description";
            if (item.Key.StartsWith("mermaid-source-font-size-icon-", StringComparison.Ordinal) && result.Artifact.Model is ChartForgeX.Topology.TopologyChart icons) {
                icons.Title = ""; icons.Subtitle = "";
                icons.DefaultRenderOptions = new ChartForgeX.Topology.TopologyRenderOptions { IncludeTitle = false, IncludeLegend = false, IncludeIconLabels = true };
                icons.Nodes[0].DisplayMode = ChartForgeX.Topology.TopologyNodeDisplayMode.Icon;
                ChartForgeX.Topology.TopologyChartExtensions.WithViewport(icons, 360, 240, 16);
            }
            if (item.Key == "mermaid-source-font-size-axis-rotated-fit" && result.Artifact.Model is Chart rotated) {
                rotated.WithSize(360, 420); rotated.Options.XAxis.LabelAngle = 90;
            }
            if (item.Key == "mermaid-source-font-size-actor-compact-fit" && result.Artifact.Model is ChartForgeX.Topology.TopologyChart actors) {
                actors.Title = ""; actors.Subtitle = ""; actors.LayoutMode = ChartForgeX.Topology.TopologyLayoutMode.Manual;
                actors.DefaultRenderOptions = new ChartForgeX.Topology.TopologyRenderOptions { IncludeTitle = false, IncludeLegend = false, IncludeIconLabels = true };
                ChartForgeX.Topology.TopologyChartExtensions.WithViewport(actors, 360, 240, 16);
                for (var index = 0; index < actors.Nodes.Count; index++) {
                    var node = actors.Nodes[index]; node.X = 60 + index * 160; node.Y = 70;
                    node.PreserveDisplayModeSize = false;
                    node.DisplayMode = index == 0 ? ChartForgeX.Topology.TopologyNodeDisplayMode.Tile : ChartForgeX.Topology.TopologyNodeDisplayMode.Icon;
                    node.ShowStatusBadge = false;
                }
            }
            if (item.Key == "mermaid-source-font-size-git-cherry-fit" && result.Artifact.Model is ChartForgeX.VisualBlocks.GitGraphBlock git) {
                git.WithTitle("").WithSubtitle("").WithSize(320, 460).WithCommitLabels(false);
                git.ShowBranchLabels = false;
            }
            if ((item.Key == "mermaid-source-font-size-actor-fit" || item.Key == "mermaid-source-font-size-tile-fit") &&
                result.Artifact.Model is ChartForgeX.Topology.TopologyChart topology) {
                topology.Title = ""; topology.Subtitle = ""; topology.LayoutMode = ChartForgeX.Topology.TopologyLayoutMode.Manual;
                topology.DefaultRenderOptions = new ChartForgeX.Topology.TopologyRenderOptions { IncludeTitle = false, IncludeLegend = false, IncludeTileSubtitles = true };
                ChartForgeX.Topology.TopologyChartExtensions.WithViewport(topology, 320, 360, 16);
                for (var index = 0; index < topology.Nodes.Count; index++) {
                    var node = topology.Nodes[index]; node.X = 32 + index * 176; node.Y = 90; node.Width = 80;
                    node.Height = item.Key == "mermaid-source-font-size-tile-fit" ? 48 : 108;
                    if (item.Key == "mermaid-source-font-size-tile-fit") {
                        node.DisplayMode = ChartForgeX.Topology.TopologyNodeDisplayMode.Tile; node.Subtitle = "Ready";
                    }
                }
            }
            if (item.Key == "mermaid-radar-hidden-fractional-guides" && result.Artifact.Model is Chart chart) chart.Options.ShowGrid = false;
            if (item.Key.StartsWith("mermaid-quadrant-source-", StringComparison.Ordinal))
                File.WriteAllText(Path.Combine(output, item.Key + ".mmd"), item.Value, System.Text.Encoding.UTF8);
            result.Artifact.SaveSvg(Path.Combine(output, item.Key + ".svg"));
            result.Artifact.SavePng(Path.Combine(output, item.Key + ".png"));
            var htmlPath = Path.Combine(output, item.Key + ".html");
            if (proofTypography)
                File.WriteAllText(htmlPath, result.Artifact.ToHtmlPage().Replace("</head>", ExampleTypography.EmbeddedStyle + "</head>", StringComparison.Ordinal), System.Text.Encoding.UTF8);
            else result.Artifact.SaveHtml(htmlPath);
        }
    }
}
