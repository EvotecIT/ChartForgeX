using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;

internal static partial class Program {
    private static void NativeDiagramContracts() {
        NestedSwimlaneContracts();
        FlowchartMembershipContracts();
        NativeShapeCatalogContracts();
        GraphStyleSettingsContracts();
        FlowchartCurveContracts();
        FlowchartAssetContracts();
        NativeSurfacePaintContracts();
        const string state = "stateDiagram-v2\nclassDef marked fill:#fef9c3,stroke:#92400e,color:#78350f\nstate Active {\n[*] --> A:::marked\nA --> [*]\n--\n[*] --> B\nB --> [*]\n}\nActive --> Done";
        var stateArtifact = PackedNativeDiagram("state-regions", state);
        var stateModel = (TopologyChart)stateArtifact.Model!;
        PackageAssertions.Require(stateModel.Groups.Count(group => group.Presentation == TopologyGroupPresentation.Compartment) == 2
            && stateModel.Nodes.Single(node => node.Id == "A").BackgroundColor == "#fef9c3"
            && stateModel.Nodes.Count(node => node.Metadata.TryGetValue("mermaid.kind", out var kind) && (kind == "start" || kind == "end")) == 4,
            "Packed State lost independent regional controls or shared safe class paints.");
        var stateJson = VisualArtifactInterchangeEnvelope.FromJson(stateArtifact.ToInterchangeJson());
        PackageAssertions.Require(stateJson.Groups.Count(group => group.Topology?.Presentation == TopologyGroupPresentation.Compartment) == 2,
            "Packed State interchange lost its generic native compartment presentation.");

        const string classes = "classDiagram\nclass Store~Item~ {\n+lookup(int id) Item*\n+int count$\n}\nclass User\nStore --> User";
        var classModel = (TopologyChart)PackedNativeDiagram("class-members", classes).Model!;
        var store = classModel.Nodes.Single(node => node.Id == "Store");
        PackageAssertions.Require(store.Label == "Store<Item>" && store.Details.Any(detail => detail.TextStyle?.Italic == true)
            && store.Details.Any(detail => detail.TextStyle?.Underline == true), "Packed Class generics or abstract/static member typography was lost.");
        const string entities = "erDiagram\nclassDef marked fill:#fef9c3,color:#7c2d12\nC[\"Customer account\"]:::marked {\nstring id PK, FK \"External key\"\nstring? display_name\n}\nO[Orders] {\nstring id PK\n}\nC ||--o{ O : places";
        var entityModel = (TopologyChart)PackedNativeDiagram("er-fields", entities).Model!;
        var customer = entityModel.Nodes.Single(node => node.Id == "C");
        PackageAssertions.Require(customer.Label == "Customer account" && customer.BackgroundColor == "#fef9c3"
            && customer.Details.Any(detail => (detail.Text ?? "").Contains("PK", StringComparison.Ordinal) && (detail.Text ?? "").Contains("FK", StringComparison.Ordinal))
            && entityModel.Edges.Count == 1, "Packed ER aliases, typed keys or native paint was lost.");

        // Topology's global text role is textColor; primaryTextColor has a separate source-preset role.
        const string themed = "%%{init: {'theme':'dark','themeVariables':{'background':'#112233','primaryColor':'#334455','textColor':'#FFEEDD','lineColor':'#CCDDEE'}}}%%\nflowchart LR\nA[Start] --> B[Finish]";
        var theme = ((TopologyChart)PackedNativeDiagram("theme-roles", themed).Model!).Theme.Resolve(VisualThemeMode.Dark);
        PackageAssertions.Require(theme.Background.Equals(ChartColor.FromHex("#112233")) && theme.Surface.Equals(ChartColor.FromHex("#334455"))
            && theme.Foreground.Equals(ChartColor.FromHex("#FFEEDD")) && theme.Status.Neutral.Fill.Equals(ChartColor.FromHex("#CCDDEE")),
            "Packed source theme variables did not use the existing immutable native color roles.");

        const string agentflow = "agentflow-beta LR\nflow review[\"Review\"]\nrequest[\"Request\"]@{ shape: input }\nwork[\"Work\"]@{ shape: tool }\nrequest --> work\nend";
        var agent = (TopologyChart)PackedNativeDiagram("agentflow", agentflow).Model!;
        PackageAssertions.Require(agent.Groups.Any(group => group.Id == "review") && agent.Nodes.Count == 2 && agent.Edges.Count == 1
            && agent.Nodes.All(node => node.GroupId == "review"), "Packed Agentflow rendered without preserving its actual native flow parentage.");
        const string zenuml = "zenuml\n@Actor User\n@Control Service\n@Starter(User)\nService.checkout() {\nreturn accepted\n}";
        var zen = (SequenceArtifact)PackedNativeDiagram("zenuml", zenuml).Model!;
        PackageAssertions.Require(zen.Participants.Any(item => item.Id == "User") && zen.Participants.Any(item => item.Id == "Service")
            && zen.Messages.Count >= 2, "Packed ZenUML omitted authored participant occurrences, call or return.");

        const string timeline = "timeline\nsection Discovery\nBefore launch: Interview users\n : Review findings\nsection Delivery\nLaunch: Ship product : Invite users";
        var time = (TimelineDiagramBlock)PackedNativeDiagram("timeline", timeline).Model!;
        PackageAssertions.Require(time.Periods.Select(period => period.Label).SequenceEqual(new[] { "Before launch", "Launch" })
            && time.Periods[0].Events.SequenceEqual(new[] { "Interview users", "Review findings" }) && time.Sections.Count == 2,
            "Packed categorical Timeline lost ordered period/events or invented a numeric calendar duration.");
        const string venn = "venn-beta\nset A[First]: 100\nset B[Second]: 60\nunion A,B[Shared]: 25";
        var layout = ((VennDiagramBlock)PackedNativeDiagram("venn", venn).Model!).MeasureLayout();
        var overlap = layout.Regions.Single();
        PackageAssertions.Require(layout.IsAreaAccurate && Math.Abs(overlap.RequestedArea - 25) < 1e-7
            && Math.Abs(overlap.ActualArea - overlap.RequestedArea) < .0001 && overlap.Path.Length > 0
            && Math.Abs(layout.Circles[0].Radius * layout.Circles[0].Radius / (layout.Circles[1].Radius * layout.Circles[1].Radius) - 100d / 60) < 1e-7,
            "Packed two-set Venn painted false overlap geometry or lost requested set-area ratios.");
        const string c4 = "C4Dynamic\nEnterprise_Boundary(company, \"Company\") {\nSystem_Boundary(shop, \"Store\") {\nContainer(web, \"Web\", \"Browser\")\nContainerDb(db, \"Orders\", \"SQL\")\n}\n}\nRel_D(web, db, \"Writes\")\nUpdateElementStyle(web, $bgColor=\"#123456\")\nUpdateRelStyle(web, db, $textColor=\"#ccbb99\")";
        var c4Model = (TopologyChart)PackedNativeDiagram("c4", c4, strict: false).Model!;
        PackageAssertions.Require(c4Model.Groups.Single(group => group.Id == "shop").ParentGroupId == "company"
            && c4Model.Nodes.Single(node => node.Id == "web").GroupId == "shop"
            && c4Model.Nodes.Single(node => node.Id == "web").BackgroundColor == "#123456"
            && c4Model.Edges.Single().Label == "1: Writes" && c4Model.Edges[0].LabelColor == "#ccbb99",
            "Packed C4 lost real nested shells, dynamic ordering or native relationship text paint.");
        Console.WriteLine("Packed native State/Class/ER/theme/Agentflow/ZenUML/Timeline/Venn/C4 public contracts passed.");
    }

    private static VisualArtifact PackedNativeDiagram(string name, string source, bool strict = true) {
        var options = new MermaidRenderOptions { WarningPolicy = strict ? MermaidWarningPolicy.Reject : MermaidWarningPolicy.Allow };
        var direct = RenderFixture(source, options);
        var fenced = CreateFixtureParser(options).Parse("```mermaid\n" + source + "\n```");
        PackageAssertions.Require(!direct.HasErrors && direct.Artifact != null && !fenced.HasErrors && fenced.Artifacts.Count == 1,
            "Packed native diagram failed direct or Markdown rendering: " + name + "; " + string.Join("; ", direct.Diagnostics.Select(item => item.Message)));
        PackageAssertions.Require(direct.Diagnostics.All(item => !strict && item.Code == MermaidDiagnosticCodes.ApproximateMapping),
            "Packed native diagram produced an unaccounted warning: " + name);
        var artifact = direct.Artifact!; var svg = artifact.ToSvg(); var png = artifact.ToPng();
        PackageAssertions.Require(svg == fenced.Artifacts[0].ToSvg() && png.SequenceEqual(fenced.Artifacts[0].ToPng()),
            "Packed direct and Markdown native exports differ: " + name);
        PackageAssertions.Require(!svg.Contains("<script", StringComparison.OrdinalIgnoreCase) && !svg.Contains("foreignObject", StringComparison.Ordinal),
            "Packed diagram used an active or foreign fallback surface: " + name);
        PackageAssertions.Png(png);
        var json = artifact.ToInterchangeJson();
        PackageAssertions.Require(VisualArtifactInterchangeEnvelope.FromJson(json).ToJson() == json,
            "Packed diagram common interchange envelope did not round trip: " + name);
        // VisualBlocks use the common envelope with SVG fallback; this does not assert typed reconstruction.
        var output = Environment.GetEnvironmentVariable("CFX_CONSUMER_OUTPUT_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            if (!Path.IsPathRooted(output)) throw new InvalidOperationException("CFX_CONSUMER_OUTPUT_DIRECTORY must be absolute.");
            output = Path.Combine(output, "net" + Environment.Version.Major, "native-diagrams"); Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output, name + ".svg"), svg); File.WriteAllBytes(Path.Combine(output, name + ".png"), png);
            File.WriteAllText(Path.Combine(output, name + ".json"), json);
        }
        return artifact;
    }
}
