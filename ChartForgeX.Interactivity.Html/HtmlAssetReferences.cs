using System;

namespace ChartForgeX.Interactivity.Html;

/// <summary>
/// Makes generated pages reference the interactive runtimes as shared asset files instead of inlining them, so a
/// bundle of many reports ships one copy of each runtime. Write the files with <see cref="HtmlInteractiveAssetFiles"/>.
/// </summary>
public sealed class HtmlAssetReferences {
    /// <summary>Initializes asset references rooted at <paramref name="basePath"/>.</summary>
    /// <param name="basePath">The folder or URL the pages load assets from, for example <c>assets/</c>,
    /// <c>../shared/cfx/</c>, or <c>https://reports.example.test/assets/</c>. A trailing slash is added when missing.
    /// Only relative paths and <c>http</c>/<c>https</c> URLs are accepted; network paths (<c>//host</c>, <c>\\server</c>),
    /// queries, and fragments are rejected.</param>
    public HtmlAssetReferences(string basePath) {
        if (string.IsNullOrWhiteSpace(basePath)) throw new ArgumentException("Asset base path must not be empty.", nameof(basePath));
        var value = basePath.Trim().Replace('\\', '/');
        foreach (var character in value) {
            if (char.IsControl(character) || char.IsWhiteSpace(character) || character == '"' || character == '\'' || character == '<' || character == '>' || character == '`') {
                throw new ArgumentException("Asset base path must not contain whitespace, quotes, or angle brackets.", nameof(basePath));
            }
        }

        if (value.StartsWith("//", StringComparison.Ordinal)) throw new ArgumentException("Asset base path must not be a network or protocol-relative path.", nameof(basePath));
        if (value.IndexOf('?') >= 0 || value.IndexOf('#') >= 0) throw new ArgumentException("Asset base path must not contain a query or fragment.", nameof(basePath));
        var colon = value.IndexOf(':');
        var slash = value.IndexOf('/');
        if (colon >= 0 && (slash < 0 || colon < slash) &&
            !value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !value.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) {
            throw new ArgumentException("Asset base path must be relative or an http/https URL.", nameof(basePath));
        }

        BasePath = value.EndsWith("/", StringComparison.Ordinal) ? value : value + "/";
    }

    /// <summary>Gets the normalized base path, always ending with <c>/</c>.</summary>
    public string BasePath { get; }

    /// <summary>
    /// Gets or sets a value indicating whether script and stylesheet references carry a Subresource Integrity
    /// (<c>sha384</c>) attribute. Off by default because browsers skip integrity-checked assets loaded from
    /// <c>file://</c> pages; enable it for hosted bundles. Integrity-checked assets are requested with
    /// <c>crossorigin="anonymous"</c>, so assets on another origin must be served with CORS headers.
    /// </summary>
    public bool IncludeIntegrity { get; set; }

    internal string Href(HtmlAssetFile file) => BasePath + file.FileName;
}
