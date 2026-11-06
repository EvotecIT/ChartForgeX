namespace ChartForgeX.Themes;

/// <summary>
/// Provides dependency-free CSS font-family stacks for SVG and HTML chart output.
/// </summary>
public static class ChartFontStacks {
    /// <summary>The shared product stack. Archivo is bundled for native and browser rendering.</summary>
    public const string ProductSans = "Archivo, \"Segoe UI Variable Text\", \"Segoe UI\", system-ui, -apple-system, sans-serif";

    /// <summary>The bundled technical face, with platform fallbacks for unsupported characters.</summary>
    public const string ProductMono = "\"JetBrains Mono\", \"Cascadia Mono\", Consolas, monospace";
    /// <summary>
    /// Gets an opt-in platform sans-serif stack. Built-in sans presets use <see cref="ProductSans"/>.
    /// </summary>
    public const string SystemSans = "-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif";

    /// <summary>
    /// Gets a warm humanist sans-serif stack for reports and dashboards.
    /// </summary>
    public const string Humanist = "Aptos, Calibri, 'Segoe UI', Candara, Arial, sans-serif";

    /// <summary>
    /// Gets a geometric sans-serif stack for modern editorial charts.
    /// </summary>
    public const string Geometric = "Avenir Next, Avenir, Montserrat, 'Segoe UI', Arial, sans-serif";

    /// <summary>
    /// Gets a rounded sans-serif stack for softer product and education charts.
    /// </summary>
    public const string Rounded = "'SF Pro Rounded', 'Arial Rounded MT Bold', Nunito, Arial, sans-serif";

    /// <summary>
    /// Gets a serif stack for editorial and publication-style charts.
    /// </summary>
    public const string Serif = "Charter, Georgia, Cambria, 'Times New Roman', serif";

    /// <summary>
    /// Gets a monospace stack for technical, operations, and terminal-inspired charts.
    /// </summary>
    public const string Mono = "'SFMono-Regular', Consolas, 'Liberation Mono', Menlo, monospace";
}
