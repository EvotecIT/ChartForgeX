using System;
using System.Text;

namespace ChartForgeX.Themes;

public sealed partial class SvgColorVariables {
    /// <summary>The shared design-role property for the page's proportional font family.</summary>
    public const string SharedFontFamily = "--hfx-font-sans";

    /// <summary>
    /// Returns the shared design-role property CanopyX and HtmlForgeX pages define for a token path (see
    /// <see cref="VisualDesignTokens.ToSvgColorVariables"/>): <c>ramps.*</c> become <c>--hfx-ramp-*</c> and every other
    /// path <c>--hfx-color-*</c>, in kebab case. For example <c>series.1</c> is <c>--hfx-color-series-1</c>,
    /// <c>surface.card</c> is <c>--hfx-color-surface-card</c>, <c>surface.line</c> is <c>--hfx-color-surface-line</c>,
    /// <c>text.primary</c> is <c>--hfx-color-text-primary</c>, <c>text.secondary</c> is <c>--hfx-color-text-secondary</c>,
    /// <c>accent.base</c> is <c>--hfx-color-accent-base</c> and <c>ramps.sequential.1</c> is <c>--hfx-ramp-sequential-1</c>.
    /// </summary>
    /// <param name="tokenPath">A token path such as <c>surface.cardAlt</c>.</param>
    /// <returns>The custom property name.</returns>
    /// <exception cref="ArgumentException">The path is empty or holds characters other than ASCII letters, digits and dots.</exception>
    public static string SharedRoleName(string tokenPath) {
        if (string.IsNullOrEmpty(tokenPath)) throw new ArgumentException("A token path is required.", nameof(tokenPath));
        var ramp = tokenPath.StartsWith("ramps.", StringComparison.Ordinal);
        var rest = ramp ? tokenPath.Substring("ramps.".Length) : tokenPath;
        var name = new StringBuilder(ramp ? "--hfx-ramp-" : "--hfx-color-", rest.Length + 16);
        foreach (var ch in rest) {
            if (ch == '.') name.Append('-');
            else if (ch is >= 'A' and <= 'Z') name.Append('-').Append((char)(ch + ('a' - 'A')));
            else if (ch is >= 'a' and <= 'z' or >= '0' and <= '9') name.Append(ch);
            else throw new ArgumentException("A token path holds ASCII letters, digits and dots.", nameof(tokenPath));
        }
        return name.ToString();
    }

    /// <summary>
    /// Creates colour variables named after the shared <c>--hfx-*</c> design roles (<see cref="SharedRoleName"/>), with
    /// the theme's colours as fallbacks: a chart on a CanopyX or HtmlForgeX page takes the page's theme, and a page that
    /// defines none of the properties shows the theme unchanged. Series paints keep their palette slot, so two slots
    /// with equal colours still write <c>--hfx-color-series-n</c> of their own slot.
    /// </summary>
    /// <param name="theme">The theme the chart is prepared with; null uses <see cref="VisualTheme.Graphite"/>.</param>
    /// <param name="mode">The mode the chart is prepared in.</param>
    /// <returns>The colour variables, for <c>Chart.WithSvgColorVariables</c>, <c>ChartGrid.WithSvgColorVariables</c> or a <c>VisualSvgOptions</c>.</returns>
    public static SvgColorVariables SharedRoles(VisualTheme? theme = null, VisualThemeMode mode = VisualThemeMode.Light) =>
        (theme ?? VisualTheme.Graphite()).Resolve(mode).ToTokens().ToSvgColorVariables(SharedRoleName);
}
