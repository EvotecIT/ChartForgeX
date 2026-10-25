using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>Applies an absolute native visibility view after full-source geometry and global label placement are captured.</summary>
internal sealed partial class VisualSeriesVisibilityCapture {
    private readonly PreparedVisual _basis;
    private readonly SpanTree _tree;
    private readonly SeriesRow[] _series;
    private readonly IReadOnlyList<string> _seriesKeys;
    private readonly Dictionary<int, SeriesRow> _plots = new();
    private readonly Dictionary<int, SeriesRow> _labels = new();
    private readonly Dictionary<string, SeriesRow> _regionOwners = new(StringComparer.Ordinal);
    private readonly bool _labelsEnabled;
    private readonly string _sourceSvg;
    private readonly bool _groupingOnly;
    private VisualSeriesVisibilityCapture(PreparedVisual basis, ChartVisibilityView view, bool labelsEnabled, bool groupingOnly = false) {
        _groupingOnly = groupingOnly;
        _basis = basis; View = view; _labelsEnabled = labelsEnabled; _tree = new SpanTree(basis.Scene.Nodes);
        _series = CaptureSeries(); _seriesKeys = Array.AsReadOnly(_series.Select(row => row.Key).ToArray());
        _sourceSvg = basis.ToSvg("cfx-native-visibility-source");
    }
    internal ChartVisibilityView View { get; }
    internal IReadOnlyList<string> SeriesKeys => _seriesKeys;
    internal static PreparedVisual Prepare(PreparedVisual basis, ChartVisibilityView view, bool labelsEnabled) {
        var capture = new VisualSeriesVisibilityCapture(basis, view, labelsEnabled);
        return capture.Paint(basis, capture._series.Select(row => view.IsHidden(row.Key) ? 0d : 1d).ToArray(), view);
    }

    internal PreparedVisual ValueFrame(PreparedVisual basis, double[] values) {
        if (ReflowSource != null) throw new NotSupportedException("Native Reflow plus value morph requires a separately represented source/domain plan.");
        var capture = _basis.Scene.ValueCapture ?? throw new NotSupportedException("The captured native source has no value plan.");
        var full = capture.Frame(_basis, values);
        return ApplyFreshVisibilityAppearance(Prepare(full, View, _labelsEnabled));
    }

    /// <summary>Captures finite native isolated composites; no mutable chart or browser geometry is retained.</summary>
    internal static IReadOnlyList<PreparedVisual> Frames(PreparedVisual from, PreparedVisual to, int frameCount, CancellationToken cancellationToken = default) {
        if (from == null) throw new ArgumentNullException(nameof(from));
        if (to == null) throw new ArgumentNullException(nameof(to));
        if (frameCount < 2 || frameCount > 16) throw new ArgumentOutOfRangeException(nameof(frameCount));
        cancellationToken.ThrowIfCancellationRequested();
        var first = from.Scene.VisibilityCapture; var last = to.Scene.VisibilityCapture;
        if (first?.ReflowSource != null || last?.ReflowSource != null)
            throw new NotSupportedException("Reflow visibility requires the native source/destination-basis fade capture.");
        if (first == null || last == null || first._appearanceKey != last._appearanceKey || first.View.LabelLayering != last.View.LabelLayering
            || first._labelsEnabled != last._labelsEnabled || first._sourceSvg != last._sourceSvg || !SameAxes(first._basis, last._basis)
            || !first._series.Select(row => row.Key).SequenceEqual(last._series.Select(row => row.Key)))
            throw new NotSupportedException("Native visibility motion requires the same captured Line/Bar source, domains, layout, fonts, paint and label policy.");
        var before = first._series.Select(row => first.View.IsHidden(row.Key) ? 0d : 1d).ToArray();
        var after = first._series.Select(row => last.View.IsHidden(row.Key) ? 0d : 1d).ToArray();
        if (before.SequenceEqual(after)) throw new NotSupportedException("Native visibility motion requires at least one changed authored series visibility.");
        if (first._labelsEnabled && first.View.LabelLayering != ChartVisibilityLabelLayering.GroupWithSeries)
            throw new NotSupportedException("Enabled-label visibility motion requires explicit GroupWithSeries so plot and labels form one native composite.");
        const string scope = "cfx-native-visibility-endpoint";
        if (first.Paint(from, before, first.View).ToSvg(scope) != from.ToSvg(scope)
            || first.Paint(from, after, last.View).ToSvg(scope) != to.ToSvg(scope))
            throw new NotSupportedException("Native visibility endpoints must be exact captured full-source views.");
        var frames = new PreparedVisual[frameCount]; frames[0] = from; frames[frameCount - 1] = to;
        for (var frame = 1; frame < frameCount - 1; frame++) {
            cancellationToken.ThrowIfCancellationRequested(); var ratio = frame / (double)(frameCount - 1);
            var opacities = new double[before.Length];
            for (var row = 0; row < opacities.Length; row++) opacities[row] = ChartMath.InterpolateRange(before[row], after[row], ratio);
            frames[frame] = first.Paint(from, opacities, last.View);
        }
        cancellationToken.ThrowIfCancellationRequested(); return Array.AsReadOnly(frames);
    }

    private sealed class SeriesRow {
        internal SeriesRow(string key, int index, int start, int scope) { Key = key; Index = index; Start = start; Scope = scope; }
        internal string Key { get; }
        internal int Index { get; }
        internal int Start { get; }
        internal int Scope { get; }
        internal List<int> Labels { get; } = new();
    }
}
