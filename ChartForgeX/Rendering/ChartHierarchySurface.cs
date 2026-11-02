using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

/// <summary>Independent authored color observations; leaf sizes never supply a color domain.</summary>
internal sealed class ChartHierarchySurface {
    private readonly ChartSeries _series;
    private readonly VisualThemeColors _colors;
    private readonly int[] _parents;
    private readonly int[] _siblingOrdinals;
    private readonly ChartColorBlend?[] _inherited;
    private readonly InheritedTone[] _tones;
    internal ChartColorScale? Scale { get; }
    internal double Minimum { get; }
    internal double Maximum { get; }
    internal bool HasDomain { get; }
    internal bool HasMissing { get; }
    internal bool ShowLegend => Presentation.ShowLegend;
    internal string Title => Presentation.Name;
    internal ChartColorPresentation Presentation { get; }
    internal string RolePrefix { get; }

    internal ChartHierarchySurface(Chart chart, VisualThemeColors colors, ChartColorPresentation presentation) {
        _series = chart.Series[0]; _colors = colors;
        var indices = _series.HierarchyItems.Select((item, index) => (item.Id, index)).ToDictionary(item => item.Id, item => item.index, StringComparer.Ordinal);
        _parents = _series.HierarchyItems.Select(item => item.ParentId != null && indices.TryGetValue(item.ParentId, out var parent) ? parent : -1).ToArray();
        _siblingOrdinals = new int[_parents.Length];
        var siblings = new System.Collections.Generic.Dictionary<int, int>();
        for (var i = 0; i < _parents.Length; i++) {
            siblings.TryGetValue(_parents[i], out var ordinal);
            _siblingOrdinals[i] = ordinal; siblings[_parents[i]] = ordinal + 1;
        }
        _inherited = new ChartColorBlend?[_parents.Length];
        _tones = new InheritedTone[_parents.Length];
        var sunburst = _series.Kind == ChartSeriesKind.Sunburst;
        var configured = sunburst ? chart.Options.Sunburst.ColorScale : chart.Options.Treemap.ColorScale;
        Presentation = presentation;
        RolePrefix = sunburst ? "sunburst" : "treemap";
        var observations = _series.HierarchyItems.Where(item => item.ColorValue.HasValue).Select(item => item.ColorValue!.Value).ToArray();
        Scale = configured ?? (observations.Length > 0 ? ChartColorScaleSurface.Default(colors) : null);
        Minimum = observations.Length > 0 ? observations.Min() : 0;
        Maximum = observations.Length > 0 ? observations.Max() : 0;
        HasDomain = ChartColorScaleSurface.HasDomain(Scale, observations.Length > 0 ? Minimum : null, observations.Length > 0 ? Maximum : null);
        HasMissing = _series.HierarchyItems.Any(item => !item.ColorValue.HasValue);
    }

    internal ChartColorBlend Blend(int index, int? paletteIndex = null) {
        if (index < _series.PointColors.Count && _series.PointColors[index] is ChartColor custom)
            return ChartColorBlend.Solid(custom, SvgColorRole.Series);
        if (Scale != null) return _series.HierarchyItems[index].ColorValue is double value
            ? Scale.BlendFor(value, Minimum, Maximum) : ChartColorScaleSurface.NoData(Scale, _colors);
        if (!_series.Color.HasValue && ChartRelationshipPaint.State(_series, index) == ChartSeriesState.None) return Inherited(index);
        var color = ChartRelationshipPaint.Color(_series, index, _colors, paletteIndex);
        return ChartColorBlend.Solid(color, ChartRelationshipPaint.Role(_series, index));
    }

    private ChartColorBlend Inherited(int index) {
        if (_inherited[index] is ChartColorBlend cached) return cached;
        var trail = new System.Collections.Generic.Stack<int>();
        for (var node = index; node >= 0 && !_inherited[node].HasValue; node = _parents[node]) {
            if (trail.Count >= _parents.Length) throw new InvalidOperationException("Hierarchy contains a parent cycle.");
            trail.Push(node);
        }
        while (trail.Count != 0) InheritOne(trail.Pop());
        return _inherited[index]!.Value;
    }

    private void InheritOne(int index) {
        var parent = _parents[index];
        InheritedTone tone;
        if (parent < 0 && _series.Kind == ChartSeriesKind.Sunburst
            && !_series.NodeStates.ContainsKey(_series.HierarchyItems[index].Id))
            tone = new InheritedTone(_colors.Neutral2, SvgPaint.Of(_colors.Neutral2, SvgColorRole.Surface));
        else if (parent < 0 || _series.Kind == ChartSeriesKind.Sunburst && _parents[parent] < 0) {
            var ordinal = _siblingOrdinals[index]; var color = _colors.CategoricalColor(ordinal);
            tone = new InheritedTone(color, SvgPaint.Palette(color, ordinal));
        } else {
            var parentBlend = Blend(parent);
            var inheritedParent = !ChartRelationshipPaint.HasExplicitColor(_series, parent)
                && ChartRelationshipPaint.State(_series, parent) == ChartSeriesState.None;
            var basis = inheritedParent ? _tones[parent] : new InheritedTone(parentBlend.Color, parentBlend.Paint);
            tone = basis.Tint(_siblingOrdinals[index] % 2 == 0, _colors.ChildLightnessOffset);
        }
        _tones[index] = tone;
        _inherited[index] = tone.Blend();
    }

    /// <summary>An affine tint stays bounded in size at every depth and retains the original palette ordinal.</summary>
    private readonly struct InheritedTone {
        private readonly ChartColor _basis;
        private readonly SvgPaint _paint;
        private readonly double _white, _black;
        internal InheritedTone(ChartColor basis, SvgPaint paint, double white = 0, double black = 0) {
            _basis = basis; _paint = paint; _white = white; _black = black;
        }
        internal InheritedTone Tint(bool white, double amount) => new(_basis, _paint,
            _white * (1 - amount) + (white ? amount : 0), _black * (1 - amount) + (white ? 0 : amount));
        internal ChartColorBlend Blend() {
            var black = Math.Min(1, _black); var white = Math.Min(1 - black, _white);
            if (white + black == 0) return new ChartColorBlend(_basis, _paint, _basis, _paint, 0);
            var basisShare = Math.Max(0, 1 - white - black);
            var basisAlpha = _basis.A / 255d;
            var alpha = basisAlpha * basisShare + white + black;
            double Channel(byte channel) => alpha == 0 ? 0 : (channel * basisAlpha * basisShare + 255 * white) / alpha;
            var color = ChartColorMath.PreciseColor.FromRgb(Channel(_basis.R), Channel(_basis.G), Channel(_basis.B), alpha).ToColor();
            var innerShare = black == 1 ? 0 : white / (1 - black);
            var inner = ChartColorMath.BlendPremultiplied(_basis, ChartColor.White, innerShare);
            var innerPaint = SvgPaint.Mix(inner, _paint, SvgPaint.Literal(ChartColor.White), innerShare);
            return new ChartColorBlend(inner, innerPaint, ChartColor.Black, SvgPaint.Literal(ChartColor.Black), black, color);
        }
    }
}
