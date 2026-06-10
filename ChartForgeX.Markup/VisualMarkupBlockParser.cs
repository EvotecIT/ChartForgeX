namespace ChartForgeX.Markup;

/// <summary>
/// Provides an extension point for parsing specific visual markup blocks without coupling ChartForgeX.Markup to optional visual languages.
/// </summary>
public interface IVisualMarkupBlockParser {
    /// <summary>
    /// Returns true when this parser can parse the supplied visual block.
    /// </summary>
    /// <param name="block">The scanned visual markup block.</param>
    /// <returns>True when this parser should handle the block.</returns>
    bool CanParse(VisualMarkupBlock block);

    /// <summary>
    /// Parses the supplied visual block and appends artifacts and diagnostics to the shared parse result.
    /// </summary>
    /// <param name="block">The scanned visual markup block.</param>
    /// <param name="result">The parse result receiving artifacts and diagnostics.</param>
    void Parse(VisualMarkupBlock block, VisualMarkupParseResult result);
}
