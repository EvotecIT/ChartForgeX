using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>One captured color policy and its formatted strings, shared by marks, key measurement and descriptive facts.</summary>
internal sealed class ChartColorPresentation {
    private readonly ChartValueFormat _format;
    private readonly bool _compactDefault, _refineDefaults;
    private readonly Dictionary<double, string> _values;
    private readonly List<(double[] Values, string[] Captions)> _scales = new();

    internal ChartColorPresentation(ChartColorDimensionOptions options, ChartLabels labels, string defaultName,
        ChartValueFormat defaultFormat, bool compactDefault = false, ChartColorPresentationSet? preparation = null) {
        HasName = options.Name != null; HasFormat = options.ValueFormat != null;
        Name = options.Name ?? defaultName; Labels = labels.Capture();
        _format = options.ValueFormat ?? defaultFormat;
        _values = preparation?.Values(_format) ?? new Dictionary<double, string>();
        _compactDefault = !HasFormat && compactDefault;
        _refineDefaults = !HasFormat && (compactDefault || ReferenceEquals(defaultFormat, ChartValueFormat.ExistingValue));
    }

    internal string Name { get; }
    internal bool HasName { get; }
    internal bool HasFormat { get; }
    internal bool HasOverrides => HasName || HasFormat;
    internal ChartLabels Labels { get; }
    internal string NoData => Labels.NoData;

    internal string Value(double value) {
        var text = Raw(value);
        return _compactDefault ? ChartNumericFormatter.RefineCompactAxisValue(value, text, Raw(0)) : text;
    }

    internal string[] ScaleValues(IReadOnlyList<double> values) {
        foreach (var cached in _scales) if (cached.Values.SequenceEqual(values)) return cached.Captions;
        var captions = values.Select(Raw).ToArray();
        if (_refineDefaults) ChartNumericFormatter.RefineDefaults(values, captions, Raw(0));
        _scales.Add((values.ToArray(), captions));
        return captions;
    }

    private string Raw(double value) {
        if (_values.TryGetValue(value, out var text)) return text;
        text = _format.Format(value); _values.Add(value, text); return text;
    }
}
