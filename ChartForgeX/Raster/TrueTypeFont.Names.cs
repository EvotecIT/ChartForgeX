using System;
using System.Collections.Generic;
using System.Text;

namespace ChartForgeX.Raster;

internal sealed partial class TrueTypeFont {
    internal string? DisplayName => FirstName(4) ?? FirstName(1) ?? FirstName(6) ?? FirstName(2);

    private bool MatchesName(string? faceName) {
        if (string.IsNullOrWhiteSpace(faceName)) return true;
        var requested = faceName!;
        foreach (var name in ReadNames()) {
            if (name.Equals(requested, StringComparison.OrdinalIgnoreCase)) return true;
            if (name.IndexOf(requested, StringComparison.OrdinalIgnoreCase) >= 0) return true;
        }

        return false;
    }

    private IEnumerable<string> ReadNames() {
        if (_name < 0 || _name + 6 > _data.Length) yield break;
        var count = ReadUInt16(_data, _name + 2);
        var stringOffset = _name + ReadUInt16(_data, _name + 4);
        for (var i = 0; i < count; i++) {
            var record = _name + 6 + i * 12;
            if (record + 12 > _data.Length) yield break;
            var nameId = ReadUInt16(_data, record + 6);
            if (nameId != 1 && nameId != 2 && nameId != 4 && nameId != 6) continue;
            var platform = ReadUInt16(_data, record);
            var length = ReadUInt16(_data, record + 8);
            var offset = stringOffset + ReadUInt16(_data, record + 10);
            if (offset < 0 || length == 0 || offset + length > _data.Length) continue;
            var value = DecodeName(platform, offset, length).Trim();
            if (value.Length > 0) yield return value;
        }
    }

    private string DecodeName(ushort platform, int offset, int length) {
        if (platform == 0 || platform == 3) return Encoding.BigEndianUnicode.GetString(_data, offset, length);
        return Encoding.ASCII.GetString(_data, offset, length);
    }

    private string? FirstName(ushort requestedNameId) {
        if (_name < 0 || _name + 6 > _data.Length) return null;
        var count = ReadUInt16(_data, _name + 2);
        var stringOffset = _name + ReadUInt16(_data, _name + 4);
        for (var i = 0; i < count; i++) {
            var record = _name + 6 + i * 12;
            if (record + 12 > _data.Length) return null;
            if (ReadUInt16(_data, record + 6) != requestedNameId) continue;
            var platform = ReadUInt16(_data, record);
            var length = ReadUInt16(_data, record + 8);
            var offset = stringOffset + ReadUInt16(_data, record + 10);
            if (offset < 0 || length == 0 || offset + length > _data.Length) continue;
            var value = DecodeName(platform, offset, length).Trim();
            if (value.Length > 0) return value;
        }

        return null;
    }
}
