using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualArtifacts;

public static partial class VisualArtifactInterchangeMapping {
    /// <summary>Rebuilds one editable exclusive waterfall from validated authored data, identities and native presentation.</summary>
    /// <param name="envelope">A Waterfall-family envelope. Artifact metadata and source-language facts remain on this envelope.</param>
    /// <returns>A new chart and series. Imported collections and mutable presentation are detached from the envelope.</returns>
    /// <remarks>Numeric changes retain their calculated appended total; typed checkpoints never acquire authored values.
    /// Edit through Points or UpdateWaterfallItem as appropriate, then prepare again. No SVG fallback or scene-command inference is used.</remarks>
    public static Chart ToWaterfallChart(this VisualArtifactInterchangeEnvelope envelope) {
        if (envelope == null) throw new ArgumentNullException(nameof(envelope));
        envelope.Validate();
        RequireEditableSourcePresentation(envelope);
        if (envelope.Family != VisualArtifactInterchangeFamily.Waterfall || envelope.Waterfall == null)
            throw new NotSupportedException("Only a typed Waterfall-family envelope can rebuild an editable waterfall.");
        return CreateWaterfallChart(envelope);
    }

    internal static Chart CreateWaterfallChart(VisualArtifactInterchangeEnvelope envelope) {
        var source = envelope.Waterfall!;
        var chart = Chart.Create().WithTitle(envelope.Title).WithSubtitle(envelope.Subtitle);
        var points = source.TypedItems ? Array.Empty<ChartPoint>() : source.DeltaPoints.ToArray();
        var series = source.AutomaticSeriesId
            ? new ChartSeries(source.SeriesName, ChartSeriesKind.Waterfall, points)
            : new ChartSeries(new ChartSeriesId(source.SeriesId), source.SeriesName, ChartSeriesKind.Waterfall, points);
        if (source.TypedItems) series.ReplaceWaterfallItems(source.Items);
        if (source.AutomaticSeriesId) chart.AddRestoredAutomaticSeries(series, WaterfallAutomaticSequence(source.SeriesId));
        else chart.AddSeries(series);
        series.Color = source.Color; series.StateRole = source.StateRole; series.FillPattern = source.FillPattern;
        series.ShowInLegend = source.ShowInLegend; series.SemanticRole = source.SemanticRole;
        series.ValueAxis = source.ValueAxis; series.ShowDataLabels = source.SeriesShowDataLabels;
        series.DataLabelPlacement = source.SeriesDataLabelPlacement;
        series.ConfigureDataLabelStyle(style => CopyWaterfallTextStyle(source.SeriesDataLabelStyle, style));
        if (source.StrokeWidth.HasValue) series.StrokeWidth = source.StrokeWidth.Value;
        series.RestoreObservationIds(source.PointPresentation.Where(point => point.ObservationId != null)
            .Select(point => new KeyValuePair<int, ChartObservationId>(point.Index, new ChartObservationId(point.ObservationId!))));
        foreach (var point in source.PointPresentation) {
            // The numeric final total is a display mark, outside authored observation APIs. Its native presentation lists remain authoritative.
            if (!source.TypedItems && point.Index == source.DeltaPoints.Count) {
                while (series.PointColors.Count <= point.Index) series.PointColors.Add(null);
                while (series.PointFillPatterns.Count <= point.Index) series.PointFillPatterns.Add(null);
                while (series.PointLabels.Count <= point.Index) series.PointLabels.Add(null);
                while (series.PointDataLabelStyles.Count <= point.Index) series.PointDataLabelStyles.Add(null);
                series.PointColors[point.Index] = point.Color; series.PointFillPatterns[point.Index] = point.FillPattern;
                series.PointLabels[point.Index] = point.Label; series.PointDataLabelStyles[point.Index] = point.TextStyle?.Clone();
            } else {
                if (point.Color.HasValue) series.WithPointColor(point.Index, point.Color);
                if (point.FillPattern.HasValue) series.WithPointFillPattern(point.Index, point.FillPattern);
                if (point.Label != null) series.WithPointLabel(point.Index, point.Label);
                if (point.TextStyle != null) series.ConfigurePointDataLabelStyle(point.Index, style => CopyWaterfallTextStyle(point.TextStyle, style));
            }
        }
        ApplyWaterfallPresentation(chart, source);
        if (envelope.Width.HasValue) chart.WithSize((int)envelope.Width.Value, (int)envelope.Height!.Value);
        chart.Accessibility.Name = envelope.AccessibleName; chart.Accessibility.Description = envelope.AccessibleDescription;
        chart.Accessibility.Language = envelope.Language; chart.Accessibility.IsDecorative = envelope.IsDecorative;
        return chart;
    }

    internal static long WaterfallAutomaticSequence(string id) {
        const string prefix = "cfx:auto:";
        if (!id.StartsWith(prefix, StringComparison.Ordinal) || !long.TryParse(id.Substring(prefix.Length), NumberStyles.None,
            CultureInfo.InvariantCulture, out var sequence) || sequence <= 0 || ChartSeriesId.Automatic(sequence).Value != id)
            throw new ArgumentException("Automatic waterfall IDs must retain their canonical positive chart-local sequence.", nameof(id));
        return sequence;
    }

    private static void MapWaterfall(VisualArtifactInterchangeEnvelope envelope, Chart chart) {
        if (chart.Series.Count != 1 || chart.Series[0].Kind != ChartSeriesKind.Waterfall)
            throw new NotSupportedException("Editable Waterfall interchange requires one exclusive waterfall series.");
        ValidateWaterfallSourceBoundary(chart);
        var series = chart.Series[0];
        if (series.ObservationCount > VisualArtifactInterchangeValidation.MaximumWaterfallItems)
            throw new ArgumentOutOfRangeException(nameof(chart), "Waterfall authoring exceeds the portable item budget.");
        var source = new VisualArtifactInterchangeWaterfallArtifact {
            TypedItems = series.IsTypedWaterfall, SeriesId = series.Id.Value, AutomaticSeriesId = series.Id.IsAutomatic,
            SeriesName = series.Name, ValueAxis = series.ValueAxis, Color = series.Color, StateRole = series.StateRole,
            FillPattern = series.FillPattern, ShowInLegend = series.ShowInLegend, SemanticRole = series.SemanticRole,
            SeriesShowDataLabels = series.ShowDataLabels, SeriesDataLabelPlacement = series.DataLabelPlacement,
            SeriesDataLabelStyle = series.DataLabelStyle.Clone(), StrokeWidth = series.HasExplicitStrokeWidth ? series.StrokeWidth : null
        };
        CaptureWaterfallPresentation(chart, source);
        if (series.IsTypedWaterfall) source.Items.AddRange(series.WaterfallItems);
        else source.DeltaPoints.AddRange(series.Points);
        var displayCount = series.ObservationCount + (!series.IsTypedWaterfall && series.ObservationCount > 0 ? 1 : 0);
        if (series.PointColors.Count > displayCount || series.PointLabels.Count > displayCount || series.PointFillPatterns.Count > displayCount || series.PointDataLabelStyles.Count > displayCount)
            throw new NotSupportedException("Waterfall interchange cannot discard presentation outside its authored or appended-total display items.");
        for (var index = 0; index < displayCount; index++) {
            var point = new VisualArtifactInterchangeWaterfallPoint { Index = index,
                ObservationId = series.ObservationIds.TryGetValue(index, out var id) ? id.Value : null,
                Color = index < series.PointColors.Count ? series.PointColors[index] : null,
                FillPattern = index < series.PointFillPatterns.Count ? series.PointFillPatterns[index] : null,
                Label = index < series.PointLabels.Count ? series.PointLabels[index] : null,
                TextStyle = index < series.PointDataLabelStyles.Count ? series.PointDataLabelStyles[index]?.Clone() : null
            };
            if (point.ObservationId != null || point.Color.HasValue || point.FillPattern.HasValue || point.Label != null || point.TextStyle != null)
                source.PointPresentation.Add(point);
        }
        envelope.Family = VisualArtifactInterchangeFamily.Waterfall; envelope.Waterfall = source;
        envelope.Title = chart.Title; envelope.Subtitle = chart.Subtitle;
        envelope.Width = chart.Options.Size.Width; envelope.Height = chart.Options.Size.Height;
        envelope.AccessibleName ??= chart.Accessibility.Name; envelope.AccessibleDescription ??= chart.Accessibility.Description;
        envelope.Language ??= chart.Accessibility.Language; envelope.IsDecorative |= chart.Accessibility.IsDecorative;
    }

    private static void CopyWaterfallTextStyle(TextStyleOverride source, TextStyleOverride target) {
        target.Color = source.Color; target.FontFamily = source.FontFamily; target.FontWeight = source.FontWeight; target.FontSize = source.FontSize;
        if (source.ItalicOverride.HasValue) target.Italic = source.Italic;
        if (source.UnderlineOverride.HasValue) target.Underline = source.Underline;
        if (source.StrikethroughOverride.HasValue) target.Strikethrough = source.Strikethrough;
        target.UnderlineStyle = source.UnderlineStyle; target.StrikethroughStyle = source.StrikethroughStyle;
        target.Baseline = source.Baseline; target.TextCase = source.TextCase; target.OpenTypeLanguageTag = source.OpenTypeLanguageTag;
        target.ColorPaletteIndex = source.ColorPaletteIndex; target.Variations = source.Variations;
    }
}
