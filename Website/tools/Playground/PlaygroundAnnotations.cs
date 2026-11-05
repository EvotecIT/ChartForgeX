using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Playground;

/// <summary>
/// Native host for the annotation editor page. The HTML runtime sends each accepted edit as a request carrying the
/// basis of the scene it was drawn on; this rebuilds the chart from that scene, applies the edit and returns the
/// re-prepared section with a new basis. Scenes are kept by basis so a replayed or abandoned request stays exact.
/// </summary>
public static class PlaygroundAnnotations {
    private const string ChartId = "playground-annotations";
    private const int MaxDrawings = 24;
    private const int MaxLabelLength = 120;
    private const int RetainedScenes = 24;

    private static readonly Dictionary<string, Drawing[]> Scenes = new(StringComparer.Ordinal);
    private static readonly Queue<string> SceneOrder = new();
    private static int _sequence;

    /// <summary>Returns the runtime, stylesheet and feature bundles matching the sections this engine prepares: <c>{ ok, css, script }</c>.</summary>
    public static string RuntimeJson() => Guard(() => new PlaygroundJson().BeginObject().Property("ok", true)
        .Property("css", HtmlInteractiveChartRenderer.BuildFragmentStyle())
        .Property("script", HtmlInteractiveChartRenderer.BuildFeatureScript() + ";\n" + HtmlInteractiveChartRenderer.BuildInteractionScript())
        .EndObject().ToString());

    /// <summary>Prepares the starting scene, or an existing scene in another theme: <c>{ ok, html, basis }</c>.</summary>
    public static string Open(string? basis, string? theme) => Guard(() => {
        var mode = Mode(theme);
        var drawings = string.IsNullOrEmpty(basis) ? Initial() : Scene(basis!);
        return Reply(drawings, mode);
    });

    /// <summary>Applies one runtime edit request to the scene named by its basis: <c>{ ok, html, basis }</c> or <c>{ ok: false, error }</c>.</summary>
    public static string Prepare(string? basis, string? theme, string? operation, string? id, string? kind,
        double startX, double startY, double endX, double endY, double yOffset, string? label, string? yAxis) => Guard(() => {
        var mode = Mode(theme);
        var drawings = new List<Drawing>(Scene(basis ?? string.Empty));
        var annotationId = new ChartAnnotationId(id ?? string.Empty);
        var index = drawings.FindIndex(drawing => drawing.Id.Equals(annotationId));
        if (operation == "remove") {
            if (index < 0) throw new ArgumentException("The drawing '" + id + "' is not on the chart.");
            drawings.RemoveAt(index);
            return Reply(drawings.ToArray(), mode);
        }

        if (operation != "add" && operation != "replace") throw new ArgumentException("Unknown operation '" + operation + "'.");
        if (operation == "add" ? index >= 0 : index < 0) throw new ArgumentException("Add needs an unused ID; replace needs an existing one.");
        if (yAxis != "Primary") throw new ArgumentException("This chart has one value axis.");
        if ((label ?? string.Empty).Length > MaxLabelLength) throw new ArgumentException("Captions are limited to " + MaxLabelLength + " characters.");
        var drawing = new Drawing(annotationId, kind switch {
            "Note" or "LineSegment" or "ParallelChannel" => kind!,
            _ => throw new ArgumentException("Unknown drawing kind '" + kind + "'.")
        }, new ChartPoint(startX, startY), kind == "Note" ? default : new ChartPoint(endX, endY), yOffset, label ?? string.Empty);
        if (operation == "add") {
            if (drawings.Count >= MaxDrawings) throw new ArgumentException("The chart holds at most " + MaxDrawings + " drawings.");
            drawings.Add(drawing);
        } else drawings[index] = drawing;
        return Reply(drawings.ToArray(), mode);
    });

    private static Drawing[] Initial() => new[] {
        new Drawing(new ChartAnnotationId("review"), "Note", new ChartPoint(3, 40), default, 0, "Review"),
        new Drawing(new ChartAnnotationId("plan"), "LineSegment", new ChartPoint(2, 20), new ChartPoint(8, 30), 0, "Plan"),
        new Drawing(new ChartAnnotationId("target"), "ParallelChannel", new ChartPoint(2, 65), new ChartPoint(8, 75), 10, "Target")
    };

    private static Chart Create(IEnumerable<Drawing> drawings, VisualThemeMode mode) {
        var chart = Chart.Create().WithSize(800, 480)
            .WithTheme(VisualTheme.Graphite().WithFontFamily("Carlito, sans-serif"), mode)
            .WithTitle("Review a measured trend").WithSubtitle("Add, move or delete drawings; the engine re-prepares the chart")
            .WithXAxisBounds(0, 10).WithYAxisBounds(0, 100).WithLegend(false)
            .AddLine("Observed", new[] { new ChartPoint(0, 10), new ChartPoint(2, 24), new ChartPoint(3, 40), new ChartPoint(5, 46),
                new ChartPoint(7, 58), new ChartPoint(8, 61), new ChartPoint(10, 70) });
        // Theme-owned colours keep each drawing readable after a theme switch.
        foreach (var drawing in drawings) {
            switch (drawing.Kind) {
                case "Note": chart.AddNote(drawing.Start, drawing.Label, id: drawing.Id); break;
                case "LineSegment": chart.AddLineSegment(drawing.Start, drawing.End, drawing.Label, id: drawing.Id); break;
                default: chart.AddParallelChannel(drawing.Start, drawing.End, drawing.YOffset, drawing.Label, id: drawing.Id); break;
            }
        }

        return chart;
    }

    private static void Options(HtmlChartInteractionOptions options) {
        options.Interaction.ChartId = ChartId;
        options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
        options.Tooltip.PinOnSelect = false;
    }

    private static string Reply(Drawing[] drawings, VisualThemeMode mode) {
        PlaygroundFonts.EnsureRegistered();
        var html = Create(drawings, mode).ToInteractiveHtmlFragmentWithoutAssets(Options);
        var basis = "scene-" + (++_sequence);
        Scenes[basis] = drawings;
        SceneOrder.Enqueue(basis);
        while (SceneOrder.Count > RetainedScenes) Scenes.Remove(SceneOrder.Dequeue());
        return new PlaygroundJson().BeginObject().Property("ok", true).Property("html", html).Property("basis", basis).EndObject().ToString();
    }

    private static Drawing[] Scene(string basis) => Scenes.TryGetValue(basis, out var drawings) ? drawings
        : throw new ArgumentException("The scene '" + basis + "' is no longer held; reset the chart.");

    private static VisualThemeMode Mode(string? theme) => theme switch {
        "dark" => VisualThemeMode.Dark,
        "light" or null or "" => VisualThemeMode.Light,
        _ => throw new ArgumentException("Unknown theme '" + theme + "'. Use 'light' or 'dark'.")
    };

    private static string Guard(Func<string> action) {
        try {
            return action();
        } catch (Exception exception) {
            return new PlaygroundJson().BeginObject().Property("ok", false).Property("error", exception.Message).EndObject().ToString();
        }
    }

    private readonly struct Drawing {
        public Drawing(ChartAnnotationId id, string kind, ChartPoint start, ChartPoint end, double yOffset, string label) {
            Id = id; Kind = kind; Start = start; End = end; YOffset = yOffset; Label = label;
        }

        public ChartAnnotationId Id { get; }
        public string Kind { get; }
        public ChartPoint Start { get; }
        public ChartPoint End { get; }
        public double YOffset { get; }
        public string Label { get; }
    }
}
