using ChartForgeX.Core;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeValidation {
    private static void ValidateWaterfallWords(ChartLabels labels) {
        Text(labels.Now, "waterfall Now");
        Text(labels.Ongoing, "waterfall Ongoing");
        Text(labels.SoFar, "waterfall SoFar");
        Text(labels.HourOfDay, "waterfall HourOfDay");
        Text(labels.Level, "waterfall Level");
        Text(labels.Less, "waterfall Less");
        Text(labels.More, "waterfall More");
        Text(labels.NoData, "waterfall NoData");
        Text(labels.Color, "waterfall Color");
        Text(labels.Size, "waterfall Size");
        Text(labels.Target, "waterfall Target");
        Text(labels.AuthoredValue, "waterfall AuthoredValue");
        Text(labels.Remainder, "waterfall Remainder");
        Text(labels.AllValues, "waterfall AllValues");
        Text(labels.Value, "waterfall Value");
        Text(labels.Change, "waterfall Change");
        Text(labels.Category, "waterfall Category");
        Text(labels.Start, "waterfall Start");
        Text(labels.End, "waterfall End");
        Text(labels.Minimum, "waterfall Minimum");
        Text(labels.Q1, "waterfall Q1");
        Text(labels.Median, "waterfall Median");
        Text(labels.Q3, "waterfall Q3");
        Text(labels.Maximum, "waterfall Maximum");
        Text(labels.OpeningBalance, "waterfall OpeningBalance");
        Text(labels.Subtotal, "waterfall Subtotal");
        Text(labels.Total, "waterfall Total");
        Text(labels.UntitledChart, "waterfall UntitledChart");
        Text(labels.LowerWhisker, "waterfall LowerWhisker");
        Text(labels.UpperWhisker, "waterfall UpperWhisker");
        Text(labels.SampleCount, "waterfall SampleCount");
        Text(labels.SourceMinimum, "waterfall SourceMinimum");
        Text(labels.SourceMaximum, "waterfall SourceMaximum");
        Text(labels.InlierMinimum, "waterfall InlierMinimum");
        Text(labels.InlierMaximum, "waterfall InlierMaximum");
        Text(labels.OutlierCount, "waterfall OutlierCount");
        Text(labels.QuartileEstimator, "waterfall QuartileEstimator");
        Text(labels.WhiskerPolicy, "waterfall WhiskerPolicy");
        Text(labels.IqrMultiplier, "waterfall IqrMultiplier");
        Text(labels.NoInliers, "waterfall NoInliers");
        Text(labels.Outlier, "waterfall Outlier");
    }
}
