using System.Diagnostics;
using System.Reflection;
using ChartForgeX.Markup;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects the architecture grammar, its coded diagnostics, and the agent-facing render command.</summary>
public sealed class MarkupArchitectureParserTests {
    private static MarkupParseResult<TopologyChart> Parse(string payload) =>
        new MarkupArchitectureParser().Parse("```chartforgex architecture v1\n" + payload + "\n```");

    private static MarkupDiagnostic Single(MarkupParseResult<TopologyChart> result, string code) =>
        Assert.Single(result.Diagnostics, diagnostic => diagnostic.Code == code);

    [Fact]
    public void Parse_AdReplicationExample_BuildsSitesConnectionsAndUseCases() {
        var chart = TopologyCellLayoutTests.Example("ad-replication");

        Assert.Equal("ad-replication", chart.Id);
        Assert.Equal(new[] { "hq", "warsaw" }, chart.Groups.Select(group => group.Id));
        Assert.Equal(6, chart.Nodes.Count);
        Assert.Equal("hq", chart.Nodes.Single(node => node.Id == "dc01").GroupId);
        Assert.Equal(TopologyNodeKind.Endpoint, chart.Nodes.Single(node => node.Id == "adminpc").Kind);
        Assert.Equal(5, chart.Edges.Count);
        Assert.All(chart.Edges, edge => Assert.Equal(TopologyEdgeLineStyle.Solid, edge.LineStyle));
        Assert.Equal(new[] { "replicate", "link-down" }, chart.Scenarios.Select(scenario => scenario.Id));
        var replicate = chart.Scenarios[0];
        Assert.True(replicate.Spotlight);
        Assert.Equal(12, replicate.Steps.Count);
        Assert.Contains("\n  \"replace\": { \"title\": \"Site manager\" }\n}", replicate.Steps[1].Payload, StringComparison.Ordinal);
        Assert.Equal(TopologyScenarioPayloadFormat.Json, replicate.Steps[1].PayloadFormat);
        Assert.True(replicate.Steps[2].Reverse);
        Assert.Equal(TopologyScenarioInteraction.Response, replicate.Steps[2].Interaction);
        Assert.Equal(TopologyScenarioStepKind.Node, replicate.Steps[3].Kind);
        Assert.Equal(TopologyScenarioPayloadFormat.Text, replicate.Steps[3].PayloadFormat);
        Assert.Equal(TopologyScenarioInteraction.Failure, chart.Scenarios[1].Steps[0].Interaction);
    }

    [Theory]
    [InlineData("ad-replication", "right")]
    [InlineData("tierbridge-file-journey", "right")]
    [InlineData("ad-replication", "down")]
    [InlineData("tierbridge-file-journey", "left")]
    public void Parse_WithoutViewport_DiagramKeepsItsNaturalSizeAlongTheFlow(string example, string direction) {
        var source = File.ReadAllText(Path.Combine(TestRepository.Root, "ChartForgeX.Examples", "Assets", "Architecture", example + ".md")).Replace("direction right", "direction " + direction);
        var chart = new MarkupArchitectureParser().Parse(source).Document!;
        var vertical = direction == "down";
        var natural = vertical ? chart.Prepare().Height : chart.Prepare().Width;
        if (vertical) chart.WithViewport(chart.Viewport.Width, 300, chart.Viewport.Padding);
        else chart.WithViewport(480, chart.Viewport.Height, chart.Viewport.Padding);

        // The estimate is a lower bound along the flow: the layout's own spacing sets the length, nothing spreads it further.
        Assert.Equal(natural, vertical ? chart.Prepare().Height : chart.Prepare().Width, 1);
    }

    [Fact]
    public void StaticHtml_DiagramWiderThanItsViewport_WrapperFollowsTheSvgWidth() {
        var chart = TopologyCellLayoutTests.Example("ad-replication");
        var html = chart.ToHtmlPage();
        var svgWidth = double.Parse(System.Text.RegularExpressions.Regex.Match(html, "<svg[^>]*?\\swidth=\"([0-9.]+)\"").Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);

        Assert.True(svgWidth > chart.Viewport.Width);
        Assert.Contains("max-width:" + svgWidth.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + "px", html, StringComparison.Ordinal);
    }
    [Fact]
    public void Parse_TierBridgeExample_KeepsBoundariesDashedAndUncPathsReadable() {
        var chart = TopologyCellLayoutTests.Example("tierbridge-file-journey");

        Assert.NotEmpty(chart.Groups.Single(group => group.Id == "bridge").DashPattern);
        Assert.Empty(chart.Groups.Single(group => group.Id == "source").DashPattern);
        Assert.Equal(@"\\fs02\drop$", chart.Nodes.Single(node => node.Id == "inbox").Subtitle);
        Assert.Equal(TopologyHealthStatus.Warning, chart.Nodes.Single(node => node.Id == "quarantine").Status);
        Assert.Equal(new[] { "approved", "blocked" }, chart.Scenarios.Select(scenario => scenario.Id));
        Assert.True(chart.Scenarios[0].Steps[7].Reverse);
    }

    [Fact]
    public void Parse_VisualMarkupParser_ReturnsATopologyArtifactWithScenarios() {
        var source = File.ReadAllText(Path.Combine(TestRepository.Root, "ChartForgeX.Examples", "Assets", "Architecture", "ad-replication.md"));
        var result = new VisualMarkupParser().Parse(source);

        Assert.False(result.HasErrors);
        var artifact = Assert.Single(result.Artifacts);
        Assert.Equal(VisualArtifactKind.Topology, artifact.Kind);
        Assert.Equal("architecture", artifact.Metadata["fence"].Split(' ').Last());
        Assert.Equal(2, Assert.IsType<TopologyChart>(artifact.Model).Scenarios.Count);
        Assert.Contains("<svg", artifact.ToSvg(), StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_StepWithoutConnection_AddsADashedEdgeAndWarns() {
        var result = Parse("service a \"A\"\nservice b \"B\"\nusecase u \"U\"\n  a -> b \"call\"\nend");

        Assert.False(result.HasErrors);
        var warning = Single(result, MarkupArchitectureDiagnosticCodes.ImplicitConnection);
        Assert.Equal(MarkupDiagnosticSeverity.Warning, warning.Severity);
        Assert.Equal(5, warning.Line);
        Assert.Equal(3, warning.Column);
        Assert.Equal(TopologyEdgeLineStyle.Dashed, Assert.Single(result.Document!.Edges).LineStyle);
    }

    [Fact]
    public void Parse_ConnectionToUndeclaredNode_AddsAGenericNodeAndWarns() {
        var result = Parse("service a \"A\"\na -> ghost \"call\"");

        Assert.Equal(MarkupDiagnosticSeverity.Warning, Single(result, MarkupArchitectureDiagnosticCodes.ImplicitNode).Severity);
        Assert.Contains(result.Document!.Nodes, node => node.Id == "ghost");
    }

    [Theory]
    [InlineData("service a \"A\"\nusecase u \"U\"\n  a -> nobody \"x\"\nend", MarkupArchitectureDiagnosticCodes.UnknownReference, 4)]
    [InlineData("group g \"G\"\n  service a \"A\"", MarkupArchitectureDiagnosticCodes.UnclosedBlock, 2)]
    [InlineData("service a \"A\"\nend", MarkupArchitectureDiagnosticCodes.UnexpectedEnd, 3)]
    [InlineData("service a \"A\"\nservice a \"Again\"", MarkupArchitectureDiagnosticCodes.DuplicateId, 3)]
    [InlineData("service a \"A\"\nservice b \"B\"\na -> b\nusecase u \"U\"\n  a -> b \"x\" json { \"open\": [1, 2 }\nend", MarkupArchitectureDiagnosticCodes.InvalidPayload, 6)]
    [InlineData("service a \"A\" status:sideways", MarkupArchitectureDiagnosticCodes.InvalidValue, 2)]
    [InlineData("service a \"A", MarkupArchitectureDiagnosticCodes.InvalidStatement, 2)]
    [InlineData("title \"Only a title\"", MarkupArchitectureDiagnosticCodes.EmptyDiagram, 1)]
    [InlineData("service a \"A\"\nservice b \"B\"\na -> b\nusecase u \"U\"\n  a -> b \"x\" duration:50\nend", MarkupArchitectureDiagnosticCodes.InvalidValue, 6)]
    [InlineData("service a \"A\"\nusecase u \"U\" delay:10\nend", MarkupArchitectureDiagnosticCodes.InvalidValue, 3)]
    [InlineData("service a \"A\"\nservice b \"B\"\na -> b id:", MarkupArchitectureDiagnosticCodes.InvalidValue, 4)]
    [InlineData("service a \"A\" stray-word", MarkupArchitectureDiagnosticCodes.InvalidStatement, 2)]
    public void Parse_InvalidMarkup_ReportsCodedErrorsOnTheRightLine(string payload, string code, int line) {
        var result = Parse(payload);

        Assert.True(result.HasErrors);
        Assert.Null(result.Document);
        var diagnostic = Single(result, code);
        Assert.Equal(MarkupDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal(line, diagnostic.Line);
    }

    [Theory]
    [InlineData("service a \"A\" shape:round", MarkupArchitectureDiagnosticCodes.UnknownAttribute)]
    [InlineData("service a \"A\"\nfrobnicate a", MarkupArchitectureDiagnosticCodes.UnknownCommand)]
    [InlineData("service a \"A\" icon:no-such-icon", MarkupArchitectureDiagnosticCodes.UnknownIcon)]
    [InlineData("service a \"A\"\nservice b \"B\"\na -> b\nusecase u \"U\"\n  b --> a \"reply\"\nend", MarkupArchitectureDiagnosticCodes.UnansweredResponse)]
    [InlineData("service a \"A\"\nusecase u \"U\"\nend", MarkupArchitectureDiagnosticCodes.EmptyUseCase)]
    public void Parse_QuestionableMarkup_ReportsCodedWarningsAndStillRenders(string payload, string code) {
        var result = Parse(payload);

        Assert.False(result.HasErrors, string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Code + " " + diagnostic.Message)));
        Assert.Equal(MarkupDiagnosticSeverity.Warning, Single(result, code).Severity);
        Assert.Contains("<svg", result.Document!.ToSvg(), StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_NodeNamedLikeAKeyword_StillReadsArrowsAsSteps() {
        var result = Parse("service at \"At\"\ndatabase db \"DB\"\nat -> db\nusecase u \"U\"\n  at -> db \"query\" json { \"url\": \"https://example.test/a\" }\nend");

        Assert.False(result.HasErrors, string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Code + " " + diagnostic.Message)));
        var step = Assert.Single(result.Document!.Scenarios.Single().Steps);
        Assert.Equal(TopologyScenarioStepKind.Edge, step.Kind);
        Assert.Contains("https://example.test/a", step.Payload, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_RawText_ReadsTheWholeTextAsOneDiagram() {
        var result = new MarkupArchitectureParser().Parse("title \"Raw\"\nuser u \"User\"\nservice s \"Service\"\nu -> s \"HTTPS\"\nusecase \"Sign in\"\n  u -> s \"POST /login\"\n  s --> u \"200\"\nend\n");

        Assert.False(result.HasErrors);
        Assert.Equal("sign-in", result.Document!.Scenarios.Single().Id);
        Assert.Equal("Raw", result.Document.Title);
    }

    [Fact]
    public void Cli_RenderText_DrawsTheDiagramAndListsUseCases() {
        var file = Fixture("ad-replication");
        var text = RunCli("render", file, "--columns", "110");

        Assert.Equal(0, text.ExitCode);
        Assert.Contains("Active Directory replication", text.StandardOutput, StringComparison.Ordinal);
        Assert.Contains("DC04", text.StandardOutput, StringComparison.Ordinal);
        Assert.Contains("replicate  Replicate a change (12 steps)", text.StandardOutput, StringComparison.Ordinal);
        var step = RunCli("render", file, "--scenario", "link-down", "--step", "1", "--ascii");
        Assert.Equal(0, step.ExitCode);
        Assert.Contains("> 1. DC03 -> DC02  [failure]  Scheduled pull", step.StandardOutput, StringComparison.Ordinal);
        Assert.Contains("RPC server is unavailable (1722)", step.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void Cli_RenderFiles_WritesSvgPngHtmlAndOneFilePerStep() {
        var folder = Path.Combine(Path.GetTempPath(), "cfx-render-" + Guid.NewGuid().ToString("N"));
        try {
            var file = Fixture("tierbridge-file-journey");
            Assert.Equal(0, RunCli("render", file, "--output", Path.Combine(folder, "journey.svg")).ExitCode);
            Assert.StartsWith("<svg", File.ReadAllText(Path.Combine(folder, "journey.svg")).TrimStart(), StringComparison.Ordinal);
            Assert.Equal(0, RunCli("render", file, "--format", "html", "--output", Path.Combine(folder, "journey.html")).ExitCode);
            Assert.Contains("data-cfx-scenario-sequence=\"approved\"", File.ReadAllText(Path.Combine(folder, "journey.html")), StringComparison.Ordinal);
            var steps = RunCli("render", file, "--format", "png", "--scenario", "blocked", "--step", "all", "--output", folder);
            Assert.Equal(0, steps.ExitCode);
            Assert.Equal(7, Directory.GetFiles(folder, "tierbridge-file-journey.step-*.png").Length);
            Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, File.ReadAllBytes(Path.Combine(folder, "tierbridge-file-journey.step-07.png")).Take(4));
        } finally {
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
    }

    [Fact]
    public void Cli_RenderRawFileAndErrors_ReportCodesAndExitCodes() {
        var folder = Path.Combine(Path.GetTempPath(), "cfx-render-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try {
            var raw = Path.Combine(folder, "flow.cfx");
            File.WriteAllText(raw, "service a \"A\"\nservice b \"B\"\nusecase u \"U\"\n  a -> b \"call\"\nend\n");
            var warned = RunCli("render", raw);
            Assert.Equal(0, warned.ExitCode);
            Assert.Contains("warning CFXA006(4):", warned.StandardError, StringComparison.Ordinal);
            var broken = Path.Combine(folder, "broken.cfx");
            File.WriteAllText(broken, "group g \"G\"\n  service a \"A\"\n");
            var failed = RunCli("render", broken);
            Assert.Equal(2, failed.ExitCode);
            Assert.Contains("error CFXA007(1):", failed.StandardError, StringComparison.Ordinal);
            var frames = Path.Combine(folder, "frames");
            Assert.Equal(0, RunCli("render", raw, "--format", "png", "--step", "all", "--output", frames).ExitCode);
            Assert.True(File.Exists(Path.Combine(frames, "flow.step-01.png")));
            var badStep = RunCli("render", raw, "--step", "9");
            Assert.Equal(1, badStep.ExitCode);
            Assert.Contains("--step accepts all or a step number from 1 to 1", badStep.StandardError, StringComparison.Ordinal);
        } finally {
            Directory.Delete(folder, true);
        }
    }

    private static string Fixture(string name) => Path.Combine(TestRepository.Root, "ChartForgeX.Examples", "Assets", "Architecture", name + ".md");

    private static (int ExitCode, string StandardOutput, string StandardError) RunCli(params string[] arguments) {
        var startInfo = new ProcessStartInfo("dotnet") {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            StandardOutputEncoding = System.Text.Encoding.UTF8
        };
        startInfo.ArgumentList.Add(CliPath());
        foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);
        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start ChartForgeX.Markup.Cli.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(60000)) {
            process.Kill(true);
            throw new TimeoutException("ChartForgeX.Markup.Cli timed out.");
        }

        return (process.ExitCode, output.GetAwaiter().GetResult(), error.GetAwaiter().GetResult());
    }

    private static string CliPath() {
        var routed = typeof(TestRepository).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().SingleOrDefault(attribute => attribute.Key == "MarkupCliPath")?.Value;
        if (routed != null && File.Exists(routed)) return routed;
        foreach (var configuration in new[] { "Release", "Debug" }) {
            var candidate = Path.Combine(TestRepository.Root, "ChartForgeX.Markup.Cli", "bin", configuration, "net8.0", "ChartForgeX.Markup.Cli.dll");
            if (File.Exists(candidate)) return candidate;
        }

        throw new FileNotFoundException("Build ChartForgeX.Markup.Cli before running the render command tests.");
    }
}
