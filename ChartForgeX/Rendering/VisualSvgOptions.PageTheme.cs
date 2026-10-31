using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

public sealed partial class VisualSvgOptions {
    private string? _pageFontFamily;
    private string? _pageFontSize;
    internal bool HasPageFonts => _pageFontFamily != null;
    internal string? PageFontFamily => _pageFontFamily;
    internal string? PageFontSize => _pageFontSize;

    /// <summary>Lets browser SVG text inherit the embedding page's font family and size.</summary>
    /// <returns>An independent export policy retaining the other settings.</returns>
    /// <remarks>
    /// Text positions, wrapping, clipping and line spacing still use the font and size resolved during preparation.
    /// Choose matching layout fonts/sizes when exact fitting matters. Native glyph contours, captured text images and raster output retain their prepared fonts.
    /// Ordinary SVG export keeps explicit fonts unless this policy is selected.
    /// </remarks>
    public VisualSvgOptions WithInheritedFonts() => WithPageFonts("inherit", "inherit");

    /// <summary>Lets browser SVG text use page custom properties for font family and/or size, with page inheritance as fallback.</summary>
    /// <param name="familyVariable">A validated custom property such as <c>--body-font</c>; null inherits the page family.</param>
    /// <param name="sizeVariable">A validated custom property such as <c>--chart-font-size</c>; null inherits the page size.</param>
    /// <returns>An independent export policy retaining the other settings.</returns>
    /// <remarks>
    /// The page supplies valid CSS font values, including a unit for the size (for example <c>14px</c>).
    /// Layout still uses prepared font metrics: this does not reflow, remeasure or scale chart geometry.
    /// Native glyph contours, captured text images and raster output retain their prepared fonts.
    /// </remarks>
    /// <exception cref="ArgumentException">Neither property was supplied, or a property name is invalid.</exception>
    public VisualSvgOptions WithFontVariables(string? familyVariable = null, string? sizeVariable = null) {
        if (familyVariable == null && sizeVariable == null)
            throw new ArgumentException("Supply a font family or size property, or use WithInheritedFonts().");
        string Font(string? name, string parameter) => name == null ? "inherit"
            : "var(" + ChartColor.ValidateCssVariableName(name, parameter) + ", inherit)";
        return WithPageFonts(Font(familyVariable, nameof(familyVariable)), Font(sizeVariable, nameof(sizeVariable)));
    }

    private VisualSvgOptions WithPageFonts(string family, string size) {
        var copy = Copy(); copy._pageFontFamily = family; copy._pageFontSize = size; return copy;
    }
}
