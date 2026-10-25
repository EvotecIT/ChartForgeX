using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidKanbanNativeBoardTests {
    [Fact]
    public void TypedFieldsShareTheCanonicalDictionaryWhileSourceFactsRemainAuthored() {
        var parsed = new MermaidParser().ParseKanban(Source); Assert.False(parsed.HasErrors);
        var document = parsed.Document!; var column = document.Columns[0]; var task = column.Tasks[0];
        Assert.Equal(new[] { "todo", "doing", "waiting", "done" }, document.Columns.Select(item => item.Id));
        Assert.Equal(("Delivery team", "BOARD-1", "**Plan**<br>next"), (column.Assigned, column.Ticket, column.Label));
        Assert.Equal(("Alex Morgan", "CFX/7 ?", "High"), (task.Assigned, task.Ticket, task.Priority));
        Assert.Equal("2 points", task.Metadata["estimate"]); Assert.Contains("Alex Morgan", task.MetadataSource);
        var raw = task.MetadataSource; task.Assigned = "Casey Avery"; task.Priority = null; task.Metadata["TICKET"] = "EDIT-1";
        Assert.Equal("Casey Avery", task.Metadata["assigned"]); Assert.False(task.Metadata.ContainsKey("priority")); Assert.Equal("EDIT-1", task.Ticket);
        Assert.Equal(raw, task.MetadataSource); Assert.Equal(Source, document.SourceText);
        var chart = document.ToTopologyChart(); var node = chart.Nodes.Single(item => item.Id == "review");
        Assert.Contains(node.Details, detail => detail.Text == "Assigned: Casey Avery");
        Assert.Contains(node.Details, detail => detail.Text == "Ticket: EDIT-1");
        Assert.DoesNotContain(node.Details, detail => detail.Text!.StartsWith("Priority:", StringComparison.Ordinal));
        Assert.Equal("2 points", node.Metadata["mermaid.meta.estimate"]); Assert.Equal(raw, node.Metadata["mermaid.metadata.source"]);
        Assert.Equal("https://tickets.example.invalid/item/EDIT-1", node.Href);
    }

    [Theory]
    [InlineData(false, 390)]
    [InlineData(true, 390)]
    [InlineData(false, 1200)]
    [InlineData(true, 1200)]
    public void SourceBoardKeepsOrderedFullFactsAndDisjointMeasuredCardsInDirectFenceSvgPngAndHtml(bool dark, int width) {
        var options = Options(dark, width); var direct = MermaidRenderer.Render(Source, options);
        Assert.False(direct.HasErrors); Assert.Contains(direct.Diagnostics, item => item.Code == MermaidDiagnosticCodes.UnsupportedStatement);
        Assert.All(direct.Diagnostics, item => Assert.True(item.Span.Line > 1 && item.Span.Column > 0 && item.Span.Length > 0));
        var artifact = direct.Artifact!; var chart = Assert.IsType<TopologyChart>(artifact.Model);
        Assert.Equal(6, chart.Nodes.Count); Assert.Empty(chart.Edges);
        Assert.All(chart.Groups, group => Assert.Equal(TopologyGroupLayoutPolicy.VerticalStack, group.LayoutPolicy));
        Assert.All(chart.Nodes, node => { Assert.Equal(TopologyHealthStatus.Unknown, node.Status); Assert.Null(node.Symbol); Assert.Null(node.IconId); });
        var prepared = chart.Prepare(); AssertBoard(prepared.ToInterchangeEnvelope());
        var svg = artifact.ToSvg(); var png = artifact.ToPng(); var xml = XDocument.Parse(svg);
        var allText = string.Join("\n", xml.Descendants().Where(item => item.Name.LocalName == "text").Select(item => item.Value));
        foreach (var expected in new[] { "Plan", "next", "Review", "the request", "Assigned: Alex Morgan", "Ticket: CFX/7 ?", "Priority: High", "estimate: 2 points", "icon: fa:bug", "0 tasks" })
            Assert.Contains(expected, allText, StringComparison.Ordinal);
        Assert.DoesNotContain(xml.Descendants(), item => item.Name.LocalName is "script" or "foreignObject" or "image");
        Assert.Contains(xml.Descendants().Attributes(), attr => attr.Name.LocalName == "href" && attr.Value == "https://tickets.example.invalid/item/CFX%2F7%20%3F");
        var fence = new MermaidVisualMarkupParser(options).Parse("Before\n\n```mermaid\n" + Source + "\n```");
        Assert.False(fence.HasErrors); Assert.Equal(svg, Assert.Single(fence.Artifacts).ToSvg()); Assert.Equal(png, fence.Artifacts[0].ToPng());
        var image = RasterImageDecoder.Decode(png); Assert.True(image.Width >= width && image.Height > 0);
        var html = artifact.ToHtmlPage(new VisualArtifactRenderOptions { HtmlSizing = VisualArtifactHtmlSizing.PreserveSize });
        Assert.Contains("data-cfx-html-sizing=\"preserve-size\"", html, StringComparison.Ordinal); Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
        var json = artifact.ToInterchangeJson(); var saved = VisualArtifactInterchangeEnvelope.FromJson(json);
        Assert.Equal(saved.ToJson(), VisualArtifactInterchangeEnvelope.FromUtf8Json(saved.ToUtf8Json()).ToJson());
        Assert.Equal("Delivery board with authored task facts", saved.AccessibleName);
        Assert.Equal("**Plan**<br>next", saved.Groups.Single(group => group.Id == "todo").Extensions["mermaid.meta.label"]);
        Assert.Equal("Plan the delivery", saved.Groups.Single(group => group.Id == "todo").Extensions["mermaid.source.label"]);
        Assert.Equal("0", saved.Nodes.Single(node => node.Id == "review").Extensions["mermaid.order"]);
        Assert.Equal("todo", saved.Nodes.Single(node => node.Id == "review").Extensions["mermaid.column"]);
        Assert.All(saved.Groups, group => Assert.Equal(TopologyGroupLayoutPolicy.VerticalStack, group.Topology!.LayoutPolicy));
        ScatterBubbleColorRenderingTests.Capture(prepared.Visual, "kanban-board-" + width + (dark ? "-dark" : "-light"));
    }

    [Fact]
    public void NativeEditsReexportWithoutMutatingSourceOrDetachedInterchangeSnapshots() {
        var result = MermaidRenderer.Render(Source, Options(false, 1200)); var artifact = result.Artifact!;
        var old = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson()); var json = old.ToJson();
        var svg = artifact.ToSvg(); var png = artifact.ToPng();
        var chart = Assert.IsType<TopologyChart>(artifact.Model); var task = chart.Nodes.Single(node => node.Id == "review");
        task.Label = "Native edited title"; task.Details[0].Text = "Assigned: Native owner";
        task.Href = "https://tickets.example.invalid/native";
        Assert.NotEqual(svg, artifact.ToSvg()); Assert.False(png.SequenceEqual(artifact.ToPng()));
        var edited = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson()).Nodes.Single(node => node.Id == "review");
        Assert.Equal("Native edited title", edited.Label); Assert.Equal("Assigned: Native owner", edited.Details[0].Text);
        Assert.Equal("Review the request", edited.Extensions["mermaid.source.label"]);
        Assert.Equal("Alex Morgan", edited.Extensions["mermaid.meta.assigned"]);
        Assert.Equal("https://tickets.example.invalid/native", edited.Href);
        Assert.Equal(json, old.ToJson()); Assert.Equal(Source, Assert.IsType<MermaidKanbanDocument>(result.Document).SourceText);
    }

    [Theory]
    [InlineData("todo[A]\n  todo[Task]")]
    [InlineData("todo[A]\n  same[Task]\nother[B]\n  same[Again]")]
    [InlineData("bad id[Title]")]
    [InlineData("todo[Title")]
    [InlineData("todo[Title] extra")]
    [InlineData("todo[A]\n  task[B] @{assigned: 'One', ASSIGNED: 'Two'}")]
    [InlineData("todo[A]\n  task[B] @{assigned: [One,Two]}")]
    [InlineData("todo[A]\n  task[B] @{assigned: 'Unclosed}")]
    [InlineData("todo[A]\n  task[B] @{assigned: 'One',}")]
    [InlineData("todo[A]\n  task[B]\n    nested[C]")]
    public void MalformedDeclarationsAreLocatedAndProduceNoPartialDirectOrFenceArtifact(string body) {
        var source = "kanban\n" + body; var rendered = MermaidRenderer.Render(source);
        Assert.True(rendered.HasErrors); Assert.Null(rendered.Artifact);
        Assert.Contains(rendered.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidStatement && item.Span.Line >= 2 && item.Span.Column > 0 && item.Span.Length > 0);
        var fence = new MermaidVisualMarkupParser().Parse("Intro\n\n```mermaid\n" + source + "\n```");
        Assert.True(fence.HasErrors); Assert.Empty(fence.Artifacts);
        Assert.Contains(fence.Diagnostics, item => item.Line >= 5);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/plain,x")]
    [InlineData("//example.invalid/#TICKET#")]
    [InlineData("/relative/#TICKET#")]
    public void UnsafeTicketTemplatesAreRejectedThroughTheConfigurationOwner(string template) {
        var source = "---\nconfig:\n  kanban:\n    ticketBaseUrl: '" + template + "'\n---\nkanban\ntodo[A]\n  task[B] @{ticket: 'A-1'}";
        var rendered = MermaidRenderer.Render(source); Assert.True(rendered.HasErrors); Assert.Null(rendered.Artifact);
        Assert.Contains(rendered.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidConfiguration && item.Span.Line == 4);
        Assert.Empty(new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```").Artifacts);
    }

    [Fact]
    public void QuotedScalarsAndLegacyEqualsRemainAtomicAndExternalFactsStayInert() {
        const string source = "kanban\ntodo['Literal [title]'] @{label: 'O''Brien'}\n  task[Title] @{ticket=\"https://literal.invalid/a:b\", assigned= 'One, Two', icon: 'fa:bug'}";
        var result = MermaidRenderer.Render(source); Assert.False(result.HasErrors);
        Assert.Contains(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.ApproximateMapping);
        Assert.Contains(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.UnsupportedStatement);
        var document = Assert.IsType<MermaidKanbanDocument>(result.Document); var task = document.Columns[0].Tasks[0];
        Assert.Equal("https://literal.invalid/a:b", task.Ticket); Assert.Equal("One, Two", task.Assigned); Assert.Equal("O'Brien", document.Columns[0].Label);
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model); Assert.Null(chart.Nodes[0].Href); Assert.Null(chart.Nodes[0].IconId);
        Assert.Contains(chart.Nodes[0].Details, detail => detail.Text == "icon: fa:bug");
        Assert.Null(MermaidRenderer.Render(source, new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject }).Artifact);
    }

    [Fact]
    public void TypedInvalidStateAndCapturedLimitsCannotEscapeThroughNativeConversionOrDeferredExports() {
        var parsed = new MermaidParser().ParseKanban("kanban\ntodo[A]\n  task[B] @{assigned: 'One', ticket: 'T-1'}");
        var document = parsed.Document!; var task = document.Columns[0].Tasks[0];
        Assert.Throws<ArgumentException>(() => task.Label = " "); Assert.Throws<ArgumentException>(() => task.Assigned = new string('x', 4097));
        task.Metadata["label"] = " "; Assert.Throws<ArgumentException>(() => document.ToTopologyChart()); task.Metadata.Remove("label");
        document.Columns.Add(new MermaidKanbanColumn("task", "Collision", new MermaidSourceSpan(4, 1, 9)));
        Assert.Throws<ArgumentException>(() => document.ToVisualArtifact()); document.Columns.RemoveAt(1);
        var limits = new MermaidResourceLimits { MaximumElements = 3 };
        var options = new MermaidRenderOptions { ParseOptions = new MermaidParseOptions { ResourceLimits = limits } };
        var rejected = MermaidRenderer.Render(document.SourceText, options); Assert.Null(rejected.Artifact);
        Assert.Contains(rejected.Diagnostics, item => item.Code == MermaidDiagnosticCodes.ResourceLimitExceeded && item.Span.Line == 3);
        limits.MaximumElements = 4; limits.MaximumOutputBytes = 32; limits.MaximumRasterPixels = 1;
        var bounded = MermaidRenderer.Render(document.SourceText, options); Assert.False(bounded.HasErrors);
        limits.MaximumOutputBytes = 1_000_000; limits.MaximumRasterPixels = 1_000_000;
        Assert.Equal(nameof(limits.MaximumOutputBytes), Assert.Throws<MermaidResourceLimitException>(() => bounded.Artifact!.ToSvg()).LimitName);
        Assert.Equal(nameof(limits.MaximumRasterPixels), Assert.Throws<MermaidResourceLimitException>(() => bounded.Artifact!.ToPng()).LimitName);
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => new MermaidParser().Parse(document.SourceText, null, cancellation.Token));
    }

    [Theory]
    [InlineData("identity")]
    [InlineData("value")]
    [InlineData("fields")]
    [InlineData("aggregate")]
    public void WireAndScalarBudgetsRejectOversizedDeclarationsWithSourceLocations(string boundary) {
        var declaration = boundary == "identity" ? new string('i', 513) + "[Too long]"
            : "task[Task] @{" + (boundary == "value" ? "assigned: '" + new string('x', 4097) + "'"
                : boundary == "fields" ? string.Join(",", Enumerable.Range(0, 257).Select(index => "field" + index + ": 'x'"))
                : string.Join(",", Enumerable.Range(0, 17).Select(index => "field" + index + ": '" + new string('x', 4096) + "'"))) + "}";
        var source = "kanban\ntodo[Column]\n  " + declaration;
        var options = new MermaidRenderOptions { ParseOptions = new MermaidParseOptions { ResourceLimits = new MermaidResourceLimits {
            MaximumLineCharacters = 100000, MaximumSourceCharacters = 200000 } } };
        var result = MermaidRenderer.Render(source, options); Assert.Null(result.Artifact); Assert.True(result.HasErrors);
        Assert.Contains(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidStatement && item.Span.Line == 3 && item.Span.Column >= 3);
        var fence = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```");
        Assert.True(fence.HasErrors); Assert.Empty(fence.Artifacts);
    }

    internal static string Source => File.ReadAllText(Path.Combine(TestRepository.Root, "ChartForgeX.Examples", "Assets", "Mermaid", "kanban-native-board.mmd"));
    internal static MermaidRenderOptions Options(bool dark, int width) => new() {
        Presentation = new MermaidPresentationOptions { ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light,
            FontFamily = BrowserProofFonts.Family, FontSize = 12, TopologySizing = MermaidTopologySizing.Natural },
        Kanban = new MermaidTopologyRenderOptions { Width = width, Height = 700 }
    };
    internal static void AssertBoard(VisualArtifactInterchangeEnvelope facts) {
        Assert.Empty(facts.Edges); Assert.Equal(new[] { "todo", "doing", "waiting", "done" }, facts.Groups.Select(group => group.Id));
        var columns = facts.Groups.ToArray();
        for (var i = 1; i < columns.Length; i++) Assert.True(columns[i].X >= columns[i - 1].X + columns[i - 1].Width);
        Assert.DoesNotContain(facts.Nodes, node => node.GroupId == "waiting");
        foreach (var column in columns) {
            var tasks = facts.Nodes.Where(node => node.GroupId == column.Id).ToArray();
            for (var index = 0; index < tasks.Length; index++) {
                var task = tasks[index];
                Assert.True(task.X >= column.X && task.X + task.Width <= column.X + column.Width);
                Assert.True(task.Y >= column.Y && task.Y + task.Height <= column.Y + column.Height);
                Assert.True(task.Width >= 180 && task.Height >= 64);
                if (index > 0) Assert.True(task.Y >= tasks[index - 1].Y + tasks[index - 1].Height + 15.9);
            }
        }
    }
}
