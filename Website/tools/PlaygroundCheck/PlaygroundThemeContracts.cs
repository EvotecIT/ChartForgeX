using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Playground;
using ChartForgeX.VisualArtifacts;

/// <summary>Checks authored Mermaid palette precedence through the actual playground and Markdown bridge.</summary>
internal static class PlaygroundThemeContracts {
    internal static void Check(List<string> failures, string? output) {
        if (output != null) Directory.CreateDirectory(output);
        var sources = new[] {
            (Id: "pie", Source: "pie\n\"Completed\" : 7\n\"Remaining\" : 3"),
            (Id: "flowchart", Source: "flowchart LR\na[Start] --> b[Done]"),
            (Id: "sequence", Source: "sequenceDiagram\nAlice->>Bob: Completed"),
            (Id: "packet", Source: "packet-beta\n0-3: \"Flags\"\n4-7: \"Payload\"")
        };
        var attempted = 0;
        foreach (var item in sources) {
            foreach (var sourceTheme in new[] { "default", "dark" }) {
                var source = "---\nconfig:\n  theme: " + sourceTheme + "\n---\n" + item.Source;
                var fence = "```mermaid\n" + source + "\n```";
                var direct = MermaidRenderer.Render(source).Artifact;
                var bridge = new MermaidVisualMarkupParser().Parse(fence);
                if (direct == null || bridge.HasErrors || bridge.Artifacts.Count != 1) {
                    failures.Add("theme contract " + item.Id + ": direct/fenced artifacts failed");
                    continue;
                }
                if (Normalize(direct.ToSvg(null, "theme-proof")) != Normalize(bridge.Artifacts[0].ToSvg(null, "theme-proof")))
                    failures.Add("theme contract " + item.Id + ": direct/fenced SVG differs for " + sourceTheme);
                var png = direct.ToPng();
                if (!png.SequenceEqual(bridge.Artifacts[0].ToPng()))
                    failures.Add("theme contract " + item.Id + ": direct/fenced native PNG differs for " + sourceTheme);
                string? expected = null;
                foreach (var page in new[] { "light", "dark" }) {
                    foreach (var language in new[] { "mermaid", "chartforgex" }) {
                        attempted++;
                        var result = Render(language, language == "mermaid" ? source : fence, page);
                        if (!result.GetProperty("ok").GetBoolean()) {
                            failures.Add("theme contract " + item.Id + "/" + sourceTheme + "/" + language + ": render failed");
                            continue;
                        }
                        var svg = Normalize(result.GetProperty("svg").GetString()!);
                        expected ??= svg;
                        if (svg != expected) failures.Add("theme contract " + item.Id + "/" + sourceTheme + "/" + language + "/" + page + ": page theme replaced the authored palette");
                        if (result.GetProperty("themed").GetBoolean()) failures.Add("theme contract " + item.Id + ": an authored palette is reported as page-themed");
                        if (output != null) File.WriteAllText(Path.Combine(output, item.Id + "-" + sourceTheme + "-" + language + "-" + page + ".svg"), result.GetProperty("svg").GetString());
                    }
                }
                if (output != null) File.WriteAllBytes(Path.Combine(output, item.Id + "-" + sourceTheme + "-native.png"), png);
            }
            string? light = null;
            foreach (var page in new[] { "light", "dark" }) {
                var direct = Render("mermaid", item.Source, page);
                var fenced = Render("chartforgex", "```mermaid\n" + item.Source + "\n```", page);
                attempted += 2;
                if (!direct.GetProperty("ok").GetBoolean() || !fenced.GetProperty("ok").GetBoolean()
                    || Normalize(direct.GetProperty("svg").GetString()!) != Normalize(fenced.GetProperty("svg").GetString()!)
                    || !direct.GetProperty("themed").GetBoolean() || !fenced.GetProperty("themed").GetBoolean())
                    failures.Add("theme contract " + item.Id + "/" + page + ": unthemed source did not use the page palette consistently");
                var svg = Normalize(direct.GetProperty("svg").GetString()!);
                if (page == "light") light = svg;
                else if (svg == light) failures.Add("theme contract " + item.Id + ": page palette selection had no visible effect for unthemed source");
            }
        }
        Console.WriteLine("Playground theme contracts: " + attempted + " actual engine calls plus direct/fenced native exports.");
    }

    private static JsonElement Render(string language, string source, string theme) {
        using var document = JsonDocument.Parse(PlaygroundEngine.Render(language, source, theme));
        return document.RootElement.Clone();
    }

    private static string Normalize(string svg) {
        // Producers scope their document IDs differently. Compare all geometry,
        // typography, source facts and paint after replacing only that namespace.
        var id = (string?)XDocument.Parse(svg).Root?.Attribute("id");
        if (!string.IsNullOrEmpty(id)) svg = svg.Replace(id, "cfx-theme-proof", StringComparison.Ordinal);
        return Regex.Replace(svg, "cfx-pg-[0-9]+", "cfx-pg");
    }
}
