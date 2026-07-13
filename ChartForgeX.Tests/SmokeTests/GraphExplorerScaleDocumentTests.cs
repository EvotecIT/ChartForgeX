using System;
using System.Text.Json;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static (double MinX, double MinY, double MaxX, double MaxY, double Width, double Height, double CenterX, double CenterY) GraphDocumentNodeBounds(string html, int expectedCount) {
        const string marker = "<script type=\"application/json\" data-cfx-role=\"graph-document\">";
        var start = html.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0) throw new InvalidOperationException("Missing compact graph document.");
        start += marker.Length;
        var end = html.IndexOf("</script>", start, StringComparison.Ordinal);
        if (end < start) throw new InvalidOperationException("Malformed compact graph document.");
        using var document = JsonDocument.Parse(html.Substring(start, end - start));
        var nodes = document.RootElement.GetProperty("n");
        if (nodes.GetArrayLength() != expectedCount) throw new InvalidOperationException("Compact graph document node count did not match the scene.");
        var minX = double.PositiveInfinity;
        var minY = double.PositiveInfinity;
        var maxX = double.NegativeInfinity;
        var maxY = double.NegativeInfinity;
        foreach (var node in nodes.EnumerateArray()) {
            var x = node[24].GetDouble();
            var y = node[25].GetDouble();
            minX = Math.Min(minX, x);
            minY = Math.Min(minY, y);
            maxX = Math.Max(maxX, x);
            maxY = Math.Max(maxY, y);
        }

        return (minX, minY, maxX, maxY, maxX - minX, maxY - minY, (minX + maxX) / 2, (minY + maxY) / 2);
    }
}
