using System;
using System.Linq;
using ChartForgeX.Effects;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.SvgRaster;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void OrderedFilterContracts() {
        const string filterSource = "<svg xmlns='http://www.w3.org/2000/svg' width='1' height='1'><defs><filter id='f' filterUnits='userSpaceOnUse' primitiveUnits='userSpaceOnUse' x='0' y='0' width='1' height='1' color-interpolation-filters='sRGB'>"
            + "<feFlood flood-color='rgb(.2490234375,.2490234375,.2490234375)' result='low'/>"
            + "<feComponentTransfer in='low' result='lift'><feFuncR type='linear' slope='8'/><feFuncG type='linear' slope='8'/><feFuncB type='linear' slope='8'/></feComponentTransfer>"
            + "<feComponentTransfer in='lift'><feFuncR type='linear' slope='8'/><feFuncG type='linear' slope='8'/><feFuncB type='linear' slope='8'/></feComponentTransfer>"
            + "</filter></defs><rect width='1' height='1' filter='url(#f)'/></svg>";
        var actual = SvgRasterizer.Rasterize(filterSource, strict: true, filters: new SvgRasterFilterOptions(maximumApplications: 1));
        PackageAssertions.Require(actual.Diagnostics.Count == 0 && actual.Image.Pixels.SequenceEqual(new byte[] { 16, 16, 16, 255 }),
            "Named native filter results lost their retained high-precision channels.");
        var model = Chart.Create().WithSize(160, 120).WithHeader(false).WithLegend(false).WithAxes(false).WithDataLabels(false)
            .AddLine("Source", new[] { new ChartPoint(0, 2), new ChartPoint(1, 5) });
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(160, 120), 12));
        var source = model.Prepare(context);
        var visual = source.WithOutline(new VisualOutline(ChartColor.FromRgb(0, 0, 255), 1, false));
        var svg = visual.ToSvg(new VisualSvgOptions(effectProfile: VisualSvgEffectProfile.NativeFilters));
        var imported = SvgRasterizer.Rasterize(svg, strict: true, filters: new SvgRasterFilterOptions());
        PackageAssertions.Require(imported.Diagnostics.Count == 0 && Enumerable.Range(0, imported.Image.Width * imported.Image.Height).Any(index =>
            imported.Image.Pixels.Skip(index * 4).Take(4).SequenceEqual(new byte[] { 0, 0, 255, 255 })),
            "The ordinary public outline NativeFilters output is not consumable by the native SVG importer.");
    }
}
