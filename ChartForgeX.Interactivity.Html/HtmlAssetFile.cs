using System;
using System.Security.Cryptography;
using System.Text;

namespace ChartForgeX.Interactivity.Html;

/// <summary>
/// One shared interactive runtime file. The file name carries a content hash, so identical runtimes resolve to the
/// same file across every page of a bundle and a changed runtime never collides with a cached copy.
/// </summary>
public sealed class HtmlAssetFile {
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    internal HtmlAssetFile(string name, string extension, string content) {
        Content = content ?? throw new ArgumentNullException(nameof(content));
        var bytes = Utf8NoBom.GetBytes(content);
        using (var sha256 = SHA256.Create()) FileName = name + "." + Hex(sha256.ComputeHash(bytes), 6) + "." + extension;
        using (var sha384 = SHA384.Create()) Integrity = "sha384-" + Convert.ToBase64String(sha384.ComputeHash(bytes));
        ContentType = extension == "css" ? "text/css" : "text/javascript";
    }

    /// <summary>Gets the content-addressed file name, for example <c>cfx-interactive.1a2b3c4d5e6f.js</c>.</summary>
    public string FileName { get; }

    /// <summary>Gets the file content (UTF-8, no byte order mark when written).</summary>
    public string Content { get; }

    /// <summary>Gets the MIME type: <c>text/javascript</c> or <c>text/css</c>.</summary>
    public string ContentType { get; }

    /// <summary>Gets the Subresource Integrity value (<c>sha384-…</c>) of the UTF-8 content.</summary>
    public string Integrity { get; }

    internal static byte[] Encode(string content) => Utf8NoBom.GetBytes(content);

    private static string Hex(byte[] bytes, int count) {
        var builder = new StringBuilder(count * 2);
        for (var i = 0; i < count; i++) builder.Append(bytes[i].ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
        return builder.ToString();
    }
}
