using System;
using System.Text;
using ChartForgeX.Primitives;

namespace ChartForgeX.Svg;

internal sealed class SvgPathDataBuilder {
    private readonly StringBuilder _builder;

    public SvgPathDataBuilder() : this(128) { }

    public SvgPathDataBuilder(int capacity) {
        _builder = new StringBuilder(Math.Max(16, capacity));
    }

    public bool IsEmpty => _builder.Length == 0;

    public SvgPathDataBuilder MoveTo(double x, double y) {
        AppendCommand('M');
        AppendPoint(x, y);
        return this;
    }

    public SvgPathDataBuilder MoveTo(ChartPoint point) => MoveTo(point.X, point.Y);

    public SvgPathDataBuilder LineTo(double x, double y) {
        AppendCommand('L');
        AppendPoint(x, y);
        return this;
    }

    public SvgPathDataBuilder LineTo(ChartPoint point) => LineTo(point.X, point.Y);

    public SvgPathDataBuilder QuadraticTo(double controlX, double controlY, double x, double y) {
        AppendCommand('Q');
        AppendPoint(controlX, controlY);
        _builder.Append(' ');
        AppendPoint(x, y);
        return this;
    }

    public SvgPathDataBuilder CubicTo(double control1X, double control1Y, double control2X, double control2Y, double x, double y) {
        AppendCommand('C');
        AppendPoint(control1X, control1Y);
        _builder.Append(' ');
        AppendPoint(control2X, control2Y);
        _builder.Append(' ');
        AppendPoint(x, y);
        return this;
    }

    public SvgPathDataBuilder ArcTo(double radiusX, double radiusY, double xAxisRotation, bool largeArc, bool sweep, double x, double y) {
        AppendCommand('A');
        AppendNumber(radiusX);
        _builder.Append(' ');
        AppendNumber(radiusY);
        _builder.Append(' ');
        AppendNumber(xAxisRotation);
        _builder.Append(' ').Append(largeArc ? '1' : '0').Append(' ').Append(sweep ? '1' : '0').Append(' ');
        AppendPoint(x, y);
        return this;
    }

    public SvgPathDataBuilder Close() {
        AppendCommand('Z');
        return this;
    }

    public string Build() => _builder.ToString();

    public override string ToString() => Build();

    private void AppendCommand(char command) {
        if (_builder.Length > 0) _builder.Append(' ');
        _builder.Append(command);
        if (command != 'Z') _builder.Append(' ');
    }

    private void AppendPoint(double x, double y) {
        AppendNumber(x);
        _builder.Append(' ');
        AppendNumber(y);
    }

    private void AppendNumber(double value) =>
        _builder.Append(SvgMarkupWriter.FormatNumber(value));
}
