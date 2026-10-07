using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using System.Xml.Linq;

/// <summary>Equivalent legacy public-API workloads; this type can load against the frozen pre-v2 assembly.</summary>
public static partial class LegacySceneBenchmarkCases {
    private static string _tokens = "";
    internal const int Width = 800, Height = 440;
    internal const string Title = "Checks over the reporting period";

    /// <summary>Reads canonical theme input outside timing.</summary>
    public static void Initialize(string tokenPath, string fontPath, string boldFontPath) {
        _tokens = File.ReadAllText(tokenPath);
        FontRegistry.Register("CFX Proof Carlito", fontPath, 400);
        FontRegistry.Register("CFX Proof Carlito", boldFontPath, 700);
    }

    /// <summary>Builds a deterministic model with the same categories and values used by the scene lane.</summary>
    public static object Create(string fixture) {
        if (!Fixtures.Contains(fixture)) throw new ArgumentOutOfRangeException(nameof(fixture));
        var tokens = VisualDesignTokens.FromJson(_tokens);
        tokens.UseGraphiteLayout = true;
        var theme = tokens.ApplyTo(ChartTheme.GraphiteLight()).WithFontFamily("CFX Proof Carlito")
            .WithTypography(22, 13, 11, 11, 12, 11);
        var chart = Chart.Create().WithSize(Width, Height).WithTheme(theme).WithTitle(Title)
            .WithSubtitle("One prepared scene for SVG and native PNG").WithLegend().WithPngOutputScale(1)
            .WithXLabels(Enumerable.Range(1, fixture.EndsWith("bars", StringComparison.Ordinal) ? 24 : 7).Select(index => "Day " + index).ToArray());
        // Explicit marker density keeps the same marks visible in the two rendering paths.
        chart.Options.LineMarkerMode = ChartLineMarkerMode.All;
        switch (fixture) {
            case "donut": chart.AddDonut("Findings", Points(0)); break;
            case "cartesian": chart.AddLine("Observed", Points(0)).AddArea("Expected", Points(1)).AddBar("Capacity", Points(2)); break;
            case "grouped-bars": case "stacked-bars":
                for (var series = 0; series < 3; series++) chart.AddBar("Measure " + (series + 1), Enumerable.Range(0, 24)
                    .Select(index => new ChartPoint(index + 1, index % 6 == 0 ? 0 : (index * 7 + series * 11) % 61 - 30)));
                if (fixture == "stacked-bars") chart.WithStackedBars();
                break;
            case "scatter": chart.AddScatter("Samples", Enumerable.Range(0, 500)
                .Select(index => new ChartPoint(index / 5d, 50 + 30 * Math.Sin(index * .37) + index % 17))); break;
            case "dense-line": chart.AddLine("Signal", Enumerable.Range(0, 1000)
                .Select(index => new ChartPoint(index / 10d, 50 + 30 * Math.Sin(index * .03) + index % 11, index > 0 && index % 200 == 0))); break;
        }
        if (fixture == "scatter" || fixture == "dense-line") chart.Options.XAxisLabels.Clear();
        return chart;
    }

    /// <summary>The bounded proof matrix includes sparse, signed, numeric and dense models.</summary>
    public static readonly string[] Fixtures = { "cartesian", "donut", "grouped-bars", "stacked-bars", "scatter", "dense-line" };

    private static ChartPoint[] Points(int series) => Enumerable.Range(0, 7)
        .Select(index => new ChartPoint(index + 1, 20 + series * 14 + index * (series + 3) % 23)).ToArray();

    /// <summary>The legacy model has no public prepared-scene boundary.</summary>
    public static object Prepare(object model) => throw new NotSupportedException("Legacy prepared-scene operations are not measured.");

    /// <summary>Renders through the existing public export surface.</summary>
    public static object Execute(object model, string operation) {
        var chart = (Chart)model;
        return operation switch { "Svg" => chart.ToSvg(), "Png" => chart.ToPng(), "Rgba" => chart.ToRgbaImage(), _ => throw new ArgumentOutOfRangeException(nameof(operation)) };
    }

    /// <summary>Checks equivalent dimensions, visible raster data, SVG title and substantial marks outside timing.</summary>
    public static long Validate(object result, string operation, string fixture) {
        if (result is string svg) {
            var root = XDocument.Parse(svg).Root ?? throw new InvalidOperationException("SVG root is missing.");
            if ((double?)root.Attribute("width") != Width || (double?)root.Attribute("height") != Height)
                throw new InvalidOperationException("SVG dimensions changed.");
            if (!svg.Contains(Title, StringComparison.Ordinal))
                throw new InvalidOperationException("SVG content is incomplete.");
            ValidateMarks(root, (Chart)Create(fixture));
            return System.Text.Encoding.UTF8.GetByteCount(svg);
        }
        if (result is byte[] png) {
            if (png.Length < 1000 || png[0] != 137 || png[1] != 80 || ReadBigEndian(png, 16) != Width || ReadBigEndian(png, 20) != Height)
                throw new InvalidOperationException("PNG payload or dimensions changed.");
            Validate(RasterImageDecoder.Decode(png), "Rgba", fixture);
            return png.Length;
        }
        if (result is RgbaImage image) {
            if (image.Width != Width || image.Height != Height || image.Pixels.Length != Width * Height * 4)
                throw new InvalidOperationException("RGBA dimensions changed.");
            var first = image.Pixels.AsSpan(0, 4);
            var changed = 0;
            for (var index = 4; index < image.Pixels.Length; index += 4)
                if (!image.Pixels.AsSpan(index, 4).SequenceEqual(first)) changed++;
            if (changed < 1000) throw new InvalidOperationException("RGBA output has insufficient visible content.");
            return image.Pixels.Length;
        }
        throw new InvalidOperationException("Unknown benchmark output for " + operation);
    }

    private static int ReadBigEndian(byte[] bytes, int offset) => bytes[offset] << 24 | bytes[offset + 1] << 16 | bytes[offset + 2] << 8 | bytes[offset + 3];
}
