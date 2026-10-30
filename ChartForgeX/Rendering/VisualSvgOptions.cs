using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

/// <summary>Controls safe SVG host integration and explicit captured-effect representation without repeating layout.</summary>
public sealed partial class VisualSvgOptions {
    private SvgColorVariables? _colorVariables;
    private bool _patternTiles;
    /// <summary>Gets whether native hatch marks serialize as user-space SVG tiles instead of individual clipped lines.</summary>
    public bool PatternTiles => _patternTiles;
    /// <summary>Returns an export policy with native user-space pattern tiles, retaining the other host settings.</summary>
    /// <param name="enabled">Whether to use tiles. Static exports keep individual native lines by default.</param>
    /// <returns>An independent export policy.</returns>
    public VisualSvgOptions WithPatternTiles(bool enabled = true) {
        var copy = Copy(); copy._patternTiles = enabled; return copy;
    }

    // Policies own detached maps and expose only clones; immutable values and partial policy fields can be shared safely.
    private VisualSvgOptions Copy() => (VisualSvgOptions)MemberwiseClone();

    /// <summary>Retains the complete export policy when an adapter scopes another emission.</summary>
    internal VisualSvgOptions WithIdPrefix(string? idPrefix) {
        var copy = Copy(); copy.IdPrefix = idPrefix == null ? null : VisualSceneSvgRenderer.ValidatePrefix(idPrefix); return copy;
    }

    internal VisualSvgOptions WithColorScope(SvgColorVariables? variables) {
        var copy = Copy(); copy._colorVariables = variables?.Clone(); return copy;
    }

    /// <summary>Creates a detached SVG export policy.</summary>
    /// <param name="idPrefix">Unique host namespace; null uses a deterministic content namespace.</param>
    /// <param name="colorVariables">Host CSS properties with resolved static fallback colors. The mapping is copied.</param>
    /// <param name="linkTarget">Whether safe scene links use the current browsing context or a new protected context.</param>
    /// <param name="effectProfile">Baked native effect pixels, or supported typed native SVG effects on retained vectors.</param>
    /// <param name="effectRasterOptions">Existing native raster options for baked effects; unused by native SVG filter math.</param>
    /// <param name="responsive">Whether inline SVG shrinks to its host width while preserving the logical viewport and aspect ratio.</param>
    public VisualSvgOptions(string? idPrefix = null, SvgColorVariables? colorVariables = null, VisualSvgLinkTarget linkTarget = VisualSvgLinkTarget.SameContext,
        bool responsive = false, VisualSvgEffectProfile effectProfile = VisualSvgEffectProfile.Baked, VisualRenderOptions? effectRasterOptions = null) {
        if (!Enum.IsDefined(typeof(VisualSvgLinkTarget), linkTarget)) throw new ArgumentOutOfRangeException(nameof(linkTarget));
        if (!Enum.IsDefined(typeof(VisualSvgEffectProfile), effectProfile)) throw new ArgumentOutOfRangeException(nameof(effectProfile));
        EffectProfile = effectProfile;
        EffectRasterOptions = effectRasterOptions;
        IdPrefix = idPrefix == null ? null : VisualSceneSvgRenderer.ValidatePrefix(idPrefix);
        _colorVariables = colorVariables?.Clone();
        LinkTarget = linkTarget;
        Responsive = responsive;
    }

    /// <summary>Gets how retained typed scene effects lower. Explicit captured images remain images.</summary>
    public VisualSvgEffectProfile EffectProfile { get; }
    /// <summary>Gets immutable density/coverage/cap options for baked effects; null resolves native defaults only when needed.</summary>
    public VisualRenderOptions? EffectRasterOptions { get; }
    /// <summary>Gets the host ID namespace, or null for a deterministic content namespace.</summary>
    public string? IdPrefix { get; private set; }
    /// <summary>Gets an independent copy of the host color mapping.</summary>
    public SvgColorVariables? ColorVariables => _colorVariables?.Clone();
    /// <summary>Gets the safe link target policy.</summary>
    public VisualSvgLinkTarget LinkTarget { get; }
    /// <summary>Gets whether the SVG carries self-contained responsive sizing for inline hosts.</summary>
    public bool Responsive { get; }
    internal SvgColorVariables? Variables => _colorVariables;

    /// <summary>Maps an external host identifier to a deterministic valid namespace without losing identity to sanitization collisions.</summary>
    internal static string? NamespaceFromExternalId(string? value) {
        if (string.IsNullOrWhiteSpace(value)) return null;
        try { return VisualSceneSvgRenderer.ValidatePrefix(value!); }
        catch (ArgumentException) {
            using var hash = SHA256.Create();
            var result = new StringBuilder("cfx-scope-");
            foreach (var item in hash.ComputeHash(Encoding.UTF8.GetBytes(value!))) result.Append(item.ToString("x2", CultureInfo.InvariantCulture));
            return result.ToString();
        }
    }
}

/// <summary>A constrained SVG anchor policy; arbitrary browsing context names and attributes are not accepted.</summary>
public enum VisualSvgLinkTarget {
    /// <summary>Navigate in the existing browsing context.</summary>
    SameContext,
    /// <summary>Open a new context with noopener and noreferrer protection.</summary>
    NewContext
}
