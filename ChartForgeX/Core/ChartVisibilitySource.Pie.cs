using System;

namespace ChartForgeX.Core;

internal sealed partial class ChartVisibilitySource {
    internal PiePolicy? Pie => _pie;
    internal sealed class PiePolicy {
        internal PiePolicy(ChartOptions source) {
            Maximum = source.MaximumPieSlices; Hole = source.DonutInnerRadiusRatio; Center = source.ShowDonutCenterLabel;
            CenterValue = source.DonutCenterValue; CenterLabel = source.DonutCenterLabel; Outside = source.PieOutsideLabelDistanceRatio;
            Content = source.PieSliceLabelContent; Inside = source.PieInsideLabelRadiusRatio; Opacity = source.PieSliceOpacity;
            SliceOutline = source.PieSliceOutline; OuterOutline = source.PieOuterOutline; PreserveOrder = source.PiePreserveSourceOrder;
            LegendContent = source.PieLegendContent; Bands = source.RadialBandAllocation;
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
            target.MaximumPieSlices = Maximum; target.DonutInnerRadiusRatio = Hole; target.ShowDonutCenterLabel = Center;
            target.DonutCenterValue = CenterValue; target.DonutCenterLabel = CenterLabel; target.PieOutsideLabelDistanceRatio = Outside;
            target.PieSliceLabelContent = Content; target.PieInsideLabelRadiusRatio = Inside; target.PieSliceOpacity = Opacity;
            target.PieSliceOutline = SliceOutline; target.PieOuterOutline = OuterOutline; target.PiePreserveSourceOrder = PreserveOrder;
            target.PieLegendContent = LegendContent; target.RadialBandAllocation = Bands;
        }
    }
}
