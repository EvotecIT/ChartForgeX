using ChartForgeX.Core;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

public static partial class LegacySceneBenchmarkCases {
    /// <summary>Records input values, breaks, series kinds and bar mode independently of renderer output.</summary>
    public static string SourceDigest(string fixture) {
        var chart = (Chart)Create(fixture);
        var source = new StringBuilder(chart.Options.BarMode.ToString());
        foreach (var series in chart.Series) {
            source.Append('|').Append(series.Kind).Append(':').Append(series.Name);
            foreach (var point in series.Points) source.Append('|').Append(point.X.ToString("G17", CultureInfo.InvariantCulture))
                .Append(',').Append(point.Y.ToString("G17", CultureInfo.InvariantCulture)).Append(',').Append(point.BreakBefore);
        }
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source.ToString())));
    }

    // Every expected source point must survive in actual semantic SVG metadata. This catches
    // empty/partial exports even when frame text and dimensions remain valid.
    private static void ValidateMarks(XElement root, Chart chart) {
        if (chart.Series[0].Kind == ChartSeriesKind.Donut) {
            var slices = root.Descendants().Where(element => element.Attribute("data-cfx-source-points") != null).ToArray();
            var covered = new HashSet<int>();
            foreach (var slice in slices) {
                var indices = ((string)slice.Attribute("data-cfx-source-points")!).Split(',').Select(int.Parse).ToArray();
                foreach (var index in indices)
                    if (!covered.Add(index) || index < 0 || index >= chart.Series[0].Points.Count)
                        throw new InvalidOperationException("A radial source point was duplicated or changed.");
                var expected = indices.Sum(index => chart.Series[0].Points[index].Y);
                if (!Near(Parse(slice, "data-cfx-value"), expected)) throw new InvalidOperationException("Radial values changed.");
                var visible = slice.Name.LocalName == "path" ? slice : slice.Descendants().FirstOrDefault(element => element.Name.LocalName == "path");
                if (string.IsNullOrWhiteSpace((string?)visible?.Attribute("d"))) throw new InvalidOperationException("A radial mark is empty.");
            }
            if (covered.Count != chart.Series[0].Points.Count) throw new InvalidOperationException("Radial source points were lost.");
            return;
        }
        var marks = root.Descendants().Where(element => element.Attribute("data-cfx-x") != null && element.Attribute("data-cfx-y") != null)
            .GroupBy(element => (Series: (int)element.Attribute("data-cfx-series")!, Point: (int)element.Attribute("data-cfx-point")!))
            .ToDictionary(group => group.Key, group => group.ToArray());
        if (marks.Count != chart.Series.Sum(series => series.Points.Count)) throw new InvalidOperationException("Cartesian source-point count changed.");
        for (var seriesIndex = 0; seriesIndex < chart.Series.Count; seriesIndex++) {
            var series = chart.Series[seriesIndex];
            for (var pointIndex = 0; pointIndex < series.Points.Count; pointIndex++) {
                if (!marks.TryGetValue((seriesIndex, pointIndex), out var elements)) throw new InvalidOperationException("A Cartesian source point was lost.");
                var point = series.Points[pointIndex];
                if (elements.Any(element => !Near(Parse(element, "data-cfx-x"), point.X) || !Near(Parse(element, "data-cfx-y"), point.Y)))
                    throw new InvalidOperationException("Cartesian source values changed.");
                if (!elements.SelectMany(element => element.DescendantsAndSelf()).Any(element => element.Name.LocalName is "circle" or "ellipse" or "rect" or "path"))
                    throw new InvalidOperationException("A Cartesian source point has no drawing command.");
            }
        }
    }

    private static double Parse(XElement element, string attribute) => double.Parse((string)element.Attribute(attribute)!, CultureInfo.InvariantCulture);
    private static bool Near(double actual, double expected) => Math.Abs(actual - expected) <= 1e-9 * Math.Max(1, Math.Abs(expected));
}
