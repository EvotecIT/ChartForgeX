using System;
using System.Collections.Generic;
using System.Text;

namespace ChartForgeX.VisualArtifacts;

public static partial class VisualArtifactInterchangeMapping {
    internal static string BoundedGeneratedText(string value, string suffix) {
        if (value == null) throw new ArgumentNullException(nameof(value));
        if (suffix == null) throw new ArgumentNullException(nameof(suffix));
        if (suffix.Length > VisualArtifactInterchangeValidation.MaximumTextCharacters) {
            throw new ArgumentException("Generated text suffixes must fit the interchange text limit.", nameof(suffix));
        }
        if (value.Length <= VisualArtifactInterchangeValidation.MaximumTextCharacters - suffix.Length) return value + suffix;
        int prefixLength = VisualArtifactInterchangeValidation.MaximumTextCharacters - suffix.Length;
        if (prefixLength > 0 && prefixLength < value.Length && char.IsHighSurrogate(value[prefixLength - 1]) && char.IsLowSurrogate(value[prefixLength])) {
            prefixLength--;
        }
        return value.Substring(0, prefixLength) + suffix;
    }

    private static void AddProjectedSourceIds(VisualArtifactInterchangeEnvelope envelope, IEnumerable<ProjectedSourceId> candidates) {
        string baseJson = VisualArtifactInterchangeJson.Serialize(envelope);
        int remainingCharacters = VisualArtifactInterchangeEnvelope.MaximumJsonCharacters - baseJson.Length;
        int remainingUtf8Bytes = VisualArtifactInterchangeEnvelope.MaximumJsonUtf8Bytes - Encoding.UTF8.GetByteCount(baseJson);
        long remainingValues = VisualArtifactInterchangeValueBudget.Remaining(envelope);
        foreach (var candidate in candidates) {
            IDictionary<string, string> extensions = candidate.Extensions;
            string sourceId = candidate.SourceId;
            if (sourceId.Length > VisualArtifactInterchangeValidation.MaximumTextCharacters ||
                extensions.Count >= VisualArtifactInterchangeValidation.MaximumExtensionEntries) {
                continue;
            }

            JsonStringSize sourceSize = JsonStringSize.Measure(sourceId);
            JsonStringSize addedSize;
            if (extensions.TryGetValue(ProjectedSourceIdExtension, out string? existingValue)) {
                string relocatedKey = AllocateMetadataKey(extensions, ProjectedSourceIdExtension);
                JsonStringSize existingSize = JsonStringSize.Measure(existingValue);
                JsonStringSize relocatedKeySize = JsonStringSize.Measure(relocatedKey);
                addedSize = new JsonStringSize(
                    sourceSize.Characters - existingSize.Characters + 1 + relocatedKeySize.Characters + 1 + existingSize.Characters,
                    sourceSize.Utf8Bytes - existingSize.Utf8Bytes + 1 + relocatedKeySize.Utf8Bytes + 1 + existingSize.Utf8Bytes);
            } else {
                JsonStringSize keySize = JsonStringSize.Measure(ProjectedSourceIdExtension);
                int separator = extensions.Count == 0 ? 0 : 1;
                addedSize = new JsonStringSize(keySize.Characters + 1 + sourceSize.Characters + separator,
                    keySize.Utf8Bytes + 1 + sourceSize.Utf8Bytes + separator);
            }
            if (addedSize.Characters > remainingCharacters || addedSize.Utf8Bytes > remainingUtf8Bytes || remainingValues < 1) continue;
            if (!TrySetBoundedExtension(extensions, ProjectedSourceIdExtension, sourceId)) continue;
            remainingCharacters -= addedSize.Characters;
            remainingUtf8Bytes -= addedSize.Utf8Bytes;
            remainingValues--;
        }
    }

    internal static bool TrySetBoundedExtension(IDictionary<string, string> extensions, string key, string value) {
        if (string.IsNullOrWhiteSpace(key) || key.Length > VisualArtifactInterchangeValidation.MaximumIdCharacters ||
            value.Length > VisualArtifactInterchangeValidation.MaximumTextCharacters) return false;
        if (extensions.TryGetValue(key, out string? existingValue)) {
            if (extensions.Count >= VisualArtifactInterchangeValidation.MaximumExtensionEntries) return false;
            extensions[AllocateMetadataKey(extensions, key)] = existingValue;
            extensions[key] = value;
            return true;
        }
        if (extensions.Count >= VisualArtifactInterchangeValidation.MaximumExtensionEntries) return false;
        extensions[key] = value;
        return true;
    }

    private readonly struct ProjectedSourceId {
        public ProjectedSourceId(IDictionary<string, string> extensions, string sourceId) {
            Extensions = extensions;
            SourceId = sourceId;
        }

        public IDictionary<string, string> Extensions { get; }
        public string SourceId { get; }
    }

    private readonly struct JsonStringSize {
        public JsonStringSize(int characters, int utf8Bytes) {
            Characters = characters;
            Utf8Bytes = utf8Bytes;
        }

        public int Characters { get; }
        public int Utf8Bytes { get; }

        public static JsonStringSize Measure(string value) {
            int characters = 2;
            int utf8Bytes = 2;
            for (var index = 0; index < value.Length; index++) {
                char character = value[index];
                if (char.IsHighSurrogate(character)) {
                    if (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1])) {
                        throw new ArgumentException("Interchange JSON strings cannot contain unpaired UTF-16 surrogate characters.", nameof(value));
                    }
                    characters += 2;
                    utf8Bytes += 4;
                    index++;
                } else if (char.IsLowSurrogate(character)) {
                    throw new ArgumentException("Interchange JSON strings cannot contain unpaired UTF-16 surrogate characters.", nameof(value));
                } else if (character == '"' || character == '\\' || character == '\b' || character == '\f' ||
                           character == '\n' || character == '\r' || character == '\t') {
                    characters += 2;
                    utf8Bytes += 2;
                } else if (character < 0x20) {
                    characters += 6;
                    utf8Bytes += 6;
                } else {
                    characters++;
                    utf8Bytes += character <= 0x7f ? 1 : character <= 0x7ff ? 2 : 3;
                }
            }
            return new JsonStringSize(characters, utf8Bytes);
        }
    }
}
