using System;
using System.Globalization;
using ChartForgeX.Core;

namespace ChartForgeX.VisualArtifacts;

/// <summary>
/// Provides product-neutral visual artifact wrappers for renderer-independent charts.
/// </summary>
public static class ChartArtifactRendering {
    /// <summary>
    /// Wraps a ChartForgeX chart in a visual artifact envelope.
    /// </summary>
    /// <param name="chart">The chart model.</param>
    /// <param name="id">Optional stable artifact identifier.</param>
    /// <param name="kind">The artifact family to report.</param>
    /// <param name="sourceLanguage">The source language that produced the chart.</param>
    /// <returns>A visual artifact envelope backed by the chart model.</returns>
    public static VisualArtifact ToVisualArtifact(this Chart chart, string? id = null, VisualArtifactKind kind = VisualArtifactKind.Chart, VisualArtifactSourceLanguage sourceLanguage = VisualArtifactSourceLanguage.Native) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        var artifact = VisualArtifact.Create(string.IsNullOrWhiteSpace(id) ? "chart" : id!.Trim(), kind, chart);
        artifact.SourceLanguage = sourceLanguage;
        artifact.Title = chart.Title ?? string.Empty;
        artifact.Subtitle = chart.Subtitle ?? string.Empty;
        artifact.NaturalSize = new VisualArtifactSize(chart.Options.Size.Width, chart.Options.Size.Height);
        artifact.ExportFormats = VisualArtifactExportFormat.Svg | VisualArtifactExportFormat.Png | VisualArtifactExportFormat.Html;
        artifact.Metadata["render.model"] = nameof(Chart);
        artifact.Metadata["chart.series"] = chart.Series.Count.ToString(CultureInfo.InvariantCulture);
        if (chart.Series.Count > 0) artifact.Metadata["chart.kind"] = chart.Series[0].Kind.ToString();
        return artifact;
    }
}
