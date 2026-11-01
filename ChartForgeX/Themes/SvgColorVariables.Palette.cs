using ChartForgeX.Primitives;

namespace ChartForgeX.Themes;

public sealed partial class SvgColorVariables {
    private SvgColorVariable?[]? _paletteSlots;

    /// <summary>Reserves explicit token slots, including slots deliberately left literal by the host.</summary>
    internal SvgColorVariables WithPaletteSlots(int count) {
        _paletteSlots = new SvgColorVariable?[count];
        return this;
    }

    internal void AddPalette(string name, ChartColor color, int index) {
        Add(name, color, SvgColorRole.Series);
        _paletteSlots![index] = _variables[_variables.Count - 1];
    }

    internal bool TryPalettePaint(ChartColor color, int index, bool opaque, out string paint) {
        paint = string.Empty;
        if (!TryPaletteVariable(color, index, out var variable)) return false;
        if (!opaque) return TryWrite(variable, color.A / 255d, out paint);
        paint = OpaquePaint(variable);
        return true;
    }

    /// <summary>
    /// Resolves a series paint by its palette slot (ordinal modulo the slot count) while that slot holds the colour, so
    /// equal RGB values in two slots keep their own properties; otherwise by the series role and colour.
    /// </summary>
    internal bool TrySeriesSlotPaint(ChartColor color, int ordinal, out string paint) {
        paint = string.Empty;
        if (_paletteSlots != null && _paletteSlots.Length > 0 && color.A != 0) {
            var slot = _paletteSlots[ordinal % _paletteSlots.Length];
            if (slot.HasValue && slot.Value.Color.R == color.R && slot.Value.Color.G == color.G && slot.Value.Color.B == color.B
                && TryWrite(slot.Value, color.A / 255d, out paint)) return true;
        }
        return TryPaint(color, SvgColorRole.Series, out paint);
    }

    internal bool TryPaletteVariable(ChartColor color, int index, out SvgColorVariable variable) {
        variable = default;
        if (_paletteSlots == null) {
            if (!TryVariable(color, SvgColorRole.Series, out variable)) return false;
        } else {
            if (index < 0 || index >= _paletteSlots.Length || !_paletteSlots[index].HasValue) return false;
            variable = _paletteSlots[index]!.Value;
            if (variable.Color.R != color.R || variable.Color.G != color.G || variable.Color.B != color.B) return false;
        }
        return true;
    }
}
