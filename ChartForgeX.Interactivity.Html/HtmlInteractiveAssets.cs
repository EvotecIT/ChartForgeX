using System;
using System.IO;

namespace ChartForgeX.Interactivity.Html;

internal static class HtmlInteractiveAssets {
    private const string StyleResourceName = "ChartForgeX.Interactivity.Html.Assets.interactive.css";
    private const string ChromeStyleResourceName = "ChartForgeX.Interactivity.Html.Assets.interactive.chrome.css";
    private const string ScriptResourceName = "ChartForgeX.Interactivity.Html.Assets.interactive.js";

    private static readonly Lazy<string> StyleResource = new Lazy<string>(() =>
        ReadResource(StyleResourceName) + ReadResource(ChromeStyleResourceName)
            + ReadResource("ChartForgeX.Interactivity.Html.Assets.interactive.annotations.css"));
    private static readonly Lazy<string> ScriptResource = new Lazy<string>(() =>
        ReadResource("ChartForgeX.Interactivity.Html.Assets.adapter-native-text-export.js") + "\n" + ReadResource(ScriptResourceName));
    private static readonly Lazy<string> CoreScriptResource = new Lazy<string>(() => ReadResource("ChartForgeX.Interactivity.Html.Assets.interactive.core.js"));
    private static readonly Lazy<string> StandaloneScriptResource = new Lazy<string>(() =>
        NativeTextScript + "\n" + ReadResource("ChartForgeX.Interactivity.Html.Assets.interactive.standalone.js"));

    internal static string AdapterLifetimeScript => ReadResource("ChartForgeX.Interactivity.Html.Assets.adapter-lifetime.js");
    internal static string NativeTextScript => ReadResource("ChartForgeX.Interactivity.Html.Assets.adapter-native-text-export.js");

    internal static string LocalizationScript => ReadResource("ChartForgeX.Interactivity.Html.Assets.adapter-localization.js");

    internal static string Style => StyleResource.Value;

    internal static string Script => ScriptResource.Value;

    internal static string ScriptFor(HtmlChartRuntime runtime, ChartForgeX.Interactivity.ChartInteractionFeatures features,
        bool adaptive = false) {
        if (runtime == HtmlChartRuntime.Full) return Script;
        if (runtime != HtmlChartRuntime.Standalone) throw new ArgumentOutOfRangeException(nameof(runtime));
        if (adaptive) return Script;
        return UsesCoreReader(features) ? CoreScriptResource.Value : StandaloneScriptResource.Value;
    }

    // Read-only reader features: hover readouts, keyboard traversal and the crosshair guide.
    private static bool UsesCoreReader(ChartForgeX.Interactivity.ChartInteractionFeatures features) =>
        (features & ~(ChartForgeX.Interactivity.ChartInteractionFeatures.Tooltips | ChartForgeX.Interactivity.ChartInteractionFeatures.KeyboardNavigation
            | ChartForgeX.Interactivity.ChartInteractionFeatures.Crosshair)) == 0;

    internal static readonly (HtmlChartFeatureBundles Bundle, string Name)[] FeatureBundles = {
        (HtmlChartFeatureBundles.Motion, "motion"), (HtmlChartFeatureBundles.Data, "data"), (HtmlChartFeatureBundles.Isolate, "isolate"),
        (HtmlChartFeatureBundles.Hierarchy, "hierarchy")
    };

    internal static string FeatureScript(string name) => ReadResource("ChartForgeX.Interactivity.Html.Assets.interactive.feature." + name + ".js");

    /// <summary>Selects the feature bundles a page needs. The core tooltip reader has no feature hooks.</summary>
    internal static HtmlChartFeatureBundles FeatureBundlesFor(HtmlChartRuntime runtime, ChartForgeX.Interactivity.ChartInteractionFeatures features,
        bool adaptive, bool motion, bool contextMenu, bool hierarchy = true) {
        if (runtime == HtmlChartRuntime.Standalone && !adaptive && UsesCoreReader(features))
            return HtmlChartFeatureBundles.None;
        var bundles = HtmlChartFeatureBundles.None;
        if (motion) bundles |= HtmlChartFeatureBundles.Motion;
        if (contextMenu || (features & ChartForgeX.Interactivity.ChartInteractionFeatures.Export) != 0) bundles |= HtmlChartFeatureBundles.Data;
        if ((features & ChartForgeX.Interactivity.ChartInteractionFeatures.LegendToggles) != 0) bundles |= HtmlChartFeatureBundles.Isolate;
        if (hierarchy) bundles |= HtmlChartFeatureBundles.Hierarchy;
        return bundles;
    }

    internal static HtmlChartFeatureBundles FeatureBundlesFor(HtmlChartInteractionOptions options, ChartForgeX.Core.Chart chart) =>
        FeatureBundlesFor(options.Runtime, options.Interaction.Features, options.ResponsiveLayout == HtmlChartResponsiveLayout.Adaptive,
            options.Motion.EffectiveDuration > 0, options.HasContextMenu, HasHierarchy(chart));

    internal static bool HasHierarchy(ChartForgeX.Core.Chart chart) => System.Linq.Enumerable.Any(chart.Series,
        series => series.Kind == ChartForgeX.Core.ChartSeriesKind.Treemap || series.Kind == ChartForgeX.Core.ChartSeriesKind.Sunburst);

    private static string ReadResource(string resourceName) {
        var assembly = typeof(HtmlInteractiveAssets).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) {
            throw new InvalidOperationException("Embedded interactive HTML asset was not found: " + resourceName);
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
