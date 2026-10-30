using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ChartForgeX.Rendering;

namespace ChartForgeX.Core;

/// <summary>Immutable navigation, accessibility and host hooks for an authored mark, series or row.</summary>
/// <remarks>Class tokens and complete data-* names are validated and retained verbatim. Data-cfx-* is reserved.
/// A series link is the default for its marks; an item's non-null options replace that default.</remarks>
public sealed class ChartMarkOptions {
    /// <summary>Creates detached mark metadata. Relative, fragment, HTTP, HTTPS and mailto links are accepted.</summary>
    /// <param name="href">Optional navigation address.</param>
    /// <param name="linkTarget">Per-link browsing context, overriding the export policy.</param>
    /// <param name="tooltip">Plain text emitted in an SVG title.</param>
    /// <param name="accessibleLabel">Explicit accessible name; otherwise a chart-generated name is used.</param>
    /// <param name="cssClass">Space-separated CSS identifier tokens, retained without rewriting.</param>
    /// <param name="dataAttributes">Complete lowercase data-* attribute names and plain text values.</param>
    /// <param name="current">Whether this is the current item, shown with a dashed outline.</param>
    /// <param name="selected">Whether this is the page's selected item, shown with a solid outline and aria-current.
    /// This static state does not turn navigation links into selection widgets.</param>
    public ChartMarkOptions(string? href = null, VisualSvgLinkTarget linkTarget = VisualSvgLinkTarget.SameContext,
        string? tooltip = null, string? accessibleLabel = null, string? cssClass = null,
        IReadOnlyDictionary<string, string>? dataAttributes = null, bool current = false, bool selected = false) {
        if (href != null && ChartHyperlink.SafeHref(href) == null)
            throw new ArgumentException("Mark links must be relative, fragment, http, https, or mailto links.", nameof(href));
        if (!Enum.IsDefined(typeof(VisualSvgLinkTarget), linkTarget)) throw new ArgumentOutOfRangeException(nameof(linkTarget));
        if (accessibleLabel != null && string.IsNullOrWhiteSpace(accessibleLabel))
            throw new ArgumentException("An accessible label must not be empty.", nameof(accessibleLabel));
        ValidateClasses(cssClass);
        var attributes = new SortedDictionary<string, string>(StringComparer.Ordinal);
        if (dataAttributes != null) foreach (var entry in dataAttributes) {
            if (!IsDataName(entry.Key)) throw new ArgumentException("Host attributes require a safe lowercase data-* name outside data-cfx-*.", nameof(dataAttributes));
            attributes.Add(entry.Key, entry.Value ?? throw new ArgumentException("Host attribute values must not be null.", nameof(dataAttributes)));
        }
        Href = href; LinkTarget = linkTarget; Tooltip = tooltip; AccessibleLabel = accessibleLabel; CssClass = cssClass;
        DataAttributes = new ReadOnlyDictionary<string, string>(attributes); Current = current; Selected = selected;
    }

    /// <summary>Gets the optional navigation address.</summary>
    public string? Href { get; }
    /// <summary>Gets this link's browsing context.</summary>
    public VisualSvgLinkTarget LinkTarget { get; }
    /// <summary>Gets plain-text hover content.</summary>
    public string? Tooltip { get; }
    /// <summary>Gets the optional explicit accessible name.</summary>
    public string? AccessibleLabel { get; }
    /// <summary>Gets validated caller class tokens without rewriting.</summary>
    public string? CssClass { get; }
    /// <summary>Gets a detached, ordinally sorted, read-only set of complete host data-* attributes.</summary>
    public IReadOnlyDictionary<string, string> DataAttributes { get; }
    /// <summary>Gets whether this is the current item.</summary>
    public bool Current { get; }
    /// <summary>Gets whether this is the page's selected item. Emits aria-current and a data-cfx-selected hook.</summary>
    public bool Selected { get; }

    private static void ValidateClasses(string? value) {
        if (value == null) return;
        if (value.Length == 0) throw new ArgumentException("Class tokens must not be empty.", nameof(value));
        var tokens = value.Split(' ');
        foreach (var token in tokens) {
            if (token.Length == 0 || !(Letter(token[0]) || token[0] == '_' || token[0] == '-'))
                throw new ArgumentException("Class tokens must be CSS identifiers separated by single spaces.", nameof(value));
            if (token == "-" || token.Length > 1 && token[0] == '-' && char.IsDigit(token[1]))
                throw new ArgumentException("Class tokens must be CSS identifiers.", nameof(value));
            foreach (var c in token) if (!(Letter(c) || c >= '0' && c <= '9' || c == '_' || c == '-'))
                throw new ArgumentException("Class tokens must be CSS identifiers.", nameof(value));
        }
    }

    private static bool IsDataName(string name) {
        if (name == null || !name.StartsWith("data-", StringComparison.Ordinal) || name.Length <= 5
            || name.StartsWith("data-cfx-", StringComparison.Ordinal) || name == "data-cfx") return false;
        if (name[5] < 'a' || name[5] > 'z') return false;
        for (var i = 5; i < name.Length; i++) {
            var c = name[i];
            if (!(c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == '-' || c == '_')) return false;
        }
        return true;
    }

    private static bool Letter(char c) => c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z';
}
