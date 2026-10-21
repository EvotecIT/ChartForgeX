using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Provides ChartForgeX rendering adapters for parsed Mermaid Gantt diagrams.
/// </summary>
public static class MermaidGanttRendering {
    /// <summary>
    /// Converts a Mermaid Gantt document into a renderer-independent ChartForgeX chart.
    /// </summary>
    public static Chart ToChart(this MermaidGanttDocument document, MermaidGanttRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidGanttRenderOptions();
        var chart = Chart.Create()
            .WithTitle(ResolveTitle(document, options))
            .WithSubtitle(MermaidHeading.Subtitle(options.Subtitle))
            .WithSize(options.Width, options.Height);
        if (!string.IsNullOrWhiteSpace(document.AxisFormat))
            chart.WithXAxisValueFormatter(value => FormatAxisValue(value, document, null, chart.Options.XAxis.TimeZone));
        chart.Options.XAxis.TimeTickLabelFormatter = (value, effectiveInterval) => FormatAxisValue(value, document, effectiveInterval, chart.Options.XAxis.TimeZone);
        if (options.Today.HasValue && !string.Equals(document.TodayMarker, "off", StringComparison.OrdinalIgnoreCase)) chart.WithGanttToday(options.Today.Value);
        chart.Options.GanttTodayStyle = options.TodayStyle ?? MermaidGanttTodayStyle.Parse(document.TodayMarker, out _);
        chart.WithGanttTickInterval(MermaidGanttTicks.Parse(document.TickInterval, document.Weekday));
        foreach (var task in document.Tasks) {
            var name = string.IsNullOrWhiteSpace(task.Section) ? task.Title : task.Section + " / " + task.Title;
            if (task.IsVerticalMarker) chart.AddGanttMarker(name, task.Start, task.End);
            else if (task.IsMilestone) chart.AddGanttMilestone(name, task.Start.AddTicks((task.End.Ticks - task.Start.Ticks) / 2));
            else chart.AddGanttTask(name, task.Start, task.RenderEnd, task.Progress);
        }
        var ids = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var index = 0; index < document.Tasks.Count; index++) {
            var id = document.Tasks[index].Id;
            if (string.IsNullOrWhiteSpace(id)) continue;
            if (ids.ContainsKey(id!)) throw new ArgumentException("Gantt task id '" + id + "' is declared more than once.", nameof(document));
            ids.Add(id!, index);
        }
        for (var index = 0; index < document.Tasks.Count; index++) {
            var task = document.Tasks[index];
            if (task.DependencyIndex >= 0) chart.AddGanttDependency(chart.Series[task.DependencyIndex], chart.Series[index]);
            foreach (var id in task.DependencyIds) {
                if (!ids.TryGetValue(id, out var predecessor)) throw new ArgumentException("Gantt dependency id '" + id + "' is not declared.", nameof(document));
                chart.AddGanttDependency(chart.Series[predecessor], chart.Series[index]);
            }
        }

        return MermaidPresentation.Apply(chart, document, options.Presentation);
    }

    /// <summary>
    /// Wraps a Mermaid Gantt document in a visual artifact envelope backed by a ChartForgeX chart.
    /// </summary>
    public static VisualArtifact ToVisualArtifact(this MermaidGanttDocument document, MermaidGanttRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidGanttRenderOptions();
        var id = string.IsNullOrWhiteSpace(options.Id) ? "mermaid-gantt" : options.Id!.Trim();
        var chart = document.ToChart(options);
        var artifact = chart.ToVisualArtifact(id, VisualArtifactKind.Mermaid, VisualArtifactSourceLanguage.Mermaid);
        artifact.Metadata["mermaid.kind"] = document.Kind.ToString();
        artifact.Metadata["mermaid.header"] = document.Header;
        artifact.Metadata["mermaid.tasks"] = document.Tasks.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.sections"] = document.Sections.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.milestones"] = MilestoneCount(document).ToString(CultureInfo.InvariantCulture);
        var verticalMarkers = 0;
        foreach (var task in document.Tasks) if (task.IsVerticalMarker) verticalMarkers++;
        artifact.Metadata["mermaid.verticalMarkers"] = verticalMarkers.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.dependencies"] = DependencyCount(document).ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.untilReferences"] = UntilReferenceCount(document).ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.dateFormat"] = document.DateFormat;
        if (document.DateAnchor.HasValue) artifact.Metadata["mermaid.dateAnchor"] = document.DateAnchor.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        if (!string.IsNullOrWhiteSpace(document.AxisFormat)) artifact.Metadata["mermaid.axisFormat"] = document.AxisFormat!;
        if (!string.IsNullOrWhiteSpace(document.TickInterval)) artifact.Metadata["mermaid.tickInterval"] = document.TickInterval!;
        artifact.Metadata["mermaid.weekday"] = document.Weekday.ToString().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(document.Excludes)) artifact.Metadata["mermaid.excludes"] = document.Excludes!;
        if (!string.IsNullOrWhiteSpace(document.TodayMarker)) artifact.Metadata["mermaid.todayMarker"] = document.TodayMarker!;
        artifact.Metadata["render.model"] = nameof(Chart);
        return MermaidPresentation.Apply(artifact, document);
    }

    /// <summary>
    /// Renders a Mermaid Gantt document to static SVG through ChartForgeX chart rendering.
    /// </summary>
    public static string ToSvg(this MermaidGanttDocument document, MermaidGanttRenderOptions? options = null) =>
        document.ToVisualArtifact(options).ToSvg();

    /// <summary>
    /// Renders a Mermaid Gantt document to static PNG through ChartForgeX chart rendering.
    /// </summary>
    public static byte[] ToPng(this MermaidGanttDocument document, MermaidGanttRenderOptions? options = null) =>
        document.ToVisualArtifact(options).ToPng();

    private static string ResolveTitle(MermaidGanttDocument document, MermaidGanttRenderOptions options) {
        return MermaidHeading.Title(document, options.Title, document.Title, "Mermaid Gantt");
    }

    private static string FormatAxisValue(double value, MermaidGanttDocument document, ChartTimeTickUnit? effectiveInterval, TimeZoneInfo? displayZone) {
        try {
            var format = string.IsNullOrWhiteSpace(document.AxisFormat) ? DefaultAxisFormat(effectiveInterval) : document.AxisFormat!;
            return MermaidGanttAxisFormat.Format(DateTime.FromOADate(value), format, displayZone);
        } catch (ArgumentException) {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }

    private static string DefaultAxisFormat(ChartTimeTickUnit? effectiveInterval) {
        switch (effectiveInterval) {
            case ChartTimeTickUnit.Millisecond: return "%Y-%m-%d %H:%M:%S.%L";
            case ChartTimeTickUnit.Second: return "%Y-%m-%d %H:%M:%S";
            case ChartTimeTickUnit.Minute:
            case ChartTimeTickUnit.Hour: return "%Y-%m-%d %H:%M";
            default: return "%Y-%m-%d";
        }
    }

    private static int MilestoneCount(MermaidGanttDocument document) {
        var count = 0;
        foreach (var task in document.Tasks) if (task.IsMilestone) count++;
        return count;
    }

    private static int DependencyCount(MermaidGanttDocument document) {
        var count = 0;
        foreach (var task in document.Tasks) if (task.DependencyIds.Count > 0) count += task.DependencyIds.Count;
        return count;
    }

    private static int UntilReferenceCount(MermaidGanttDocument document) {
        var count = 0;
        foreach (var task in document.Tasks) count += task.UntilTaskIds.Count;
        return count;
    }
}

/// <summary>
/// Defines conversion and rendering defaults for Mermaid Gantt diagrams.
/// </summary>
public sealed class MermaidGanttRenderOptions : MermaidDiagramRenderOptions {
    /// <summary>Gets or sets the natural preview width.</summary>
    public int Width { get; set; } = 960;

    /// <summary>Gets or sets the natural preview height.</summary>
    public int Height { get; set; } = 560;

    /// <summary>Gets or sets an optional current-day marker.</summary>
    public DateTime? Today { get; set; }

    /// <summary>Gets or sets a native today-line stroke override. A supplied style takes precedence over source styling; source off still hides the guide.</summary>
    public ChartStrokeStyle? TodayStyle { get; set; }
}
