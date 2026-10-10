using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;

internal static class MermaidExamples {
    internal static void Write(string output) {
        var examples = new Dictionary<string, string> {
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
            ["mermaid-gantt-forward"] = "gantt\ndateFormat YYYY-MM-DD\naxisFormat %b %d\nTail :tail, after first other, 1d\nFirst :first, 2026-01-01, 2d\nOther :other, 2026-01-01, 4d\nGate :milestone, gate, after tail, 0d\n",
            ["mermaid-gantt-axis"] = "gantt\naxisFormat %a %d %b\nFirst :first, 2026-01-04, 4d\nSecond :second, 2026-01-05, 2d\n",
            ["mermaid-gantt-markers"] = "gantt\ntodayMarker off\naxisFormat %b %d\nDesign :design,2026-01-01,3d\nDeadline :vert,deadline,2026-01-02,4d\nAfter marker :aftermark,after deadline,2d\nDelivery :delivery,after design,2d\n",
            ["mermaid-gantt-ticks"] = "gantt\ntitle Fortnightly delivery\naxisFormat %a %d %b\ntickInterval 2week\nweekday tuesday\nWindow :window, 2026-01-01, 2026-03-01\nBuild :build, 2026-01-13, 14d\nVerify :after build, 14d\n",
            ["mermaid-swimlane-basic"] = "swimlane-beta LR\nsubgraph Requester\n A[Submit] --> B[Review]\nend\nsubgraph Service\n C{Approved?} --> D[Deliver]\nend\nB --> C\n",
            ["mermaid-usecase-basic"] = "usecase-beta\ndirection LR\nactor Customer\nsystemBoundary Store[Online store]\n Browse(\"Browse products\")\n Pay(\"Pay for order\")\n Authenticate(\"Authenticate\")\nend\nCustomer --> Browse\nBrowse ..> : include Authenticate\nPay --|> Browse\n",
            ["mermaid-usecase-vertical"] = "usecase-beta\nactor User\nUser --> Action(Do work)\n",
            ["mermaid-cynefin-basic"] = "cynefin-beta\ntitle Service decisions\ncomplex\n \"Discover demand\"\ncomplicated\n \"Analyze capacity\"\nchaotic\n \"Contain outage\"\nclear\n \"Run checklist\"\nconfusion\n \"Investigate context\"\ncomplex --> complicated : \"Learn\"\n",
        };
        foreach (var item in examples) {
            var result = MermaidRenderer.Render(item.Value);
            if (result.HasErrors || result.Artifact == null) throw new InvalidOperationException(string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Message)));
            result.Artifact.SaveSvg(Path.Combine(output, item.Key + ".svg"));
            result.Artifact.SavePng(Path.Combine(output, item.Key + ".png"));
            result.Artifact.SaveHtml(Path.Combine(output, item.Key + ".html"));
        }
    }
}
