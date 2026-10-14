using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;

internal static class MermaidExamples {
    internal static void Write(string output) {
        const string mindMap = "mindmap\n    root((Roadmap))\n          plan[Planning]\n            tasks[Tasks]\n                review[Review]\n          delivery[Delivery]\n              releases[Releases]\n                support[Support]\n";
        var examples = new Dictionary<string, string> {
            ["mermaid-mindmap-branch-light"] = "---\nconfig:\n  mindmap:\n    colorBy: branch\n---\n" + mindMap,
            ["mermaid-mindmap-depth-light"] = "---\nconfig:\n  mindmap:\n    colorBy: depth\n---\n" + mindMap,
            ["mermaid-mindmap-branch-dark"] = "---\nconfig:\n  theme: dark\n  mindmap:\n    colorBy: branch\n---\n" + mindMap,
            ["mermaid-mindmap-depth-dark"] = "---\nconfig:\n  theme: dark\n  mindmap:\n    colorBy: depth\n---\n" + mindMap,
            ["mermaid-flowchart-shape-aliases"] = "flowchart LR\nA@{ shape: fr-rect, label: \"Framed process\" } --> B@{ shape: framed-rectangle, label: \"Subprocess\" }\nC@{ shape: question, label: \"Decision\" } --> D@{ shape: odd, label: \"Asymmetric\" }\nE@{ shape: lean-right, label: \"Input\" } --> F@{ shape: lean-left, label: \"Output\" }\nG@{ shape: manual, label: \"Manual task\" } --> H@{ shape: trapezoid-top, label: \"Operation\" }\nI@{ shape: priority, label: \"Priority\" } --> J@{ shape: trapezoid-bottom, label: \"Action\" }\n",
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
            if ((item.Key.StartsWith("mermaid-source-font-size-", StringComparison.Ordinal) || item.Key.StartsWith("mermaid-packet-", StringComparison.Ordinal)) && result.Artifact.Model is ChartForgeX.VisualBlocks.IVisualBlock block)
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
            result.Artifact.SaveSvg(Path.Combine(output, item.Key + ".svg"));
            result.Artifact.SavePng(Path.Combine(output, item.Key + ".png"));
            result.Artifact.SaveHtml(Path.Combine(output, item.Key + ".html"));
        }
    }
}
