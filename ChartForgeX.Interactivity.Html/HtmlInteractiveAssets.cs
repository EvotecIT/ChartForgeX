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
        return (features & ~ChartForgeX.Interactivity.ChartInteractionFeatures.Tooltips) == 0
            ? CoreScriptResource.Value : StandaloneScriptResource.Value;
    }

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
