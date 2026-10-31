using System;
using ChartForgeX.Accessibility;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Core;

/// <summary>A detached horizontal domain shared by plot strips and their axis strip.</summary>
public sealed class ChartStripScale {
    private readonly ChartAxis _axis;

    /// <summary>Copies an axis with explicit finite increasing bounds, including its tick and formatting policy.</summary>
    /// <remarks>Time bounds use OLE Automation dates, as on <see cref="ChartAxis"/>. Data-dependent ordinal
    /// spacing is not supported: every row must map the same source coordinate to the same horizontal position.</remarks>
    public ChartStripScale(ChartAxis axis) {
        if (axis == null) throw new ArgumentNullException(nameof(axis));
        if (!axis.Minimum.HasValue || !axis.Maximum.HasValue || axis.Maximum <= axis.Minimum)
            throw new ArgumentException("A shared strip scale requires explicit increasing minimum and maximum bounds.", nameof(axis));
        _axis = axis.Clone();
        if (ChartDataAxisProjection.TryCreate(Minimum, Maximum, _axis) == null)
            throw new ArgumentException("The strip bounds must be finite and valid for the axis scale.", nameof(axis));
        if (_axis.TimeSpacing != null)
            throw new NotSupportedException("Shared strips require a continuous scale rather than data-dependent time spacing.");
    }

    /// <summary>Gets the shared lower bound.</summary>
    public double Minimum => _axis.Minimum!.Value;
    /// <summary>Gets the shared upper bound.</summary>
    public double Maximum => _axis.Maximum!.Value;
    /// <summary>Gets a detached copy of the axis policy.</summary>
    public ChartAxis Axis => _axis.Clone();

    /// <summary>Prepares a horizontal axis without marks, legend or outer padding, aligned with plot strips of the same width.</summary>
    /// <remarks>Labels retain their prepared font size. Boundary labels align inward so they stay within the SVG.
    /// For fluid widths, choose a tick count suitable for the smallest intended container.</remarks>
    public PreparedVisual PrepareAxis(VisualRenderContext context) {
        if (context == null) throw new ArgumentNullException(nameof(context));
        var size = context.Layout.Size;
        var builder = new VisualSceneBuilder(size, context.Font);
        var colors = context.Theme.Resolve(context.ThemeMode);
        var style = new TextStyle { Font = context.Font, FontSize = context.Theme.Typography.AxisSize,
            Color = colors.MutedForeground, LineHeight = 1 };
        var bounds = new ChartRect(0, 0, size.Width, size.Height);
        var projection = new ChartAxisProjection(Minimum, Maximum, Minimum, Maximum, bounds, _axis, ChartAxisDimension.X);
        var ticks = ChartTicks.ForAxis(_axis, Minimum, Maximum);
        var format = ChartAxisValueFormatter.Create(_axis, ticks);
        var baseline = Math.Min(size.Height - 3, style.EffectiveFontSize + 7);
        using (builder.PushGroup("strip-axis", ChartDomRoles.StripAxis)) {
            builder.Line(0, .5, size.Width, .5, colors.Border, context.Theme.AxisStrokeWidth,
                ChartDomRoles.StripAxisLine, paint: VisualChartPaint.Stroke(colors.Border, SvgColorRole.Axis));
            foreach (var tick in ticks) {
                if (tick < Minimum || tick > Maximum) continue;
                var x = projection.Project(tick);
                var label = format(tick);
                var half = builder.MeasureText(label, style).Width / 2;
                var alignment = x < half ? TextAlignment.Left : x > size.Width - half ? TextAlignment.Right : TextAlignment.Center;
                builder.Line(x, 0, x, 4, colors.Border, context.Theme.AxisStrokeWidth,
                    ChartDomRoles.StripAxisTick, paint: VisualChartPaint.Stroke(colors.Border, SvgColorRole.Axis));
                builder.Text(label, x, baseline, style, ChartDomRoles.StripAxisLabel, paint: SvgPaint.Of(colors.MutedForeground, SvgColorRole.Text), alignment: alignment);
            }
        }
        builder.DataProjection = ChartDataProjection.TryCreate(bounds, Minimum, Maximum, _axis);
        return new PreparedVisual(builder.Build(), new VisualAccessibility { Name = "Axis" }, xProjection: projection);
    }
}
