using System;
using System.Collections;
using System.Collections.Generic;

namespace ChartForgeX.Rendering;

/// <summary>
/// Immutable scene metadata in ordinal key order, so exporters write it deterministically. Two arrays
/// hold it instead of a tree of nodes, which keeps a series with one group per point small.
/// </summary>
internal sealed class VisualSceneMetadata : IReadOnlyDictionary<string, string> {
    internal static readonly VisualSceneMetadata Empty = new(Array.Empty<string>(), Array.Empty<string>());
    private readonly string[] _keys;
    private readonly string[] _values;

    private VisualSceneMetadata(string[] keys, string[] values) {
        _keys = keys;
        _values = values;
    }

    internal static VisualSceneMetadata Create(IReadOnlyDictionary<string, string>? metadata) {
        if (metadata == null || metadata.Count == 0) return Empty;
        if (metadata is VisualSceneMetadata existing) return existing;
        var keys = new string[metadata.Count];
        var values = new string[keys.Length];
        var index = 0;
        foreach (var item in metadata) {
            keys[index] = item.Key ?? throw new ArgumentNullException(nameof(metadata), "Scene metadata keys cannot be null.");
            values[index++] = item.Value;
        }

        Array.Sort(keys, values, StringComparer.Ordinal);
        for (var i = 1; i < keys.Length; i++) {
            if (string.Equals(keys[i - 1], keys[i], StringComparison.Ordinal)) throw new ArgumentException("Scene metadata repeats the key '" + keys[i] + "'.", nameof(metadata));
        }

        return new VisualSceneMetadata(keys, values);
    }

    public int Count => _keys.Length;

    public string this[string key] => TryGetValue(key, out var value) ? value : throw new KeyNotFoundException("Scene metadata has no key '" + key + "'.");

    public IEnumerable<string> Keys => _keys;

    public IEnumerable<string> Values => _values;

    public bool ContainsKey(string key) => IndexOf(key) >= 0;

    public bool TryGetValue(string key, out string value) {
        var index = IndexOf(key);
        value = index >= 0 ? _values[index] : null!;
        return index >= 0;
    }

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() {
        for (var index = 0; index < _keys.Length; index++) yield return new KeyValuePair<string, string>(_keys[index], _values[index]);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private int IndexOf(string key) {
        if (key == null) throw new ArgumentNullException(nameof(key));
        return Array.BinarySearch(_keys, key, StringComparer.Ordinal);
    }
}
