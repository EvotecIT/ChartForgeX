using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Topology;

/// <summary>
/// Defines the severity of a topology icon catalog validation issue.
/// </summary>
public enum TopologyIconValidationSeverity {
    /// <summary>The issue should block the pack or catalog from being used.</summary>
    Error,

    /// <summary>The issue is usable but should be shown to pack authors.</summary>
    Warning
}

/// <summary>
/// Describes one topology icon pack or catalog validation issue.
/// </summary>
public sealed class TopologyIconValidationIssue {
    /// <summary>
    /// Initializes a validation issue.
    /// </summary>
    public TopologyIconValidationIssue(TopologyIconValidationSeverity severity, string path, string message) {
        Severity = severity;
        Path = RequiredText(path, nameof(path));
        Message = RequiredText(message, nameof(message));
    }

    /// <summary>Gets the issue severity.</summary>
    public TopologyIconValidationSeverity Severity { get; }

    /// <summary>Gets the pack or icon path that caused the issue.</summary>
    public string Path { get; }

    /// <summary>Gets the issue message.</summary>
    public string Message { get; }

    private static string RequiredText(string? value, string parameterName) {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value cannot be empty.", parameterName);
        return value!.Trim();
    }
}

/// <summary>
/// Contains validation diagnostics for one topology icon pack.
/// </summary>
public sealed class TopologyIconPackValidationResult {
    internal TopologyIconPackValidationResult(TopologyIconPack pack, IReadOnlyList<TopologyIconValidationIssue> issues) {
        Pack = pack ?? throw new ArgumentNullException(nameof(pack));
        Issues = issues ?? throw new ArgumentNullException(nameof(issues));
    }

    /// <summary>Gets the validated pack.</summary>
    public TopologyIconPack Pack { get; }

    /// <summary>Gets all validation issues.</summary>
    public IReadOnlyList<TopologyIconValidationIssue> Issues { get; }

    /// <summary>Gets issues with error severity.</summary>
    public IReadOnlyList<TopologyIconValidationIssue> Errors => Issues.Where(issue => issue.Severity == TopologyIconValidationSeverity.Error).ToList();

    /// <summary>Gets issues with warning severity.</summary>
    public IReadOnlyList<TopologyIconValidationIssue> Warnings => Issues.Where(issue => issue.Severity == TopologyIconValidationSeverity.Warning).ToList();

    /// <summary>Gets whether the pack can be used without blocking issues.</summary>
    public bool IsValid => !Issues.Any(issue => issue.Severity == TopologyIconValidationSeverity.Error);
}

/// <summary>
/// Contains validation diagnostics for a topology icon catalog.
/// </summary>
public sealed class TopologyIconCatalogValidationResult {
    internal TopologyIconCatalogValidationResult(IReadOnlyList<TopologyIconPackValidationResult> packResults, IReadOnlyList<TopologyIconValidationIssue> catalogIssues) {
        PackResults = packResults ?? throw new ArgumentNullException(nameof(packResults));
        CatalogIssues = catalogIssues ?? throw new ArgumentNullException(nameof(catalogIssues));
    }

    /// <summary>Gets per-pack validation diagnostics.</summary>
    public IReadOnlyList<TopologyIconPackValidationResult> PackResults { get; }

    /// <summary>Gets catalog-level validation diagnostics.</summary>
    public IReadOnlyList<TopologyIconValidationIssue> CatalogIssues { get; }

    /// <summary>Gets all validation issues.</summary>
    public IReadOnlyList<TopologyIconValidationIssue> Issues => CatalogIssues.Concat(PackResults.SelectMany(result => result.Issues)).ToList();

    /// <summary>Gets whether the catalog can be used without blocking issues.</summary>
    public bool IsValid => !Issues.Any(issue => issue.Severity == TopologyIconValidationSeverity.Error);
}

/// <summary>
/// Provides validation helpers for reusable topology icon packs.
/// </summary>
public static class TopologyIconValidationExtensions {
    /// <summary>
    /// Validates a topology icon pack for authoring issues and renderer compatibility.
    /// </summary>
    /// <param name="pack">The icon pack to validate.</param>
    /// <returns>Validation diagnostics for the pack.</returns>
    public static TopologyIconPackValidationResult Validate(this TopologyIconPack pack) {
        if (pack == null) throw new ArgumentNullException(nameof(pack));
        var issues = new List<TopologyIconValidationIssue>();
        if (pack.Icons.Count == 0) Add(issues, TopologyIconValidationSeverity.Warning, PackPath(pack), "Icon pack does not contain any icons.");
        if (!pack.IsBuiltIn && string.IsNullOrWhiteSpace(pack.Vendor)) Add(issues, TopologyIconValidationSeverity.Warning, PackPath(pack), "Custom icon packs should define a vendor.");
        AddDuplicateWarnings(issues, PackPath(pack) + ".tags", pack.Tags);

        var iconIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var icon in pack.Icons) {
            var path = PackPath(pack) + ".icons[" + icon.Id + "]";
            if (!iconIds.Add(icon.Id)) Add(issues, TopologyIconValidationSeverity.Error, path, "Icon id is duplicated inside the pack.");
            if (!string.Equals(icon.PackId, pack.Id, StringComparison.OrdinalIgnoreCase)) Add(issues, TopologyIconValidationSeverity.Error, path + ".packId", "Icon pack id does not match the containing pack.");
            if (!string.IsNullOrWhiteSpace(icon.Color) && !LooksLikeHexColor(icon.Color!)) Add(issues, TopologyIconValidationSeverity.Warning, path + ".color", "Icon color should use #RGB or #RRGGBB for portable rendering.");
            if (!string.IsNullOrWhiteSpace(icon.Symbol) && icon.Symbol!.Trim().Length > 8) Add(issues, TopologyIconValidationSeverity.Warning, path + ".symbol", "Icon symbol is long and may not fit compact palette nodes.");
            if (icon.Artwork != null && !icon.Artwork.HasSvgBody && !icon.Artwork.HasSvgPath && !icon.Artwork.HasImageHref) Add(issues, TopologyIconValidationSeverity.Warning, path + ".artwork", "Icon artwork is empty.");
            if (icon.Artwork != null && !icon.Artwork.IsSafe) Add(issues, TopologyIconValidationSeverity.Error, path + ".artwork", "Icon artwork contains unsafe SVG or image href content.");
            AddDuplicateWarnings(issues, path + ".tags", icon.Tags);
        }

        return new TopologyIconPackValidationResult(pack, issues);
    }

    /// <summary>
    /// Validates all icon packs in a catalog.
    /// </summary>
    /// <param name="catalog">The icon catalog to validate.</param>
    /// <returns>Validation diagnostics for the catalog.</returns>
    public static TopologyIconCatalogValidationResult Validate(this TopologyIconCatalog catalog) {
        if (catalog == null) throw new ArgumentNullException(nameof(catalog));
        var catalogIssues = new List<TopologyIconValidationIssue>();
        var packIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pack in catalog.Packs) {
            if (!packIds.Add(pack.Id)) Add(catalogIssues, TopologyIconValidationSeverity.Error, "catalog.packs[" + pack.Id + "]", "Pack id is duplicated inside the catalog.");
        }

        return new TopologyIconCatalogValidationResult(catalog.Packs.Select(pack => pack.Validate()).ToList(), catalogIssues);
    }

    private static void AddDuplicateWarnings(List<TopologyIconValidationIssue> issues, string path, IReadOnlyList<string> values) {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in values) {
            if (string.IsNullOrWhiteSpace(value)) continue;
            if (!seen.Add(value.Trim())) Add(issues, TopologyIconValidationSeverity.Warning, path, "Tag '" + value.Trim() + "' is duplicated.");
        }
    }

    private static void Add(List<TopologyIconValidationIssue> issues, TopologyIconValidationSeverity severity, string path, string message) {
        issues.Add(new TopologyIconValidationIssue(severity, path, message));
    }

    private static string PackPath(TopologyIconPack pack) {
        return "packs[" + pack.Id + "]";
    }

    private static bool LooksLikeHexColor(string value) {
        var text = value.Trim();
        if (text.Length != 4 && text.Length != 7) return false;
        if (text[0] != '#') return false;
        for (var i = 1; i < text.Length; i++) {
            var ch = text[i];
            if (!char.IsDigit(ch) && (ch < 'a' || ch > 'f') && (ch < 'A' || ch > 'F')) return false;
        }

        return true;
    }
}
