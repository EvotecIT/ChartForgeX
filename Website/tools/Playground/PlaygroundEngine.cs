using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Markup;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Playground;

/// <summary>
/// Renders playground source to SVG and reports the outcome as JSON. Shared by the browser exports and the desktop sample check,
/// so every call path is identical. Nothing in here throws: every failure is returned as structured JSON.
/// </summary>
public static class PlaygroundEngine {
    /// <summary>Largest accepted source, in UTF-16 characters.</summary>
    public const int MaxSourceLength = 64 * 1024;

    /// <summary>Language name for raw Mermaid source.</summary>
    public const string MermaidLanguage = "mermaid";

    /// <summary>Language name for ChartForgeX markup: Markdown containing <c>chartforgex &lt;kind&gt; v1</c> (and optionally <c>mermaid</c>) fences.</summary>
    public const string ChartForgeXLanguage = "chartforgex";

    private static int _renderCount;

    /// <summary>Renders <paramref name="source"/> and returns <c>{ ok, svg, error, line, column, diagramType, elapsedMs, ... }</c>.</summary>
    public static string Render(string? language, string? source, string? theme) {
        var stopwatch = Stopwatch.StartNew();
        var outcome = new RenderOutcome();
        try {
            PlaygroundFonts.EnsureRegistered();
            RenderCore(language, source, theme, outcome);
        } catch (Exception exception) {
            outcome.Fail("Internal renderer error (" + exception.GetType().Name + "): " + exception.Message, null, null);
        }

        stopwatch.Stop();
        return outcome.ToJson(stopwatch.Elapsed.TotalMilliseconds);
    }

    /// <summary>Returns the bundled samples as a JSON array of <c>{ id, title, language, source }</c>.</summary>
    public static string SamplesJson() {
        var json = new PlaygroundJson().BeginArray();
        foreach (var sample in PlaygroundSamples.All) {
            json.BeginObject()
                .Property("id", sample.Id)
                .Property("title", sample.Title)
                .Property("language", sample.Language)
                .Property("source", sample.Source)
                .EndObject();
        }

        return json.EndArray().ToString();
    }

    /// <summary>Returns engine facts for the page: version, languages, themes and the input limit.</summary>
    public static string InfoJson() {
        var assembly = typeof(Chart).Assembly;
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                      ?? assembly.GetName().Version?.ToString() ?? "unknown";
        var plus = version.IndexOf('+');
        if (plus > 0) version = version.Substring(0, plus);
        return new PlaygroundJson().BeginObject()
            .Property("version", version)
            .Property("runtime", Environment.Version.ToString())
            .Property("maxSourceLength", MaxSourceLength)
            .BeginArray("languages").Value(MermaidLanguage).Value(ChartForgeXLanguage).EndArray()
            .BeginArray("themes").Value("light").Value("dark").EndArray()
            .EndObject().ToString();
    }

    private static void RenderCore(string? language, string? source, string? theme, RenderOutcome outcome) {
        var normalizedLanguage = NormalizeLanguage(language);
        if (normalizedLanguage == null) {
            outcome.Fail("Unknown language '" + language + "'. Use '" + MermaidLanguage + "' or '" + ChartForgeXLanguage + "'.", null, null);
            return;
        }

        outcome.Language = normalizedLanguage;
        var normalizedTheme = (theme ?? "light").Trim().ToLowerInvariant();
        if (normalizedTheme.Length == 0) normalizedTheme = "light";
        if (normalizedTheme != "light" && normalizedTheme != "dark") {
            outcome.Fail("Unknown theme '" + theme + "'. Use 'light' or 'dark'.", null, null);
            return;
        }

        outcome.Theme = normalizedTheme;
        if (string.IsNullOrWhiteSpace(source)) {
            outcome.Fail("Source is empty.", null, null);
            return;
        }

        if (source!.Length > MaxSourceLength) {
            outcome.Fail("Source is " + source.Length + " characters; the playground accepts at most " + MaxSourceLength + ".", null, null);
            return;
        }

        var dark = normalizedTheme == "dark";
        var parseOptions = new MermaidParseOptions { GanttDateAnchor = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
        if (normalizedLanguage == MermaidLanguage) RenderMermaid(source, dark, parseOptions, outcome);
        else RenderMarkup(source, dark, parseOptions, outcome);
    }

    private static string? NormalizeLanguage(string? language) {
        switch ((language ?? string.Empty).Trim().ToLowerInvariant()) {
            case "mermaid":
            case "mmd":
                return MermaidLanguage;
            case "chartforgex":
            case "chartforgex-markup":
            case "cfx":
            case "markup":
                return ChartForgeXLanguage;
            default:
                return null;
        }
    }

    private static void RenderMermaid(string source, bool dark, MermaidParseOptions parseOptions, RenderOutcome outcome) {
        var text = StripMermaidFence(source, out var lineOffset);
        var parsed = new MermaidParser().Parse(text, parseOptions);
        foreach (var diagnostic in parsed.Diagnostics) {
            outcome.Add(diagnostic.Severity.ToString(), diagnostic.Code, diagnostic.Message, diagnostic.Span.Line + lineOffset, diagnostic.Span.Column);
        }

        var document = parsed.Document;
        if (document != null) outcome.DiagramType = TypeName(document.Kind.ToString());
        if (parsed.HasErrors) {
            outcome.FailFromFirstError();
            return;
        }

        if (document == null || document.Kind == MermaidDiagramKind.Unknown) {
            outcome.Fail("The Mermaid diagram type was not recognized.", 1 + lineOffset, 1);
            return;
        }

        if (document.IsDiagnosticOnly) {
            outcome.Fail("Mermaid '" + document.Header.Trim() + "' is recognized, but ChartForgeX does not render this diagram family yet.",
                document.HeaderSpan.Line + lineOffset, document.HeaderSpan.Column);
            return;
        }

        // The page theme applies unless the source chose its own theme through front matter or an init directive.
        if (document.Theme == null) {
            if (dark) document.Theme = "dark";
            outcome.Themed = true;
        }

        VisualArtifact artifact;
        try {
            artifact = document.ToVisualArtifact(new MermaidRenderOptions { ParseOptions = parseOptions });
        } catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException || exception is OverflowException) {
            outcome.Fail(exception.Message, document.HeaderSpan.Line + lineOffset, document.HeaderSpan.Column);
            return;
        }

        RenderArtifacts(new[] { artifact }, null, outcome);
    }

    private static void RenderMarkup(string source, bool dark, MermaidParseOptions parseOptions, RenderOutcome outcome) {
        var scan = VisualMarkupScanner.Scan(source);
        if (scan.Blocks.Count == 0 && scan.Diagnostics.Count == 0) {
            outcome.Fail("No visual fence found. Wrap ChartForgeX markup in a fence such as ```chartforgex chart v1 … ```.", 1, 1);
            return;
        }

        var parsed = new MermaidVisualMarkupParser(new MermaidRenderOptions { ParseOptions = parseOptions }).Parse(scan);
        foreach (var diagnostic in parsed.Diagnostics) {
            outcome.Add(diagnostic.Severity.ToString(), diagnostic.Code, diagnostic.Message, diagnostic.Line > 0 ? diagnostic.Line : null, null);
        }

        outcome.DiagramType = string.Join(",", scan.Blocks.Select(block => block.Kind == VisualMarkupKind.Mermaid
            ? "mermaid"
            : TypeName(block.Kind.ToString())).Distinct());
        if (parsed.HasErrors) {
            outcome.FailFromFirstError();
            return;
        }

        if (parsed.Artifacts.Count == 0) {
            outcome.FailFromFirstWarning("The markup did not produce a visual.");
            return;
        }

        outcome.Themed = true;
        RenderArtifacts(parsed.Artifacts, new PageTheme(dark), outcome);
    }

    // ChartForgeX markup has no theme syntax, so the page theme is applied to each model the way the site generator does.
    // Returns null when the model has no theme hook; the caller then renders its fixed palette.
    private static string? RenderThemed(VisualArtifact artifact, PageTheme theme, string scope) {
        switch (artifact.Model) {
            case Chart chart:
                chart.WithThemeMode(theme.Mode);
                return artifact.ToSvg(null, scope);
            case TopologyChart topology:
                topology.WithThemeMode(theme.Mode);
                return artifact.ToSvg(null, scope);
            case SequenceArtifact sequence:
                sequence.ThemeMode = theme.Mode;
                return artifact.ToSvg(null, scope);
            case FlowArtifact flow:
                // Same context as the convenience export, with the requested palette mode.
                var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(flow.Width, flow.Height), flow.Padding),
                    themeMode: theme.Mode, frame: new VisualFrame(showLegend: false));
                return flow.Prepare(context).ToSvg(new VisualSvgOptions(scope));
            case TableArtifact table:
                var preview = table.ToPreviewBlock();
                preview.Options.ThemeMode = theme.Mode;
                return preview.RenderSvg(scope);
            case IVisualBlock block when artifact.RenderSource == null:
                block.Options.ThemeMode = theme.Mode;
                return artifact.ToSvg(null, scope);
            default:
                return null;
        }
    }

    private static void RenderArtifacts(IReadOnlyList<VisualArtifact> artifacts, PageTheme? theme, RenderOutcome outcome) {
        var svg = new StringBuilder();
        foreach (var artifact in artifacts) {
            var scope = "cfx-pg-" + (++_renderCount).ToString(System.Globalization.CultureInfo.InvariantCulture);
            try {
                if (svg.Length > 0) svg.Append('\n');
                var themed = theme == null ? null : RenderThemed(artifact, theme, scope);
                if (theme != null && themed == null) outcome.Themed = false;
                svg.Append(themed ?? artifact.ToSvg(null, scope));
            } catch (TopologyValidationException exception) {
                foreach (var error in exception.Result.Errors) outcome.Add("Error", error.Code, error.Message, SourceLine(artifact), null);
                foreach (var warning in exception.Result.Warnings) outcome.Add("Warning", warning.Code, warning.Message, SourceLine(artifact), null);
                outcome.FailFromFirstError();
                return;
            } catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException || exception is OverflowException) {
                outcome.Fail(exception.Message, SourceLine(artifact), null);
                return;
            }
        }

        outcome.Succeed(svg.ToString(), artifacts.Count);
    }

    private static int? SourceLine(VisualArtifact artifact) =>
        artifact.Metadata.TryGetValue("sourceLine", out var value) && int.TryParse(value, out var line) ? line : null;

    // Accept a pasted ```mermaid fence; diagnostics keep pointing at the lines the author sees.
    private static string StripMermaidFence(string source, out int lineOffset) {
        lineOffset = 0;
        var normalized = source.Replace("\r\n", "\n").Replace('\r', '\n');
        var lines = normalized.Split('\n');
        var first = 0;
        while (first < lines.Length && lines[first].Trim().Length == 0) first++;
        if (first >= lines.Length) return normalized;
        var opening = lines[first].Trim();
        if (!(opening.StartsWith("```", StringComparison.Ordinal) || opening.StartsWith("~~~", StringComparison.Ordinal))) return normalized;
        var marker = opening.Substring(0, 3);
        var info = opening.TrimStart(marker[0]).Trim();
        if (!info.StartsWith("mermaid", StringComparison.OrdinalIgnoreCase)) return normalized;
        var last = lines.Length - 1;
        while (last > first && lines[last].Trim().Length == 0) last--;
        var end = last > first && lines[last].Trim().StartsWith(marker, StringComparison.Ordinal) ? last : last + 1;
        lineOffset = first + 1;
        return string.Join("\n", lines, first + 1, Math.Max(0, end - first - 1));
    }

    private static string TypeName(string value) => value.ToLowerInvariant();

    private sealed class PageTheme {
        public PageTheme(bool dark) {
            Mode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light;
        }

        public VisualThemeMode Mode { get; }
    }

    private sealed class RenderOutcome {
        private readonly List<Diagnostic> _diagnostics = new();

        public string? Language { get; set; }
        public string? Theme { get; set; }
        public string? DiagramType { get; set; }
        public bool Themed { get; set; }
        private bool Ok { get; set; }
        private string? Svg { get; set; }
        private string? Error { get; set; }
        private int? Line { get; set; }
        private int? Column { get; set; }
        private int ArtifactCount { get; set; }

        public void Add(string severity, string? code, string message, int? line, int? column) =>
            _diagnostics.Add(new Diagnostic(severity.ToLowerInvariant(), code ?? string.Empty, message, line, column));

        public void Succeed(string svg, int artifactCount) {
            Ok = true;
            Svg = svg;
            ArtifactCount = artifactCount;
            Error = null;
            Line = null;
            Column = null;
        }

        public void Fail(string error, int? line, int? column) {
            Ok = false;
            Svg = null;
            Error = error;
            Line = line;
            Column = column;
        }

        public void FailFromFirstError() {
            var first = _diagnostics.FirstOrDefault(diagnostic => diagnostic.Severity == "error");
            if (first == null) Fail("Rendering failed.", null, null);
            else Fail(Format(first), first.Line, first.Column);
        }

        public void FailFromFirstWarning(string fallback) {
            var first = _diagnostics.FirstOrDefault(diagnostic => diagnostic.Severity == "warning");
            if (first == null) Fail(fallback, null, null);
            else Fail(fallback + " " + Format(first), first.Line, first.Column);
        }

        private static string Format(Diagnostic diagnostic) =>
            diagnostic.Code.Length == 0 ? diagnostic.Message : diagnostic.Code + ": " + diagnostic.Message;

        public string ToJson(double elapsedMs) {
            var json = new PlaygroundJson().BeginObject()
                .Property("ok", Ok)
                .Property("svg", Svg)
                .Property("error", Error)
                .Property("line", Line)
                .Property("column", Column)
                .Property("diagramType", DiagramType)
                .Number("elapsedMs", elapsedMs)
                .Property("language", Language)
                .Property("theme", Theme)
                .Property("themed", Ok && Themed)
                .Property("artifactCount", ArtifactCount)
                .BeginArray("diagnostics");
            foreach (var diagnostic in _diagnostics) {
                json.BeginObject()
                    .Property("severity", diagnostic.Severity)
                    .Property("code", diagnostic.Code)
                    .Property("message", diagnostic.Message)
                    .Property("line", diagnostic.Line)
                    .Property("column", diagnostic.Column)
                    .EndObject();
            }

            return json.EndArray().EndObject().ToString();
        }
    }

    private sealed class Diagnostic {
        public Diagnostic(string severity, string code, string message, int? line, int? column) {
            Severity = severity;
            Code = code;
            Message = message;
            Line = line;
            Column = column;
        }

        public string Severity { get; }
        public string Code { get; }
        public string Message { get; }
        public int? Line { get; }
        public int? Column { get; }
    }
}
