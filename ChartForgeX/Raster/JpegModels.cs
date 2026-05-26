using System;
using System.Collections.Generic;
using System.IO;

namespace ChartForgeX.Raster;

internal static partial class JpegReader {
    private sealed class JpegState {
        public readonly int[][] QuantizationTables = new int[4][];
        public readonly JpegHuffmanTable?[] DcTables = new JpegHuffmanTable?[4];
        public readonly JpegHuffmanTable?[] AcTables = new JpegHuffmanTable?[4];
        public readonly List<JpegScan> Scans = new();
        public JpegFrame? Frame;
        public int RestartInterval;
        public int AdobeTransform = -1;
        public bool HasJfif;
        public bool Progressive;
        public int Orientation = 1;
    }

    private sealed class JpegScan {
        public readonly List<JpegScanComponent> Components = new();
        public int SpectralStart;
        public int SpectralEnd;
        public int SuccessiveHigh;
        public int SuccessiveLow;
        public int DataOffset;
        public int DataEnd;

        public JpegScanComponent Find(JpegComponent component) {
            foreach (var scanComponent in Components) {
                if (ReferenceEquals(scanComponent.Component, component)) return scanComponent;
            }

            throw new InvalidDataException("JPEG scan is missing a component table mapping.");
        }
    }

    private sealed class JpegScanComponent {
        public readonly JpegComponent Component;
        public readonly int DcTable;
        public readonly int AcTable;

        public JpegScanComponent(JpegComponent component, int dcTable, int acTable) {
            Component = component;
            DcTable = dcTable;
            AcTable = acTable;
        }
    }

    private sealed class JpegFrame {
        public readonly int Width;
        public readonly int Height;
        public readonly JpegComponent[] Components;
        public int MaxH;
        public int MaxV;

        public JpegFrame(int width, int height, int componentCount) {
            Width = width;
            Height = height;
            Components = new JpegComponent[componentCount];
        }

        public JpegComponent Find(int id) {
            foreach (var component in Components) {
                if (component.Id == id) return component;
            }

            throw new InvalidDataException("JPEG scan references an unknown component.");
        }
    }

    private sealed class JpegComponent {
        public readonly int Id;
        public readonly int H;
        public readonly int V;
        public readonly int QuantizationTable;
        public int DcTable;
        public int AcTable;
        public int PreviousDc;
        public int WidthInBlocks;
        public int HeightInBlocks;
        public int CurrentBlockOffset;
        public int[] Coefficients = Array.Empty<int>();
        public byte[] Samples = Array.Empty<byte>();

        public JpegComponent(int id, int h, int v, int quantizationTable) {
            Id = id;
            H = h;
            V = v;
            QuantizationTable = quantizationTable;
        }
    }
}
