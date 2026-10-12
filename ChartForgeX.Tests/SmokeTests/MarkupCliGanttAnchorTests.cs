using System;
using System.IO;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void MarkupCliAppliesGanttDateAnchor() {
        var root = Path.Combine(Path.GetTempPath(), "chartforgex-cli-anchor-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try {
            const string source = "```mermaid\ngantt\ndateFormat MM-DD\nTask :02-29,1d\n```";
            var input = Path.Combine(root, "schedule.md");
            File.WriteAllText(input, source);
            var options = new MermaidRenderOptions { ParseOptions = new MermaidParseOptions { GanttDateAnchor = new DateTime(2028, 1, 1) } };
            var artifact = new MermaidVisualMarkupParser(options).Parse(source).Artifacts[0];
            var validate = RunMarkupCli("validate", input, "--gantt-date-anchor", "2028-01-01");
            Assert(validate.ExitCode == 0 && validate.StandardOutput.Contains("Artifacts: 1", StringComparison.Ordinal), validate.StandardError);
            var preview = RunMarkupCli("preview", input, "--gantt-date-anchor", "2028-01-01");
            Assert(preview.ExitCode == 0, preview.StandardError);
            AssertAnchoredGanttTask(preview.StandardOutput);
            AssertAnchoredGanttTask(artifact.ToSvg());
            foreach (var extension in new[] { "svg", "html", "png" }) {
                var output = Path.Combine(root, "schedule." + extension);
                var export = RunMarkupCli("export", input, "--gantt-date-anchor", "2028-01-01", "--output", output);
                Assert(export.ExitCode == 0, export.StandardError);
                if (extension == "png") {
                    var png = File.ReadAllBytes(output);
                    Assert(png.Length > 64 && png.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }), "CLI must render an anchored PNG.");
                } else AssertAnchoredGanttTask(File.ReadAllText(output));
            }
        } finally { Directory.Delete(root, true); }
    }

    private static void AssertAnchoredGanttTask(string markup) {
        var start = markup.IndexOf("<svg", StringComparison.Ordinal);
        var end = markup.IndexOf("</svg>", start, StringComparison.Ordinal) + 6;
        var svg = XDocument.Parse(markup.Substring(start, end - start));
        var task = svg.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "gantt-task");
        Assert((double)task.Attribute("data-cfx-start")! == new DateTime(2028, 2, 29).ToOADate(), "CLI must resolve the anchored leap-day start.");
        Assert((double)task.Attribute("data-cfx-end")! == new DateTime(2028, 3, 1).ToOADate(), "CLI must resolve the anchored task end.");
    }

    private static void MarkupCliRejectsInvalidGanttDateAnchor() {
        var root = Path.Combine(Path.GetTempPath(), "chartforgex-cli-anchor-errors-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try {
            var input = Path.Combine(root, "schedule.md");
            File.WriteAllText(input, "```mermaid\ngantt\ndateFormat MM-DD\nTask :02-29,1d\n```");
            foreach (var command in new[] { "validate", "preview", "export" }) {
                var output = Path.Combine(root, "invalid.svg");
                var suffix = command == "export" ? new[] { "--output", output } : Array.Empty<string>();
                var missing = RunMarkupCli(command, input, suffix);
                Assert(missing.ExitCode == 2 && missing.StandardError.Contains("GanttDateAnchor", StringComparison.Ordinal), "An omitted anchor must retain the source diagnostic.");
                foreach (var anchor in new[] { "2027-02-29", "2028-1-1", "2028-01-01T00:00:00", "garbage" }) {
                    var args = new[] { "--gantt-date-anchor", anchor }.Concat(suffix).ToArray();
                    var invalid = RunMarkupCli(command, input, args);
                    Assert(invalid.ExitCode == 1 && invalid.StandardError.Contains("YYYY-MM-DD", StringComparison.Ordinal), "Invalid anchor must be an option error.");
                }
                var missingValue = RunMarkupCli(command, input, "--gantt-date-anchor");
                Assert(missingValue.ExitCode == 1 && missingValue.StandardError.Contains("requires a value", StringComparison.Ordinal), "A missing option value must not use the current date.");
                Assert(!File.Exists(output), "Invalid date context must not produce an export.");
            }
        } finally { Directory.Delete(root, true); }
    }
}
