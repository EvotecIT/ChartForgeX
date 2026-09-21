using System.Globalization;
using System.Text;

namespace ChartForgeX.Interactivity.Html;

internal static class HtmlJsonString {
    internal static string Encode(string value) {
        var writer = new StringBuilder(value.Length + 2);
        writer.Append('"');
        foreach (var ch in value) {
            switch (ch) {
                case '\\':
                    writer.Append("\\\\");
                    break;
                case '"':
                    writer.Append("\\\"");
                    break;
                case '\b':
                    writer.Append("\\b");
                    break;
                case '\f':
                    writer.Append("\\f");
                    break;
                case '\n':
                    writer.Append("\\n");
                    break;
                case '\r':
                    writer.Append("\\r");
                    break;
                case '\t':
                    writer.Append("\\t");
                    break;
                case '<':
                    writer.Append("\\u003c");
                    break;
                case '>':
                    writer.Append("\\u003e");
                    break;
                case '&':
                    writer.Append("\\u0026");
                    break;
                default:
                    if (char.IsControl(ch)) writer.Append("\\u").Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
                    else writer.Append(ch);
                    break;
            }
        }

        writer.Append('"');
        return writer.ToString();
    }

}
