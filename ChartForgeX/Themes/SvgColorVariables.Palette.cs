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
