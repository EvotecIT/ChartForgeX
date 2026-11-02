using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ChartForgeX.SvgRaster;
using ChartForgeX.Typography;

/// <summary>Records font provenance for reviewed, content-sized gallery exports.</summary>
public static partial class GalleryWriter {
    // These convenience exports measure their outer heading before choosing natural height.
    // New examples stay strict until their size policy is reviewed here.
    private static readonly HashSet<string> NaturalHeightExamples = new(StringComparer.Ordinal) {
        "advanced-topology",
        "brand-kit-showcase-grid",
        "cartesian-automatic-category-margins",
        "chart-mark-surfaces-showcase-grid",
        "control-scorecards-grid",
        "data-label-placement-showcase-grid",
        "dc-connectivity",
        "foundation-dashboard-typography-dark",
        "foundation-dashboard-typography-light",
        "foundation-typed-facets",
        "funnel-surfaces-showcase-grid",
        "icon-palette",
        "label-placement-scorecards-dark",
        "label-placement-scorecards-light",
        "map-viewport-showcase-grid",
        "mermaid-cynefin-basic",
        "mermaid-er-direction",
        "mermaid-source-font-size-group-header-fit",
        "mermaid-state-direction",
        "mermaid-swimlane-basic",
        "mermaid-usecase-basic",
        "paged-facets-1",
        "paged-facets-2",
        "paged-facets-3",
        "palette-swatch-showcase-grid",
        "people-infographic-showcase-grid",
        "point-color-customization-showcase-grid",
        "replication-mesh",
        "replication-mesh-critical-view",
        "replication-mesh-dc-view",
        "replication-mesh-offenders-view",
        "service-dependency",
        "service-dependency-api-neighbors-view",
        "service-dependency-compact-view",
        "shared-axis-coverage-grid",
        "site-topology",
        "subnets-site-links",
        "theme-font-showcase-grid",
        "visual-directory-health-replication",
        "visual-directory-level-window",
        "visual-entity-relationship-overview",
        "visual-evidence-timeline-relationship",
        "visual-force-busy-relationship-graph",
        "visual-force-relationship-graph",
        "visual-impact-dependency-overview",
        "visual-mini-correlation-map",
        "visual-ownership-evidence-bundle",
        "visual-readable-dense-replication",
        "visual-relationship-radial-500-ego-graph",
        "visual-relationship-radial-ego-graph",
        "visual-replication-health-hub",
        "visual-replication-mesh-explorer",
        "visual-replication-mesh-route-motion",
        "visual-reusable-regional-topology",
        "visual-subnets-site-links-map",
        "visual-team-hierarchy-builder",
        "visual-topology-explorer",
        "visual-topology-mixed-routing",
        "visual-topology-shared-trunks",
    };

    // WidthMode is natural when the export fits both canvas dimensions to its measured content.
    private readonly record struct LayoutProvenance(string HeightMode, string FrameFontRequest, string FrameFontFingerprint, string WidthMode = "fixed");

    private static LayoutProvenance ReadLayoutProvenance(string name, string svgPath) {
        var document = SvgRasterParser.ParseDocument(File.ReadAllText(svgPath));
        // A content-fitted topology (natural Mermaid/topology sizing) measures every caption for both axes,
        // so all of its text faces, not only the heading, decide its canvas.
        var fitted = FitsContent(document.Root);
        if (!fitted && !NaturalHeightExamples.Contains(name)) return new("fixed", "", "");
        var requests = new SortedSet<string>(StringComparer.Ordinal);
        var faces = new SortedSet<string>(StringComparer.Ordinal);
        var definitions = SvgRasterDefinitions.From(document);
        var ancestors = new List<SvgRasterElement>();
        var unavailable = false;
        // Only the outer frame controls natural height; nested panels have their own fixed viewports.
        Inspect(document.Root, SvgRasterStyle.Default, fitted);
        return new("natural", string.Join("\n", requests), unavailable || faces.Count == 0 ? "" : Hash(Encoding.UTF8.GetBytes(string.Join("\n", faces))),
            fitted || name == "advanced-topology" ? "natural" : "fixed");

        void Inspect(SvgRasterElement element, SvgRasterStyle parent, bool heading, string? authoredRequest = null) {
            if (element.Name is "defs" or "style" or "title" or "desc" || element.Get("data-cfx-role") == "panel" || element.Name == "svg" && ancestors.Count > 0) return;
            var style = SvgRasterStyle.Resolve(parent, element, definitions.StyleSheet, ancestors);
            authoredRequest = element.Get("data-cfx-authored-font-request") ?? authoredRequest;
            heading |= element.Get("data-cfx-role") is "frame-heading" or "frame-heading-continuation" or "legend-title" or "legend-label";
            if (heading && element.Name == "text") {
                var request = authoredRequest ?? string.Join("|", style.FontFamily, style.FontWeight.ToString(CultureInfo.InvariantCulture), style.FontStyle, style.FontSize.ToString("R", CultureInfo.InvariantCulture), style.Variations?.Key ?? "");
                requests.Add(request);
                var face = TypographyFontResolver.WithVariations(TypographyFontResolver.ResolveFace(style.FontFamily, style.FontWeight, style.FontStyle != "normal"), style.Variations);
                // Font-size fitting changes the paint request, not the identity of the resolved face.
                if (face.Font == null) faces.Add(request + "|bitmap");
                // A bundled product face has no file; its family and captured identity are stable across hosts.
                else if (face.Path == null) faces.Add(request + "|bundled|" + face.Font.SelectedFamily + "|" + face.Font.CollectionIndex + "|" + face.Font.Variations.Key + "|" + face.SynthesizeBold + "|" + face.SynthesizeItalic);
                else if (!File.Exists(face.Path)) unavailable = true;
                else {
                    try {
                        faces.Add(request + "|" + Hash(File.ReadAllBytes(face.Path)) + "|" + face.Font.CollectionIndex + "|" + face.Font.SelectedFamily + "|" + face.Font.Variations.Key + "|" + face.SynthesizeBold + "|" + face.SynthesizeItalic);
                    } catch (IOException) { unavailable = true; }
                    catch (UnauthorizedAccessException) { unavailable = true; }
                }
            }
            ancestors.Add(element);
            foreach (var child in element.Children) Inspect(child, style, heading, authoredRequest);
            ancestors.RemoveAt(ancestors.Count - 1);
        }
    }

    // The first topology or panel in document order owns the outer canvas; panels keep their own fixed viewports.
    private static bool FitsContent(SvgRasterElement root) => FirstCanvasOwner(root) is { } owner
        && owner.Get("data-cfx-role") == "topology" && owner.Get("data-fit-content-to-viewport") == "false";

    private static SvgRasterElement? FirstCanvasOwner(SvgRasterElement element) {
        if (element.Get("data-cfx-role") is "topology" or "panel") return element;
        foreach (var child in element.Children) if (FirstCanvasOwner(child) is { } owner) return owner;
        return null;
    }

    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));

    private static bool AllowsNaturalHeight(System.Text.Json.JsonElement expected, LayoutProvenance actual) =>
        actual.HeightMode == "natural" && actual.FrameFontFingerprint.Length > 0 &&
        expected.TryGetProperty("layout", out var layout) &&
        layout.TryGetProperty("heightMode", out var mode) && mode.GetString() == "natural" &&
        layout.TryGetProperty("frameFontRequest", out var request) && request.GetString() == actual.FrameFontRequest &&
        layout.TryGetProperty("frameFontFingerprint", out var fingerprint) && !string.IsNullOrEmpty(fingerprint.GetString()) && fingerprint.GetString() != actual.FrameFontFingerprint;
}
