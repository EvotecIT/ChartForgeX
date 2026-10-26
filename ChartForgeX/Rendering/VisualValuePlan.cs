using System.Collections.Generic;
using ChartForgeX.Typography;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>Detached value facts shared only by the finite supported native value painters.</summary>
internal readonly struct VisualValueReading {
    internal VisualValueReading(string? observationId, double value, double minimum, double maximum, string? seriesKey = null)
        : this(observationId, value, minimum, maximum, seriesKey, null) { }
    internal VisualValueReading(string? observationId, double value, double minimum, double maximum, string? seriesKey, string? component) {
        SeriesKey = seriesKey; ObservationId = observationId; Value = value; Minimum = minimum; Maximum = maximum; Component = component;
    }
    internal string? Component { get; }
    internal string? SeriesKey { get; }
    internal string? ObservationId { get; }
    internal double Value { get; }
    internal double Minimum { get; }
    internal double Maximum { get; }
}

/// <summary>The finite native capture boundary; no public timeline, browser geometry or mutable producer model.</summary>
internal interface IVisualValuePlan {
    string Kind { get; }
    string SeriesKey { get; }
    VisualSize Size { get; }
    FontSpec Font { get; }
    IReadOnlyList<VisualValueReading> Readings { get; }
    bool AllowsOutOfDomainReadings { get; }
    bool SamePolicy(IVisualValuePlan other);
    void Paint(VisualSceneBuilder builder, double[]? readings = null, bool captured = false);
}

/// <summary>A finite detached plan that recompiles complete native layout and semantics instead of replacing a fragment.</summary>
internal interface IVisualCompleteValuePlan : IVisualValuePlan {
    PreparedVisual Frame(PreparedVisual basis, double[] values);
}

/// <summary>Compares retained text policy even when both endpoint captions happen to be unpainted.</summary>
internal static class VisualValuePolicy {
    /// <summary>Compares geometry and colors that an intermediate native scalar reading can reveal.</summary>
    internal static bool SameNativeTheme(VisualRenderContext first, VisualRenderContext second) {
        var a = first.Theme; var b = second.Theme; var x = a.Resolve(first.ThemeMode); var y = b.Resolve(second.ThemeMode);
        if (a.Spacing != b.Spacing || a.SeriesStrokeWidth != b.SeriesStrokeWidth || a.AxisStrokeWidth != b.AxisStrokeWidth
            || a.GaugeStrokeWidth != b.GaugeStrokeWidth || a.GaugeBandWidth != b.GaugeBandWidth
            || a.Typography.AxisSize != b.Typography.AxisSize || a.Typography.DataLabelSize != b.Typography.DataLabelSize
            || a.Typography.ScalarValueSize != b.Typography.ScalarValueSize || a.Typography.CenterValueSize != b.Typography.CenterValueSize
            || !x.Foreground.Equals(y.Foreground) || !x.MutedForeground.Equals(y.MutedForeground) || !x.Surface.Equals(y.Surface)
            || !x.Border.Equals(y.Border) || !x.Neutral.Equals(y.Neutral) || !x.Neutral2.Equals(y.Neutral2) || !x.Neutral3.Equals(y.Neutral3)
            || !x.Accent.Equals(y.Accent) || !x.Status.Critical.Ink.Equals(y.Status.Critical.Ink)
            || x.Palette.Count != y.Palette.Count) return false;
        for (var index = 0; index < x.Palette.Count; index++) if (!x.Palette[index].Equals(y.Palette[index])) return false;
        return x.Status.Critical.Fill.Equals(y.Status.Critical.Fill) && x.Status.Medium.Fill.Equals(y.Status.Medium.Fill)
            && x.Status.Pass.Fill.Equals(y.Status.Pass.Fill) && x.Status.Info.Fill.Equals(y.Status.Info.Fill)
            && x.Status.Neutral.Fill.Equals(y.Status.Neutral.Fill);
    }

    internal static bool SameText(VisualSceneTextFace first, TextStyle a, VisualSceneTextFace second, TextStyle b) {
        var x = a.Font; var y = b.Font;
        if (a.FontSize != b.FontSize || a.LineHeight != b.LineHeight || a.LetterSpacing != b.LetterSpacing || !a.Color.Equals(b.Color) || a.Alignment != b.Alignment
            || a.UnderlineStyle != b.UnderlineStyle || a.StrikethroughStyle != b.StrikethroughStyle || a.Baseline != b.Baseline
            || a.TextCase != b.TextCase || a.Hinting != b.Hinting || a.OpenTypeLanguageTag != b.OpenTypeLanguageTag
            || x.Family != y.Family || x.FilePath != y.FilePath || x.CollectionIndex != y.CollectionIndex || x.FaceName != y.FaceName
            || x.Weight != y.Weight || x.Italic != y.Italic || x.ColorPaletteIndex != y.ColorPaletteIndex || x.Variations.Css != y.Variations.Css) return false;
        var capturedA = first.Prepare("0", a.EffectiveFontSize).Face; var capturedB = second.Prepare("0", b.EffectiveFontSize).Face;
        return object.ReferenceEquals(capturedA.Font, capturedB.Font) && capturedA.SynthesizeBold == capturedB.SynthesizeBold
            && capturedA.SynthesizeItalic == capturedB.SynthesizeItalic;
    }
}

/// <summary>Opt-in raw rows remain distinct from native painted/aggregate membership. All geometry stays in the same complete native plan.</summary>
internal interface IVisualSourceValuePlan : IVisualCompleteValuePlan {
    bool CapturesRawSource { get; }
    string StructureKey { get; }
    string SourceKey(PreparedVisual complete);
}

/// <summary>Typed components retain their true observation and native member identities.</summary>
internal interface IVisualComponentValuePlan : IVisualSourceValuePlan {
    string SourceFamily { get; }
    VisualComponentSizePolicy? SizePolicy { get; }
}

/// <summary>Only a real native size owner supplies these immutable facts; range components have no size policy.</summary>
internal sealed class VisualComponentSizePolicy {
    internal VisualComponentSizePolicy(ChartBubbleSizeScale native) {
        Minimum = native.MinimumValue; Maximum = native.MaximumValue; Mapping = native.Mapping; Reversed = native.Reversed;
    }
    internal double Minimum { get; }
    internal double Maximum { get; }
    internal string Mapping { get; }
    internal bool Reversed { get; }
}

/// <summary>Radial source slices retain the incumbent raw transport shape.</summary>
internal interface IVisualRawValuePlan : IVisualSourceValuePlan {
    IReadOnlyList<VisualRawValueSlice> CaptureSlices(PreparedVisual drawing, ChartVisibilityView? view);
}

/// <summary>One actual native GetSlices result and its independently observed paint ownership; no fabricated observation ID.</summary>
internal sealed class VisualRawValueSlice {
    internal VisualRawValueSlice(string seriesKey, int pointIndex, IReadOnlyList<int> sourceIndices, IReadOnlyDictionary<string, string>? paintedMetadata) {
        SeriesKey = seriesKey; PointIndex = pointIndex; SourceIndices = sourceIndices; PaintedMetadata = paintedMetadata;
    }
    internal string SeriesKey { get; }
    internal int PointIndex { get; }
    internal IReadOnlyList<int> SourceIndices { get; }
    internal IReadOnlyDictionary<string, string>? PaintedMetadata { get; }
}
