using System;
using System.Collections.Generic;
using ChartForgeX.Rendering;

namespace ChartForgeX.Themes;

/// <summary>Extends an immutable categorical palette without repeating a resolved colour.</summary>
internal sealed class CategoricalColorSequence {
    private readonly IReadOnlyList<ChartColor> _palette;
    private readonly List<Entry> _colors = new();
    private readonly HashSet<ChartColor> _used = new();
    private readonly object _sync = new();

    internal CategoricalColorSequence(IReadOnlyList<ChartColor> palette) { _palette = palette; }

    internal ChartColor At(int index) => Describe(index).Color;

    internal readonly struct Entry {
        internal Entry(ChartColor color, int basis, ChartColor target, double share) { Color = color; Basis = basis; Target = target; Share = share; }
        internal ChartColor Color { get; }
        internal int Basis { get; }
        internal ChartColor Target { get; }
        internal double Share { get; }
    }

    internal Entry Describe(int index) {
        if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
        lock (_sync) {
            while (_colors.Count <= index) {
                var ordinal = _colors.Count;
                var cycle = ordinal / _palette.Count;
                var basis = _palette[ordinal % _palette.Count];
                var target = cycle % 2 == 1 ? ChartColor.White : ChartColor.Black;
                var share = cycle == 0 ? 0 : Math.Min(.72, .18 * ((cycle + 1) / 2));
                var color = share == 0 ? basis : ChartColorMath.Blend(basis, target, share);
                var basisIndex = ordinal % _palette.Count;
                var shadeAttempt = 0;
                // Repeated custom slots and rounded shade collisions also get a distinct deterministic colour.
                var salt = (uint)ordinal;
                while (!_used.Add(color)) {
                    if (shadeAttempt++ < 8) {
                        var alternate = cycle + shadeAttempt;
                        target = alternate % 2 == 1 ? ChartColor.White : ChartColor.Black;
                        share = Math.Min(.72, .18 * ((alternate + 1) / 2));
                        color = ChartColorMath.Blend(basis, target, share);
                        continue;
                    }
                    basisIndex = -1;
                    salt = unchecked(salt + 0x9e3779b9u);
                    color = ChartColor.FromRgb((byte)(32 + ((salt >> 16) % 192)),
                        (byte)(32 + ((salt >> 8) % 192)), (byte)(32 + (salt % 192)));
                }
                _colors.Add(new Entry(color, basisIndex, target, share));
            }
            return _colors[index];
        }
    }
}
