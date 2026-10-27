using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ChartForgeX.Raster;

/// <summary>Exercises native language families through the actual raw, pasted-fence and Markdown host boundaries.</summary>
internal static partial class PlaygroundPngContracts {
    internal static void CheckNativeFamilies(List<string> failures, string? output) {
        var rows = new List<object>();
        var initialFailures = failures.Count;
        if (output != null) Directory.CreateDirectory(output);
        var fixtures = new[] {
            (Id: "agentflow-native-workflow", Family: "agentflow", Text: new[] { "Review team", "Request", "Publish" }),
            (Id: "zenuml-native-occurrences", Family: "zenuml", Text: new[] { "Checkout occurrences", "User", "Store" }),
            (Id: "railroad-ir-native", Family: "railroad", Text: new[] { "Expression grammar", "expression", "term" }),
            (Id: "railroad-ebnf-native", Family: "railroad", Text: new[] { "Identifier grammar", "identifier", "keyword" }),
            (Id: "railroad-abnf-native", Family: "railroad", Text: new[] { "Address grammar", "address", "local-part" }),
            (Id: "railroad-abnf-case-sensitive", Family: "railroad", Text: new[] { "Case-sensitive grammar", "literal", "\"Exact\"" }),
            (Id: "railroad-peg-native", Family: "railroad", Text: new[] { "Identifier grammar", "identifier", "keyword" })
        };
        foreach (var fixture in fixtures) {
            string source;
            try { source = ReadFixture(fixture.Id); }
            catch (InvalidOperationException exception) { failures.Add("native family fixture " + fixture.Id + ": " + exception.Message); continue; }
            string? lightSvg = null;
            foreach (var theme in new[] { "light", "dark" }) {
                string? expectedSvg = null;
                byte[]? expectedPng = null;
                foreach (var route in Routes(source)) {
                    try {
                        var svg = Render(route.Language, route.Source, theme);
                        RequireOk(svg);
                        if (output != null) {
                            var observed = Path.Combine(output, fixture.Id + "-" + theme + "-" + route.Id);
                            File.WriteAllText(observed + ".reply.json", svg.GetRawText());
                            File.WriteAllText(observed + ".observed.svg", svg.GetProperty("downloadSvg").GetString());
                        }
                        if (svg.GetProperty("artifactCount").GetInt32() != 1 || !svg.GetProperty("themed").GetBoolean())
                            throw new InvalidOperationException("The native family lost its single artifact or unowned page-theme selection.");
                        if (route.Language == "mermaid" && svg.GetProperty("diagramType").GetString() != fixture.Family)
                            throw new InvalidOperationException("The declared family did not reach its native parser.");
                        var frame = CheckComposition(svg);
                        var text = XDocument.Parse(svg.GetProperty("downloadSvg").GetString()!).Descendants()
                            .Where(element => element.Name.LocalName == "text").Select(element => element.Value).ToArray();
                        foreach (var fact in fixture.Text)
                            if (!text.Contains(fact, StringComparer.Ordinal)) throw new InvalidOperationException("A visible authored fact was lost: " + fact);
                        var normalized = NormalizeFamilySvg(svg.GetProperty("downloadSvg").GetString()!);
                        if (expectedSvg != null && normalized != expectedSvg)
                            throw new InvalidOperationException("Raw, pasted and Markdown source changed native geometry, text or paint.");
                        var png = Render(route.Language, route.Source, theme, "png");
                        RequireOk(png);
                        var bytes = CheckPng(png, frame.Width, frame.Height);
                        if (png.GetProperty("artifactCount").GetInt32() != 1 || png.GetProperty("svg").ValueKind != JsonValueKind.Null
                            || png.GetProperty("downloadSvg").ValueKind != JsonValueKind.Null)
                            throw new InvalidOperationException("The native PNG reply lost a fence or duplicated its SVG payload.");
                        if (expectedPng != null && !bytes.SequenceEqual(expectedPng))
                            throw new InvalidOperationException("Native PNG bytes differ between raw, pasted and Markdown source.");
                        if (expectedPng == null && !HasPaintVariation(RasterImageDecoder.Decode(bytes).Pixels))
                            throw new InvalidOperationException("Native PNG contains no distinct painted content.");
                        expectedSvg ??= normalized; expectedPng ??= bytes;
                        if (output != null) {
                            var stem = Path.Combine(output, fixture.Id + "-" + theme + "-" + route.Id);
                            File.WriteAllText(stem + ".svg", svg.GetProperty("downloadSvg").GetString());
                            File.WriteAllBytes(stem + ".png", bytes);
                        }
                        rows.Add(new { fixture.Id, fixture.Family, theme, route = route.Id,
                            sourceTextSha256 = Digest(Encoding.UTF8.GetBytes(source)), pngSha256 = Digest(bytes),
                            width = png.GetProperty("width").GetInt32(), height = png.GetProperty("height").GetInt32() });
                    } catch (Exception exception) {
                        failures.Add("native family host " + fixture.Id + "/" + theme + "/" + route.Id + ": " + exception.Message);
                    }
                }
                if (theme == "light") lightSvg = expectedSvg;
                else if (lightSvg != null && lightSvg == expectedSvg)
                    failures.Add("native family host " + fixture.Id + ": selecting dark mode had no visible effect.");
            }
        }
        CheckNativeFamilyRejections(failures, output);
        if (output != null) File.WriteAllText(Path.Combine(output, "native-family-host-proof.json"), JsonSerializer.Serialize(new {
            passed = failures.Count == initialFailures, successfulSvgPngPairs = rows.Count, rows
        }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("Playground native family host contracts: " + rows.Count + " actual source/theme/route SVG+PNG pairs; structured rejections checked.");
    }

    private static void CheckNativeFamilyRejections(List<string> failures, string? output) {
        var rows = new List<object>();
        var cases = new[] {
            (Id: "agentflow-unclosed-metadata", Source: "agentflow-beta\n  a@{ policy: { retries: 2 }"),
            (Id: "zenuml-unsupported-declaration", Source: "zenuml\n  participant Alice unsupported"),
            (Id: "railroad-malformed-case-sensitive-literal", Source: "railroad-abnf-beta\n  rule = %s 'x' ;")
        };
        foreach (var item in cases) foreach (var route in Routes(item.Source)) foreach (var format in new[] { "svg", "png" }) {
            var result = Render(route.Language, route.Source, "light", format);
            var line = route.Id == "raw" ? 2 : 3;
            if (result.GetProperty("ok").GetBoolean() || result.GetProperty("error").ValueKind != JsonValueKind.String
                || result.GetProperty("error").GetString()!.Length == 0 || result.GetProperty("line").ValueKind != JsonValueKind.Number
                || result.GetProperty("line").GetInt32() != line
                || !result.GetProperty("diagnostics").EnumerateArray().Any(diagnostic => diagnostic.GetProperty("severity").GetString() == "error"
                    && diagnostic.GetProperty("code").GetString() == "CFXM008"))
                failures.Add("native family rejection lost its located invalid/unsupported grammar: " + item.Id + "/" + route.Id + "/" + format);
            if (new[] { "svg", "downloadSvg", "png", "width", "height" }.Any(property => result.GetProperty(property).ValueKind != JsonValueKind.Null)
                || result.GetProperty("artifactCount").GetInt32() != 0)
                failures.Add("native family rejection returned partial output: " + item.Id + "/" + route.Id + "/" + format);
            rows.Add(new { item.Id, route = route.Id, format, result });
        }
        if (output != null) File.WriteAllText(Path.Combine(output, "native-family-rejections.json"),
            JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static (string Id, string Language, string Source)[] Routes(string source) => new[] {
        ("raw", "mermaid", source), ("pasted", "mermaid", "```mermaid\n" + source + "\n```"),
        ("markdown", "chartforgex", "```mermaid\n" + source + "\n```")
    };

    private static string ReadFixture(string id) {
        using var stream = typeof(PlaygroundPngContracts).Assembly.GetManifestResourceStream("PlaygroundCheck.MermaidFixtures." + id + ".mmd")
            ?? throw new InvalidOperationException("Missing native family host fixture: " + id);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    private static string NormalizeFamilySvg(string svg) {
        var id = (string?)XDocument.Parse(svg).Root?.Attribute("id");
        if (!string.IsNullOrEmpty(id)) svg = svg.Replace(id, "cfx-native-family-proof", StringComparison.Ordinal);
        return Regex.Replace(svg, "cfx-pg-[0-9]+", "cfx-pg");
    }

    private static bool HasPaintVariation(byte[] pixels) {
        var first = -1;
        for (var index = 0; index < pixels.Length; index += 4) {
            if (pixels[index + 3] == 0) continue;
            if (first < 0) first = index;
            else if (pixels[index] != pixels[first] || pixels[index + 1] != pixels[first + 1]
                || pixels[index + 2] != pixels[first + 2] || pixels[index + 3] != pixels[first + 3]) return true;
        }
        return false;
    }
    private static string Digest(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
}
