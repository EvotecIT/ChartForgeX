namespace ChartForgeX.Interactivity.Html;

/// <summary>Emits the enhance runtime once for one HTML document, however many charts the document contains.</summary>
public sealed class HtmlEnhanceDocument {
    private readonly string? _nonce;
    private readonly HtmlAssetReferences? _assets;
    private bool _written;

    /// <summary>Creates a per-document emitter.</summary>
    /// <param name="nonce">An optional CSP nonce.</param>
    /// <param name="assets">When set, reference the shared files instead of inlining them.</param>
    public HtmlEnhanceDocument(string? nonce = null, HtmlAssetReferences? assets = null) { _nonce = nonce; _assets = assets; }

    /// <summary>Gets whether the runtime markup has been returned for this document.</summary>
    public bool Written => _written;

    /// <summary>Returns the runtime markup the first time and an empty string afterwards.</summary>
    /// <returns>HTML markup, or an empty string.</returns>
    public string Assets() {
        if (_written) return string.Empty;
        _written = true;
        return _assets == null ? HtmlEnhanceAssets.InlineMarkup(_nonce) : HtmlEnhanceAssets.ReferenceMarkup(_assets, _nonce);
    }
}
