using System;
using System.Diagnostics;
using System.IO;
using ChartForgeX.Markup;
using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void MarkupTopologyParsesFencedCommandDiagram() {
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "ChartForgeX.Tests", "Fixtures", "markup", "topology-service-map.md"));
        var result = new MarkupTopologyParser().Parse(source);

        Assert(!result.HasErrors, "Command topology markup should parse without errors: " + Diagnostics(result));
        Assert(result.Document != null, "Topology markup should produce a document.");
        Assert(result.Document!.Groups.Count == 2, "Topology markup should parse groups.");
        Assert(result.Document.Nodes.Count == 3, "Topology markup should parse nodes.");
        Assert(result.Document.Edges.Count == 2, "Topology markup should parse edges.");

        var chart = result.Document.ToTopologyChart();
        Assert(chart.LayoutMode == TopologyLayoutMode.Layered && chart.LayoutDirection == TopologyLayoutDirection.LeftToRight, "Topology markup should map compact layout aliases.");
        Assert(chart.ToSvg().Contains("data-cfx-role=\"topology\"", System.StringComparison.Ordinal), "Topology markup should render through the ChartForgeX SVG renderer.");
    }

    private static void MarkupTopologyParsesTableDiagramAndEmitsCSharp() {
        const string source = @"```chartforgex topology
title: ""Regional Directory Topology""
layout: densegrouped tb
groups:
| id | label | status | icon |
| -- | ----- | ------ | ---- |
| emea | EMEA | warning | microsoft-ad:site |
| amer | AMER | healthy | microsoft-ad:site |
nodes:
| id | label | group | kind | status | badge |
| -- | ----- | ----- | ---- | ------ | ----- |
| dc-emea | EMEA DC01 | emea | server | warning | GC |
| dc-amer | AMER DC01 | amer | server | healthy | GC |
edges:
| from | to | label | status | direction |
| ---- | -- | ----- | ------ | --------- |
| dc-emea | dc-amer | 92 ms | warning | bidirectional |
```";

        var result = new MarkupTopologyParser().Parse(source);

        Assert(!result.HasErrors, "Table topology markup should parse without errors: " + Diagnostics(result));
        var code = MarkupTopologyCSharpEmitter.Emit(result.Document!);
        Assert(code.Contains("TopologyChart.Create()", System.StringComparison.Ordinal), "C# emitter should create a topology chart.");
        Assert(code.Contains(".AddGroup(\"emea\", \"EMEA\", 0, 0, 260, 160, TopologyHealthStatus.Warning", System.StringComparison.Ordinal), "C# emitter should include parsed groups.");
        Assert(code.Contains(".WithNodeBadge(\"dc-emea\", \"GC\")", System.StringComparison.Ordinal), "C# emitter should include node badge helpers.");
    }

    private static void MarkupTopologyReportsMissingNodes() {
        var result = new MarkupTopologyParser().Parse("title \"Empty\"");

        Assert(result.HasErrors, "Topology markup without nodes should report a parser error.");
        Assert(Diagnostics(result).Contains("at least one node", System.StringComparison.Ordinal), "Missing-node diagnostic should be actionable.");
    }

    private static void MarkupTopologyCliKeepsWarningsOffGeneratedStreams() {
        var fixture = Path.Combine(Path.GetTempPath(), "chartforgex-markup-warning-" + Guid.NewGuid().ToString("N") + ".md");
        File.WriteAllText(fixture, "title \"Warning Stream Check\"\nunknownThing yes\nnode api \"API\" kind:service status:healthy\n");
        try {
            var preview = RunMarkupCli("preview", fixture);
            Assert(preview.ExitCode == 0, "CLI preview should succeed for warning-only markup: " + preview.StandardError);
            Assert(preview.StandardOutput.TrimStart().StartsWith("<!doctype html>", StringComparison.Ordinal), "CLI preview stdout should start with HTML.");
            Assert(!preview.StandardOutput.Contains("warning(", StringComparison.OrdinalIgnoreCase), "CLI preview stdout should not be contaminated by diagnostics.");
            Assert(preview.StandardError.Contains("warning(2): Unknown topology command 'unknownThing'.", StringComparison.Ordinal), "CLI preview should write parser warnings to stderr.");

            var emit = RunMarkupCli("emit", fixture, "--target", "csharp");
            Assert(emit.ExitCode == 0, "CLI emit should succeed for warning-only markup: " + emit.StandardError);
            Assert(emit.StandardOutput.TrimStart().StartsWith("using ChartForgeX.Topology;", StringComparison.Ordinal), "CLI emit stdout should start with generated C#.");
            Assert(!emit.StandardOutput.Contains("warning(", StringComparison.OrdinalIgnoreCase), "CLI emit stdout should not be contaminated by diagnostics.");
            Assert(emit.StandardError.Contains("warning(2): Unknown topology command 'unknownThing'.", StringComparison.Ordinal), "CLI emit should write parser warnings to stderr.");
        } finally {
            try {
                File.Delete(fixture);
            } catch (IOException) {
            } catch (UnauthorizedAccessException) {
            }
        }
    }

    private static (int ExitCode, string StandardOutput, string StandardError) RunMarkupCli(string command, string input, params string[] extraArguments) {
        var cli = FindMarkupCliDll();
        var startInfo = new ProcessStartInfo("dotnet") {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(cli);
        startInfo.ArgumentList.Add(command);
        startInfo.ArgumentList.Add(input);
        foreach (var argument in extraArguments) startInfo.ArgumentList.Add(argument);

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start ChartForgeX.Markup.Cli.");
        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(30000)) {
            process.Kill(true);
            throw new TimeoutException("ChartForgeX.Markup.Cli timed out.");
        }

        return (process.ExitCode, standardOutput.GetAwaiter().GetResult(), standardError.GetAwaiter().GetResult());
    }

    private static string FindMarkupCliDll() {
        var root = FindRepositoryRoot();
        foreach (var configuration in new[] { "Release", "Debug" }) {
            var candidate = Path.Combine(root, "ChartForgeX.Markup.Cli", "bin", configuration, "net8.0", "ChartForgeX.Markup.Cli.dll");
            if (File.Exists(candidate)) return candidate;
        }

        throw new FileNotFoundException("Build ChartForgeX.Markup.Cli before running CLI stream smoke tests.");
    }

    private static string Diagnostics<TDocument>(MarkupParseResult<TDocument> result) where TDocument : class =>
        string.Join("; ", result.Diagnostics.ConvertAll(diagnostic => diagnostic.Severity + ":" + diagnostic.Message));
}
