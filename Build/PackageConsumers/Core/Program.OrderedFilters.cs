using System;
using System.Linq;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.SvgRaster;
using ChartForgeX.Typography;

internal static partial class Program {
    private static void OrderedFilterContracts() {
        const string source = "<svg xmlns='http://www.w3.org/2000/svg' width='1' height='1'><defs><filter id='f' filterUnits='userSpaceOnUse' primitiveUnits='userSpaceOnUse' x='0' y='0' width='1' height='1' color-interpolation-filters='sRGB'>"
            + "<feFlood flood-color='rgb(.2490234375,.2490234375,.2490234375)' result='low'/>"
            + "<feComponentTransfer in='low' result='lift'><feFuncR type='linear' slope='8'/><feFuncG type='linear' slope='8'/><feFuncB type='linear' slope='8'/></feComponentTransfer>"
            + "<feComponentTransfer in='lift'><feFuncR type='linear' slope='8'/><feFuncG type='linear' slope='8'/><feFuncB type='linear' slope='8'/></feComponentTransfer>"
            + "</filter></defs><rect width='1' height='1' filter='url(#f)'/></svg>";
        var actual = SvgRasterizer.Rasterize(source, strict: true, filters: new SvgRasterFilterOptions(maximumApplications: 1));
        PackageAssertions.Require(actual.Diagnostics.Count == 0 && actual.Image.Pixels.SequenceEqual(new byte[] { 16, 16, 16, 255 }),
            "Named native filter results lost their retained high-precision channels.");
        var builder = new VisualSceneBuilder(new VisualSize(8, 6), FontSpec.SystemSans());
        builder.Rect(new ChartRect(2, 2, 3, 2), ChartColor.FromRgba(255, 0, 0, 128));
        var visual = new PreparedVisual(builder.Build()).WithOutline(new VisualOutline(ChartColor.FromRgb(0, 0, 255), 1, false));
        var svg = visual.ToSvg(new VisualSvgOptions(effectProfile: VisualSvgEffectProfile.NativeFilters));
        var imported = SvgRasterizer.Rasterize(svg, strict: true, filters: new SvgRasterFilterOptions());
        var at = (1 * imported.Image.Width + 1) * 4;
        PackageAssertions.Require(imported.Diagnostics.Count == 0 && imported.Image.Pixels.Skip(at).Take(4).SequenceEqual(new byte[] { 0, 0, 255, 128 }),
            "The ordinary public outline NativeFilters output is not consumable by the native SVG importer.");
    }
}
