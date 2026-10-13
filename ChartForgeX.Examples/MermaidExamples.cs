using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;

internal static class MermaidExamples {
    internal static void Write(string output) {
        var examples = new Dictionary<string, string> {
            ["mermaid-quadrant-regions"] = "quadrantChart\ntitle Work priorities\nx-axis Low effort --> High effort\ny-axis Low impact --> High impact\nquadrant-1 Plan\nquadrant-2 Prioritize\nquadrant-3 Consider\nquadrant-4 Reassess\nOpportunity: [0.2, 0.8]\nInvestment: [0.8, 0.8]\nRoutine: [0.2, 0.2]\nReview: [0.8, 0.2]\n",
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
            ["mermaid-swimlane-basic"] = "swimlane-beta LR\nsubgraph Requester\n A[Submit] --> B[Review]\nend\nsubgraph Service\n C{Approved?} --> D[Deliver]\nend\nB --> C\n",
            ["mermaid-usecase-basic"] = "usecase-beta\ndirection LR\nactor Customer\nsystemBoundary Store[Online store]\n Browse(\"Browse products\")\n Pay(\"Pay for order\")\n Authenticate(\"Authenticate\")\nend\nCustomer --> Browse\nBrowse ..> : include Authenticate\nPay --|> Browse\n",
            ["mermaid-usecase-vertical"] = "usecase-beta\nactor User\nUser --> Action(Do work)\n",
            ["mermaid-cynefin-basic"] = "cynefin-beta\ntitle Service decisions\ncomplex\n \"Discover demand\"\ncomplicated\n \"Analyze capacity\"\nchaotic\n \"Contain outage\"\nclear\n \"Run checklist\"\nconfusion\n \"Investigate context\"\ncomplex --> complicated : \"Learn\"\n",
        };
        foreach (var item in examples) {
            var options = item.Key switch {
                "mermaid-gantt-today-style" => new MermaidRenderOptions { Gantt = new MermaidGanttRenderOptions { Today = new DateTime(2026, 1, 3) } },
                "mermaid-gantt-date-anchor" => new MermaidRenderOptions { ParseOptions = new MermaidParseOptions { GanttDateAnchor = new DateTime(2028, 1, 1) } },
                _ => null
            };
            var result = MermaidRenderer.Render(item.Value, options);
            if (result.HasErrors || result.Artifact == null) throw new InvalidOperationException(string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Message)));
            if (item.Key == "mermaid-radar-hidden-fractional-guides" && result.Artifact.Model is Chart chart) chart.Options.ShowGrid = false;
            result.Artifact.SaveSvg(Path.Combine(output, item.Key + ".svg"));
            result.Artifact.SavePng(Path.Combine(output, item.Key + ".png"));
            result.Artifact.SaveHtml(Path.Combine(output, item.Key + ".html"));
        }
    }
}
