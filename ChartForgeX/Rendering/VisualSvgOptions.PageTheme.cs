using System;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

public sealed partial class VisualSvgOptions {
    private string? _pageFontFamily;
    private string? _pageFontSize;
    internal bool HasPageFonts => _pageFontFamily != null;
    internal string? PageFontFamily => _pageFontFamily;
    /// <summary>The page size expression, or null to keep each line's prepared size.</summary>
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

    /// <summary>
    /// Follows the shared design roles that CanopyX and HtmlForgeX pages define: colours are written as the
    /// <c>--hfx-*</c> role properties (see <see cref="SvgColorVariables.SharedRoles"/>) with this theme's colours as
    /// fallbacks, and text uses <c>var(--hfx-font-sans, inherit)</c> while keeping its prepared sizes.
    /// </summary>
    /// <param name="theme">The theme the visual was prepared with; null uses <see cref="VisualTheme.Graphite"/>.</param>
    /// <param name="mode">The mode the visual was prepared in.</param>
    /// <returns>An independent export policy retaining the other settings.</returns>
    /// <remarks>
    /// A page that defines none of the properties shows the prepared colours and its own font. Layout still uses the
    /// prepared font metrics; raster output keeps literal colours and prepared fonts.
    /// </remarks>
    public VisualSvgOptions WithSharedDesignRoles(VisualTheme? theme = null, VisualThemeMode mode = VisualThemeMode.Light) {
        var copy = WithPageFonts("var(" + SvgColorVariables.SharedFontFamily + ", inherit)", null);
        copy._colorVariables = SvgColorVariables.SharedRoles(theme, mode);
        return copy;
    }

    private VisualSvgOptions WithPageFonts(string family, string? size) {
        var copy = Copy(); copy._pageFontFamily = family; copy._pageFontSize = size; return copy;
    }
}
