using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;

internal static class MermaidExamples {
    internal static void Write(string output) {
        var examples = new Dictionary<string, string> {
            ["mermaid-flowchart-modern"] = "flowchart LR\nA@{ shape: cloud, label: \"API, cloud\" } & B --> C & D --> E\n",
            ["mermaid-accessibility-dark"] = "---\nconfig:\n  theme: dark\n---\nflowchart LR\naccTitle: Service path\naccDescr {\n API to storage\n}\nA --> B\n",
            ["mermaid-class-notation"] = "classDiagram\nnamespace Services {\nclass User {\n+string name\n+save() void\n}\nclass Admin\n}\n<<interface>> User\nUser <|-- Admin\nUser \"1\" o-- \"0..*\" Session : opens\n",
            ["mermaid-er-notation"] = "erDiagram\nCUSTOMER {\n int id PK \"Customer key\"\n string name\n}\nORDER {\n int id PK\n int customerId FK\n}\nCUSTOMER ||--o{ ORDER : places\n",
            ["mermaid-gantt-calendar"] = "gantt\ndateFormat YYYY-MM-DD\nexcludes weekends\nsection Delivery\nBuild :active, build, 2026-01-02, 2d\nVerify :after build, 1d\nExplicit :2026-01-02, 2026-01-04\n",
            ["mermaid-xychart-stable"] = "xychart\nx-axis [Jan, Feb, Mar]\ny-axis \"Revenue\" 0 --> 50\nline Revenue [10, 25, 40]\n",
            ["mermaid-swimlane-basic"] = "swimlane-beta LR\nsubgraph Requester\n A[Submit] --> B[Review]\nend\nsubgraph Service\n C{Approved?} --> D[Deliver]\nend\nB --> C\n",
            ["mermaid-usecase-basic"] = "usecase-beta\ndirection LR\nactor Customer\nsystemBoundary Store[Online store]\n Browse(\"Browse products\")\n Pay(\"Pay for order\")\n Authenticate(\"Authenticate\")\nend\nCustomer --> Browse\nBrowse ..> : include Authenticate\nPay --|> Browse\n",
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
