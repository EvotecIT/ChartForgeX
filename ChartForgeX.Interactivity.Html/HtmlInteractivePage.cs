using ChartForgeX.Html;

namespace ChartForgeX.Interactivity.Html;

internal static class HtmlInteractivePage {
    internal static HtmlMarkupWriter StartDocument(string title, HtmlAssetReferences? assets = null) {
        var writer = new HtmlMarkupWriter();
        writer.Doctype().Line()
            .StartElement("html").Attribute("lang", "en").EndStartElement().Line()
            .StartElement("head").EndStartElement().Line();
        if (assets == null) {
            HtmlChartRenderer.WriteDocumentHead(writer, title, HtmlInteractiveAssets.Style);
        } else {
            HtmlChartRenderer.WriteDocumentHead(writer, title, null);
            HtmlInteractiveAssetFiles.WriteStylesheet(writer, assets, HtmlInteractiveAssetFiles.ChartStyle);
        }

        writer.EndElement().Line()
            .StartElement("body").EndStartElement().Line();
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
