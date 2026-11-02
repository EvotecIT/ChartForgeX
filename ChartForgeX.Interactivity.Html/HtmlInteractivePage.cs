using ChartForgeX.Html;
using ChartForgeX.Core;

namespace ChartForgeX.Interactivity.Html;

internal static class HtmlInteractivePage {
    internal static string ChartSurfaceStyle(Chart chart) => "background:" + chart.Options.ResolvedTheme.Background.ToCss()
        + ";color:" + chart.Options.ResolvedTheme.Text.ToCss() + ";font-family:" + HtmlChartRenderer.CssFontFamily(chart.Options.ResolvedTheme.FontFamily);

    internal static HtmlMarkupWriter StartDocument(string title, HtmlAssetReferences? assets = null, string? bodyStyle = null,
        string language = "en", string? direction = null, string? productFontStyle = null) {
        var writer = new HtmlMarkupWriter();
        writer.Doctype().Line()
            .StartElement("html").Attribute("lang", language).Attribute("dir", direction).EndStartElement().Line()
            .StartElement("head").EndStartElement().Line();
        var pageStyle = "body{" + HtmlSurfacePolish.BrowserTextPolishCss + "}"
            + HtmlSurfacePolish.PrintBodyCss("0", "body{background:transparent!important}.cfx-shell{min-height:auto;padding:0;background:transparent!important}");
        HtmlChartRenderer.WriteDocumentHead(writer, title, assets == null ? HtmlInteractiveAssets.Style : null);
        if (assets != null) {
            HtmlInteractiveAssetFiles.WriteStylesheet(writer, assets, HtmlInteractiveAssetFiles.ChartStyle);
        }
        writer.StartElement("style").EndStartElement().RawTrusted(pageStyle).EndElement().Line();
        if (productFontStyle != null)
            writer.StartElement("style").Attribute("data-cfx-product-fonts", "true").EndStartElement()
                .RawTrusted(productFontStyle).EndElement().Line();

        writer.EndElement().Line()
            .StartElement("body").Attribute("style", bodyStyle).EndStartElement().Line();
        return writer;
    }

    internal static void EndDocument(HtmlMarkupWriter writer, string? scriptNonce, HtmlAssetReferences? assets = null,
        HtmlChartRuntime runtime = HtmlChartRuntime.Full,
        ChartForgeX.Interactivity.ChartInteractionFeatures features = ChartForgeX.Interactivity.ChartInteractionFeatures.ReportReview,
        bool adaptive = false, HtmlChartFeatureBundles bundles = HtmlChartFeatureBundles.None) {
        if (assets == null) {
            WriteInlineFeatures(writer, scriptNonce, bundles);
            writer.StartElement("script")
                .Attribute("nonce", scriptNonce)
                .EndStartElement()
                .RawTrusted(HtmlInteractiveAssets.ScriptFor(runtime, features, adaptive))
                .EndElement();
        } else {
            HtmlInteractiveAssetFiles.WriteScript(writer, assets, HtmlInteractiveAssetFiles.ChartScriptFor(runtime, features, adaptive), scriptNonce, bundles);
        }

        writer.Line()
            .EndElement().Line()
            .EndElement();
    }

    /// <summary>Embeds declared feature bundles ahead of the runtime so they are registered before charts mount.</summary>
    internal static void WriteInlineFeatures(HtmlMarkupWriter writer, string? scriptNonce, HtmlChartFeatureBundles bundles) {
        foreach (var bundle in HtmlInteractiveAssets.FeatureBundles) {
            if ((bundles & bundle.Bundle) == 0) continue;
            writer.StartElement("script").Attribute("nonce", scriptNonce).Attribute("data-cfx-feature", bundle.Name).EndStartElement()
                .RawTrusted(HtmlInteractiveAssets.FeatureScript(bundle.Name)).EndElement().Line();
        }
    }
}
