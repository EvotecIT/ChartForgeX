using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;

internal static class MermaidExamples {
    internal static void Write(string output) {
        var examples = new Dictionary<string, string> {
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
            ["mermaid-block-circle-routes"] = "block-beta\ncolumns 2\napi[API] worker((Worker))\nstore[(Store)] queue((Queue))\napi --> worker\nworker --> queue\nqueue --> store\n",
            ["mermaid-flowchart-modern"] = "flowchart LR\nA@{ shape: cloud, label: \"API, cloud\" } & B --> C & D --> E\n",
            ["mermaid-accessibility-dark"] = "---\nconfig:\n  theme: dark\n---\nflowchart LR\naccTitle: Service path\naccDescr {\n API to storage\n}\nA --> B\n",
            ["mermaid-source-font"] = "---\nconfig:\n  theme: dark\n  themeVariables:\n    fontFamily: Georgia, serif\n---\nflowchart LR\nA[Source typography] --> B[Shared native scene]\n",
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
            ["mermaid-swimlane-basic"] = "swimlane-beta LR\nsubgraph Requester\n A[Submit] --> B[Review]\nend\nsubgraph Service\n C{Approved?} --> D[Deliver]\nend\nB --> C\n",
            ["mermaid-usecase-basic"] = "usecase-beta\ndirection LR\nactor Customer\nsystemBoundary Store[Online store]\n Browse(\"Browse products\")\n Pay(\"Pay for order\")\n Authenticate(\"Authenticate\")\nend\nCustomer --> Browse\nBrowse ..> : include Authenticate\nPay --|> Browse\n",
            ["mermaid-usecase-vertical"] = "usecase-beta\nactor User\nUser --> Action(Do work)\n",
            ["mermaid-cynefin-basic"] = "cynefin-beta\ntitle Service decisions\ncomplex\n \"Discover demand\"\ncomplicated\n \"Analyze capacity\"\nchaotic\n \"Contain outage\"\nclear\n \"Run checklist\"\nconfusion\n \"Investigate context\"\ncomplex --> complicated : \"Learn\"\n",
        };
        foreach (var item in examples) {
            var result = MermaidRenderer.Render(item.Value);
            if (result.HasErrors || result.Artifact == null) throw new InvalidOperationException(string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Message)));
            if (item.Key.StartsWith("mermaid-source-font-size-", StringComparison.Ordinal) && result.Artifact.Model is ChartForgeX.VisualBlocks.IVisualBlock block)
                block.Options.ShowCard = false;
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
            result.Artifact.SaveSvg(Path.Combine(output, item.Key + ".svg"));
            result.Artifact.SavePng(Path.Combine(output, item.Key + ".png"));
            result.Artifact.SaveHtml(Path.Combine(output, item.Key + ".html"));
        }
    }
}
