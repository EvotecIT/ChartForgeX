using System.Text.Json;
using System.Text.Json.Nodes;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Shares source-authored semantic expectations with the independently pinned upstream lanes.</summary>
public sealed class MermaidNotationReferenceCorpusTests {
    [Theory]
    [InlineData("class-native-notes-generics")]
    [InlineData("class-native-members-namespaces")]
    [InlineData("class-two-way")]
    [InlineData("state-controls-notes")]
    [InlineData("state-nested-directions")]
    [InlineData("state-native-safe-styles")]
    [InlineData("er-alias-attributes")]
    public void NativeNotationMatchesTheSharedReferenceFacts(string name) {
        var path = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures", name);
        using var expected = JsonDocument.Parse(File.ReadAllText(path + ".expected.json"));
        var parsed = new MermaidParser().Parse(File.ReadAllText(path + ".mmd"));
        Assert.Empty(parsed.Diagnostics);
        object facts;
        string key;
        TopologyChart? captured = null;
        switch (parsed.Document) {
            case MermaidClassDocument document:
                var chart = document.ToTopologyChart();
                captured = chart;
                key = "classFacts";
                facts = new {
                    nodes = document.Classes.Select(item => {
                        var node = chart.Nodes.Single(candidate => candidate.Id == item.Id);
                        return new object[] { item.Id, node.Label,
                            item.Members.Select((member, index) => (member, index)).Where(pair => !pair.member.IsMethod).Select(pair => node.Details[pair.index].Text).ToArray(),
                            item.Members.Select((member, index) => (member, index)).Where(pair => pair.member.IsMethod).Select(pair => node.Details[pair.index].Text).ToArray() };
                    }).ToArray(),
                    relationships = chart.Edges.Select(item => new[] { item.SourceNodeId, item.TargetNodeId, item.SourceMarker.ToString(), item.TargetMarker.ToString(),
                        item.LineStyle.ToString(), item.Label ?? "", item.SourceLabel ?? "", item.TargetLabel ?? "" }).ToArray(),
                    notes = document.Notes.Select(note => new object?[] { note.NodeId, note.Text }).ToArray()
                };
                break;
            case MermaidStateDocument document:
                key = "stateFacts";
                captured = document.ToTopologyChart();
                var directions = new Dictionary<string, string>();
                if (document.Direction != null) directions["root"] = document.Direction;
                foreach (var state in document.States) if (state.Direction != null) directions[state.Id] = state.Direction;
                var states = document.States.ToDictionary(state => state.Id);
                string Endpoint(string id) => states[id].Kind is "start" or "end" ? "[*]" : id;
                facts = new {
                    states = document.States.Where(state => state.Kind is not ("start" or "end")).Select(state => new object?[] { state.Id, state.Kind ?? "state", state.ParentId }).ToArray(),
                    edges = document.Transitions.Select(edge => new[] { Endpoint(edge.SourceId), Endpoint(edge.TargetId), edge.Label ?? "" }).ToArray(),
                    notes = document.Notes.Select(note => new[] { note.NodeId, note.LeftOfNode ? "Left" : "Right", note.Text }).ToArray(), directions
                };
                break;
            case MermaidEntityRelationshipDocument document:
                key = "erFacts";
                var er = document.ToTopologyChart();
                captured = er;
                facts = new {
                    entities = document.Entities.Select(item => new object[] { item.Id, item.Label ?? item.Id,
                        item.Attributes.Select(attribute => new object[] { attribute.Type, attribute.Name,
                            (attribute.Key ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries), attribute.Comment ?? "" }).ToArray() }).ToArray(),
                    relationships = er.Edges.Select(item => new[] { item.SourceNodeId, item.TargetNodeId, item.SourceMarker.ToString(), item.TargetMarker.ToString(), item.LineStyle.ToString(), item.Label ?? "" }).ToArray()
                };
                break;
            default: throw new InvalidOperationException("The fixture must use one of the common native notation owners.");
        }
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(expected.RootElement.GetProperty(key).GetRawText()), JsonSerializer.SerializeToNode(facts)),
            "The native semantic facts must match the shared source-authored contract: " + JsonSerializer.Serialize(facts));
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (captured != null && !string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output, "reference-" + name + ".native.svg"), captured.ToSvg());
            File.WriteAllBytes(Path.Combine(output, "reference-" + name + ".native.png"), captured.ToPng());
        }
    }

    [Fact]
    public void SharedLollipopFixtureMapsItsInterfaceToADistinctNativeEndpoint() {
        var path = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures", "class-lollipop-retained");
        var parsed = new MermaidParser().ParseClass(File.ReadAllText(path + ".mmd"));
        using var expected = JsonDocument.Parse(File.ReadAllText(path + ".expected.json"));
        Assert.False(parsed.HasErrors);
        Assert.Empty(parsed.Diagnostics);
        var chart = parsed.Document!.ToTopologyChart();
        var endpoint = Assert.Single(chart.Nodes, item => item.Id != "Service");
        Assert.Equal(TopologyNodeShape.Circle, endpoint.Shape);
        Assert.Equal("Interface", endpoint.Metadata["mermaid.id"]);
        Assert.Equal(endpoint.Id, Assert.Single(chart.Edges).TargetNodeId);
        Assert.Equal(expected.RootElement.GetProperty("nativeClasses").EnumerateArray().Select(item => item.GetString()), parsed.Document!.Classes.Select(item => item.Id));
        Assert.Equal(expected.RootElement.GetProperty("nativeRetainedStatements").EnumerateArray().Select(item => item.GetString()), parsed.Document.RawStatements.Select(item => item.Text));
    }
}
