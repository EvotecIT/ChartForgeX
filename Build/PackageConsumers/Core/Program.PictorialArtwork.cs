using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

internal static partial class Program {
    private static void PictorialArtworkContracts() {
        var pixels = new byte[] { 24, 96, 160, 255, 48, 128, 192, 255 };
        var artwork = ChartPictorialArtwork.FromImage(new RgbaImage(2, 1, pixels)); pixels[0] = 255;
        var chart = Chart.Create().WithSize(420, 260).WithHeader(false).WithLegend(false)
            .WithPictorialColumns(4).WithPictorialValuePerSymbol(1).WithPictorialArtwork(artwork)
            .WithPictorialFillDirection(ChartPictorialFillDirection.BottomToTop)
            .AddPictorial("Assessments", new[] { new ChartPictorialItem("Team A", 2.5) });
        var document = XDocument.Parse(chart.ToSvg());
        var image = document.Descendants().First(node => node.Name.LocalName == "image"); var href = image.Attribute("href")!.Value;
        var captured = RasterImageDecoder.Decode(Convert.FromBase64String(href.Substring(href.IndexOf(',') + 1)));
        PackageAssertions.Require(captured.Width == 2 && captured.Height == 1 && captured.Pixels[0] == 24
            && document.Descendants().Any(node => (string?)node.Attribute("data-cfx-value") == "2.5")
            && document.Descendants().Any(node => (string?)node.Attribute("data-cfx-fill-direction") == "BottomToTop"),
            "Packed pictorial artwork lost detached pixels, raw value or its declared partial direction.");
        PackageAssertions.Png(chart.ToPng());
        var fallback = Chart.Create().WithSize(420, 260).WithHeader(false).WithLegend(false)
            .WithPictorialSvgPath("M0 0H24V24H0Z M8 8V16H16V8Z")
            .AddPictorial("Assessments", new[] { new ChartPictorialItem("Team A", 2.5) });
        var fallbackSvg = fallback.ToSvg(); var fallbackPng = fallback.ToPng();
        fallback.WithPictorialArtwork(ChartPictorialArtwork.FromSvg("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 2 1'><rect width='2' height='1' fill='#2468AC'/></svg>", 16, 8));
        PackageAssertions.Require(fallback.ToSvg().Contains("data-cfx-image-artwork", StringComparison.Ordinal), "Packed strict SVG artwork did not reach native rendering.");
        fallback.WithPictorialArtwork(null);
        PackageAssertions.Require(fallback.ToSvg() == fallbackSvg && fallback.ToPng().SequenceEqual(fallbackPng),
            "Clearing packed pictorial artwork changed the retained custom contour or its default direction.");
    }
}
