using System;
using System.IO;

namespace ChartForgeX.Raster;

internal static partial class JpegReader {
    private sealed class JpegBitReader {
        private readonly byte[] _data;
        private readonly int _end;
        private int _offset;
        private int _bits;
        private int _bitCount;

        public JpegBitReader(byte[] data, int offset, int end) {
            _data = data;
            _offset = offset;
            _end = Math.Min(end <= 0 ? data.Length : end, data.Length);
        }

        public int ReadBit() {
            if (_bitCount == 0) Fill();
            _bitCount--;
            return (_bits >> _bitCount) & 1;
        }

        public int ReadBits(int count) {
            var value = 0;
            for (var i = 0; i < count; i++) value = (value << 1) | ReadBit();
            return value;
        }

        public void AlignToByte() {
            _bitCount = 0;
        }

        private void Fill() {
            while (_offset < _end && _data[_offset] == 0xFF) {
                _offset++;
                while (_offset < _end && _data[_offset] == 0xFF) _offset++;
                if (_offset >= _end) throw new InvalidDataException("JPEG entropy data is truncated.");
                var marker = _data[_offset++];
                if (marker == 0x00) {
                    _bits = 0xFF;
                    _bitCount = 8;
                    return;
                }

                if (marker >= 0xD0 && marker <= 0xD7) {
                    _bitCount = 0;
                    continue;
                }

                throw new InvalidDataException("Unexpected JPEG marker inside entropy data.");
            }

            if (_offset >= _end) throw new InvalidDataException("JPEG entropy data is truncated.");
            _bits = _data[_offset++];
            _bitCount = 8;
        }
    }
}
