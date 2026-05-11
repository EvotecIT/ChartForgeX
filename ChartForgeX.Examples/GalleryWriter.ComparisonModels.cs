/// <summary>
/// Holds model helpers for the SVG/PNG comparison gallery.
/// </summary>
public static partial class GalleryWriter {
    private static string Slugify(string value) {
        var sb = new System.Text.StringBuilder(value.Length);
        var previousWasDash = false;
        foreach (var ch in value) {
            if (char.IsLetterOrDigit(ch)) {
                sb.Append(char.ToLowerInvariant(ch));
                previousWasDash = false;
            } else if (!previousWasDash && sb.Length > 0) {
                sb.Append('-');
                previousWasDash = true;
            }
        }

        if (previousWasDash && sb.Length > 0) sb.Length--;
        return sb.Length == 0 ? "section" : sb.ToString();
    }

    private readonly struct ComparisonGroup {
        public ComparisonGroup(string name, string description, ComparisonAsset[] pairs) {
            Name = name;
            Description = description;
            Pairs = pairs;
        }

        public string Name { get; }

        public string Description { get; }

        public ComparisonAsset[] Pairs { get; }

        public int CleanPairs => Pairs.Count(pair => pair.Warnings.Length == 0);

        public int WarningCount => Pairs.Sum(pair => pair.Warnings.Length);

        public bool IsClean => WarningCount == 0;
    }

    private readonly struct AssetDimensions {
        public AssetDimensions(int width, int height) {
            Width = width;
            Height = height;
        }

        public int Width { get; }

        public int Height { get; }
    }

    private readonly struct ComparisonAsset {
        public ComparisonAsset(string name, AssetDimensions svgDimensions, AssetDimensions pngDimensions, long svgBytes, long pngBytes, SvgHealth svgHealth, PngHealth pngHealth, HtmlHealth htmlHealth) {
            Name = name;
            SvgDimensions = svgDimensions;
            PngDimensions = pngDimensions;
            SvgBytes = svgBytes;
            PngBytes = pngBytes;
            SvgHealth = svgHealth;
            PngHealth = pngHealth;
            HtmlHealth = htmlHealth;
        }

        public string Name { get; }

        public AssetDimensions SvgDimensions { get; }

        public AssetDimensions PngDimensions { get; }

        public long SvgBytes { get; }

        public long PngBytes { get; }

        public SvgHealth SvgHealth { get; }

        public PngHealth PngHealth { get; }

        public HtmlHealth HtmlHealth { get; }

        public int PngScale {
            get {
                if (SvgDimensions.Width <= 0 || SvgDimensions.Height <= 0 || PngDimensions.Width <= 0 || PngDimensions.Height <= 0) return 0;
                if (PngDimensions.Width % SvgDimensions.Width != 0 || PngDimensions.Height % SvgDimensions.Height != 0) return 0;
                var widthScale = PngDimensions.Width / SvgDimensions.Width;
                var heightScale = PngDimensions.Height / SvgDimensions.Height;
                return widthScale == heightScale ? widthScale : 0;
            }
        }

        public bool HasMatchingDimensions =>
            SvgDimensions.Width > 0 &&
            SvgDimensions.Height > 0 &&
            PngScale >= 1;

        public string[] Warnings {
            get {
                var warnings = new List<string>();
                if (!HasMatchingDimensions) warnings.Add("dimension mismatch");
                if (!SvgHealth.IsHealthy) warnings.Add("SVG health warning");
                if (SvgHealth.TinyTextNodes > 0) warnings.Add("SVG tiny text warning");
                if (SvgHealth.TinyStrokeNodes > 0) warnings.Add("SVG tiny stroke warning");
                if (SvgHealth.TinyMarkerNodes > 0) warnings.Add("SVG tiny marker warning");
                if (SvgHealth.ClippedTextNodes > 0) warnings.Add("SVG text bounds warning");
                if (!PngHealth.IsHealthy) warnings.Add("PNG health warning");
                if (PngHealth.EdgeInkPixels > MaximumHealthyPngEdgeInkPixels) warnings.Add("PNG edge pressure warning");
                if (!HtmlHealth.IsHealthy) warnings.Add("HTML health warning");
                return warnings.ToArray();
            }
        }
    }

    private readonly struct SvgHealth {
        public SvgHealth(int visualNodes, int textNodes, double minimumTextFontSize, int tinyTextNodes, int strokedNodes, double minimumStrokeWidth, int tinyStrokeNodes, int markerNodes, double minimumMarkerRadius, int tinyMarkerNodes, int clippedTextNodes, int nearEdgeTextNodes) {
            VisualNodes = visualNodes;
            TextNodes = textNodes;
            MinimumTextFontSize = minimumTextFontSize;
            TinyTextNodes = tinyTextNodes;
            StrokedNodes = strokedNodes;
            MinimumStrokeWidth = minimumStrokeWidth;
            TinyStrokeNodes = tinyStrokeNodes;
            MarkerNodes = markerNodes;
            MinimumMarkerRadius = minimumMarkerRadius;
            TinyMarkerNodes = tinyMarkerNodes;
            ClippedTextNodes = clippedTextNodes;
            NearEdgeTextNodes = nearEdgeTextNodes;
        }

        public int VisualNodes { get; }

        public int TextNodes { get; }

        public double MinimumTextFontSize { get; }

        public int TinyTextNodes { get; }

        public int StrokedNodes { get; }

        public double MinimumStrokeWidth { get; }

        public int TinyStrokeNodes { get; }

        public int MarkerNodes { get; }

        public double MinimumMarkerRadius { get; }

        public int TinyMarkerNodes { get; }

        public int ClippedTextNodes { get; }

        public int NearEdgeTextNodes { get; }

        public bool IsHealthy => VisualNodes >= MinimumHealthySvgVisualNodes && TinyTextNodes == 0 && TinyStrokeNodes == 0 && TinyMarkerNodes == 0;
    }

    private readonly struct PngHealth {
        public PngHealth(long visiblePixels, long foregroundPixels, PngContentBounds contentBounds, int distinctColors, long edgeInkPixels, long edgeBandPixels) {
            VisiblePixels = visiblePixels;
            ForegroundPixels = foregroundPixels;
            ContentBounds = contentBounds;
            DistinctColors = distinctColors;
            EdgeInkPixels = edgeInkPixels;
            EdgeBandPixels = edgeBandPixels;
        }

        public long VisiblePixels { get; }

        public long ForegroundPixels { get; }

        public PngContentBounds ContentBounds { get; }

        public int DistinctColors { get; }

        public long EdgeInkPixels { get; }

        public long EdgeBandPixels { get; }

        public bool IsHealthy => VisiblePixels >= MinimumHealthyPngVisiblePixels && ForegroundPixels >= MinimumHealthyPngVisiblePixels && DistinctColors >= MinimumHealthyPngDistinctColors && EdgeInkPixels <= MaximumHealthyPngEdgeInkPixels;
    }

    private readonly struct HtmlHealth {
        public HtmlHealth(long bytes, bool hasDocumentShell, bool hasViewport, bool hasInlineSvg, bool hasSurfaceGradient, bool hasTextPolish, bool hasVisibleOverflow, bool hasPrintCss) {
            Bytes = bytes;
            HasDocumentShell = hasDocumentShell;
            HasViewport = hasViewport;
            HasInlineSvg = hasInlineSvg;
            HasSurfaceGradient = hasSurfaceGradient;
            HasTextPolish = hasTextPolish;
            HasVisibleOverflow = hasVisibleOverflow;
            HasPrintCss = hasPrintCss;
        }

        public long Bytes { get; }

        public bool HasDocumentShell { get; }

        public bool HasViewport { get; }

        public bool HasInlineSvg { get; }

        public bool HasSurfaceGradient { get; }

        public bool HasTextPolish { get; }

        public bool HasVisibleOverflow { get; }

        public bool HasPrintCss { get; }

        public bool IsHealthy => Bytes > 0 && HasDocumentShell && HasViewport && HasInlineSvg && HasSurfaceGradient && HasTextPolish && HasVisibleOverflow && HasPrintCss;
    }

    private readonly struct PngContentBounds {
        public PngContentBounds(int left, int top, int right, int bottom) {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public int Left { get; }

        public int Top { get; }

        public int Right { get; }

        public int Bottom { get; }

        public int Width => IsEmpty ? 0 : Right - Left + 1;

        public int Height => IsEmpty ? 0 : Bottom - Top + 1;

        public bool IsEmpty => Right < Left || Bottom < Top;
    }

    private readonly struct BaselineSummary {
        public BaselineSummary(bool isPresent, int chartMatches, int warnings) {
            IsPresent = isPresent;
            ChartMatches = chartMatches;
            Warnings = warnings;
        }

        public bool IsPresent { get; }

        public int ChartMatches { get; }

        public int Warnings { get; }

        public bool IsClean => IsPresent && Warnings == 0;
    }
}
