using System;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void MermaidParserParsesC4ElementsBoundariesAndRelationships() {
        const string source = @"C4Context
title System Context
Enterprise_Boundary(bank, ""Bank"") {
  Person(customer, ""Banking Customer"", ""Uses online banking"")
  System(system, ""Internet Banking System"", ""Allows customers to view balances"")
  System_Ext(mail, ""E-mail System"", ""Sends notifications"")
}
Rel(customer, system, ""Uses"")
BiRel(system, mail, ""Sends e-mails"", ""SMTP"")
UpdateElementStyle(system, $fontColor=""#111"")";

        var result = new MermaidParser().ParseC4(source);

        Assert(!result.HasErrors, "Mermaid C4 parser should parse common C4 context syntax: " + MermaidDiagnostics(result));
        Assert(result.Diagnostics.Exists(diagnostic => diagnostic.Severity == MermaidDiagnosticSeverity.Warning && diagnostic.Message.Contains("retained", StringComparison.OrdinalIgnoreCase)), "Mermaid C4 parser should warn for retained style/layout statements.");
        var document = result.Document ?? throw new InvalidOperationException("Mermaid C4 parser should produce a document.");
        Assert(document.Kind == MermaidDiagramKind.C4 && document.DiagramType == "C4Context", "Mermaid C4 parser should preserve C4 diagram kind and header type.");
        Assert(document.Title == "System Context", "Mermaid C4 parser should parse title statements.");
        Assert(document.Boundaries.Count == 1 && document.Boundaries[0].Id == "bank", "Mermaid C4 parser should parse boundary blocks.");
        Assert(document.Elements.Count == 3 && document.Elements[0].BoundaryId == "bank", "Mermaid C4 parser should associate elements with the active boundary.");
        Assert(document.Elements[2].Kind == "system_external", "Mermaid C4 parser should normalize external C4 element kinds.");
        Assert(document.Relationships.Count == 2 && document.Relationships[1].Technology == "SMTP", "Mermaid C4 parser should parse relationship labels and technology.");
        Assert(document.RetainedStatements.Count == 1, "Mermaid C4 parser should retain unsupported update/style statements explicitly.");
    }

    private static void MermaidC4ConvertsToTopologyArtifactAndRenders() {
        const string source = @"C4Container
title Container View
System_Boundary(bank, ""Bank"") {
  Person(customer, ""Customer"", ""Uses banking services"")
  Container(web, ""Web Application"", ""ASP.NET"", ""Provides online banking"")
  ContainerDb(db, ""Database"", ""SQL"", ""Stores accounts"")
}
Rel(customer, web, ""Uses"", ""HTTPS"")
Rel_D(web, db, ""Reads and writes"", ""SQL"")";

        var result = new MermaidParser().ParseC4(source);
        Assert(!result.HasErrors, "Mermaid C4 parser should parse renderable source: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid C4 parser should produce a document.");

        var topology = document.ToTopologyChart(new MermaidTopologyRenderOptions { Id = "c4-container" });
        Assert(topology.Id == "c4-container", "Mermaid C4 conversion should preserve caller-provided ids.");
        Assert(topology.Title == "Container View", "Mermaid C4 conversion should use Mermaid titles by default.");
        Assert(topology.Groups.Count == 1 && topology.Groups[0].Metadata["mermaid.kind"] == "systemboundary", "Mermaid C4 conversion should map boundaries into topology groups.");
        Assert(topology.Nodes.Count == 3 && topology.Nodes[2].Kind == TopologyNodeKind.Database, "Mermaid C4 conversion should map C4 elements into topology nodes.");
        Assert(topology.Edges.Count == 2 && topology.Edges[0].SecondaryLabel == "HTTPS", "Mermaid C4 conversion should map relationship technology to secondary labels.");
        Assert(topology.Edges[1].TargetPort == TopologyEdgePort.Bottom, "Mermaid C4 conversion should preserve direction-specific relationship hints.");

        var artifact = document.ToVisualArtifact(new MermaidTopologyRenderOptions { Id = "c4-container" });
        Assert(artifact.Kind == VisualArtifactKind.Mermaid, "Mermaid C4 visual artifact should report Mermaid artifact kind.");
        Assert(artifact.Model is TopologyChart, "Mermaid C4 visual artifact should carry the topology model.");
        Assert(artifact.Metadata["mermaid.elements"] == "3" && artifact.Metadata["mermaid.relationships"] == "2", "Mermaid C4 artifacts should expose model counts.");
        Assert(artifact.Metadata["render.model"] == nameof(TopologyChart), "Mermaid C4 artifacts should expose the topology render model.");

        var svg = document.ToSvg(new MermaidTopologyRenderOptions { Id = "c4-container" });
        var png = document.ToPng(new MermaidTopologyRenderOptions { Id = "c4-container" });
        Assert(svg.Contains("data-node-id=\"web\"", StringComparison.Ordinal), "Mermaid C4 SVG rendering should include C4 element nodes.");
        Assert(png.Length > 64 && png[0] == 0x89 && png[1] == 0x50 && png[2] == 0x4E && png[3] == 0x47, "Mermaid C4 PNG rendering should emit a valid PNG.");
    }

    private static void MermaidC4RejectsUnknownRelationshipEndpoints() {
        const string source = @"C4Context
Person(customer, ""Customer"")
Rel(customer, missing, ""Uses"")";

        var result = new MermaidParser().ParseC4(source);

        Assert(result.HasErrors, "Mermaid C4 parser should reject relationships that reference unknown elements.");
        Assert(result.Diagnostics.Exists(diagnostic => diagnostic.Message.Contains("target 'missing'", StringComparison.Ordinal)), "Invalid C4 relationships should identify the missing endpoint.");
    }

    private static void MermaidC4PreservesSpriteTagsAndLinks() {
        const string source = @"C4Context
Person(user, ""User"", ""Uses the system"", ""person"", ""external"", ""https://example.test/user"")
Container(api, ""API"", ""ASP.NET"", ""Serves traffic"", ""service"", ""internal"", ""https://example.test/api"")";

        var result = new MermaidParser().ParseC4(source);

        Assert(!result.HasErrors, "Mermaid C4 parser should parse elements with optional sprite, tags, and link arguments: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid C4 parser should produce a document.");
        var person = document.Elements[0];
        var container = document.Elements[1];
        Assert(person.Description == "Uses the system" && person.Sprite == "person" && person.Tags == "external" && person.Link == "https://example.test/user", "Mermaid C4 parser should preserve person sprite, tags, and link metadata.");
        Assert(container.Technology == "ASP.NET" && container.Description == "Serves traffic" && container.Sprite == "service" && container.Tags == "internal" && container.Link == "https://example.test/api", "Mermaid C4 parser should preserve container sprite, tags, and link metadata.");

        var topology = document.ToTopologyChart(new MermaidTopologyRenderOptions { Id = "c4-sprite-metadata" });
        Assert(topology.Nodes[0].Metadata["mermaid.sprite"] == "person" && topology.Nodes[0].Metadata["mermaid.tags"] == "external" && topology.Nodes[0].Metadata["mermaid.link"] == "https://example.test/user", "Mermaid C4 conversion should expose person sprite, tags, and link metadata.");
        Assert(topology.Nodes[1].Metadata["mermaid.sprite"] == "service" && topology.Nodes[1].Metadata["mermaid.tags"] == "internal" && topology.Nodes[1].Metadata["mermaid.link"] == "https://example.test/api", "Mermaid C4 conversion should expose container sprite, tags, and link metadata.");
    }

    private static void MermaidC4PreservesPercentSignsInsideQuotedLabels() {
        const string source = @"C4Context
Person(user, ""99%% user"", ""Completion is 99%% ready"")";

        var result = new MermaidParser().ParseC4(source);

        Assert(!result.HasErrors, "Mermaid C4 parser should not strip percent signs inside quoted arguments: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid C4 parser should produce a document.");
        Assert(document.Elements.Count == 1 && document.Elements[0].Label == "99%% user" && document.Elements[0].Description == "Completion is 99%% ready", "Mermaid C4 parser should preserve quoted percent text as element metadata.");
    }

    private static void MermaidC4ParsesNamedOptionalArguments() {
        const string source = @"C4Context
Person(user, ""User"", ""Uses the system"", $tags=""v1.0"", $link=""https://example.test/user"")
Container(api, ""API"", ""ASP.NET"", ""Serves traffic"", $sprite=""service"", $tags=""internal"", $link=""https://example.test/api"")
Rel(user, api, ""Uses"", $tags=""sync"", $link=""https://example.test/rel"")";

        var result = new MermaidParser().ParseC4(source);

        Assert(!result.HasErrors, "Mermaid C4 parser should parse named optional arguments: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid C4 parser should produce a document.");
        var person = document.Elements[0];
        var container = document.Elements[1];
        var relationship = document.Relationships[0];
        Assert(person.Description == "Uses the system" && person.Sprite == null && person.Tags == "v1.0" && person.Link == "https://example.test/user", "Mermaid C4 parser should map named person tags and links without treating them as sprites.");
        Assert(container.Technology == "ASP.NET" && container.Description == "Serves traffic" && container.Sprite == "service" && container.Tags == "internal" && container.Link == "https://example.test/api", "Mermaid C4 parser should map named container sprite, tags, and link metadata.");
        Assert(relationship.Technology == null && relationship.Tags == "sync" && relationship.Link == "https://example.test/rel", "Mermaid C4 parser should map named relationship tags and links.");
    }
}
