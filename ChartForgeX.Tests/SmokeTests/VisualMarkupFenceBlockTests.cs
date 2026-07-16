using System.Linq;
using ChartForgeX.Markup;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void VisualMarkupScannerParsesHostDiscoveredFenceBlocks() {
        var result = VisualMarkupScanner.ParseFenceBlock(
            "chartforgex table v1 {#users .compact mode=\"native\"}",
            "| Name | State |\n| --- | --- |\n| Ada | Enabled |",
            fenceLine: 20,
            payloadStartLine: 21,
            payloadEndLine: 23);

        Assert(result.Diagnostics.Count == 0, "A supported host-discovered fence should not produce diagnostics.");
        var block = result.Blocks.Single();
        Assert(block.Kind == VisualMarkupKind.Table, "The direct host path should use the same visual-kind contract as Markdown scanning.");
        Assert(block.FenceLine == 20 && block.StartLine == 21 && block.EndLine == 23, "The direct host path should preserve source lines.");
        Assert(block.Attributes["id"] == "users" && block.Attributes["class"] == "compact" && block.Attributes["mode"] == "native", "The direct host path should use the shared fence attribute parser.");

        var invalid = VisualMarkupScanner.ParseFenceBlock(
            "chartforgex table",
            "| Name |",
            fenceLine: 40,
            payloadStartLine: 41,
            payloadEndLine: 41);
        Assert(invalid.Blocks.Count == 0, "An unversioned direct fence should not become a visual block.");
        Assert(invalid.Diagnostics.Count == 1 && invalid.Diagnostics[0].Line == 40, "The direct host path should return shared line-aware validation diagnostics.");

        var parsedInvalid = new VisualMarkupParser().Parse(invalid);
        Assert(parsedInvalid.Artifacts.Count == 0, "An invalid host-discovered fence should not produce an artifact.");
        Assert(parsedInvalid.Diagnostics.Count == 1 && parsedInvalid.Diagnostics[0].Line == 40, "The scan-result parser path should preserve host fence diagnostics.");

        var invalidDimensions = VisualMarkupScanner.ParseFenceBlock(
            "chartforgex topology v1",
            "group D1 \"Primary\" width:0\nnode dc1 \"DC1\" group:D1",
            fenceLine: 50,
            payloadStartLine: 51,
            payloadEndLine: 52);
        var parsedDimensions = new VisualMarkupParser().Parse(invalidDimensions);
        Assert(parsedDimensions.Artifacts.Count == 0, "A topology with invalid dimensions should not escape as an artifact that fails during rendering.");
        Assert(parsedDimensions.Diagnostics.Any(diagnostic => diagnostic.Line == 51 && diagnostic.Message.Contains("width must be a positive finite number", System.StringComparison.Ordinal)), "Invalid topology dimensions should be rejected with a line-aware parse diagnostic.");
    }
}
