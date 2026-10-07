/// <summary>Reads and evaluates the gallery's recorded visual baseline.</summary>
public static partial class GalleryWriter {
    private static BaselineSummary ReadBaselineSummary(string output, ComparisonAsset[] pairs) {
        var baselineFile = FindVisualBaselineFile(output);
        if (baselineFile.Length == 0) return default;
        try {
            using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(baselineFile));
            var baselineCharts = document.RootElement.GetProperty("charts").EnumerateArray().ToArray();
            var generated = pairs.ToDictionary(pair => pair.Name, StringComparer.OrdinalIgnoreCase);
            var expected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var matches = 0;
            var warnings = 0;
            foreach (var baselineChart in baselineCharts) {
                var name = baselineChart.GetProperty("name").GetString() ?? string.Empty;
                if (!expected.Add(name) || !generated.TryGetValue(name, out var actual)) {
                    warnings++;
                    continue;
                }

                var width = baselineChart.GetProperty("width").GetInt32();
                var height = baselineChart.GetProperty("height").GetInt32();
                // Legacy baselines only constrain rounded dimensions. New baselines also
                // preserve the exact logical viewport, before native raster allocation.
                var logicalWidth = baselineChart.TryGetProperty("logicalWidth", out var storedWidth)
                    ? storedWidth.GetDouble() : actual.SvgDimensions.LogicalWidth;
                var logicalHeight = baselineChart.TryGetProperty("logicalHeight", out var storedHeight)
                    ? storedHeight.GetDouble() : actual.SvgDimensions.LogicalHeight;
                var svgBaseline = baselineChart.GetProperty("svg");
                var pngBaseline = baselineChart.GetProperty("png");
                var minVisualNodes = svgBaseline.GetProperty("minVisualNodes").GetInt32();
                var maxClippedTextNodes = ReadBaselineInt32(svgBaseline, "maxClippedTextNodes", int.MaxValue);
                var maxNearEdgeTextNodes = ReadBaselineInt32(svgBaseline, "maxNearEdgeTextNodes", int.MaxValue);
                var minVisiblePixels = pngBaseline.GetProperty("minVisiblePixels").GetInt64();
                var minTransparentPixels = ReadBaselineInt64(pngBaseline, "minTransparentPixels", 0);
                var minDistinctColors = pngBaseline.GetProperty("minDistinctColors").GetInt32();
                var outputScale = ReadBaselineInt32(pngBaseline, "outputScale", actual.PngScale);
                var maxEdgeInkPixels = ReadBaselineInt64(pngBaseline, "maxEdgeInkPixels", long.MaxValue);
                if (actual.SvgDimensions.Width == width &&
                    actual.SvgDimensions.Height == height &&
                    actual.SvgDimensions.LogicalWidth == logicalWidth &&
                    actual.SvgDimensions.LogicalHeight == logicalHeight &&
                    actual.PngScale == outputScale &&
                    actual.PngDimensions.Width == Math.Ceiling(logicalWidth * outputScale) &&
                    actual.PngDimensions.Height == Math.Ceiling(logicalHeight * outputScale) &&
                    actual.SvgHealth.VisualNodes >= minVisualNodes &&
                    actual.SvgHealth.ClippedTextNodes <= maxClippedTextNodes &&
                    actual.SvgHealth.NearEdgeTextNodes <= maxNearEdgeTextNodes &&
                    actual.PngHealth.VisiblePixels >= minVisiblePixels &&
                    actual.PngHealth.TransparentPixels >= minTransparentPixels &&
                    actual.PngHealth.DistinctColors >= minDistinctColors &&
                    actual.PngHealth.EdgeInkPixels <= maxEdgeInkPixels) {
                    matches++;
                } else {
                    warnings++;
                }
            }

            foreach (var pair in pairs) {
                if (!expected.Contains(pair.Name)) warnings++;
            }

            return new BaselineSummary(true, matches, warnings);
        } catch (IOException) {
        } catch (UnauthorizedAccessException) {
        } catch (System.Text.Json.JsonException) {
        } catch (InvalidOperationException) {
        } catch (KeyNotFoundException) {
        }

        return new BaselineSummary(true, 0, pairs.Length);
    }

    private static int ReadBaselineInt32(System.Text.Json.JsonElement element, string name, int fallback) {
        return element.TryGetProperty(name, out var value) &&
            value.ValueKind == System.Text.Json.JsonValueKind.Number &&
            value.TryGetInt32(out var number)
                ? number
                : fallback;
    }

    private static long ReadBaselineInt64(System.Text.Json.JsonElement element, string name, long fallback) {
        return element.TryGetProperty(name, out var value) &&
            value.ValueKind == System.Text.Json.JsonValueKind.Number &&
            value.TryGetInt64(out var number)
                ? number
                : fallback;
    }

    private static string FindVisualBaselineFile(string output) {
        var directory = new DirectoryInfo(Path.GetFullPath(output));
        while (directory != null) {
            var candidate = Path.Combine(directory.FullName, VisualBaselineFileName);
            if (File.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }

        return string.Empty;
    }

    private static string FormatBaselinePill(BaselineSummary baseline) {
        if (!baseline.IsPresent) return "<span class=\"pill warn\">no baseline</span>";
        return "<span class=\"pill " + (baseline.IsClean ? "ok" : "warn") + "\">" + baseline.ChartMatches.ToString(System.Globalization.CultureInfo.InvariantCulture) + " baseline passes</span><span class=\"pill " + (baseline.Warnings == 0 ? "ok" : "warn") + "\">" + baseline.Warnings.ToString(System.Globalization.CultureInfo.InvariantCulture) + " baseline warnings</span>";
    }

}
