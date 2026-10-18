using ChartForgeX.Html;
using ChartForgeX.Core;

namespace ChartForgeX.Interactivity.Html;

internal static class HtmlInteractivePage {
    internal static string ChartSurfaceStyle(Chart chart) => "background:" + chart.Options.ResolvedTheme.Background.ToCss()
        + ";color:" + chart.Options.ResolvedTheme.Text.ToCss() + ";font-family:" + HtmlChartRenderer.CssFontFamily(chart.Options.ResolvedTheme.FontFamily);

    internal static HtmlMarkupWriter StartDocument(string title, HtmlAssetReferences? assets = null, string? bodyStyle = null,
        string language = "en", string direction = "auto") {
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

        writer.EndElement().Line()
            .StartElement("body").Attribute("style", bodyStyle).EndStartElement().Line();
        return writer;
    }

    internal static void EndDocument(HtmlMarkupWriter writer, string? scriptNonce, HtmlAssetReferences? assets = null) {
        if (assets == null) {
            writer.StartElement("script")
                .Attribute("nonce", scriptNonce)
                .EndStartElement()
                .RawTrusted(HtmlInteractiveAssets.Script)
                .EndElement();
        } else {
            HtmlInteractiveAssetFiles.WriteScript(writer, assets, HtmlInteractiveAssetFiles.ChartScript, scriptNonce);
        }

        writer.Line()
            .EndElement().Line()
            .EndElement();
    }
}
