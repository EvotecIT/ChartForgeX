using System;
using ChartForgeX.SvgRaster;

namespace ChartForgeX.Typography;

internal sealed partial class ColorFontData {
    private static SvgRasterMatrix ReadTransform(FontTableReader table, int at, int format) {
        var basic = format - (format & 1); var variable = (format & 1) != 0;
        if (basic == 12) {
            table.Require(at, 7); var matrix = Relative24(table, at, at + 4); table.Require(matrix, variable ? 28 : 24);
            return new SvgRasterMatrix(table.Fixed(matrix), table.Fixed(matrix + 4), table.Fixed(matrix + 8), table.Fixed(matrix + 12), table.Fixed(matrix + 16), table.Fixed(matrix + 20));
        }
        var size = basic == 14 || basic == 16 || basic == 28 ? 8 : basic == 18 || basic == 30 ? 12 : basic == 22 || basic == 26 ? 10 : 6;
        table.Require(at, size + (variable ? 4 : 0));
        if (basic == 14) return SvgRasterMatrix.Translate(table.I16(at + 4), table.I16(at + 6));
        SvgRasterMatrix transform;
        if (basic >= 16 && basic <= 22) transform = SvgRasterMatrix.Scale(table.F2Dot14(at + 4), basic == 16 || basic == 18 ? table.F2Dot14(at + 6) : table.F2Dot14(at + 4));
        else if (basic == 24 || basic == 26) transform = SvgRasterMatrix.Rotate(table.F2Dot14(at + 4) * 180);
        else transform = new SvgRasterMatrix(1, Math.Tan(table.F2Dot14(at + 6) * Math.PI), -Math.Tan(table.F2Dot14(at + 4) * Math.PI), 1, 0, 0);
        if (basic == 18 || basic == 22 || basic == 26 || basic == 30) {
            var center = basic == 18 || basic == 30 ? at + 8 : at + 6;
            var x = table.I16(center); var y = table.I16(center + 2);
            transform = SvgRasterMatrix.Translate(x, y).Multiply(transform).Multiply(SvgRasterMatrix.Translate(-x, -y));
        }
        return transform;
    }
}
