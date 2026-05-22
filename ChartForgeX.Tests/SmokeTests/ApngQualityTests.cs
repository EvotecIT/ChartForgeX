using System;
using System.Collections.Generic;
using ChartForgeX.Raster;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void ApngWriterUsesFullColorDeltaFramesForSmallMotion() {
        var first = SolidFrame(16, 16, 12, 18, 24);
        var second = SolidFrame(16, 16, 12, 18, 24);
        var changed = (5 * 16 + 4) * 4;
        second.Pixels[changed] = 249;
        second.Pixels[changed + 1] = 115;
        second.Pixels[changed + 2] = 22;
        second.Pixels[changed + 3] = 210;

        var apng = ApngWriter.WriteRgba(new[] { first, second }, 10, loop: true);
        var frames = ReadApngFrameControls(apng);
        Assert(apng[0] == 137 && apng[1] == 80 && apng[2] == 78 && apng[3] == 71, "APNG encoding should use the PNG signature.");
        Assert(frames.Length == 2, "Animated PNG encoding should preserve the requested frame count.");
        Assert(frames[0].Left == 0 && frames[0].Top == 0 && frames[0].Width == 16 && frames[0].Height == 16, "The first APNG frame should establish the full canvas.");
        Assert(frames[1].Left == 4 && frames[1].Top == 5 && frames[1].Width == 1 && frames[1].Height == 1, "Small APNG motion should encode as a cropped full-color delta frame.");
        Assert(frames[1].BlendOp == 0, "Cropped APNG frames should use source blending so alpha changes replace previous pixels.");
        Assert(frames[0].DelayNumerator == 10, "APNG frame controls should preserve the shared animated raster frame delay.");
        AssertThrows<ArgumentException>(() => ApngWriter.WriteRgba(new[] { first, SolidFrame(8, 16, 12, 18, 24) }, 10, loop: false), "APNG export should reject mismatched animated raster frame dimensions.");
    }

    private static ApngFrameControl[] ReadApngFrameControls(byte[] png) {
        var frames = new List<ApngFrameControl>();
        var offset = 8;
        while (offset < png.Length) {
            var length = ReadUInt32BigEndian(png, offset);
            var type = System.Text.Encoding.ASCII.GetString(png, offset + 4, 4);
            if (type == "fcTL") {
                frames.Add(new ApngFrameControl(
                    ReadUInt32BigEndian(png, offset + 12),
                    ReadUInt32BigEndian(png, offset + 16),
                    ReadUInt32BigEndian(png, offset + 20),
                    ReadUInt32BigEndian(png, offset + 24),
                    ReadUInt16BigEndian(png, offset + 28),
                    png[offset + 33]));
            }

            offset += 12 + length;
        }

        return frames.ToArray();
    }

    private static int ReadUInt32BigEndian(byte[] bytes, int offset) =>
        (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];

    private static int ReadUInt16BigEndian(byte[] bytes, int offset) =>
        (bytes[offset] << 8) | bytes[offset + 1];

    private readonly struct ApngFrameControl {
        public ApngFrameControl(int width, int height, int left, int top, int delayNumerator, byte blendOp) {
            Width = width;
            Height = height;
            Left = left;
            Top = top;
            DelayNumerator = delayNumerator;
            BlendOp = blendOp;
        }

        public readonly int Width;
        public readonly int Height;
        public readonly int Left;
        public readonly int Top;
        public readonly int DelayNumerator;
        public readonly byte BlendOp;
    }
}
