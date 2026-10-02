using System;
using System.Collections.Generic;
using System.IO;

namespace ChartForgeX.Raster;

internal sealed partial class TrueTypeFont {
    private const int MaximumCachedFiles = 128;
    private const long MaximumCachedFileBytes = 64L * 1024 * 1024;
    private static readonly object FontCacheLock = new();
    private static readonly Dictionary<string, CachedFontFile> FontCache = new(Path.DirectorySeparatorChar == '\\' ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
    private static readonly LinkedList<string> FontCacheOrder = new();
    private static long FontCacheBytes;
    private static long FontCacheGeneration;

    internal static void ClearFileCache() {
        lock (FontCacheLock) { FontCache.Clear(); FontCacheOrder.Clear(); FontCacheBytes = 0; FontCacheGeneration++; }
    }

    public static TrueTypeFont? TryLoadFromPath(string? path) => TryLoadFromPath(path, null, null);
    public static TrueTypeFont? TryLoadFromPath(string? path, int? collectionIndex) => TryLoadFromPath(path, collectionIndex, null);

    public static TrueTypeFont? TryLoadFromPath(string? path, int? collectionIndex, string? faceName) {
        if (string.IsNullOrWhiteSpace(path)) return null;
        try {
            var file = new FileInfo(Path.GetFullPath(path));
            var cacheKey = file.FullName + "#" + (collectionIndex.HasValue ? collectionIndex.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "auto") + "#" + (faceName ?? string.Empty);
            var exists = file.Exists;
            var length = exists ? file.Length : 0;
            var modified = exists ? file.LastWriteTimeUtc.Ticks : 0;
            long generation;
            lock (FontCacheLock) {
                generation = FontCacheGeneration;
                if (FontCache.TryGetValue(cacheKey, out var cached)) {
                    if (exists && cached.Length == length && cached.Modified == modified) {
                        FontCacheOrder.Remove(cached.Order); FontCacheOrder.AddLast(cached.Order);
                        return cached.Font;
                    }
                    FontCache.Remove(cacheKey); FontCacheOrder.Remove(cached.Order); FontCacheBytes -= cached.Length;
                }
            }
            // Missing and invalid files can become valid later; do not retain negative lookups.
            if (!exists) return null;
            var bytes = File.ReadAllBytes(file.FullName);
            var font = TryLoad(bytes, collectionIndex, faceName);
            if (font == null) return null;
            lock (FontCacheLock) {
                if (generation != FontCacheGeneration || bytes.Length != length || length > MaximumCachedFileBytes) return font;
                if (FontCache.TryGetValue(cacheKey, out var raced)) {
                    if (raced.Length == length && raced.Modified == modified) return raced.Font;
                    FontCache.Remove(cacheKey); FontCacheOrder.Remove(raced.Order); FontCacheBytes -= raced.Length;
                }
                while ((FontCache.Count >= MaximumCachedFiles || FontCacheBytes + length > MaximumCachedFileBytes) && FontCacheOrder.First != null) {
                    var oldest = FontCacheOrder.First.Value;
                    FontCacheBytes -= FontCache[oldest].Length;
                    FontCache.Remove(oldest); FontCacheOrder.RemoveFirst();
                }
                var order = FontCacheOrder.AddLast(cacheKey);
                FontCache[cacheKey] = new CachedFontFile(font, length, modified, order);
                FontCacheBytes += length;
            }
            return font;
        } catch (IOException) {
        } catch (UnauthorizedAccessException) {
        } catch (ArgumentException) {
        } catch (NotSupportedException) {
        } catch (IndexOutOfRangeException) {
        }
        return null;
    }

    private sealed class CachedFontFile {
        internal CachedFontFile(TrueTypeFont font, long length, long modified, LinkedListNode<string> order) { Font = font; Length = length; Modified = modified; Order = order; }
        internal TrueTypeFont Font { get; }
        internal long Length { get; }
        internal long Modified { get; }
        internal LinkedListNode<string> Order { get; }
    }
}
