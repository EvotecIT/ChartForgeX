using System;

namespace ChartForgeX.VisualArtifacts;

internal sealed partial class VisualArtifactInterchangeJsonWriter {
    /// <summary>Uses the same bounded writer for one exact binary string without encoding during value counting.</summary>
    internal void Base64(byte[] bytes) {
        if (bytes == null) throw new ArgumentNullException(nameof(bytes));
        BeforeValue();
        var encoded = VisualArtifactInterchangeValidation.Base64Length(bytes.LongLength);
        if (encoded + 2 > VisualArtifactInterchangeValidation.MaximumJsonCharacters - (long)_charactersWritten)
            throw new InvalidOperationException("The interchange JSON exceeds the maximum supported size before mask encoding.");
        if (encoded + 2 > VisualArtifactInterchangeEnvelope.MaximumJsonUtf8Bytes - (long)_utf8BytesWritten)
            throw new ArgumentOutOfRangeException("json", "Captured samples exceed the existing UTF-8 byte budget before mask encoding.");
        Append('"');
        if (_writeText) Append(Convert.ToBase64String(bytes));
        else {
            _charactersWritten = checked(_charactersWritten + (int)encoded);
            _utf8BytesWritten = checked(_utf8BytesWritten + (int)encoded);
        }
        Append('"');
        EnsureWithinLimit();
    }
}
