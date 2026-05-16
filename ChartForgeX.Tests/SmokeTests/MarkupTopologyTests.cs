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

    private static void MarkupTopologyExtractsTildeFenceWithMetadata() {
        const string source = @"# Diagram

~~~chartforgex topology {#service-map}
title ""Tilde Fence""
node api ""API"" kind:service status:healthy
~~~
";

        var blocks = ChartForgeXMarkdown.ExtractTopologyPayloads(source);
        Assert(blocks.Count == 1, "Markdown extraction should support standard three-tilde fences with trailing metadata.");
        var result = new MarkupTopologyParser().Parse(source);
        Assert(!result.HasErrors, "Tilde-fenced topology markup should parse without errors: " + Diagnostics(result));
        Assert(result.Document != null && result.Document.Title == "Tilde Fence", "Tilde-fenced topology markup should produce a document.");
        Assert(result.Document!.Nodes.Count == 1, "Tilde-fenced topology markup should parse nodes.");
    }

    private static void MarkupTopologyDiagnosticsUseMarkdownSourceLines() {
        const string source = @"# Diagram

```chartforgex topology
title ""Source Line Check""
unknownThing yes
node api ""API"" kind:service status:healthy
```";

        var result = new MarkupTopologyParser().Parse(source);
        Assert(!result.HasErrors, "Unknown commands should remain warnings.");
        var warning = result.Diagnostics.Find(diagnostic => diagnostic.Severity == MarkupDiagnosticSeverity.Warning);
        Assert(warning != null, "Unknown commands should produce a warning.");
        Assert(warning!.Line == 5, "Markdown parser diagnostics should use the original source line.");
    }

    private static void MarkupTopologyPreservesMarkdownTableAuthoringDetails() {
        const string source = @"```chartforgex topology
nodes:
id | label | kind | status | display | width | height | subtitle
:-- | :---- | :--- | ---: | :------ | ----: | -----: | :-------
api | API \| Gateway | service | healthy | tile | 180 | 80 | https://api.example.com
db | Database | database | warning | card | 150 | 70 | DOMAIN\user
edges:
from | to | label | status
:--- | --: | :---- | :-----
api | db | https://api.example.com | warning
```";

        var result = new MarkupTopologyParser().Parse(source);

        Assert(!result.HasErrors, "Markdown table topology should parse without errors: " + Diagnostics(result));
        Assert(result.Document!.Nodes[0].Label == "API | Gateway", "Escaped table pipes should stay inside the cell.");
        Assert(result.Document.Nodes[0].Subtitle == "https://api.example.com", "Table values should preserve URL-style text.");
        Assert(result.Document.Nodes[0].Display == TopologyNodeDisplayMode.Tile, "Node table rows should map display.");
        Assert(result.Document.Nodes[0].Width == 180 && result.Document.Nodes[0].Height == 80, "Node table rows should map explicit dimensions.");
        Assert(result.Document.Nodes[1].Subtitle == @"DOMAIN\user", "Table cells should preserve literal backslashes.");
        Assert(result.Document.Edges[0].Label == "https://api.example.com", "Edge table labels should preserve colon-containing values.");
    }

    private static void MarkupTopologyPreservesCommandAuthoringDetails() {
        const string source = @"title ""Command Details""
node api ""API"" kind:service status:healthy subtitle:https://api.example.com width:180 height:80 display:tile
node db ""Database"" kind:database status:warning
edge api -> db ""status:warning"" status:healthy
";

        var result = new MarkupTopologyParser().Parse(source);

        Assert(!result.HasErrors, "Command topology should parse without errors: " + Diagnostics(result));
        Assert(result.Document!.Nodes[0].Subtitle == "https://api.example.com", "Command attributes should preserve URL-style text.");
        Assert(result.Document.Nodes[0].Width == 180 && result.Document.Nodes[0].Height == 80, "Command nodes should map explicit dimensions.");
        Assert(result.Document.Nodes[0].Display == TopologyNodeDisplayMode.Tile, "Command nodes should map display.");
        Assert(result.Document.Edges[0].Label == "status:warning", "Quoted command edge labels that look like attributes should stay labels.");
        Assert(result.Document.Edges[0].Status == TopologyHealthStatus.Healthy, "Attributes after quoted edge labels should still be parsed.");
        Assert(result.Document.Edges[0].Direction == TopologyDirection.Forward, "Arrow commands should default -> to forward direction.");

        const string sectionSource = @"nodes:
node api ""API | Gateway"" kind:service status:healthy
";
        var sectionResult = new MarkupTopologyParser().Parse(sectionSource);
        Assert(!sectionResult.HasErrors, "Command-style section rows with pipe text should not be misread as Markdown tables: " + Diagnostics(sectionResult));
        Assert(sectionResult.Document!.Nodes[0].Label == "API | Gateway", "Command-style section rows should preserve pipe labels.");
    }

    private static void MarkupTopologyRejectsMismatchedSectionCommands() {
        const string source = @"nodes:
edge api -> db
title ""Wrong Place""
layout layered lr
";

        var result = new MarkupTopologyParser().Parse(source);

        Assert(result.HasErrors, "Mismatched section commands should produce parser errors.");
        var diagnostics = Diagnostics(result);
        Assert(diagnostics.Contains("edge' cannot appear inside nodes section", StringComparison.Ordinal), "Mismatched entry commands should identify the bad section.");
        Assert(diagnostics.Contains("title' cannot appear inside nodes section", StringComparison.Ordinal), "Document-level title commands inside typed sections should be rejected instead of coerced.");
        Assert(diagnostics.Contains("layout' cannot appear inside nodes section", StringComparison.Ordinal), "Document-level layout commands inside typed sections should be rejected instead of coerced.");
    }

    private static void MarkupTopologyHandlesEditingFriendlyMarkdownFences() {
        const string unterminated = @"# Draft

```chartforgex topology
node api ""API"" kind:service status:healthy";

        var blocks = ChartForgeXMarkdown.ExtractTopologyBlocks(unterminated);
        Assert(blocks.Count == 1, "Unterminated topology fences should remain open through EOF.");
        Assert(blocks[0].StartLine == 4, "Unterminated fence payload should preserve source line.");

        const string indented = @"    ```chartforgex topology
    node api ""API""
    ```
node raw ""Raw"" kind:service status:healthy";

        var result = new MarkupTopologyParser().Parse(indented);
        Assert(result.Diagnostics.Exists(diagnostic => diagnostic.Message.Contains("Unknown topology command", StringComparison.Ordinal)), "Four-space indented fences should stay as code block text, not live topology fences.");

        var tabIndented = "\t```chartforgex topology\n\tnode api \"API\"\n\t```\nnode raw \"Raw\" kind:service status:healthy";
        var tabResult = new MarkupTopologyParser().Parse(tabIndented);
        Assert(tabResult.Diagnostics.Exists(diagnostic => diagnostic.Message.Contains("Unknown topology command", StringComparison.Ordinal)), "Tab-indented fences should stay as code block text, not live topology fences.");

        const string closingSuffix = @"```chartforgex topology
node api ""API"" kind:service status:healthy
```not-a-close
node db ""Database"" kind:database status:warning
```";
        var closingResult = new MarkupTopologyParser().Parse(closingSuffix);
        Assert(!closingResult.HasErrors, "Closing fences with trailing text should stay inside the payload until a valid close.");
        Assert(closingResult.Document!.Nodes.Count == 2, "Invalid closing-fence suffixes should not truncate topology payloads.");
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

    private static void MarkupTopologyCliRejectsMalformedAutomationInputs() {
        var shortInvocation = RunMarkupCliRaw("validate");
        Assert(shortInvocation.ExitCode == 1, "CLI should fail when an input file is missing.");
        Assert(shortInvocation.StandardError.Contains("Missing input file", StringComparison.Ordinal), "Missing input file should be reported on stderr.");

        var fixture = Path.Combine(Path.GetTempPath(), "chartforgex-markup-options-" + Guid.NewGuid().ToString("N") + ".md");
        File.WriteAllText(fixture, "not even valid topology\n");
        try {
            var unknownCommand = RunMarkupCliRaw("valdiate", fixture);
            Assert(unknownCommand.ExitCode == 1, "CLI should reject unknown commands before parsing input.");
            Assert(unknownCommand.StandardError.Contains("Unknown command", StringComparison.Ordinal), "Unknown commands should be reported on stderr.");

            File.WriteAllText(fixture, "node api \"API\" kind:service status:healthy\n");
            var missingValue = RunMarkupCli("emit", fixture, "--target");
            Assert(missingValue.ExitCode == 1, "CLI should fail when an option value is missing.");
            Assert(missingValue.StandardError.Contains("requires a value", StringComparison.Ordinal), "Missing option value should be reported on stderr.");
        } finally {
            try {
                File.Delete(fixture);
            } catch (IOException) {
            } catch (UnauthorizedAccessException) {
            }
        }
    }

    private static void MarkupTopologyCliReportsRenderValidationErrors() {
        var fixture = Path.Combine(Path.GetTempPath(), "chartforgex-markup-invalid-" + Guid.NewGuid().ToString("N") + ".md");
        var output = Path.Combine(Path.GetTempPath(), "chartforgex-markup-invalid-" + Guid.NewGuid().ToString("N") + ".svg");
        File.WriteAllText(fixture, "node api \"API\" kind:service status:healthy\nedge api -> missing \"broken\"\n");
        try {
            var preview = RunMarkupCli("preview", fixture);
            Assert(preview.ExitCode == 2, "CLI preview should return a stable validation error exit code.");
            Assert(preview.StandardError.Contains("error ", StringComparison.Ordinal), "CLI preview should write topology validation errors to stderr.");

            var export = RunMarkupCli("export", fixture, "--output", output);
            Assert(export.ExitCode == 2, "CLI export should return a stable validation error exit code.");
            Assert(export.StandardError.Contains("error ", StringComparison.Ordinal), "CLI export should write topology validation errors to stderr.");
        } finally {
            try {
                File.Delete(fixture);
                File.Delete(output);
            } catch (IOException) {
            } catch (UnauthorizedAccessException) {
            }
        }
    }

    private static (int ExitCode, string StandardOutput, string StandardError) RunMarkupCli(string command, string input, params string[] extraArguments) {
        var arguments = new string[2 + extraArguments.Length];
        arguments[0] = command;
        arguments[1] = input;
        Array.Copy(extraArguments, 0, arguments, 2, extraArguments.Length);
        return RunMarkupCliRaw(arguments);
    }

    private static (int ExitCode, string StandardOutput, string StandardError) RunMarkupCliRaw(params string[] arguments) {
        var cli = FindMarkupCliDll();
        var startInfo = new ProcessStartInfo("dotnet") {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(cli);
        foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);

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
