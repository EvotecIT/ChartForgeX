using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Playground;
using ChartForgeX.Raster;

/// <summary>Exercises the same bounded native PNG request used by the actual playground download button.</summary>
internal static partial class PlaygroundPngContracts {
    internal static void Check(List<string> failures, string? output) {
        if (output != null) Directory.CreateDirectory(output);
        var rendered = 0;
        foreach (var sample in PlaygroundSamples.All) {
            foreach (var theme in new[] { "light", "dark" }) {
                try {
                    var svg = Render(sample.Language, sample.Source, theme);
                    var png = Render(sample.Language, sample.Source, theme, "png");
                    RequireOk(svg); RequireOk(png);
                    var frame = CheckComposition(svg);
                    var bytes = CheckPng(png, frame.Width, frame.Height);
                    if (png.GetProperty("svg").ValueKind != JsonValueKind.Null || png.GetProperty("downloadSvg").ValueKind != JsonValueKind.Null)
                        throw new InvalidOperationException("PNG JSON duplicates SVG payloads across the WebAssembly bridge.");
                    if (png.GetProperty("artifactCount").GetInt32() != svg.GetProperty("artifactCount").GetInt32())
                        throw new InvalidOperationException("PNG export lost a source fence.");
                    if (output != null) {
                        File.WriteAllText(Path.Combine(output, sample.Id + "-" + theme + ".svg"), svg.GetProperty("downloadSvg").GetString());
                        File.WriteAllBytes(Path.Combine(output, sample.Id + "-" + theme + ".png"), bytes);
                    }
                    rendered++;
                } catch (Exception exception) {
                    failures.Add("native PNG " + sample.Id + "/" + theme + ": " + exception.Message);
                }
            }
        }

        CheckFonts(failures);
        CheckStack(failures, output);
        CheckBounds(failures);
        Console.WriteLine("Playground native PNG contracts: " + rendered + " actual sample/theme exports; composition, fonts, structured errors and limits checked.");
    }

    private static void CheckFonts(List<string> failures) {
        foreach (var id in new[] { "cfx-gantt", "mermaid-flowchart", "mermaid-class" }) {
            var sample = PlaygroundSamples.All.First(item => item.Id == id);
            foreach (var language in sample.Language == "mermaid" ? new[] { "mermaid", "chartforgex" } : new[] { sample.Language }) {
                var source = language == "chartforgex" && sample.Language == "mermaid" ? "```mermaid\n" + sample.Source + "\n```" : sample.Source;
                var defaults = Render(language, source, "light");
                RequireOk(defaults);
                var texts = XDocument.Parse(defaults.GetProperty("downloadSvg").GetString()!).Descendants().Where(element => element.Name.LocalName == "text").ToArray();
                if (texts.Length == 0 || texts.Any(element => (string?)element.Attribute("font-family") != "Carlito, sans-serif"))
                    failures.Add("The completed native SVG does not explicitly select the shipped measurement font: " + id + "/" + language);
            }
        }

        const string authored = "---\nconfig:\n  fontFamily: serif\n  theme: dark\n---\npie\n  \"A\" : 1\n  \"B\" : 2";
        foreach (var language in new[] { "mermaid", "chartforgex" }) {
            var source = language == "mermaid" ? authored : "```mermaid\n" + authored + "\n```";
            var result = Render(language, source, "light");
            RequireOk(result);
            var authoredTexts = XDocument.Parse(result.GetProperty("downloadSvg").GetString()!).Descendants()
                .Where(element => element.Name.LocalName == "text").ToArray();
            if (authoredTexts.Length == 0 || authoredTexts.Any(element => (string?)element.Attribute("font-family") != "serif"))
                failures.Add("An authored Mermaid font family was replaced by the playground default: " + language);
        }
    }

    private static void CheckStack(List<string> failures, string? output) {
        var gantt = PlaygroundSamples.All.First(item => item.Id == "cfx-gantt").Source;
        var source = gantt + "\n```mermaid\npie\n  title Stacked native export\n  \"A\" : 1\n  \"B\" : 3\n```";
        var svg = Render("chartforgex", source, "dark");
        var png = Render("chartforgex", source, "dark", "png");
        RequireOk(svg); RequireOk(png);
        var frame = CheckComposition(svg);
        var bytes = CheckPng(png, frame.Width, frame.Height);
        var image = RasterImageDecoder.Decode(bytes);
        var parts = XDocument.Parse("<root>" + svg.GetProperty("svg").GetString() + "</root>").Root!.Elements().ToArray();
        if (parts.Length != 2) throw new InvalidOperationException("The stacked fixture did not produce its two native artifacts.");
        var middleGapRow = checked((int)Math.Floor((Dimension(parts[0], "height") + 16) * 2));
        if (Enumerable.Range(0, image.Width).Any(x => image.Pixels[(middleGapRow * image.Width + x) * 4 + 3] != 0))
            failures.Add("The stacked PNG lost its transparent 32-unit gap.");
        if (output != null) {
            File.WriteAllText(Path.Combine(output, "gantt-pie-stack.svg"), svg.GetProperty("downloadSvg").GetString());
            File.WriteAllBytes(Path.Combine(output, "gantt-pie-stack.png"), bytes);
        }

        // This real bundled sequence has a fractional native height. Output density must be
        // applied before ceiling, retaining the original completed viewBox and geometry.
        var sequence = PlaygroundSamples.All.First(item => item.Id == "mermaid-sequence");
        var sequenceSvg = Render(sequence.Language, sequence.Source, "light");
        var sequenceFrame = CheckComposition(sequenceSvg);
        var sequencePng = Render(sequence.Language, sequence.Source, "light", "png");
        RequireOk(sequencePng);
        CheckPng(sequencePng, sequenceFrame.Width, sequenceFrame.Height);
        if (sequenceFrame.Height == Math.Floor(sequenceFrame.Height)) failures.Add("The fractional sequence fixture no longer exercises density rounding.");
    }

    private static void CheckBounds(List<string> failures) {
        var chart = "```chartforgex chart v1\ntype bar\nsize 2100x2100\nlabels A B\nvalues 1 2\n```";
        RequireOk(Render("chartforgex", chart, "light"));
        ExpectFailure(failures, Render("chartforgex", chart, "light", "png"), "17640000", "16777216");
        var panel = "```chartforgex chart v1\ntype bar\nsize 1450x1450\nlabels A B\nvalues 1 2\n```";
        RequireOk(Render("chartforgex", panel + "\n" + panel, "light"));
        ExpectFailure(failures, Render("chartforgex", panel + "\n" + panel, "light", "png"), "17005600", "16777216");

        var captured = Render("mermaid", "pie\n  \"A\" : 1\n  \"B\" : 2", "light", "png",
            new MermaidResourceLimits { MaximumRasterPixels = 1 });
        ExpectFailure(failures, captured, "MaximumRasterPixels");
        if (captured.GetProperty("line").ValueKind != JsonValueKind.Number || captured.GetProperty("column").ValueKind != JsonValueKind.Number)
            failures.Add("A captured Mermaid PNG policy rejection lost its source location.");

        var ordinary = PlaygroundSamples.All.First(item => item.Id == "cfx-bar");
        ExpectFailure(failures, Render(ordinary.Language, ordinary.Source, "light", "png", new MermaidResourceLimits { MaximumOutputBytes = 1 }), "bytes", "1");
        ExpectFailure(failures, Render("mermaid", "sequenceDiagram\n A->>B: hello\n invalid ->>", "light", "png"));
        ExpectFailure(failures, Render("mermaid", "pie\n \"A\" : 1", "light", "jpeg"), "Unknown export format");
    }

    private static (double Width, double Height) CheckComposition(JsonElement result) {
        RequireOk(result);
        var native = XDocument.Parse("<root>" + result.GetProperty("svg").GetString() + "</root>").Root!.Elements().ToArray();
        var composed = XElement.Parse(result.GetProperty("downloadSvg").GetString()!);
        var width = native.Max(element => Dimension(element, "width"));
        var height = native.Sum(element => Dimension(element, "height")) + 32 * (native.Length - 1);
        if (Dimension(composed, "width") != width || Dimension(composed, "height") != height)
            throw new InvalidOperationException("The completed download frame differs from the visible native artifacts.");
        var children = native.Length == 1 ? new[] { composed } : composed.Elements().ToArray();
        if (children.Length != native.Length) throw new InvalidOperationException("SVG composition lost a source artifact.");
        double y = 0;
        for (var index = 0; index < native.Length; index++) {
            var expected = new XElement(native[index]);
            var actual = new XElement(children[index]);
            expected.Attribute("style")?.Remove();
            if (native.Length > 1) {
                if (Dimension(actual, "x") != Math.Floor((width - Dimension(expected, "width")) / 2 + .5) || Dimension(actual, "y") != y)
                    throw new InvalidOperationException("SVG composition changed a centered native frame.");
                actual.Attribute("x")?.Remove(); actual.Attribute("y")?.Remove();
            }
            // Namespace declarations serialize differently when a document becomes a child.
            foreach (var element in expected.DescendantsAndSelf().Concat(actual.DescendantsAndSelf()))
                foreach (var declaration in element.Attributes().Where(attribute => attribute.IsNamespaceDeclaration).ToArray()) declaration.Remove();
            if (!XNode.DeepEquals(expected, actual)) throw new InvalidOperationException("Composition changed native geometry, text or paint.");
            y += Dimension(expected, "height") + 32;
        }
        return (width, height);
    }

    private static byte[] CheckPng(JsonElement result, double width, double height) {
        if (result.GetProperty("format").GetString() != "png") throw new InvalidOperationException("PNG export did not identify its format.");
        var bytes = Convert.FromBase64String(result.GetProperty("png").GetString()!);
        var expectedWidth = checked((int)Math.Ceiling(width * 2));
        var expectedHeight = checked((int)Math.Ceiling(height * 2));
        if (bytes.Length < 24 || !bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })
            || BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(16, 4)) != expectedWidth
            || BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(20, 4)) != expectedHeight
            || result.GetProperty("width").GetInt32() != expectedWidth || result.GetProperty("height").GetInt32() != expectedHeight)
            throw new InvalidOperationException("PNG bytes or metadata lost the exact natural 2x frame.");
        return bytes;
    }

    private static JsonElement Render(string language, string source, string theme, string format = "svg", MermaidResourceLimits? limits = null) {
        using var document = JsonDocument.Parse(limits == null ? PlaygroundEngine.Render(language, source, theme, format)
            : PlaygroundEngine.Render(language, source, theme, format, limits));
        return document.RootElement.Clone();
    }

    private static void RequireOk(JsonElement result) {
        if (!result.GetProperty("ok").GetBoolean()) throw new InvalidOperationException(result.GetProperty("error").GetString());
    }

    private static void ExpectFailure(List<string> failures, JsonElement result, params string[] fragments) {
        var error = result.GetProperty("error").GetString() ?? string.Empty;
        if (result.GetProperty("ok").GetBoolean() || error.Length == 0 || fragments.Any(fragment => !error.Contains(fragment, StringComparison.Ordinal))
            || result.GetProperty("png").ValueKind != JsonValueKind.Null || result.GetProperty("width").ValueKind != JsonValueKind.Null
            || result.GetProperty("height").ValueKind != JsonValueKind.Null)
            failures.Add("A native export failure returned partial output or lost its structured error: " + error);
    }

    private static double Dimension(XElement element, string name) => double.Parse((string)element.Attribute(name)!, CultureInfo.InvariantCulture);
}
