using ChartForgeX.Html;

namespace ChartForgeX.Interactivity.Html;

internal static class HtmlInteractivePage {
    internal static HtmlMarkupWriter StartDocument(string title) {
        var writer = new HtmlMarkupWriter();
        writer.Doctype().Line()
            .StartElement("html").Attribute("lang", "en").EndStartElement().Line()
            .StartElement("head").EndStartElement().Line();
        HtmlChartRenderer.WriteDocumentHead(writer, title, HtmlInteractiveAssets.Style);
        writer.EndElement().Line()
            .StartElement("body").EndStartElement().Line();
        return writer;
    }

    internal static void EndDocument(HtmlMarkupWriter writer, string? scriptNonce) {
        writer.StartElement("script")
            .Attribute("nonce", scriptNonce)
            .EndStartElement()
            .RawTrusted(HtmlInteractiveAssets.Script)
            .EndElement().Line()
            .EndElement().Line()
            .EndElement();
    }
}
