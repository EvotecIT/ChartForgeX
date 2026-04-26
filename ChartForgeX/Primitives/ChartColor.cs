namespace ChartForgeX.Primitives;

/// <summary>
/// Represents an RGBA color used by ChartForgeX renderers.
/// </summary>
public readonly struct ChartColor {
    /// <summary>
    /// Gets the red channel.
    /// </summary>
    public readonly byte R;

    /// <summary>
    /// Gets the green channel.
    /// </summary>
    public readonly byte G;

    /// <summary>
    /// Gets the blue channel.
    /// </summary>
    public readonly byte B;

    /// <summary>
    /// Gets the alpha channel.
    /// </summary>
    public readonly byte A;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartColor"/> struct.
    /// </summary>
    /// <param name="r">The red channel.</param>
    /// <param name="g">The green channel.</param>
    /// <param name="b">The blue channel.</param>
    /// <param name="a">The alpha channel.</param>
    public ChartColor(byte r, byte g, byte b, byte a = 255) { R = r; G = g; B = b; A = a; }

    /// <summary>
    /// Creates an opaque RGB color.
    /// </summary>
    /// <param name="r">The red channel.</param>
    /// <param name="g">The green channel.</param>
    /// <param name="b">The blue channel.</param>
    /// <returns>An opaque chart color.</returns>
    public static ChartColor FromRgb(byte r, byte g, byte b) => new(r, g, b, 255);

    /// <summary>
    /// Creates a color with an explicit alpha channel.
    /// </summary>
    /// <param name="r">The red channel.</param>
    /// <param name="g">The green channel.</param>
    /// <param name="b">The blue channel.</param>
    /// <param name="a">The alpha channel.</param>
    /// <returns>A chart color.</returns>
    public static ChartColor FromRgba(byte r, byte g, byte b, byte a) => new(r, g, b, a);

    /// <summary>
    /// Converts the color to a hexadecimal RGB string.
    /// </summary>
    /// <returns>A CSS-compatible hexadecimal color string.</returns>
    public string ToHex() => $"#{R:X2}{G:X2}{B:X2}";

    /// <summary>
    /// Converts the color to a CSS color string.
    /// </summary>
    /// <returns>A hexadecimal or rgba CSS color string.</returns>
    public string ToCss() => A == 255 ? ToHex() : $"rgba({R},{G},{B},{A / 255.0:0.###})";

    /// <summary>
    /// Gets a fully transparent color.
    /// </summary>
    public static ChartColor Transparent => new(0,0,0,0);

    /// <summary>
    /// Gets opaque white.
    /// </summary>
    public static ChartColor White => new(255,255,255);

    /// <summary>
    /// Gets opaque black.
    /// </summary>
    public static ChartColor Black => new(0,0,0);
}
