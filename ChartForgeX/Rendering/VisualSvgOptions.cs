using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

/// <summary>Controls safe host integration of a detached SVG without changing its layout or static pixels.</summary>
public sealed class VisualSvgOptions {
    private readonly SvgColorVariables? _colorVariables;

    /// <summary>Creates a detached SVG export policy.</summary>
    /// <param name="idPrefix">Unique host namespace; null uses a deterministic content namespace.</param>
    /// <param name="colorVariables">Host CSS properties with resolved static fallback colors. The mapping is copied.</param>
    /// <param name="linkTarget">Whether safe scene links use the current browsing context or a new protected context.</param>
    public VisualSvgOptions(string? idPrefix = null, SvgColorVariables? colorVariables = null, VisualSvgLinkTarget linkTarget = VisualSvgLinkTarget.SameContext) {
        if (!Enum.IsDefined(typeof(VisualSvgLinkTarget), linkTarget)) throw new ArgumentOutOfRangeException(nameof(linkTarget));
        IdPrefix = idPrefix == null ? null : VisualSceneSvgRenderer.ValidatePrefix(idPrefix);
        _colorVariables = colorVariables?.Clone();
        LinkTarget = linkTarget;
    }

    /// <summary>Gets the host ID namespace, or null for a deterministic content namespace.</summary>
    public string? IdPrefix { get; }
    /// <summary>Gets an independent copy of the host color mapping.</summary>
    public SvgColorVariables? ColorVariables => _colorVariables?.Clone();
    /// <summary>Gets the safe link target policy.</summary>
    public VisualSvgLinkTarget LinkTarget { get; }
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
