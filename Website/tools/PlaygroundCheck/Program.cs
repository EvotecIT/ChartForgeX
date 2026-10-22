// Renders every playground sample through the same engine code the browser runs, in both themes.
// Usage: dotnet run --project Website/tools/PlaygroundCheck -c Release [-- --theme-proof [output-dir] --png-proof <dir> --fixtures <dir-with-.mmd-files>]
// Exit code 1 when a sample fails, a theme is not applied, or an error path stops returning structured JSON.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using ChartForgeX.Playground;

var failures = new List<string>();

JsonElement Render(string language, string source, string theme) {
    var json = PlaygroundEngine.Render(language, source, theme);
    using var document = JsonDocument.Parse(json);
    return document.RootElement.Clone();
}

static string Text(JsonElement element, string name) =>
    element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : string.Empty;

// Samples JSON must parse and match the source list.
using (var samplesDocument = JsonDocument.Parse(PlaygroundEngine.SamplesJson())) {
    if (samplesDocument.RootElement.GetArrayLength() != PlaygroundSamples.All.Count) failures.Add("Samples() JSON count does not match the sample list.");
}
using (var info = JsonDocument.Parse(PlaygroundEngine.InfoJson())) {
    Console.WriteLine("Engine info: " + info.RootElement.GetRawText());
}

// Warm up once so the timings below reflect steady state rather than JIT.
Render("mermaid", PlaygroundSamples.All[0].Source, "light");

Console.WriteLine();
Console.WriteLine($"{"Sample",-22} {"Type",-20} {"Light ms",9} {"Dark ms",8} {"SVG KB",7}  Notes");
foreach (var sample in PlaygroundSamples.All) {
    var light = Render(sample.Language, sample.Source, "light");
    var dark = Render(sample.Language, sample.Source, "dark");
    var notes = new List<string>();
    foreach (var (theme, result) in new[] { ("light", light), ("dark", dark) }) {
        if (!result.GetProperty("ok").GetBoolean()) {
            failures.Add($"{sample.Id} ({theme}): {Text(result, "error")} (line {result.GetProperty("line")})");
            notes.Add(theme + " FAILED");
        } else if (!Text(result, "svg").Contains("<svg", StringComparison.Ordinal)) {
            failures.Add($"{sample.Id} ({theme}): no <svg> in output");
        }

        foreach (var diagnostic in result.GetProperty("diagnostics").EnumerateArray()) {
            notes.Add($"{theme} {Text(diagnostic, "severity")} {Text(diagnostic, "code")}: {Text(diagnostic, "message")}");
        }
    }

    if (light.GetProperty("ok").GetBoolean() && dark.GetProperty("ok").GetBoolean()) {
        var themed = dark.GetProperty("themed").GetBoolean();
        if (!themed) notes.Add("page theme not applied (fixed palette)");
        if (themed && StripScopes(Text(light, "svg")) == StripScopes(Text(dark, "svg"))) {
            failures.Add($"{sample.Id}: reports themed=true but light and dark SVG are identical");
        }
        if (light.GetProperty("artifactCount").GetInt32() > 1) notes.Add(light.GetProperty("artifactCount").GetInt32() + " artifacts");
    }

    Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0,-22} {1,-20} {2,9:0.0} {3,8:0.0} {4,7:0.0}  {5}",
        sample.Id, Text(light, "diagramType"), light.GetProperty("elapsedMs").GetDouble(), dark.GetProperty("elapsedMs").GetDouble(),
        Text(light, "svg").Length / 1024d, string.Join("; ", notes.Distinct())));
}

// Error paths must come back as structured JSON with useful locations.
void ExpectFailure(string label, string language, string source, string theme, bool expectLine) {
    var result = Render(language, source, theme);
    var ok = result.GetProperty("ok").GetBoolean();
    var hasLine = result.GetProperty("line").ValueKind == JsonValueKind.Number;
    Console.WriteLine($"[error path] {label}: ok={ok} line={result.GetProperty("line")} column={result.GetProperty("column")} error={Text(result, "error")}");
    if (ok || Text(result, "error").Length == 0) failures.Add($"error path '{label}' did not fail with a message");
    if (expectLine && !hasLine) failures.Add($"error path '{label}' did not report a line");
}

Console.WriteLine();
ExpectFailure("unknown language", "plantuml", "@startuml", "light", false);
ExpectFailure("unknown theme", "mermaid", "pie\n\"A\" : 1", "sepia", false);
ExpectFailure("empty source", "mermaid", "   ", "light", false);
ExpectFailure("oversized source", "mermaid", "flowchart LR\n" + new string('%', PlaygroundEngine.MaxSourceLength), "light", false);
ExpectFailure("unknown mermaid type", "mermaid", "notADiagram\n  a --> b", "light", true);
ExpectFailure("mermaid syntax error", "mermaid", "sequenceDiagram\n  Alice->>Bob: hi\n  this is not valid ->>", "light", true);
ExpectFailure("unsupported railroad literal prefix", "mermaid", "railroad-abnf-beta\n  rule = %s\"x\" ;", "light", true);
ExpectFailure("markup without fence", "chartforgex", "type bar\nvalues 1 2 3", "light", true);
ExpectFailure("markup bad version", "chartforgex", "```chartforgex chart v2\nvalues 1 2\n```", "light", true);
ExpectFailure("markup bad option", "chartforgex", "```chartforgex chart v1\ntype bar\nvalues 1 2\noption legend maybe\n```", "dark", true);
var fenced = Render("mermaid", "```mermaid\npie\n  \"A\" : 1\n  \"B\" : 2\n```", "light");
if (!fenced.GetProperty("ok").GetBoolean()) failures.Add("a pasted ```mermaid fence should render");
var themeProofIndex = Array.IndexOf(args, "--theme-proof");
PlaygroundThemeContracts.Check(failures,
    themeProofIndex >= 0 && themeProofIndex + 1 < args.Length
        && !args[themeProofIndex + 1].StartsWith("--", StringComparison.Ordinal)
        ? args[themeProofIndex + 1] : null);
var pngProofIndex = Array.IndexOf(args, "--png-proof");
var pngProofOutput = pngProofIndex >= 0 && pngProofIndex + 1 < args.Length
    && !args[pngProofIndex + 1].StartsWith("--", StringComparison.Ordinal)
    ? args[pngProofIndex + 1] : null;
PlaygroundPngContracts.Check(failures, pngProofOutput);
PlaygroundPngContracts.CheckNativeFamilies(failures, pngProofOutput);

// Optional: every Mermaid fixture, to document which families or constructs the playground cannot render.
var fixtureIndex = Array.IndexOf(args, "--fixtures");
if (fixtureIndex >= 0 && fixtureIndex + 1 < args.Length) {
    Console.WriteLine();
    var rendered = 0;
    var failed = 0;
    foreach (var file in Directory.EnumerateFiles(args[fixtureIndex + 1], "*.mmd").OrderBy(path => path, StringComparer.Ordinal)) {
        var result = Render("mermaid", File.ReadAllText(file), "dark");
        if (result.GetProperty("ok").GetBoolean()) {
            rendered++;
            continue;
        }

        failed++;
        Console.WriteLine($"[fixture] {Path.GetFileName(file)} ({Text(result, "diagramType")}): line {result.GetProperty("line")}: {Text(result, "error")}");
    }

    Console.WriteLine($"[fixture] {rendered} rendered, {failed} failed (informational, not gating)");
}

Console.WriteLine();
if (failures.Count == 0) {
    Console.WriteLine($"Playground check passed: {PlaygroundSamples.All.Count} samples in light and dark.");
    return 0;
}

foreach (var failure in failures) Console.Error.WriteLine("FAIL " + failure);
return 1;

static string StripScopes(string svg) => System.Text.RegularExpressions.Regex.Replace(svg, "cfx-pg-[0-9]+", "cfx-pg");
