using System.Text;

namespace ChartForgeX.Interactivity.Html;

internal static class HtmlInteractivePage {
    internal static void AppendDocumentStart(StringBuilder sb, string title) {
        sb.AppendLine("<!doctype html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"utf-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        sb.AppendLine("<title>" + HtmlInteractiveChartRenderer.EscapeHtml(title) + "</title>");
        sb.AppendLine("<style>");
        sb.AppendLine(HtmlInteractiveAssets.Style);
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
    }

    internal static void AppendDocumentEnd(StringBuilder sb, string? scriptNonce) {
        HtmlInteractiveMarkup.AppendStartTag(sb, "script", HtmlInteractiveMarkup.OptionalAttr("nonce", scriptNonce));
        sb.AppendLine();
        sb.AppendLine(HtmlInteractiveAssets.Script);
        sb.AppendLine("</script>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
    }
}
