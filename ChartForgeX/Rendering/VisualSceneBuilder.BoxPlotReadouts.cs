using System.Collections.Generic;
using System.Text;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal sealed partial class VisualSceneBuilder {
    /// <summary>Shares captured measurement captions between BoxPlot readouts and their native text alternatives.</summary>
    private static string? BoxPlotReadoutDescription(ChartSeries series, IReadOnlyDictionary<string, string> facts) {
        if (series.Kind != ChartSeriesKind.BoxPlot) return null;
        string Read(string name, string fallback = "") => facts.TryGetValue(name, out var text) ? text : fallback;
        if (Read("data-cfx-kind") == "BoxPlotOutlier")
            return series.Name + ": " + Read("data-cfx-category-label") + " " + Read("data-cfx-formatted-value");
        if (Read("data-cfx-whisker-policy") != "Iqr") return null;
        var description = new StringBuilder(series.Name).Append(": ").Append(Read("data-cfx-label"))
            .Append(' ').Append(Read("data-cfx-label-category", "Category")).Append('=').Append(Read("data-cfx-category-label"));
        void Fact(string member, string label, string fallback) => description.Append(' ')
            .Append(Read("data-cfx-label-" + label, fallback)).Append('=').Append(Read("data-cfx-formatted-" + member));
        Fact("minimum", "lower-whisker", "Lower whisker"); Fact("q1", "q1", "Q1"); Fact("median", "median", "Median");
        Fact("q3", "q3", "Q3"); Fact("maximum", "upper-whisker", "Upper whisker");
        Fact("source-minimum", "source-minimum", "Source minimum"); Fact("source-maximum", "source-maximum", "Source maximum");
        foreach (var member in new[] { "inlier-minimum", "inlier-maximum" }) {
            description.Append(' ').Append(Read("data-cfx-label-" + member, member == "inlier-minimum" ? "Inlier minimum" : "Inlier maximum"))
                .Append('=').Append(facts.ContainsKey("data-cfx-" + member)
                    ? Read("data-cfx-formatted-" + member) : Read("data-cfx-label-no-inliers", "None"));
        }
        description.Append(' ').Append(Read("data-cfx-label-sample-count", "Sample count")).Append('=').Append(Read("data-cfx-source-count"))
            .Append(' ').Append(Read("data-cfx-label-outlier-count", "Outlier count")).Append('=').Append(Read("data-cfx-outlier-count"));
        return description.ToString();
    }
}
