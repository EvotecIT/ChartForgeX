using ChartForgeX;
using ChartForgeX.Effects;
using ChartForgeX.SvgRaster;

internal static partial class Program {
    private static void FiniteSourceCompositeContracts() {
        PackageAssertions.Owner(typeof(VisualCompositeOperator), "ChartForgeX");
        const string source = "<svg xmlns='http://www.w3.org/2000/svg' width='32' height='32'><defs>"
            + "<filter id='cut' filterUnits='userSpaceOnUse' primitiveUnits='userSpaceOnUse' x='0' y='0' width='32' height='32' color-interpolation-filters='sRGB'>"
            + "<feFlood flood-color='blue' flood-opacity='0.5019607843137255' result='backdrop'/>"
            + "<feComposite in='SourceGraphic' in2='backdrop' operator='in'/></filter></defs>"
            + "<rect id='mark' x='8' y='8' width='16' height='16' fill='red' fill-opacity='0.5019607843137255' filter='url(#cut)'/></svg>";
        var result = SvgRasterizer.Rasterize(source, strict: true, filters: new SvgRasterFilterOptions());
        var offset = (16 * result.Image.Width + 16) * 4;
        PackageAssertions.Require(result.Diagnostics.Count == 0 && result.Image.Pixels[offset] == 255 && result.Image.Pixels[offset + 3] == 64,
            "Core finite source-in composition did not use the source/backdrop alpha contract.");
        PackageAssertions.Png(result.Image.ToPng());
    }
}
