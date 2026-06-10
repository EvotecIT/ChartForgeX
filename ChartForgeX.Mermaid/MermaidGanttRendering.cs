using System;
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
            .WithSubtitle(ResolveSubtitle(document, options))
            .WithSize(options.Width, options.Height)
            .WithXAxisValueFormatter(value => FormatAxisValue(value, document));
        if (options.Today.HasValue) chart.WithGanttToday(options.Today.Value);
        foreach (var task in document.Tasks) {
            var name = string.IsNullOrWhiteSpace(task.Section) ? task.Title : task.Section + " / " + task.Title;
            if (task.IsMilestone) chart.AddGanttMilestone(name, task.Start, task.DependencyIndex);
            else chart.AddGanttTask(name, task.Start, task.End, task.Progress, task.DependencyIndex);
        }

        return chart;
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
        artifact.Metadata["mermaid.dependencies"] = DependencyCount(document).ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.dateFormat"] = document.DateFormat;
        if (!string.IsNullOrWhiteSpace(document.AxisFormat)) artifact.Metadata["mermaid.axisFormat"] = document.AxisFormat!;
        if (!string.IsNullOrWhiteSpace(document.TickInterval)) artifact.Metadata["mermaid.tickInterval"] = document.TickInterval!;
        if (!string.IsNullOrWhiteSpace(document.Excludes)) artifact.Metadata["mermaid.excludes"] = document.Excludes!;
        if (!string.IsNullOrWhiteSpace(document.TodayMarker)) artifact.Metadata["mermaid.todayMarker"] = document.TodayMarker!;
        artifact.Metadata["render.model"] = nameof(Chart);
        return artifact;
    }

    /// <summary>
    /// Renders a Mermaid Gantt document to static SVG through ChartForgeX chart rendering.
    /// </summary>
    public static string ToSvg(this MermaidGanttDocument document, MermaidGanttRenderOptions? options = null) =>
        document.ToChart(options).ToSvg();

    /// <summary>
    /// Renders a Mermaid Gantt document to static PNG through ChartForgeX chart rendering.
    /// </summary>
    public static byte[] ToPng(this MermaidGanttDocument document, MermaidGanttRenderOptions? options = null) =>
        document.ToChart(options).ToPng();

    private static string ResolveTitle(MermaidGanttDocument document, MermaidGanttRenderOptions options) {
        if (!string.IsNullOrWhiteSpace(options.Title)) return options.Title!;
        if (!string.IsNullOrWhiteSpace(document.Title)) return document.Title!;
        return "Mermaid Gantt";
    }

    private static string ResolveSubtitle(MermaidGanttDocument document, MermaidGanttRenderOptions options) {
        if (!string.IsNullOrWhiteSpace(options.Subtitle)) return options.Subtitle!;
        return document.Header;
    }

    private static string FormatAxisValue(double value, MermaidGanttDocument document) {
        try {
            var format = string.IsNullOrWhiteSpace(document.AxisFormat) ? "yyyy-MM-dd" : MermaidGanttParser.ToDotNetDateFormat(document.AxisFormat!);
            return DateTime.FromOADate(value).ToString(format, CultureInfo.InvariantCulture);
        } catch (ArgumentException) {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
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
}

/// <summary>
/// Defines conversion and rendering defaults for Mermaid Gantt diagrams.
/// </summary>
public sealed class MermaidGanttRenderOptions {
    /// <summary>Gets or sets the artifact id.</summary>
    public string? Id { get; set; }

    /// <summary>Gets or sets the artifact title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the artifact subtitle.</summary>
    public string? Subtitle { get; set; }

    /// <summary>Gets or sets the natural preview width.</summary>
    public int Width { get; set; } = 960;

    /// <summary>Gets or sets the natural preview height.</summary>
    public int Height { get; set; } = 560;

    /// <summary>Gets or sets an optional current-day marker.</summary>
    public DateTime? Today { get; set; }
}
