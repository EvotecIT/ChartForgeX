using System;
using ChartForgeX.SvgRaster;

internal static partial class Program {
    private static void ColorSourceContracts() {
        var primitives = new[] {
            "<feColorMatrix in='SourceGraphic' type='saturate' values='0'/>",
            "<feComponentTransfer in='SourceGraphic'><feFuncR type='gamma' exponent='2'/></feComponentTransfer>",
            "<feComponentTransfer in='SourceGraphic'><feFuncR type='gamma' amplitude='.25' exponent='-1'/></feComponentTransfer>",
            "<feColorMatrix in='SourceGraphic' type='saturate' values='0' result='gray'/><feComponentTransfer in='gray' result='power'><feFuncR type='gamma' exponent='2'/></feComponentTransfer><feComponentTransfer in='power'><feFuncR type='linear' slope='.5'/></feComponentTransfer>"
        };
        for (var index = 0; index < primitives.Length; index++) {
            var source = "<svg xmlns='http://www.w3.org/2000/svg' width='8' height='8'><defs><filter id='color' filterUnits='userSpaceOnUse' primitiveUnits='userSpaceOnUse'"
                + " color-interpolation-filters='sRGB' x='0' y='0' width='8' height='8'>" + primitives[index] + "</filter></defs>"
                + "<rect width='8' height='8' fill='#4080c0' filter='url(#color)'/></svg>";
            var result = SvgRasterizer.Rasterize(source, strict: true, filters: new SvgRasterFilterOptions());
            var expected = index == 0 ? new byte[] {119,119,119,255} : index == 1 ? new byte[] {16,128,192,255} : index == 2 ? new byte[] {254,128,192,255} : new byte[] {28,119,119,255};
            if (result.Diagnostics.Count != 0) throw new InvalidOperationException("Finite color source lost native membership.");
            for (var channel = 0; channel < 4; channel++) if (result.Image.Pixels[channel] != expected[channel])
                throw new InvalidOperationException("Finite color source changed its independent channel vector.");
        }
    }
}
