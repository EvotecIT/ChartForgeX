using ChartForgeX.Rendering;
using System.Collections;
using System.Reflection;

public static partial class XZoomBenchmarkCases {
    // Test-only reflective access is intentional: no production friend or public statistics API is introduced.
    private static Dictionary<string, long> NativeMetrics(PreparedChartXZoom bank, int activeLevel) {
        var output = new Dictionary<string, long>(StringComparer.Ordinal);
        var levels = ((IEnumerable)Property(bank, "Bank")).Cast<object>().ToArray();
        output["BankLevels"] = levels.Length;
        var names = new[] { "Nodes", "PathCommands", "Glyphs", "Ticks", "VisibilityIntervals", "NativeSvgUtf8Bytes" };
        foreach (var name in names) output["Bank" + name] = 0;
        var captions = 0L; var hidden = 0L;
        for (var index = 0; index < levels.Length; index++) {
            var statistics = Property(levels[index], "Statistics");
            foreach (var name in names) {
                var value = Convert.ToInt64(Property(statistics, name), System.Globalization.CultureInfo.InvariantCulture);
                output["Bank" + name] += value;
                if (index == activeLevel) output["Active" + name] = value;
            }
            foreach (var layer in ((IEnumerable)Property(levels[index], "Layers")).Cast<object>()) {
                var caption = OptionalProperty(layer, "Caption");
                if (caption == null) continue;
                captions++;
                hidden += ((IEnumerable)Property(caption, "Hidden")).Cast<object>().LongCount();
            }
        }
        output["BankCaptions"] = captions; output["BankHiddenClearanceIntervals"] = hidden;
        return output;
    }

    private static object Property(object value, string name) => OptionalProperty(value, name)
        ?? throw new InvalidOperationException("Expected native X zoom measurement member is absent: " + value.GetType().FullName + "." + name);
    private static object? OptionalProperty(object value, string name) {
        var member = value.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Expected native X zoom measurement property is absent: " + name);
        return member.GetValue(value);
    }
}
