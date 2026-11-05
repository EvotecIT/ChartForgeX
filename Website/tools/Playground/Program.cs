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

    /// <summary>Exports the same source and theme as a bounded native 2x PNG JSON result.</summary>
    [JSExport]
    internal static string RenderPng(string language, string source, string theme) => PlaygroundEngine.Render(language, source, theme, "png");

    /// <summary>Returns the bundled samples: <c>[{ id, title, language, source }]</c>.</summary>
    [JSExport]
    internal static string Samples() => PlaygroundEngine.SamplesJson();

    /// <summary>Returns engine facts: <c>{ version, runtime, maxSourceLength, languages, themes }</c>.</summary>
    [JSExport]
    internal static string Info() => PlaygroundEngine.InfoJson();

    /// <summary>Returns the HTML chart runtime matching the annotation host's sections: <c>{ ok, css, script }</c>.</summary>
    [JSExport]
    internal static string AnnotationRuntime() => PlaygroundAnnotations.RuntimeJson();

    /// <summary>Prepares the annotation chart, or re-themes a held scene: <c>{ ok, html, basis }</c>.</summary>
    [JSExport]
    internal static string OpenAnnotations(string basis, string theme) => PlaygroundAnnotations.Open(basis, theme);

    /// <summary>Applies one annotation edit request from the HTML runtime: <c>{ ok, html, basis }</c>.</summary>
    [JSExport]
    internal static string PrepareAnnotation(string basis, string theme, string operation, string id, string kind,
        double startX, double startY, double endX, double endY, double yOffset, string label, string yAxis) =>
        PlaygroundAnnotations.Prepare(basis, theme, operation, id, kind, startX, startY, endX, endY, yOffset, label, yAxis);
}
