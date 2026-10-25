using System.Threading;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidEventModelingDataTableTests {
    [Fact]
    public void PublicTablesRetainNestedTypedFactsEscapedPathsRawSourceAndDetachedMutableCells() {
        var parsed = new MermaidParser().ParseEventModeling(MermaidEventModelingDepthTests.Source); Assert.False(parsed.HasErrors);
        var document = parsed.Document!; var block = document.DataBlocks.Single(item => item.Id == "Payload");
        var table = block.ToDataTable(); var frameTable = document.ToDataTable("20");
        Assert.Equal(new[] { "field", "type", "value" }, table.Columns.Select(column => column.Id));
        Assert.Equal(9, table.Rows.Count); Assert.Equal(table.Rows.Select(row => row.Key), frameTable.Rows.Select(row => row.Key));
        Assert.Equal(new[] { "/sku", "/quantity", "/customer", "/customer/name", "/tags", "/tags/0", "/tags/1", "/escaped~1key", "/escaped~1key/~0value" }, table.Rows.Select(row => row.Key));
        Assert.Equal(block.Content, table.Metadata["mermaid.data.source"]); Assert.Equal("json", table.Metadata["mermaid.data.type"]);
        Assert.Equal(block.Span.Line.ToString(), table.Metadata["mermaid.source.line"]);
        Assert.Equal("Number", table.Rows.Single(row => row.Key == "/quantity").Cells[2].Metadata["mermaid.data.kind"]);
        Assert.Equal("Object", table.Rows.Single(row => row.Key == "/customer").Cells[1].DisplayText);
        Assert.Equal("Array", table.Rows.Single(row => row.Key == "/tags").Cells[1].DisplayText);
        Assert.Equal("", table.Rows.Single(row => row.Key == "/escaped~1key/~0value").Cells[2].DisplayText);
        table.ConfigureRow(0, row => row.Cells[2].DisplayText = "Edited detached table");
        Assert.Equal("ABC", block.ToDataTable().Rows[0].Cells[2].DisplayText); Assert.Equal("ABC", frameTable.Rows[0].Cells[2].DisplayText);
        document.Frames.Single(frame => frame.Number == "10").InlineData = "sku: changed, quantity: 2";
        var edited = document.ToDataTable("10"); Assert.Equal("changed", edited.Rows[0].Cells[2].DisplayText);
        Assert.Equal("2", edited.Rows[1].Cells[2].DisplayText); Assert.Equal(MermaidEventModelingDepthTests.Source, document.SourceText);
        Assert.Empty(document.ToDataTable("30").Rows); Assert.Throws<ArgumentException>(() => document.ToDataTable("missing"));
    }

    [Fact]
    public void ScalarKindsDistinguishNullEmptyFalseNumbersAndUninterpretedSchemaWords() {
        const string source = "eventmodeling\ntf 1 cmd Example `json`{\"number\":1e2,\"false\":false,\"null\":null,\"empty\":\"\",\"nested\":{},\"array\":[]}\ntf 2 evt Happened { type: number, active: true }";
        var parsed = new MermaidParser().ParseEventModeling(source); Assert.False(parsed.HasErrors);
        var first = parsed.Document!.ToDataTable("1");
        Assert.Equal(new[] { "Number", "Boolean", "Null", "String", "Object", "Array" }, first.Rows.Select(row => row.Cells[1].DisplayText));
        Assert.Equal(new[] { "1e2", "false", "null", "", "{0 fields}", "[0 items]" }, first.Rows.Select(row => row.Cells[2].DisplayText));
        var schema = parsed.Document!.ToDataTable("2"); Assert.Equal("String", schema.Rows[0].Cells[1].DisplayText); Assert.Equal("number", schema.Rows[0].Cells[2].DisplayText);
        Assert.Equal("Boolean", schema.Rows[1].Cells[1].DisplayText);
    }

    [Fact]
    public void ExistingCompleteObjectAndArrayBodyFormsKeepTheirTypedNestedValues() {
        const string source = "eventmodeling\ntf 1 ui Screen [[ObjectBody]]\ndata ObjectBody `json`{ { \"name\": { \"first\": \"Alex\" } } }\ndata ArrayBody `json`{ [1,null,\"\"] }";
        var parsed = new MermaidParser().ParseEventModeling(source); Assert.False(parsed.HasErrors);
        var document = parsed.Document!;
        Assert.Equal(new[] { "/name", "/name/first" }, document.ToDataTable("1").Rows.Select(row => row.Key));
        var array = document.DataBlocks.Single(block => block.Id == "ArrayBody").ToDataTable();
        Assert.Equal(new[] { "/", "/0", "/1", "/2" }, array.Rows.Select(row => row.Key));
        Assert.Equal(new[] { "Array", "Number", "Null", "String" }, array.Rows.Select(row => row.Cells[1].DisplayText));
        Assert.Equal("", array.Rows[3].Cells[2].DisplayText);
    }

    [Theory]
    [InlineData("html", "<script>globalThis.eventModelingExecuted=1</script>")]
    [InlineData("uri", "https://example.invalid/private")]
    [InlineData("figma", "asset://design")]
    [InlineData("salt", "server [[asset]]")]
    [InlineData("md", "**Caption**")]
    public void UnsupportedTypedBodiesRemainVisibleLiteralFactsWithLocatedDiagnostics(string type, string body) {
        var source = "eventmodeling\ntf 1 ui Screen `" + type + "`{" + body + "}";
        var rendered = MermaidRenderer.Render(source); Assert.False(rendered.HasErrors); Assert.NotNull(rendered.Artifact);
        Assert.Contains(rendered.Diagnostics, item => item.Code == MermaidDiagnosticCodes.UnsupportedStatement && item.Span.Line == 2);
        var document = Assert.IsType<MermaidEventModelingDocument>(rendered.Document); var row = Assert.Single(document.ToDataTable("1").Rows);
        Assert.Equal("Literal", row.Cells[1].DisplayText); Assert.Equal(body, row.Cells[2].DisplayText);
        Assert.Equal(body, Assert.Single(Assert.IsType<TopologyChart>(rendered.Artifact!.Model).Nodes).Metadata["mermaid.inlineData"]);
        var svg = rendered.Artifact.ToSvg(); Assert.DoesNotContain("<script", svg, StringComparison.OrdinalIgnoreCase); Assert.DoesNotContain("<image", svg, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PrototypeShapedSchemaKeysUseDiagnosedLiteralFallbackWhileStrictJsonKeepsAllDataKeys() {
        const string schema = "eventmodeling\ntf 1 ui Screen { '\\u0063onstructor': string }";
        const string literalKey = "eventmodeling\ntf 1 ui Screen { constructor: string, safe: 3 }";
        var fallback = MermaidRenderer.Render(literalKey); Assert.False(fallback.HasErrors);
        Assert.Contains(fallback.Diagnostics, item => item.Code == MermaidDiagnosticCodes.UnsupportedStatement);
        var frame = Assert.IsType<MermaidEventModelingDocument>(fallback.Document);
        Assert.Equal("Literal", Assert.Single(frame.ToDataTable("1").Rows).Cells[1].DisplayText);
        Assert.Contains("constructor", frame.ToDataTable("1").Rows[0].Cells[2].DisplayText);
        var json = new MermaidParser().ParseEventModeling("eventmodeling\ntf 1 ui Screen `json`{\"constructor\":{\"prototype\":true},\"__proto__\":null}"); Assert.False(json.HasErrors);
        Assert.Equal(new[] { "/constructor", "/constructor/prototype", "/__proto__" }, json.Document!.ToDataTable("1").Rows.Select(row => row.Key));
        // A quoted escaped key takes the same guard path after canonical JSON scalar decoding.
        var escaped = MermaidRenderer.Render(schema.Replace("'\\u0063onstructor'", "\"\\u0063onstructor\"")); Assert.False(escaped.HasErrors);
        Assert.Contains(escaped.Diagnostics, item => item.Code == MermaidDiagnosticCodes.UnsupportedStatement);
    }

    [Fact]
    public void CapturedLimitsBoundExpandedRowsBeforeConstructionAndRemainDetachedFromLaterHostChanges() {
        const string source = "eventmodeling\ntf 1 ui Screen [[Payload]]\ndata Payload `json`{\"a\":1,\"b\":2,\"c\":3}";
        var limits = new MermaidResourceLimits { MaximumElements = 10 };
        var parsed = new MermaidParser().Parse(source, new MermaidParseOptions { ResourceLimits = limits }); Assert.False(parsed.HasErrors);
        limits.MaximumElements = 5000;
        var document = Assert.IsType<MermaidEventModelingDocument>(parsed.Document);
        var failed = MermaidRenderer.RenderParsed(parsed); Assert.True(failed.HasErrors); Assert.Null(failed.Artifact);
        Assert.Contains(failed.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.ResourceLimitExceeded && diagnostic.Span.Line >= 2);
        Assert.Throws<MermaidResourceLimitException>(() => document.ToTopologyChart()); Assert.Equal(3, document.DataBlocks.Single().ToDataTable().Rows.Count);
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => MermaidRenderer.Render(source, null, cancellation.Token));
        var bounded = MermaidRenderer.Render("eventmodeling\ntf 1 ui Screen", new MermaidRenderOptions { ParseOptions = new MermaidParseOptions { ResourceLimits = new MermaidResourceLimits { MaximumOutputBytes = 32, MaximumRasterPixels = 1 } } });
        Assert.False(bounded.HasErrors); Assert.Throws<MermaidResourceLimitException>(() => bounded.Artifact!.ToSvg()); Assert.Throws<MermaidResourceLimitException>(() => bounded.Artifact!.ToPng());
    }

    [Fact]
    public void MalformedProgrammaticCollectionsAndDataRejectWithoutMutatingTheSource() {
        var document = new MermaidParser().ParseEventModeling("eventmodeling\ntf 1 ui Screen").Document!; var frame = document.Frames[0];
        document.Frames.Add(frame); Assert.Throws<ArgumentException>(() => document.ToTopologyChart()); Assert.Equal(2, document.Frames.Count); document.Frames.RemoveAt(1);
        document.Relations.Add(new MermaidEventModelingRelation("1", "missing", false, frame.Span)); Assert.Throws<ArgumentException>(() => document.ToTopologyChart()); document.Relations.Clear();
        frame.InlineData = "\"n\": 1e999"; frame.DataType = "json"; Assert.Throws<ArgumentException>(() => document.ToDataTable("1")); Assert.Equal("\"n\": 1e999", frame.InlineData);
        document.DataBlocks.Add(null!); Assert.Throws<ArgumentException>(() => document.ToDataTable("1"));
        var invalid = new MermaidEventModelingDataBlock("invalid id", null, "a: 1", frame.Span);
        Assert.Throws<ArgumentException>(() => invalid.ToDataTable());
        var oversized = new MermaidEventModelingDataBlock("Large", "text", new string('x', new MermaidResourceLimits().MaximumSourceCharacters + 1), frame.Span);
        Assert.Throws<MermaidResourceLimitException>(() => oversized.ToDataTable());
    }
}
