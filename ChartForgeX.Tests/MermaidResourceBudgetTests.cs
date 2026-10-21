using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidResourceBudgetTests {
    public static IEnumerable<object[]> Families() => new[] {
        "flowchart", "swimlane", "usecase", "cynefin", "sequence", "class", "state", "er", "pie", "journey",
        "gitgraph", "gantt", "mindmap", "timeline", "requirement", "quadrant", "xychart", "sankey", "radar", "treemap",
        "kanban", "architecture", "c4", "block", "packet", "venn", "ishikawa", "wardley", "treeview", "eventmodeling"
    }.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Families))]
    public void EveryImplementedFamilyAppliesTheSemanticItemBudget(string family) {
        var source = File.ReadAllText(Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures", family + "-basic.mmd"));
        var parser = new MermaidParser();
        var baseline = parser.Parse(source);
        Assert.False(baseline.HasErrors, string.Join("; ", baseline.Diagnostics.Select(item => item.Message)));
        Assert.NotNull(baseline.Document);
        var result = parser.Parse(source, Options(new MermaidResourceLimits { MaximumElements = 1 }));
        Assert.Null(result.Document);
        var error = Assert.Single(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.ResourceLimitExceeded);
        Assert.Equal(MermaidDiagnosticSeverity.Error, error.Severity);
        Assert.Contains(nameof(MermaidResourceLimits.MaximumElements), error.Message);
        Assert.InRange(error.Span.Line, 1, source.Count(character => character == '\n') + 1);
    }

    [Theory]
    [InlineData("characters", 2, 7)]
    [InlineData("lines", 3, 1)]
    [InlineData("line", 2, 13)]
    public void RawSourceBoundsReportOriginalCrlfLocationsBeforeParsing(string bound, int line, int column) {
        var source = bound == "characters" ? "flowchart LR\r\nA --> B" : bound == "lines" ? "flowchart LR\r\nA\r\nB" : "flowchart LR\r\nA[123456789012345]";
        var limits = new MermaidResourceLimits();
        if (bound == "characters") limits.MaximumSourceCharacters = source.Length - 1;
        if (bound == "lines") limits.MaximumSourceLines = 2;
        if (bound == "line") limits.MaximumLineCharacters = 12;
        var result = new MermaidParser().Parse(source, Options(limits));
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Null(result.Document);
        Assert.Equal(MermaidDiagnosticCodes.ResourceLimitExceeded, diagnostic.Code);
        Assert.Equal((line, column), (diagnostic.Span.Line, diagnostic.Span.Column));
    }

    [Theory]
    [InlineData(nameof(MermaidResourceLimits.MaximumSourceCharacters))]
    [InlineData(nameof(MermaidResourceLimits.MaximumSourceLines))]
    [InlineData(nameof(MermaidResourceLimits.MaximumLineCharacters))]
    public void OrdinaryParsesEnforceDefaultOuterSourceBounds(string limit) {
        var source = limit == nameof(MermaidResourceLimits.MaximumSourceCharacters) ? new string('x', 524289)
            : limit == nameof(MermaidResourceLimits.MaximumSourceLines) ? "flowchart LR" + new string('\n', 10000)
            : "flowchart LR\n" + new string('x', 8193);
        var result = new MermaidParser().Parse(source);
        Assert.Null(result.Document);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(MermaidDiagnosticCodes.ResourceLimitExceeded, diagnostic.Code);
        Assert.Contains(limit, diagnostic.Message);
    }

    [Theory]
    [InlineData("flowchart LR\nsubgraph one\nsubgraph two\nA\nend\nend")]
    [InlineData("sequenceDiagram\nloop one\nloop two\nA->>B: message\nend\nend")]
    [InlineData("stateDiagram-v2\nstate One {\nstate Two {\nA --> B\n}\n}")]
    [InlineData("mindmap\nroot\n  child\n    leaf")]
    [InlineData("treeview\nroot\n  child\n    leaf")]
    [InlineData("treemap-beta\n\"root\"\n  \"child\"\n    \"leaf\": 1")]
    public void NestedSourcesFailWithinTheCapturedDepthBudget(string source) {
        var result = new MermaidParser().Parse(source, Options(new MermaidResourceLimits { MaximumNestingDepth = 1 }));
        Assert.Null(result.Document);
        var diagnostic = Assert.Single(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.ResourceLimitExceeded);
        Assert.Contains(nameof(MermaidResourceLimits.MaximumNestingDepth), diagnostic.Message);
        Assert.True(diagnostic.Span.Line > 1);
    }

    [Fact]
    public void QualifiedNamespacesApplyTheirSemanticGroupDepth() {
        const string source = "classDiagram\nnamespace A.B {\nclass C\n}";
        Assert.False(new MermaidParser().Parse(source).HasErrors);
        var result = new MermaidParser().Parse(source, Options(new MermaidResourceLimits { MaximumNestingDepth = 1 }));
        Assert.Null(result.Document);
        var diagnostic = Assert.Single(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.ResourceLimitExceeded);
        Assert.Contains(nameof(MermaidResourceLimits.MaximumNestingDepth), diagnostic.Message);
        Assert.Equal(2, diagnostic.Span.Line);
    }

    [Fact]
    public void NamespaceShellsCountOnceAlongsideTheirMemberClasses() {
        const string source = "classDiagram\nnamespace A.B {\nclass C\nclass D\n}";
        var parser = new MermaidParser();
        Assert.NotNull(parser.Parse(source, Options(new MermaidResourceLimits { MaximumElements = 4 })).Document);
        var result = parser.Parse(source, Options(new MermaidResourceLimits { MaximumElements = 3 }));
        Assert.Null(result.Document);
        var diagnostic = Assert.Single(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.ResourceLimitExceeded);
        Assert.Contains(nameof(MermaidResourceLimits.MaximumElements), diagnostic.Message);
        Assert.Equal(3, diagnostic.Span.Line);
    }

    [Fact]
    public void CartesianFlowchartExpansionIsRejectedBeforeAllocatingItsEdges() {
        var left = string.Join(" & ", Enumerable.Range(0, 400).Select(index => "L" + index));
        var right = string.Join(" & ", Enumerable.Range(0, 400).Select(index => "R" + index));
        var source = "flowchart LR\n" + left + " --> " + right;
        var parser = new MermaidParser();
        parser.Parse("flowchart LR\nA --> B");
        var before = GC.GetAllocatedBytesForCurrentThread();
        var result = parser.Parse(source, Options(new MermaidResourceLimits { MaximumEdges = 2 }));
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Null(result.Document);
        Assert.Contains(nameof(MermaidResourceLimits.MaximumEdges), Assert.Single(result.Diagnostics).Message);
        // Materializing 160,000 edge objects would exceed this safety envelope by a wide margin.
        Assert.InRange(allocated, 0, 12 * 1024 * 1024);
    }

    [Theory]
    [InlineData("sequenceDiagram\nA->>B: one\nB->>A: two")]
    [InlineData("classDiagram\nA --> B\nB --> C")]
    [InlineData("erDiagram\nA ||--|| B : one\nB ||--|| C : two")]
    [InlineData("stateDiagram-v2\nA --> B\nB --> C")]
    [InlineData("sankey-beta\nA,B,1\nB,C,2")]
    public void NonFlowchartRelationshipsAlsoUseTheGlobalEdgeBudget(string source) {
        var result = new MermaidParser().Parse(source, Options(new MermaidResourceLimits { MaximumEdges = 1 }));
        Assert.Null(result.Document);
        Assert.Contains(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.ResourceLimitExceeded && item.Message.Contains(nameof(MermaidResourceLimits.MaximumEdges), StringComparison.Ordinal));
    }

    [Fact]
    public void DirectAndMarkdownConsumersShareResourceFailureAndLocatedSource() {
        const string source = "flowchart LR\nA --> B --> C";
        var options = new MermaidRenderOptions { ParseOptions = Options(new MermaidResourceLimits { MaximumEdges = 1 }) };
        var direct = MermaidRenderer.Render(source, options);
        var directDiagnostic = Assert.Single(direct.Diagnostics);
        Assert.Null(direct.Artifact);
        Assert.Equal(MermaidDiagnosticCodes.ResourceLimitExceeded, directDiagnostic.Code);
        var fenced = new MermaidVisualMarkupParser(options).Parse("Intro\n```mermaid\n" + source + "\n```");
        var fencedDiagnostic = Assert.Single(fenced.Diagnostics);
        Assert.Empty(fenced.Artifacts);
        Assert.Equal(directDiagnostic.Code, fencedDiagnostic.Code);
        Assert.Equal(directDiagnostic.Message, fencedDiagnostic.Message);
        Assert.Equal((4, directDiagnostic.Span.Column), (fencedDiagnostic.Line, fencedDiagnostic.Column));
    }

    [Fact]
    public void ClonedPoliciesAreIndependentAndFailedParsesRestoreDefaultRequests() {
        const string source = "flowchart LR\nA --> B --> C";
        var options = Options(new MermaidResourceLimits { MaximumEdges = 2 });
        var captured = options.Clone();
        options.ResourceLimits.MaximumEdges = 1;
        var parser = new MermaidParser();
        Assert.NotNull(parser.Parse(source, captured).Document);
        Assert.Null(parser.Parse(source, options).Document);
        Assert.NotNull(parser.Parse(source).Document);
        Assert.Equal(2, captured.ResourceLimits.MaximumEdges);
    }

    [Fact]
    public async Task ConcurrentParserPoliciesDoNotLeakBetweenAsyncRequests() {
        var parser = new MermaidParser();
        var tasks = new[] { 1, 2 }.Select(maximum => Task.Run(async () => {
            await Task.Yield();
            return parser.Parse("flowchart LR\nA --> B --> C", Options(new MermaidResourceLimits { MaximumEdges = maximum }));
        })).ToArray();
        var results = await Task.WhenAll(tasks);
        Assert.Null(results[0].Document);
        Assert.Equal(MermaidDiagnosticCodes.ResourceLimitExceeded, Assert.Single(results[0].Diagnostics).Code);
        Assert.NotNull(results[1].Document);
        Assert.Empty(results[1].Diagnostics);
    }

    [Fact]
    public void CancellationPropagatesAndLimitsValidateWithoutSourceDiagnostics() {
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        var failure = Assert.Throws<OperationCanceledException>(() => new MermaidParser().Parse("flowchart LR\nA --> B", null, cancellation.Token));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Throws<ArgumentOutOfRangeException>(() => new MermaidResourceLimits { MaximumElements = 0 });
        Assert.Throws<ArgumentOutOfRangeException>(() => new MermaidResourceLimits { MaximumOutputBytes = -1 });
        Assert.Throws<ArgumentNullException>(() => new MermaidParseOptions { ResourceLimits = null! });
        Assert.NotNull(new MermaidParser().ParseFlowchart("flowchart LR\nA --> B").Document);
    }

    private static MermaidParseOptions Options(MermaidResourceLimits limits) => new() { ResourceLimits = limits };
}
