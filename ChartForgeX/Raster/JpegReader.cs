using System;
using System.Collections.Generic;
using System.IO;

namespace ChartForgeX.Raster;

internal static partial class JpegReader {
    private static readonly int[] ZigZag = {
        0, 1, 8, 16, 9, 2, 3, 10,
        17, 24, 32, 25, 18, 11, 4, 5,
        12, 19, 26, 33, 40, 48, 41, 34,
        27, 20, 13, 6, 7, 14, 21, 28,
        35, 42, 49, 56, 57, 50, 43, 36,
        29, 22, 15, 23, 30, 37, 44, 51,
        58, 59, 52, 45, 38, 31, 39, 46,
        53, 60, 61, 54, 47, 55, 62, 63
    };

    private static readonly double[] Cosine = BuildCosineTable();

    public static bool IsJpeg(byte[] data) => data != null && data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF;

    public static RgbaImage Decode(byte[] data) {
        if (!IsJpeg(data)) throw new NotSupportedException("Input is not a JPEG image.");
        var state = Parse(data);
        if (state.Frame == null) throw new InvalidDataException("JPEG image is missing a frame header.");
        if (state.Scans.Count == 0) throw new InvalidDataException("JPEG image is missing scan data.");
        if (state.Progressive) DecodeProgressiveScans(data, state);
        else DecodeSequentialScans(data, state);
        return BuildImage(state);
    }

    private static JpegState Parse(byte[] data) {
        var state = new JpegState();
        var offset = 2;
        while (offset < data.Length) {
            var marker = NextMarker(data, ref offset);
            if (marker == 0xD9) break;
            if (marker == 0xDA) {
                var scan = ParseScanHeader(data, ref offset, state);
                scan.DataOffset = offset;
                scan.DataEnd = FindEntropyEnd(data, offset);
                state.Scans.Add(scan);
                offset = scan.DataEnd;
                continue;
            }

            if (marker >= 0xD0 && marker <= 0xD7) continue;
            if (marker == 0x01) continue;
            if (offset + 2 > data.Length) throw new InvalidDataException("JPEG segment exceeds input size.");
            var length = ReadUInt16(data, offset);
            if (length < 2 || offset + length > data.Length) throw new InvalidDataException("Invalid JPEG segment length.");
            var segment = offset + 2;
            var segmentLength = length - 2;
            switch (marker) {
                case 0xC0:
                    state.Progressive = false;
                    ParseFrame(data, segment, segmentLength, state);
                    break;
                case 0xC2:
                    state.Progressive = true;
                    ParseFrame(data, segment, segmentLength, state);
                    break;
                case 0xC4:
                    ParseHuffmanTables(data, segment, segmentLength, state);
                    break;
                case 0xDB:
                    ParseQuantizationTables(data, segment, segmentLength, state);
                    break;
                case 0xDD:
                    state.RestartInterval = ReadUInt16(data, segment);
                    break;
                case 0xE0:
                    ParseJfif(data, segment, segmentLength, state);
                    break;
                case 0xE1:
                    ParseExif(data, segment, segmentLength, state);
                    break;
                case 0xEE:
                    ParseAdobe(data, segment, segmentLength, state);
                    break;
            }

            offset += length;
        }

        return state;
    }

    private static void ParseFrame(byte[] data, int offset, int length, JpegState state) {
        if (length < 6) throw new InvalidDataException("Invalid JPEG frame header.");
        var precision = data[offset];
        if (precision != 8) throw new NotSupportedException("Only 8-bit JPEG images are supported.");
        var height = ReadUInt16(data, offset + 1);
        var width = ReadUInt16(data, offset + 3);
        var componentCount = data[offset + 5];
        if (componentCount != 1 && componentCount != 3) throw new NotSupportedException("Only grayscale and three-component JPEG images are supported.");
        if (length < 6 + componentCount * 3) throw new InvalidDataException("JPEG frame component data is truncated.");
        if (width <= 0 || height <= 0) throw new InvalidDataException("JPEG dimensions must be positive.");
        var frame = new JpegFrame(width, height, componentCount);
        offset += 6;
        for (var i = 0; i < componentCount; i++) {
            var id = data[offset++];
            var sampling = data[offset++];
            var quant = data[offset++];
            frame.Components[i] = new JpegComponent(id, sampling >> 4, sampling & 15, quant);
            if (frame.Components[i].H <= 0 || frame.Components[i].H > 4 || frame.Components[i].V <= 0 || frame.Components[i].V > 4) throw new NotSupportedException("JPEG sampling factors must be between one and four.");
            if (quant >= state.QuantizationTables.Length) throw new InvalidDataException("JPEG component references an invalid quantization table.");
            frame.MaxH = Math.Max(frame.MaxH, frame.Components[i].H);
            frame.MaxV = Math.Max(frame.MaxV, frame.Components[i].V);
        }

        if (frame.MaxH <= 0 || frame.MaxV <= 0) throw new InvalidDataException("JPEG frame has invalid sampling factors.");
        foreach (var component in frame.Components) {
            component.WidthInBlocks = DivideRoundUp(DivideRoundUp(width * component.H, frame.MaxH), 8);
            component.HeightInBlocks = DivideRoundUp(DivideRoundUp(height * component.V, frame.MaxV), 8);
            component.Coefficients = new int[component.WidthInBlocks * component.HeightInBlocks * 64];
            component.Samples = new byte[component.WidthInBlocks * component.HeightInBlocks * 64];
        }

        state.Frame = frame;
    }

    private static JpegScan ParseScanHeader(byte[] data, ref int offset, JpegState state) {
        if (offset + 2 > data.Length) throw new InvalidDataException("JPEG scan header is truncated.");
        var length = ReadUInt16(data, offset);
        if (length < 2 || offset + length > data.Length) throw new InvalidDataException("Invalid JPEG scan length.");
        if (length < 6) throw new InvalidDataException("JPEG scan header is too short.");
        var p = offset + 2;
        var count = data[p++];
        if (state.Frame == null) throw new InvalidDataException("JPEG scan appears before a frame header.");
        if (count <= 0 || count > state.Frame.Components.Length) throw new InvalidDataException("JPEG scan component count is invalid.");
        if (length < 6 + count * 2) throw new InvalidDataException("JPEG scan component data is truncated.");
        var scan = new JpegScan();
        for (var i = 0; i < count; i++) {
            var id = data[p++];
            var table = data[p++];
            if ((table >> 4) >= 4 || (table & 15) >= 4) throw new InvalidDataException("JPEG scan references an invalid Huffman table.");
            var component = state.Frame.Find(id);
            component.DcTable = table >> 4;
            component.AcTable = table & 15;
            scan.Components.Add(new JpegScanComponent(component, table >> 4, table & 15));
        }

        scan.SpectralStart = data[p++];
        scan.SpectralEnd = data[p++];
        var approximation = data[p++];
        scan.SuccessiveHigh = approximation >> 4;
        scan.SuccessiveLow = approximation & 15;
        if (!state.Progressive && (scan.SpectralStart != 0 || scan.SpectralEnd != 63 || scan.SuccessiveHigh != 0 || scan.SuccessiveLow != 0)) throw new NotSupportedException("Only baseline sequential JPEG scans are supported for SOF0 images.");
        if (state.Progressive) {
            if (scan.SpectralStart > scan.SpectralEnd || scan.SpectralEnd > 63) throw new InvalidDataException("Invalid progressive JPEG spectral selection.");
            if (scan.SpectralStart > 0 && count != 1) throw new NotSupportedException("Progressive JPEG AC scans must contain a single component.");
            if (scan.SuccessiveHigh > 13 || scan.SuccessiveLow > 13) throw new InvalidDataException("Invalid progressive JPEG successive approximation.");
        }

        offset += length;
        return scan;
    }

    private static void ParseQuantizationTables(byte[] data, int offset, int length, JpegState state) {
        var end = offset + length;
        while (offset < end) {
            var info = data[offset++];
            var precision = info >> 4;
            var id = info & 15;
            if (precision != 0) throw new NotSupportedException("Only 8-bit JPEG quantization tables are supported.");
            if (id >= state.QuantizationTables.Length || offset + 64 > end) throw new InvalidDataException("Invalid JPEG quantization table.");
            var table = new int[64];
            for (var i = 0; i < 64; i++) table[ZigZag[i]] = data[offset++];
            state.QuantizationTables[id] = table;
        }
    }

    private static void ParseExif(byte[] data, int offset, int length, JpegState state) {
        if (length < 14) return;
        if (data[offset] != (byte)'E' || data[offset + 1] != (byte)'x' || data[offset + 2] != (byte)'i' || data[offset + 3] != (byte)'f' || data[offset + 4] != 0 || data[offset + 5] != 0) return;
        var tiff = offset + 6;
        var end = offset + length;
        var little = data[tiff] == (byte)'I' && data[tiff + 1] == (byte)'I';
        var big = data[tiff] == (byte)'M' && data[tiff + 1] == (byte)'M';
        if (!little && !big) return;
        if (ReadUInt16(data, tiff + 2, little) != 42) return;
        var ifd = tiff + checked((int)ReadUInt32(data, tiff + 4, little));
        if (ifd < tiff || ifd + 2 > end) return;
        var count = ReadUInt16(data, ifd, little);
        var entry = ifd + 2;
        for (var i = 0; i < count && entry + 12 <= end; i++, entry += 12) {
            var tag = ReadUInt16(data, entry, little);
            if (tag != 0x0112) continue;
            var type = ReadUInt16(data, entry + 2, little);
            var valueCount = ReadUInt32(data, entry + 4, little);
            if (type == 3 && valueCount >= 1) {
                var orientation = ReadUInt16(data, entry + 8, little);
                if (orientation >= 1 && orientation <= 8) state.Orientation = orientation;
            }

            return;
        }
    }

    private static void ParseJfif(byte[] data, int offset, int length, JpegState state) {
        if (length < 5) return;
        state.HasJfif = data[offset] == (byte)'J' && data[offset + 1] == (byte)'F' && data[offset + 2] == (byte)'I' && data[offset + 3] == (byte)'F' && data[offset + 4] == 0;
    }

    private static void ParseAdobe(byte[] data, int offset, int length, JpegState state) {
        if (length < 12) return;
        if (data[offset] != (byte)'A' || data[offset + 1] != (byte)'d' || data[offset + 2] != (byte)'o' || data[offset + 3] != (byte)'b' || data[offset + 4] != (byte)'e') return;
        state.AdobeTransform = data[offset + 11];
    }

    private static void ParseHuffmanTables(byte[] data, int offset, int length, JpegState state) {
        var end = offset + length;
        while (offset < end) {
            var info = data[offset++];
            var tableClass = info >> 4;
            var id = info & 15;
            if (id >= 4 || offset + 16 > end) throw new InvalidDataException("Invalid JPEG Huffman table.");
            var counts = new byte[16];
            var symbols = 0;
            for (var i = 0; i < 16; i++) {
                counts[i] = data[offset++];
                symbols += counts[i];
            }

            if (offset + symbols > end) throw new InvalidDataException("JPEG Huffman symbols are truncated.");
            var values = new byte[symbols];
            Buffer.BlockCopy(data, offset, values, 0, symbols);
            offset += symbols;
            var table = new JpegHuffmanTable(counts, values);
            if (tableClass == 0) state.DcTables[id] = table;
            else if (tableClass == 1) state.AcTables[id] = table;
            else throw new InvalidDataException("Invalid JPEG Huffman table class.");
        }
    }

    private static void DecodeSequentialScans(byte[] data, JpegState state) {
        foreach (var scan in state.Scans) DecodeSequentialScan(data, state, scan);
    }

    private static void DecodeSequentialScan(byte[] data, JpegState state, JpegScan scan) {
        var frame = state.Frame!;
        var reader = new JpegBitReader(data, scan.DataOffset, scan.DataEnd);
        var block = new int[64];
        var samples = new byte[64];
        var restartCounter = state.RestartInterval;
        if (scan.Components.Count == 1) {
            var component = scan.Components[0].Component;
            ApplyScanTables(scan);
            ForEachComponentBlock(component, (blockX, blockY) => {
                if (state.RestartInterval > 0 && restartCounter == 0) {
                    reader.AlignToByte();
                    component.PreviousDc = 0;
                    restartCounter = state.RestartInterval;
                }

                Array.Clear(block, 0, block.Length);
                DecodeBlock(reader, state, component, block);
                InverseDct(block, state.QuantizationTables[component.QuantizationTable]!, samples);
                StoreBlock(component, blockX, blockY, samples);
                if (state.RestartInterval > 0) restartCounter--;
            });
            return;
        }

        var mcuWidth = frame.MaxH * 8;
        var mcuHeight = frame.MaxV * 8;
        var mcuColumns = DivideRoundUp(frame.Width, mcuWidth);
        var mcuRows = DivideRoundUp(frame.Height, mcuHeight);
        for (var my = 0; my < mcuRows; my++) {
            for (var mx = 0; mx < mcuColumns; mx++) {
                if (state.RestartInterval > 0 && restartCounter == 0) {
                    reader.AlignToByte();
                    foreach (var scanComponent in scan.Components) scanComponent.Component.PreviousDc = 0;
                    restartCounter = state.RestartInterval;
                }

                foreach (var scanComponent in scan.Components) {
                    var component = scanComponent.Component;
                    component.DcTable = scanComponent.DcTable;
                    component.AcTable = scanComponent.AcTable;
                    for (var vy = 0; vy < component.V; vy++) {
                        for (var hx = 0; hx < component.H; hx++) {
                            var blockX = mx * component.H + hx;
                            var blockY = my * component.V + vy;
                            if (blockX >= component.WidthInBlocks || blockY >= component.HeightInBlocks) continue;
                            Array.Clear(block, 0, block.Length);
                            DecodeBlock(reader, state, component, block);
                            InverseDct(block, state.QuantizationTables[component.QuantizationTable]!, samples);
                            StoreBlock(component, blockX, blockY, samples);
                        }
                    }
                }

                if (state.RestartInterval > 0) restartCounter--;
            }
        }
    }

    private static void ApplyScanTables(JpegScan scan) {
        foreach (var scanComponent in scan.Components) {
            scanComponent.Component.DcTable = scanComponent.DcTable;
            scanComponent.Component.AcTable = scanComponent.AcTable;
        }
    }

    private static void DecodeBlock(JpegBitReader reader, JpegState state, JpegComponent component, int[] block) {
        var dcTable = state.DcTables[component.DcTable] ?? throw new InvalidDataException("JPEG DC Huffman table is missing.");
        var acTable = state.AcTables[component.AcTable] ?? throw new InvalidDataException("JPEG AC Huffman table is missing.");
        if (state.QuantizationTables[component.QuantizationTable] == null) throw new InvalidDataException("JPEG quantization table is missing.");
        var dcSize = dcTable.Decode(reader);
        var dcDelta = ReceiveExtended(reader, dcSize);
        component.PreviousDc += dcDelta;
        block[0] = component.PreviousDc;
        var index = 1;
        while (index < 64) {
            var symbol = acTable.Decode(reader);
            if (symbol == 0) break;
            var run = symbol >> 4;
            var size = symbol & 15;
            if (size == 0) {
                if (run == 15) {
                    index += 16;
                    continue;
                }

                throw new InvalidDataException("Invalid JPEG AC coefficient run.");
            }

            index += run;
            if (index >= 64) throw new InvalidDataException("JPEG AC coefficient run exceeds block size.");
            block[ZigZag[index]] = ReceiveExtended(reader, size);
            index++;
        }
    }

    private static void DecodeProgressiveScans(byte[] data, JpegState state) {
        var frame = state.Frame!;
        foreach (var scan in state.Scans) {
            if (scan.SpectralStart == 0 && scan.SpectralEnd == 0) {
                if (scan.SuccessiveHigh == 0) DecodeProgressiveDcFirst(data, state, scan);
                else DecodeProgressiveDcRefine(data, state, scan);
            } else {
                if (scan.SuccessiveHigh == 0) DecodeProgressiveAcFirst(data, state, scan);
                else DecodeProgressiveAcRefine(data, state, scan);
            }
        }

        var samples = new byte[64];
        foreach (var component in frame.Components) {
            var quantization = state.QuantizationTables[component.QuantizationTable] ?? throw new InvalidDataException("JPEG quantization table is missing.");
            for (var blockY = 0; blockY < component.HeightInBlocks; blockY++) {
                for (var blockX = 0; blockX < component.WidthInBlocks; blockX++) {
                    var offset = CoefficientOffset(component, blockX, blockY);
                    var coefficients = new int[64];
                    Array.Copy(component.Coefficients, offset, coefficients, 0, 64);
                    InverseDct(coefficients, quantization, samples);
                    StoreBlock(component, blockX, blockY, samples);
                }
            }
        }
    }

    private static void DecodeProgressiveDcFirst(byte[] data, JpegState state, JpegScan scan) {
        var reader = new JpegBitReader(data, scan.DataOffset, scan.DataEnd);
        var restartCounter = state.RestartInterval;
        ForEachScanBlock(state.Frame!, scan, (component, beginsRestartUnit, endsRestartUnit) => {
            if (beginsRestartUnit && state.RestartInterval > 0 && restartCounter == 0) {
                reader.AlignToByte();
                foreach (var item in scan.Components) item.Component.PreviousDc = 0;
                restartCounter = state.RestartInterval;
            }

            var table = state.DcTables[scan.Find(component).DcTable] ?? throw new InvalidDataException("JPEG DC Huffman table is missing.");
            var size = table.Decode(reader);
            component.PreviousDc += ReceiveExtended(reader, size);
            component.Coefficients[CurrentBlockOffset(component)] = component.PreviousDc << scan.SuccessiveLow;
            if (endsRestartUnit && state.RestartInterval > 0) restartCounter--;
        });
    }

    private static void DecodeProgressiveDcRefine(byte[] data, JpegState state, JpegScan scan) {
        var reader = new JpegBitReader(data, scan.DataOffset, scan.DataEnd);
        var bit = 1 << scan.SuccessiveLow;
        var restartCounter = state.RestartInterval;
        ForEachScanBlock(state.Frame!, scan, (component, beginsRestartUnit, endsRestartUnit) => {
            if (beginsRestartUnit && state.RestartInterval > 0 && restartCounter == 0) {
                reader.AlignToByte();
                restartCounter = state.RestartInterval;
            }

            if (reader.ReadBit() != 0) component.Coefficients[CurrentBlockOffset(component)] |= bit;
            if (endsRestartUnit && state.RestartInterval > 0) restartCounter--;
        });
    }

    private static void DecodeProgressiveAcFirst(byte[] data, JpegState state, JpegScan scan) {
        var scanComponent = scan.Components[0];
        var component = scanComponent.Component;
        var table = state.AcTables[scanComponent.AcTable] ?? throw new InvalidDataException("JPEG AC Huffman table is missing.");
        var reader = new JpegBitReader(data, scan.DataOffset, scan.DataEnd);
        var eobRun = 0;
        var restartCounter = state.RestartInterval;
        ForEachComponentBlock(component, (blockX, blockY) => {
            if (state.RestartInterval > 0 && restartCounter == 0) {
                reader.AlignToByte();
                eobRun = 0;
                restartCounter = state.RestartInterval;
            }

            var offset = CoefficientOffset(component, blockX, blockY);
            if (eobRun > 0) {
                eobRun--;
            } else {
                var k = scan.SpectralStart;
                while (k <= scan.SpectralEnd) {
                    var symbol = table.Decode(reader);
                    var run = symbol >> 4;
                    var size = symbol & 15;
                    if (size == 0) {
                        if (run == 15) {
                            k += 16;
                            continue;
                        }

                        eobRun = (1 << run) + (run == 0 ? 0 : reader.ReadBits(run)) - 1;
                        break;
                    }

                    k += run;
                    if (k > scan.SpectralEnd) throw new InvalidDataException("Progressive JPEG AC coefficient run exceeds spectral band.");
                    component.Coefficients[offset + ZigZag[k]] = ReceiveExtended(reader, size) << scan.SuccessiveLow;
                    k++;
                }
            }

            if (state.RestartInterval > 0) restartCounter--;
        });
    }

    private static void DecodeProgressiveAcRefine(byte[] data, JpegState state, JpegScan scan) {
        var scanComponent = scan.Components[0];
        var component = scanComponent.Component;
        var table = state.AcTables[scanComponent.AcTable] ?? throw new InvalidDataException("JPEG AC Huffman table is missing.");
        var reader = new JpegBitReader(data, scan.DataOffset, scan.DataEnd);
        var bit = 1 << scan.SuccessiveLow;
        var eobRun = 0;
        var restartCounter = state.RestartInterval;
        ForEachComponentBlock(component, (blockX, blockY) => {
            if (state.RestartInterval > 0 && restartCounter == 0) {
                reader.AlignToByte();
                eobRun = 0;
                restartCounter = state.RestartInterval;
            }

            var offset = CoefficientOffset(component, blockX, blockY);
            if (eobRun > 0) {
                RefineBand(reader, component.Coefficients, offset, scan.SpectralStart, scan.SpectralEnd, bit);
                eobRun--;
            } else {
                var k = scan.SpectralStart;
                while (k <= scan.SpectralEnd) {
                    var symbol = table.Decode(reader);
                    var run = symbol >> 4;
                    var size = symbol & 15;
                    if (size == 0) {
                        if (run < 15) {
                            eobRun = (1 << run) + (run == 0 ? 0 : reader.ReadBits(run));
                            RefineBandFrom(reader, component.Coefficients, offset, k, scan.SpectralEnd, bit);
                            eobRun--;
                            break;
                        }

                        run = 16;
                    } else if (size == 1) {
                        var newCoefficient = reader.ReadBit() == 1 ? bit : -bit;
                        while (k <= scan.SpectralEnd) {
                            var index = offset + ZigZag[k];
                            var coefficient = component.Coefficients[index];
                            if (coefficient != 0) {
                                RefineCoefficient(reader, component.Coefficients, index, bit);
                            } else {
                                if (run == 0) {
                                    component.Coefficients[index] = newCoefficient;
                                    k++;
                                    break;
                                }

                                run--;
                            }

                            k++;
                        }
                    } else {
                        throw new InvalidDataException("Invalid progressive JPEG AC refinement symbol.");
                    }
                }
            }

            if (state.RestartInterval > 0) restartCounter--;
        });
    }

    private static void RefineBand(JpegBitReader reader, int[] coefficients, int offset, int spectralStart, int spectralEnd, int bit) =>
        RefineBandFrom(reader, coefficients, offset, spectralStart, spectralEnd, bit);

    private static void RefineBandFrom(JpegBitReader reader, int[] coefficients, int offset, int spectralStart, int spectralEnd, int bit) {
        for (var k = spectralStart; k <= spectralEnd; k++) {
            var index = offset + ZigZag[k];
            if (coefficients[index] != 0) RefineCoefficient(reader, coefficients, index, bit);
        }
    }

    private static void RefineCoefficient(JpegBitReader reader, int[] coefficients, int index, int bit) {
        var coefficient = coefficients[index];
        if ((Math.Abs(coefficient) & bit) == 0 && reader.ReadBit() != 0) coefficients[index] += coefficient > 0 ? bit : -bit;
    }

    private static void ForEachScanBlock(JpegFrame frame, JpegScan scan, Action<JpegComponent, bool, bool> visit) {
        if (scan.Components.Count == 1) {
            var component = scan.Components[0].Component;
            ForEachComponentBlock(component, (blockX, blockY) => {
                component.CurrentBlockOffset = CoefficientOffset(component, blockX, blockY);
                visit(component, true, true);
            });
            return;
        }

        var mcuWidth = frame.MaxH * 8;
        var mcuHeight = frame.MaxV * 8;
        var mcuColumns = DivideRoundUp(frame.Width, mcuWidth);
        var mcuRows = DivideRoundUp(frame.Height, mcuHeight);
        for (var my = 0; my < mcuRows; my++) {
            for (var mx = 0; mx < mcuColumns; mx++) {
                var blockCount = CountScanBlocksInMcu(scan, mx, my);
                var blockIndex = 0;
                foreach (var scanComponent in scan.Components) {
                    var component = scanComponent.Component;
                    for (var vy = 0; vy < component.V; vy++) {
                        for (var hx = 0; hx < component.H; hx++) {
                            var blockX = mx * component.H + hx;
                            var blockY = my * component.V + vy;
                            if (blockX >= component.WidthInBlocks || blockY >= component.HeightInBlocks) continue;
                            component.CurrentBlockOffset = CoefficientOffset(component, blockX, blockY);
                            visit(component, blockIndex == 0, blockIndex == blockCount - 1);
                            blockIndex++;
                        }
                    }
                }
            }
        }
    }

    private static int CountScanBlocksInMcu(JpegScan scan, int mx, int my) {
        var count = 0;
        foreach (var scanComponent in scan.Components) {
            var component = scanComponent.Component;
            for (var vy = 0; vy < component.V; vy++) {
                for (var hx = 0; hx < component.H; hx++) {
                    var blockX = mx * component.H + hx;
                    var blockY = my * component.V + vy;
                    if (blockX < component.WidthInBlocks && blockY < component.HeightInBlocks) count++;
                }
            }
        }

        return count;
    }

    private static void ForEachComponentBlock(JpegComponent component, Action<int, int> visit) {
        for (var blockY = 0; blockY < component.HeightInBlocks; blockY++) {
            for (var blockX = 0; blockX < component.WidthInBlocks; blockX++) visit(blockX, blockY);
        }
    }

    private static int CurrentBlockOffset(JpegComponent component) => component.CurrentBlockOffset;
    private static int CoefficientOffset(JpegComponent component, int blockX, int blockY) => (blockY * component.WidthInBlocks + blockX) * 64;

    private static void InverseDct(int[] coefficients, int[] quantization, byte[] output) {
        for (var y = 0; y < 8; y++) {
            for (var x = 0; x < 8; x++) {
                var sum = 0.0;
                for (var v = 0; v < 8; v++) {
                    for (var u = 0; u < 8; u++) {
                        var cu = u == 0 ? 1.0 / Math.Sqrt(2) : 1.0;
                        var cv = v == 0 ? 1.0 / Math.Sqrt(2) : 1.0;
                        sum += cu * cv * coefficients[v * 8 + u] * quantization[v * 8 + u] * Cosine[x * 8 + u] * Cosine[y * 8 + v];
                    }
                }

                output[y * 8 + x] = ClampToByte(Math.Round(sum / 4.0 + 128.0));
            }
        }
    }

    private static RgbaImage BuildImage(JpegState state) {
        var frame = state.Frame!;
        var rgba = new byte[frame.Width * frame.Height * 4];
        var target = 0;
        var directRgb = UsesDirectRgb(frame, state);
        for (var y = 0; y < frame.Height; y++) {
            for (var x = 0; x < frame.Width; x++) {
                if (frame.Components.Length == 1) {
                    var gray = Sample(frame.Components[0], x, y, frame);
                    rgba[target++] = gray; rgba[target++] = gray; rgba[target++] = gray; rgba[target++] = 255;
                } else if (directRgb) {
                    rgba[target++] = Sample(frame.Components[0], x, y, frame);
                    rgba[target++] = Sample(frame.Components[1], x, y, frame);
                    rgba[target++] = Sample(frame.Components[2], x, y, frame);
                    rgba[target++] = 255;
                } else {
                    var yy = Sample(frame.Components[0], x, y, frame);
                    var cb = Sample(frame.Components[1], x, y, frame) - 128;
                    var cr = Sample(frame.Components[2], x, y, frame) - 128;
                    rgba[target++] = ClampToByte(yy + 1.402 * cr);
                    rgba[target++] = ClampToByte(yy - 0.344136 * cb - 0.714136 * cr);
                    rgba[target++] = ClampToByte(yy + 1.772 * cb);
                    rgba[target++] = 255;
                }
            }
        }

        return ApplyOrientation(new RgbaImage(frame.Width, frame.Height, rgba), state.Orientation);
    }

    private static bool UsesDirectRgb(JpegFrame frame, JpegState state) =>
        frame.Components.Length == 3 &&
        (state.AdobeTransform == 0 ||
         frame.Components[0].Id == (byte)'R' && frame.Components[1].Id == (byte)'G' && frame.Components[2].Id == (byte)'B' ||
         !state.HasJfif && state.AdobeTransform < 0 && !UsesDefaultYcbcrComponentIds(frame));

    private static bool UsesDefaultYcbcrComponentIds(JpegFrame frame) =>
        frame.Components[0].Id == 1 && frame.Components[1].Id == 2 && frame.Components[2].Id == 3;

    private static RgbaImage ApplyOrientation(RgbaImage image, int orientation) {
        if (orientation <= 1 || orientation > 8) return image;
        var swap = orientation >= 5 && orientation <= 8;
        var width = swap ? image.Height : image.Width;
        var height = swap ? image.Width : image.Height;
        var pixels = new byte[width * height * 4];
        for (var y = 0; y < image.Height; y++) {
            for (var x = 0; x < image.Width; x++) {
                MapOrientation(x, y, image.Width, image.Height, orientation, out var dx, out var dy);
                Buffer.BlockCopy(image.Pixels, (y * image.Width + x) * 4, pixels, (dy * width + dx) * 4, 4);
            }
        }

        return new RgbaImage(width, height, pixels);
    }

    private static void MapOrientation(int x, int y, int width, int height, int orientation, out int dx, out int dy) {
        switch (orientation) {
            case 2:
                dx = width - 1 - x; dy = y; break;
            case 3:
                dx = width - 1 - x; dy = height - 1 - y; break;
            case 4:
                dx = x; dy = height - 1 - y; break;
            case 5:
                dx = y; dy = x; break;
            case 6:
                dx = height - 1 - y; dy = x; break;
            case 7:
                dx = height - 1 - y; dy = width - 1 - x; break;
            case 8:
                dx = y; dy = width - 1 - x; break;
            default:
                dx = x; dy = y; break;
        }
    }

    private static byte Sample(JpegComponent component, int x, int y, JpegFrame frame) {
        var sx = x * component.H / frame.MaxH;
        var sy = y * component.V / frame.MaxV;
        sx = Math.Min(sx, component.WidthInBlocks * 8 - 1);
        sy = Math.Min(sy, component.HeightInBlocks * 8 - 1);
        var blockX = sx / 8;
        var blockY = sy / 8;
        var offset = (blockY * component.WidthInBlocks + blockX) * 64 + (sy % 8) * 8 + (sx % 8);
        return component.Samples[offset];
    }

    private static void StoreBlock(JpegComponent component, int blockX, int blockY, byte[] samples) {
        if (blockX >= component.WidthInBlocks || blockY >= component.HeightInBlocks) return;
        var offset = (blockY * component.WidthInBlocks + blockX) * 64;
        Buffer.BlockCopy(samples, 0, component.Samples, offset, 64);
    }

    private static int ReceiveExtended(JpegBitReader reader, int length) {
        if (length == 0) return 0;
        var value = reader.ReadBits(length);
        var threshold = 1 << (length - 1);
        return value < threshold ? value + ((-1) << length) + 1 : value;
    }

    private static int NextMarker(byte[] data, ref int offset) {
        while (offset < data.Length && data[offset] != 0xFF) offset++;
        while (offset < data.Length && data[offset] == 0xFF) offset++;
        if (offset >= data.Length) throw new InvalidDataException("JPEG marker is truncated.");
        return data[offset++];
    }

    private static int FindEntropyEnd(byte[] data, int offset) {
        while (offset < data.Length) {
            if (data[offset] != 0xFF) {
                offset++;
                continue;
            }

            var markerOffset = offset;
            offset++;
            while (offset < data.Length && data[offset] == 0xFF) offset++;
            if (offset >= data.Length) return markerOffset;
            var marker = data[offset];
            if (marker == 0x00 || marker >= 0xD0 && marker <= 0xD7) {
                offset++;
                continue;
            }

            return markerOffset;
        }

        return data.Length;
    }

    private static int ReadUInt16(byte[] data, int offset) => (data[offset] << 8) | data[offset + 1];
    private static int ReadUInt16(byte[] data, int offset, bool little) => little ? data[offset] | (data[offset + 1] << 8) : ReadUInt16(data, offset);
    private static uint ReadUInt32(byte[] data, int offset, bool little) =>
        little
            ? (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24))
            : ((uint)data[offset] << 24) | ((uint)data[offset + 1] << 16) | ((uint)data[offset + 2] << 8) | data[offset + 3];
    private static int DivideRoundUp(int value, int divisor) => (value + divisor - 1) / divisor;
    private static byte ClampToByte(double value) => (byte)Math.Max(0, Math.Min(255, (int)Math.Round(value)));

    private static double[] BuildCosineTable() {
        var table = new double[64];
        for (var x = 0; x < 8; x++) {
            for (var u = 0; u < 8; u++) table[x * 8 + u] = Math.Cos((2 * x + 1) * u * Math.PI / 16.0);
        }

        return table;
    }

    private sealed class JpegHuffmanTable {
        private readonly Dictionary<int, byte> _symbols = new();

        public JpegHuffmanTable(byte[] counts, byte[] values) {
            var code = 0;
            var valueIndex = 0;
            for (var length = 1; length <= 16; length++) {
                var count = counts[length - 1];
                for (var i = 0; i < count; i++) _symbols[(length << 16) | code++] = values[valueIndex++];
                code <<= 1;
            }
        }

        public int Decode(JpegBitReader reader) {
            var code = 0;
            for (var length = 1; length <= 16; length++) {
                code = (code << 1) | reader.ReadBit();
                if (_symbols.TryGetValue((length << 16) | code, out var symbol)) return symbol;
            }

            throw new InvalidDataException("Invalid JPEG Huffman code.");
        }
    }

}
