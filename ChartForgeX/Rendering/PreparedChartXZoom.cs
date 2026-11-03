using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Accessibility;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>A detached finite bank of native X zoom scenes sharing one plot and full-data Y layout.</summary>
public sealed class PreparedChartXZoom {
    private readonly VisualAccessibility _accessibility;
    private readonly VisualSvgOptions _svgOptions;
    private readonly IReadOnlyList<PreparedCartesianXLevel> _bank;
    private readonly ChartAxisProjection _yProjection;
    private readonly ChartAxisProjection? _secondaryYProjection;

    internal PreparedChartXZoom(ChartXZoomLevels levels, ChartAxisWindow fullViewport, ChartRect plotBounds,
        IReadOnlyList<PreparedCartesianXLevel> bank, VisualAccessibility accessibility, VisualSvgOptions svgOptions,
        ChartAxisProjection yProjection, ChartAxisProjection? secondaryYProjection) {
        Levels = levels; FullViewport = fullViewport; PlotBounds = plotBounds;
        _yProjection = yProjection; _secondaryYProjection = secondaryYProjection;
        _bank = Array.AsReadOnly(bank.ToArray()); _accessibility = accessibility.Clone(); _svgOptions = svgOptions;
        Diagnostics = Array.AsReadOnly(bank.SelectMany(level => level.Layers.SelectMany(layer => layer.Scene.Diagnostics))
            .GroupBy(diagnostic => diagnostic.Code + "\n" + diagnostic.Message, StringComparer.Ordinal).Select(group => group.First()).ToArray());
    }

    /// <summary>Gets the immutable magnification bank.</summary>
    public ChartXZoomLevels Levels { get; }
    /// <summary>Gets the resolved full X domain, including automatic marker padding at the final bank plot.</summary>
    public ChartAxisWindow FullViewport { get; }
    /// <summary>Gets the one fixed plot rectangle used by every level and pan position.</summary>
    public ChartRect PlotBounds { get; }
    /// <summary>Gets preparation diagnostics across all levels.</summary>
    public IReadOnlyList<VisualDiagnostic> Diagnostics { get; }
    internal IReadOnlyList<PreparedCartesianXLevel> Bank => _bank;

    /// <summary>Creates a detached static scene at a prepared level and contained source-order pan position.</summary>
    /// <param name="levelIndex">Index into <see cref="Levels"/>.</param>
    /// <param name="panFraction">Zero starts at the full domain's lower end; one ends at its upper end, independently of axis reversal.</param>
    /// <returns>A native scene with its current detached X projection, suitable for SVG or PNG export.</returns>
    /// <remarks>Pan rounds to representable source bounds before translating the captured native track.
    /// The reported bounds also control source paint and caption admission; magnification and native sizes stay fixed.</remarks>
    public PreparedVisual At(int levelIndex, double panFraction = 0) {
        if (levelIndex < 0 || levelIndex >= _bank.Count) throw new ArgumentOutOfRangeException(nameof(levelIndex));
        ChartPrimitiveGuards.Finite(panFraction, nameof(panFraction));
        if (panFraction < 0 || panFraction > 1) throw new ArgumentOutOfRangeException(nameof(panFraction));
        var level = _bank[levelIndex];
        var window = level.Coordinates.At(panFraction);
        var scene = level.Compose(window.Delta, window.Projection.Minimum, window.Projection.Maximum);
        return new PreparedVisual(scene, _accessibility, svgOptions: _svgOptions, xProjection: window.Projection,
            yProjection: _yProjection, secondaryYProjection: _secondaryYProjection,
            viewport: levelIndex == 0 ? null : new ChartCartesianViewport(x: new ChartAxisWindow(window.Projection.Minimum, window.Projection.Maximum)),
            cartesianOrientation: ChartOrientation.Vertical);
    }
}
