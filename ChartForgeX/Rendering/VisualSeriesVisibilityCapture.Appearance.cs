using System;
using System.Collections.Generic;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal sealed partial class VisualSeriesVisibilityCapture {
    private readonly IReadOnlyList<VisualCapturedEffect>? _appearance;
    private readonly string? _unwrappedSourceKey;
    private readonly string? _appearanceKey;

    private VisualSeriesVisibilityCapture(VisualSeriesVisibilityCapture previous,
        PreparedVisual completed, VisualCapturedEffect effect) {
        _basis = previous._basis; _tree = previous._tree;
        _series = previous._series; _seriesKeys = previous._seriesKeys;
        foreach (var pair in previous._plots) _plots.Add(pair.Key, pair.Value);
        foreach (var pair in previous._labels) _labels.Add(pair.Key, pair.Value);
        foreach (var pair in previous._regionOwners) _regionOwners.Add(pair.Key, pair.Value);
        _labelsEnabled = previous._labelsEnabled; _groupingOnly = previous._groupingOnly; View = previous.View;
        ReflowSource = previous.ReflowSource;
        var effects = previous._appearance == null ? new List<VisualCapturedEffect>() : new List<VisualCapturedEffect>(previous._appearance);
        effects.Add(effect);
        if (effects.Count > VisualSceneRasterRenderer.MaximumIsolatedDepth)
            throw new NotSupportedException("Native visibility appearance exceeds its bounded root policy snapshot boundary.");
        _appearance = effects.AsReadOnly();
        var actualFull = _basis.WithRootEffects(_appearance);
        _sourceSvg = actualFull.ToSvg("cfx-native-visibility-source");
        _appearanceKey = actualFull.CapturedRootEffectIdentity;
        _reflowViewSvg = ReflowSource == null ? null : completed.ToSvg("cfx-native-reflow-view");
        _unwrappedSourceKey = previous._unwrappedSourceKey ?? previous.SourceKey;
        SourceKey = ReflowSource == null ? previous.SourceKey
            : ChartVisibilitySource.AppearanceSourceKey(_unwrappedSourceKey!, actualFull);
    }

    internal PreparedVisual WithAppearance(PreparedVisual previousView, PreparedVisual completed, VisualCapturedEffect effect) {
        // previousView is the accepted actual endpoint; do not reinterpret or rebuild its source chart.
        if (!ReferenceEquals(previousView.Scene.VisibilityCapture, this))
            throw new ArgumentException("Appearance must bind the actual captured visibility view.", nameof(previousView));
        var capture = new VisualSeriesVisibilityCapture(this, completed, effect);
        var scene = completed.Scene;
        return completed.WithNativeFrame(new VisualScene(scene.Size, scene.Nodes, scene.Diagnostics, scene.Regions,
            scene.DataProjection, scene.ValueCapture, capture));
    }

    private PreparedVisual ApplyAppearance(PreparedVisual result) =>
        _appearance == null ? result : result.WithRootEffects(_appearance);

    private PreparedVisual ApplyFreshVisibilityAppearance(PreparedVisual result) =>
        _appearance == null ? result : result.WithRootEffects(_appearance, rebindVisibility: true);
}
