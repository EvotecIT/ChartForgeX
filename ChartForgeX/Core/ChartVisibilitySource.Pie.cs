using System;

namespace ChartForgeX.Core;

internal sealed partial class ChartVisibilitySource {
    internal PiePolicy? Pie => _pie;
    internal sealed class PiePolicy {
        internal PiePolicy(ChartOptions source) {
            Maximum = source.Pie.MaxSlices; Hole = source.Pie.InnerRadiusRatio; Center = source.Pie.ShowCenterLabel;
            CenterValue = source.Pie.CenterValue; CenterLabel = source.Pie.CenterLabel; Outside = source.Pie.OutsideLabelDistanceRatio;
            Content = source.Pie.SliceLabelContent; Inside = source.Pie.InsideLabelRadiusRatio; Opacity = source.Pie.SliceOpacity;
            SliceOutline = source.Pie.SliceOutline; OuterOutline = source.Pie.OuterOutline; PreserveOrder = source.Pie.PreserveSourceOrder;
            LegendContent = source.Pie.LegendContent; Bands = source.RadialBandAllocation;
        }
        internal int Maximum { get; }
        internal double Hole { get; }
        internal bool Center { get; }
        internal string? CenterValue { get; }
        internal string? CenterLabel { get; }
        internal double Outside { get; }
        internal ChartPieSliceLabelContent Content { get; }
        internal double? Inside { get; }
        internal double? Opacity { get; }
        internal ChartStrokeStyle? SliceOutline { get; }
        internal ChartStrokeStyle? OuterOutline { get; }
        internal bool PreserveOrder { get; }
        internal ChartPieLegendContent? LegendContent { get; }
        internal ChartRadialBandAllocation? Bands { get; }
        internal void Apply(ChartOptions target) {
            target.Pie.MaxSlices = Maximum; target.Pie.InnerRadiusRatio = Hole; target.Pie.ShowCenterLabel = Center;
            target.Pie.CenterValue = CenterValue; target.Pie.CenterLabel = CenterLabel; target.Pie.OutsideLabelDistanceRatio = Outside;
            target.Pie.SliceLabelContent = Content; target.Pie.InsideLabelRadiusRatio = Inside; target.Pie.SliceOpacity = Opacity;
            target.Pie.SliceOutline = SliceOutline; target.Pie.OuterOutline = OuterOutline; target.Pie.PreserveSourceOrder = PreserveOrder;
            target.Pie.LegendContent = LegendContent; target.RadialBandAllocation = Bands;
        }
    }
}
