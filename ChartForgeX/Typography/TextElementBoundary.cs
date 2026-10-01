using ChartForgeX.Raster;

namespace ChartForgeX.Typography;

/// <summary>
/// Cut points that keep a character whole when text is wrapped or shortened: never between the
/// halves of a surrogate pair, before a combining mark or variation selector, or around a zero
/// width joiner, so the shaper never sees half an emoji or a mark without its base.
/// </summary>
internal static class TextElementBoundary {
    /// <summary>The largest length up to <paramref name="length"/> that ends on a boundary.</summary>
    internal static int Snap(string value, int length) {
        if (length <= 0) return 0;
        if (length >= value.Length) return value.Length;
        while (length > 0 && !IsBoundary(value, length)) length--;
        return length;
    }

    /// <summary>The next boundary after <paramref name="index"/>.</summary>
    internal static int Next(string value, int index) {
        if (index >= value.Length) return value.Length;
        index++;
        while (index < value.Length && !IsBoundary(value, index)) index++;
        return index;
    }

    private static bool IsBoundary(string value, int index) {
        var ch = value[index];
        if (char.IsLowSurrogate(ch) && char.IsHighSurrogate(value[index - 1])) return false;
        if (value[index - 1] == '\u200D') return false;
        return !TextShaper.Extends(TrueTypeFont.ReadCodePoint(value, ref index));
    }
}
