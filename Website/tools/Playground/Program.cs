using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace ChartForgeX.Playground;

/// <summary>Browser entry point. The page only needs the exports, so Main does nothing.</summary>
public static class Program {
    /// <summary>Runtime entry point.</summary>
    public static void Main() {
    }
}

/// <summary>
/// JavaScript surface of the playground engine. Strings in, JSON strings out; nothing throws across the boundary.
/// Reached from JS as <c>exports.ChartForgeX.Playground.PlaygroundExports</c>.
/// </summary>
[SupportedOSPlatform("browser")]
public static partial class PlaygroundExports {
    /// <summary>Renders source to SVG: <c>{ ok, svg, error, line, column, diagramType, elapsedMs, ... }</c>.</summary>
    [JSExport]
    internal static string Render(string language, string source, string theme) => PlaygroundEngine.Render(language, source, theme);

    /// <summary>Returns the bundled samples: <c>[{ id, title, language, source }]</c>.</summary>
    [JSExport]
    internal static string Samples() => PlaygroundEngine.SamplesJson();

    /// <summary>Returns engine facts: <c>{ version, runtime, maxSourceLength, languages, themes }</c>.</summary>
    [JSExport]
    internal static string Info() => PlaygroundEngine.InfoJson();
}
