using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeValidation {
    internal const int MaximumWaterfallItems = 50000;
    internal const int MaximumWaterfallAxisLabels = 10000;
    internal const int MaximumWaterfallCategoryDepth = 32;

    private static void ValidateWaterfall(VisualArtifactInterchangeEnvelope envelope) {
        var source = envelope.Waterfall!;
        if (envelope.Kind != VisualArtifactKind.Chart)
            throw new ArgumentException("Editable Waterfall semantics require Chart artifact kind.", nameof(envelope));
        if (envelope.Groups.Count != 0 || envelope.Nodes.Count != 0 || envelope.Edges.Count != 0 || envelope.Annotations.Count != 0 || envelope.Scenarios.Count != 0 || envelope.Presentation != null)
            throw new ArgumentException("Waterfall authoring and presentation belong only to its exclusive typed payload.", nameof(envelope));
        if (envelope.Width.HasValue != envelope.Height.HasValue) throw new ArgumentException("Waterfall frames require both dimensions or neither.", nameof(envelope));
        foreach (var size in new[] { envelope.Width, envelope.Height })
            if (size.HasValue && (size.Value > int.MaxValue || Math.Truncate(size.Value) != size.Value))
                throw new ArgumentOutOfRangeException(nameof(envelope), "Waterfall frames require positive whole native pixels.");
        Count(source.Items.Count, MaximumWaterfallItems, "waterfall items"); Count(source.DeltaPoints.Count, MaximumWaterfallItems, "waterfall deltas");
        if (source.TypedItems ? source.DeltaPoints.Count != 0 : source.Items.Count != 0)
            throw new ArgumentException("A Waterfall envelope must use exactly its declared typed-items or numeric-deltas authoring mode.", nameof(envelope));
        RequiredId(source.SeriesId, "waterfall series id"); Text(source.SeriesName, "waterfall series name");
        if (source.AutomaticSeriesId) _ = VisualArtifactInterchangeMapping.WaterfallAutomaticSequence(source.SeriesId);
        else if (new ChartSeriesId(source.SeriesId).Value != source.SeriesId) throw new ArgumentException("Series IDs must use their exact native canonical spelling.", nameof(envelope));
        Defined(source.Orientation, "waterfall orientation", envelope); Defined(source.ValueAxis, "waterfall value axis", envelope);
        Defined(source.StateRole, "waterfall state", envelope); Defined(source.FillPattern, "waterfall pattern", envelope);
        OptionalDefined(source.SeriesDataLabelPlacement, "waterfall series caption placement", envelope);
        OptionalText(source.SemanticRole, "waterfall semantic role"); NonNegativeOptional(source.StrokeWidth, "waterfall stroke width");
        if (source.SeriesDataLabelStyle == null || source.Theme == null || source.BarStyle == null || source.GridStyle == null || source.ValueFormat == null || source.Labels == null)
            throw new ArgumentException("Waterfall native presentation fields must not be null.", nameof(envelope));
        if (source.Theme.ToJson().Length > 1024 * 1024) throw new ArgumentException("Waterfall theme JSON exceeds the native theme limit.", nameof(envelope));
        ValidateDetailTextStyle(source.SeriesDataLabelStyle);
        var authoredCount = source.TypedItems ? source.Items.Count : source.DeltaPoints.Count;
        var displayCount = authoredCount + (!source.TypedItems && authoredCount > 0 ? 1 : 0);
        Count(source.PointPresentation.Count, MaximumWaterfallItems + 1, "waterfall point presentation");
        var indices = new HashSet<int>(); var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var point in source.PointPresentation) {
            if (point == null) throw new ArgumentException("Waterfall point presentation must not contain null entries.", nameof(envelope));
            if (point.Index < 0 || point.Index >= displayCount || !indices.Add(point.Index))
                throw new ArgumentException("Waterfall point presentation needs unique existing display-item indices.", nameof(envelope));
            if (point.ObservationId != null) {
                RequiredId(point.ObservationId, "waterfall observation id");
                if (point.Index >= authoredCount || new ChartObservationId(point.ObservationId).Value != point.ObservationId || !ids.Add(point.ObservationId))
                    throw new ArgumentException("Waterfall IDs must be unique exact authored identities; the appended total has no source ID.", nameof(envelope));
            }
            OptionalDefined(point.FillPattern, "waterfall point pattern", envelope); OptionalText(point.Label, "waterfall point label"); ValidateDetailTextStyle(point.TextStyle);
        }
        Count(source.TextStyles.Count, 6, "waterfall text roles");
        foreach (var pair in source.TextStyles) {
            Defined(pair.Key, "waterfall text role", envelope);
            if (pair.Value == null) throw new ArgumentException("Waterfall text styles must not be null.", nameof(envelope));
            ValidateDetailTextStyle(pair.Value);
        }
        foreach (var title in new[] { source.XAxisTitle, source.YAxisTitle, source.SecondaryXAxisTitle, source.SecondaryYAxisTitle }) Text(title, "waterfall axis title");
        foreach (var axis in new[] { source.XAxis, source.YAxis, source.SecondaryXAxis, source.SecondaryYAxis }) {
            VisualArtifactInterchangeMapping.ValidateWaterfallPortableAxis(axis);
            Count(axis.Labels.Count, MaximumWaterfallAxisLabels, "waterfall axis labels");
            OptionalText(axis.TimeZoneLabel, "waterfall time-zone caption");
            foreach (var label in axis.Labels) {
                Finite(label.Value, "waterfall axis-label coordinate"); Text(label.Text, "waterfall axis caption");
                Count(label.Groups.Count, MaximumWaterfallCategoryDepth, "waterfall category parent captions");
                foreach (var group in label.Groups) Text(group, "waterfall category parent caption");
            }
        }
        if (!source.ExplicitPadding && (source.Padding.Left != 24 || source.Padding.Top != 24 || source.Padding.Right != 24 || source.Padding.Bottom != 24))
            throw new ArgumentException("Nondefault Waterfall padding must be declared explicit.", nameof(envelope));
        if (source.Labels.AccessibleTextFormatter != null || source.Labels.DateFormatter != null)
            throw new NotSupportedException("Waterfall interchange cannot carry localization callbacks.");
        ValidateWaterfallWords(source.Labels);
        _ = source.ValueFormat.PortablePolicy();
        // The same builder used by import delegates native enums, opening placement, unique coordinates, scale and shape constraints.
        var chart = VisualArtifactInterchangeMapping.CreateWaterfallChart(envelope);
        var remaining = VisualArtifactInterchangeValueBudget.Remaining(envelope);
        if (remaining < 0) throw new ArgumentOutOfRangeException(nameof(envelope), "Waterfall data exceeds the existing interchange aggregate budget.");
        _ = ChartWaterfallSteps.Create(chart.Series[0], (int)remaining);
        ChartGuards.RenderCompatibility(chart, preparing: true);
        var coordinates = ChartBarCoordinateMap.Create(chart); var stacks = ChartStackLayout.Create(chart, coordinates);
        var range = ChartRange.FromChart(chart, coordinates, stacks);
        if (source.ValueAxis == ChartAxisSide.Secondary) _ = ChartRange.FromSecondaryValueAxis(chart, range, stacks);
    }
}
