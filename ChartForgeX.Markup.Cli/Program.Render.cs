using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Markup.Cli;

internal static partial class Program {
    /// <summary>Renders one diagram to text, SVG, PNG, or HTML for people and agents.</summary>
    private static int Render(string source, string inputPath, string[] args) {
        Console.OutputEncoding = new UTF8Encoding(false);
        var format = (Option(args, "--format") ?? Option(args, "-f") ?? FormatFromOutput(Option(args, "--output") ?? Option(args, "-o")) ?? "text").ToLowerInvariant();
        if (format != "text" && format != "svg" && format != "png" && format != "html") throw new ArgumentException("Option --format accepts text, svg, png, or html.");
        var result = ParseForRender(source, args);
        WriteDiagnostics(result);
        if (result.HasErrors) throw new MarkupCliErrorException("Markup contains errors.", 2);
        if (result.Artifacts.Count == 0) throw new MarkupCliErrorException("No supported visual artifacts were found.", 2);
        var artifact = SelectArtifact(result, Option(args, "--diagram"));
        var chart = artifact.Model as TopologyChart ?? (artifact.Model is FlowArtifact flow ? flow.ToTopologyChart() : null);
        var scenarioId = Option(args, "--scenario");
        var stepText = Option(args, "--step");
        if (chart == null && (scenarioId != null || stepText != null)) throw new ArgumentException("--scenario and --step apply to architecture and topology diagrams.");
        if (chart != null && stepText != null && scenarioId == null) scenarioId = chart.Scenarios.FirstOrDefault()?.Id ?? throw new ArgumentException("The diagram has no use cases to step through.");
        var resolved = chart != null && scenarioId != null ? chart.ResolveScenario(scenarioId) : null;
        var steps = resolved == null ? Array.Empty<int>() : Steps(stepText, resolved.Steps.Count);
        var output = Option(args, "--output") ?? Option(args, "-o");
        switch (format) {
            case "text":
                if (chart == null) throw new ArgumentException("Text output supports architecture, topology, and flow diagrams. Use --format svg, png, or html for this diagram.");
                Write(output, RenderText(chart, resolved, steps, args));
                return 0;
            case "html":
                Write(output, chart != null && chart.Scenarios.Count > 0 ? RenderInteractivePage(chart, resolved) : ArtifactsToHtmlPage(new[] { artifact }));
                return 0;
            case "svg":
                if (chart == null || resolved == null || steps.Length == 0) { Write(output, artifact.ToSvg()); return 0; }
                foreach (var (index, path) in StepOutputs(output, inputPath, "svg", steps, all: IsAll(stepText), required: IsAll(stepText))) Write(path, chart.PrepareScenarioStep(resolved.Id, index).ToSvg());
                return 0;
            default:
                if (string.IsNullOrWhiteSpace(output)) throw new ArgumentException("PNG output requires --output <file.png>, or a folder with --step all.");
                if (chart == null || resolved == null || steps.Length == 0) { WriteBytes(output!, artifact.ToPng()); return 0; }
                foreach (var (index, path) in StepOutputs(output, inputPath, "png", steps, all: IsAll(stepText), required: true)) WriteBytes(path!, chart.PrepareScenarioStep(resolved.Id, index).ToPng());
                return 0;
        }
    }

    private static VisualMarkupParseResult ParseForRender(string source, string[] args) {
        var scan = VisualMarkupScanner.Scan(source);
        if (scan.Blocks.Count > 0 || scan.Diagnostics.Count > 0) return ParseVisuals(source, args);
        // A file without fences is one architecture diagram, so `.cfx` files work like fenced Markdown.
        var lines = Math.Max(1, source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n').Length);
        var block = new VisualMarkupBlock(VisualMarkupKind.Architecture, "chartforgex architecture", string.Empty, 1, source, 1, 1, lines, new Dictionary<string, string>());
        return new MermaidVisualMarkupParser().ParseBlocks(new[] { block });
    }

    private static VisualArtifact SelectArtifact(VisualMarkupParseResult result, string? id) {
        if (id != null) {
            return result.Artifacts.FirstOrDefault(artifact => string.Equals(artifact.Id, id, StringComparison.Ordinal))
                ?? throw new ArgumentException("No diagram has id '" + id + "'. Diagrams: " + string.Join(", ", result.Artifacts.Select(artifact => artifact.Id)) + ".");
        }

        if (result.Artifacts.Count == 1) return result.Artifacts[0];
        throw new ArgumentException("The file has " + result.Artifacts.Count.ToString(CultureInfo.InvariantCulture) + " diagrams; choose one with --diagram <id>: " + string.Join(", ", result.Artifacts.Select(artifact => artifact.Id)) + ".");
    }

    private static int[] Steps(string? value, int count) {
        if (value == null) return Array.Empty<int>();
        if (string.Equals(value, "all", StringComparison.OrdinalIgnoreCase)) return Enumerable.Range(0, count).ToArray();
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var step) || step < 1 || step > count) {
            throw new ArgumentException("Option --step accepts all or a step number from 1 to " + count.ToString(CultureInfo.InvariantCulture) + ".");
        }

        return new[] { step - 1 };
    }

    private static string RenderText(TopologyChart chart, TopologyResolvedScenario? scenario, int[] steps, string[] args) {
        var columnsText = Option(args, "--columns");
        var columns = 120;
        if (columnsText != null && (!int.TryParse(columnsText, NumberStyles.Integer, CultureInfo.InvariantCulture, out columns) || columns < 40 || columns > 1000)) {
            throw new ArgumentException("Option --columns accepts a number from 40 to 1000.");
        }

        var glyphs = args.Any(arg => string.Equals(arg, "--ascii", StringComparison.OrdinalIgnoreCase)) ? TopologyCellGlyphSet.Ascii : TopologyCellGlyphSet.Unicode;
        var layout = TopologyCellLayout.Create(chart.Prepare(), new TopologyCellLayoutOptions { MaximumColumns = columns, CompactToFit = true });
        var text = new StringBuilder();
        void Header() {
            if (!string.IsNullOrWhiteSpace(chart.Title)) text.Append(chart.Title).Append('\n');
            if (!string.IsNullOrWhiteSpace(chart.Subtitle)) text.Append(chart.Subtitle).Append('\n');
            text.Append('\n');
        }

        if (scenario == null || steps.Length == 0) {
            Header();
            text.Append(layout.Paint(new TopologyCellPaintOptions { Glyphs = glyphs }).ToText()).Append('\n');
            if (scenario != null) text.Append('\n').Append(scenario.ToText(null, TopologyScenarioTextPayloads.All, glyphs)).Append('\n');
            else if (chart.Scenarios.Count > 0) {
                text.Append("\nUse cases (render one with --scenario <id> --step <n|all>):\n");
                foreach (var item in chart.Scenarios) text.Append("  ").Append(item.Id).Append("  ").Append(item.Label).Append(" (").Append(item.Steps.Count.ToString(CultureInfo.InvariantCulture)).Append(" steps)\n");
            }

            return text.ToString();
        }

        foreach (var index in steps) {
            Header();
            var paint = TopologyCellPaintOptions.ForStep(scenario.Steps[index]);
            paint.Glyphs = glyphs;
            text.Append(layout.Paint(paint).ToText()).Append("\n\n");
            text.Append(scenario.ToText(index, TopologyScenarioTextPayloads.Active, glyphs)).Append('\n');
            if (index != steps[steps.Length - 1]) text.Append("\n\n");
        }

        return text.ToString();
    }

    private static string RenderInteractivePage(TopologyChart chart, TopologyResolvedScenario? scenario) {
        var options = new TopologyRenderOptions {
            EnableHtmlScenarioUrlState = true,
            ActiveScenarioId = scenario?.Id ?? chart.Scenarios[0].Id
        }.WithHtmlScenarioPanel().WithHtmlScenarioSequence();
        return chart.ToInteractiveHtmlPage(options);
    }

    private static bool IsAll(string? step) => string.Equals(step, "all", StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<(int Index, string? Path)> StepOutputs(string? output, string inputPath, string extension, int[] steps, bool all, bool required) {
        if (!all) {
            if (required && string.IsNullOrWhiteSpace(output)) throw new ArgumentException("Option --output is required for this format.");
            yield return (steps[0], output);
            yield break;
        }

        if (string.IsNullOrWhiteSpace(output)) throw new ArgumentException("--step all writes one file per step; pass --output <folder>.");
        var folder = Path.HasExtension(output) && !Directory.Exists(output) ? Path.GetDirectoryName(Path.GetFullPath(output!))! : output!;
        var name = Path.HasExtension(output) && !Directory.Exists(output) ? Path.GetFileNameWithoutExtension(output) : Path.GetFileNameWithoutExtension(inputPath);
        var digits = Math.Max(2, steps.Length.ToString(CultureInfo.InvariantCulture).Length);
        foreach (var index in steps) yield return (index, Path.Combine(folder, name + ".step-" + (index + 1).ToString(CultureInfo.InvariantCulture).PadLeft(digits, '0') + "." + extension));
    }

    private static string? FormatFromOutput(string? output) {
        if (string.IsNullOrWhiteSpace(output) || !Path.HasExtension(output)) return null;
        var extension = Path.GetExtension(output).TrimStart('.').ToLowerInvariant();
        return extension == "htm" ? "html" : extension == "txt" ? "text" : extension;
    }

    private static void Write(string? output, string content) {
        if (string.IsNullOrWhiteSpace(output)) {
            Console.Out.Write(content);
            return;
        }

        EnsureOutputDirectory(output!);
        File.WriteAllText(output, content, new UTF8Encoding(false));
        Console.Error.WriteLine("Wrote " + output);
    }

    private static void WriteBytes(string output, byte[] content) {
        EnsureOutputDirectory(output);
        File.WriteAllBytes(output, content);
        Console.Error.WriteLine("Wrote " + output);
    }
}
