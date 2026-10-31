using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace ChartForgeX.Interactivity.Html;

/// <summary>
/// The lightweight enhance runtime for static ChartForgeX SVG exported with
/// <see cref="ChartForgeX.Rendering.VisualSvgOptions.WithEnhanceHooks"/>: page-wide states, tooltips, keyboard focus,
/// legend toggles, linked views and the behaviour registry. It upgrades the SVG in place and never re-renders it.
/// </summary>
/// <remarks>
/// The script bundle is <c>cfx-shared.js</c>, <c>cfx-enhance.js</c>, <c>cfx-enhance.behaviours.js</c> and <c>cfx-link.js</c>;
/// the stylesheet is <c>cfx-states.css</c> and <c>cfx-enhance.css</c>. Include it once per document, inline with
/// <see cref="InlineMarkup"/> or <see cref="HtmlEnhanceDocument"/>, or as shared files with <see cref="ReferenceMarkup"/>.
/// See <c>docs/enhance.md</c>.
/// </remarks>
public static class HtmlEnhanceAssets {
    private static readonly string[] ScriptModules = { "cfx-shared.js", "cfx-enhance.js", "cfx-enhance.behaviours.js", "cfx-link.js" };
    private static readonly string[] StyleModules = { "cfx-states.css", "cfx-enhance.css" };
    private static readonly Lazy<string> ScriptBundle = new(() => Join(ScriptModules));
    private static readonly Lazy<string> StyleBundle = new(() => Join(StyleModules));
    private static readonly Lazy<HtmlAssetFile> ScriptAsset = new(() => new HtmlAssetFile("cfx-enhance", "js", Script));
    private static readonly Lazy<HtmlAssetFile> StyleAsset = new(() => new HtmlAssetFile("cfx-enhance", "css", Style));

    /// <summary>Gets the complete enhance script: shared parts, core runtime, built-in behaviours and the linked-views helper.</summary>
    public static string Script => ScriptBundle.Value;

    /// <summary>Gets the complete enhance stylesheet: interaction states, tooltip and legend affordances.</summary>
    public static string Style => StyleBundle.Value;

    /// <summary>Gets one source module by file name, for hosts that bundle the parts themselves.</summary>
    /// <param name="name">One of the module names listed in the remarks of <see cref="HtmlEnhanceAssets"/>.</param>
    /// <returns>The module text.</returns>
    public static string Module(string name) {
        if (Array.IndexOf(ScriptModules, name) < 0 && Array.IndexOf(StyleModules, name) < 0)
            throw new ArgumentException("Unknown enhance module: " + name, nameof(name));
        return Read(name);
    }

    /// <summary>Gets the script as a content-addressed shared file.</summary>
    public static HtmlAssetFile ScriptFile => ScriptAsset.Value;

    /// <summary>Gets the stylesheet as a content-addressed shared file.</summary>
    public static HtmlAssetFile StyleFile => StyleAsset.Value;

    /// <summary>Returns the files a bundle writes with <see cref="HtmlInteractiveAssetFiles.WriteTo"/>.</summary>
    public static IReadOnlyList<HtmlAssetFile> Files() => new[] { StyleFile, ScriptFile };

    /// <summary>Returns an inline <c>&lt;style&gt;</c> and <c>&lt;script&gt;</c> pair. Emit it once per document.</summary>
    /// <param name="nonce">An optional CSP nonce for both elements.</param>
    /// <returns>HTML markup.</returns>
    public static string InlineMarkup(string? nonce = null) {
        var attribute = NonceAttribute(nonce);
        return "<style" + attribute + ">" + Style + "</style>\n<script" + attribute + ">" + Script.Replace("</", "<\\/") + "</script>\n";
    }

    /// <summary>Returns a stylesheet link and a deferred script element that reference the shared files.</summary>
    /// <param name="assets">Where the bundle's asset files are served from.</param>
    /// <param name="nonce">An optional CSP nonce for the script element.</param>
    /// <returns>HTML markup.</returns>
    public static string ReferenceMarkup(HtmlAssetReferences assets, string? nonce = null) {
        if (assets == null) throw new ArgumentNullException(nameof(assets));
        string Integrity(HtmlAssetFile file) => assets.IncludeIntegrity
            ? " integrity=\"" + file.Integrity + "\" crossorigin=\"anonymous\"" : string.Empty;
        return "<link rel=\"stylesheet\" href=\"" + WebUtility.HtmlEncode(assets.Href(StyleFile)) + "\"" + Integrity(StyleFile) + ">\n"
            + "<script defer src=\"" + WebUtility.HtmlEncode(assets.Href(ScriptFile)) + "\"" + Integrity(ScriptFile) + NonceAttribute(nonce) + "></script>\n";
    }

    private static string NonceAttribute(string? nonce) {
        if (nonce == null) return string.Empty;
        foreach (var c in nonce) {
            if (!(c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9' || c == '+' || c == '/' || c == '=' || c == '-' || c == '_'))
                throw new ArgumentException("A CSP nonce contains only base64 or base64url characters.", nameof(nonce));
        }
        return nonce.Length == 0 ? throw new ArgumentException("A CSP nonce must not be empty.", nameof(nonce)) : " nonce=\"" + nonce + "\"";
    }

    private static string Join(IEnumerable<string> names) {
        var builder = new StringBuilder();
        foreach (var name in names) builder.Append(Read(name)).Append('\n');
        return builder.ToString();
    }

    private static string Read(string name) {
        var resource = "ChartForgeX.Interactivity.Html.Assets." + name;
        using var stream = typeof(HtmlEnhanceAssets).Assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException("Embedded enhance asset was not found: " + resource);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
