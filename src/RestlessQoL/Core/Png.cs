using System;
using System.IO;
using System.IO.Compression;
using UnityEngine;

namespace RestlessQoL.Core;

internal static class Png
{
    public static Texture2D? Load(byte[] data)
    {
        if (data.Length < 33 || data[0] != 137 || data[1] != 80)
            return null;

        var width = 0;
        var height = 0;
        using var idat = new MemoryStream();
        var offset = 8;
        while (offset + 12 <= data.Length)
        {
            var len = ReadInt(data, offset);
            var type = data[offset + 4] << 24 | data[offset + 5] << 16 | data[offset + 6] << 8 | data[offset + 7];
            var start = offset + 8;
            if (start + len + 4 > data.Length)
                break;
            if (type == 0x49484452)
            {
                width = ReadInt(data, start);
                height = ReadInt(data, start + 4);
                if (data[start + 8] != 8 || data[start + 9] != 6 || data[start + 12] != 0)
                    return null;
            }
            else if (type == 0x49444154)
                idat.Write(data, start, len);
            else if (type == 0x49454E44)
                break;
            offset = start + len + 4;
        }

        if (width <= 0 || height <= 0 || idat.Length < 4)
            return null;

        var raw = Inflate(idat.ToArray(), (width * 4 + 1) * height);
        if (raw == null)
            return null;

        var pixels = new Color32[width * height];
        var stride = width * 4;
        var prev = new byte[stride];
        var row = new byte[stride];
        var src = 0;
        for (var y = 0; y < height; y++)
        {
            var filter = raw[src++];
            Buffer.BlockCopy(raw, src, row, 0, stride);
            src += stride;
            Unfilter(filter, row, prev);
            var dest = (height - 1 - y) * width;
            for (var x = 0; x < width; x++)
            {
                var i = x * 4;
                pixels[dest + x] = new Color32(row[i], row[i + 1], row[i + 2], row[i + 3]);
            }

            Buffer.BlockCopy(row, 0, prev, 0, stride);
        }

        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.SetPixels32(pixels);
        tex.Apply(false, false);
        return tex;
    }

    private static byte[]? Inflate(byte[] zlib, int expected)
    {
        try
        {
            using var input = new MemoryStream(zlib, 2, zlib.Length - 6);
            using var deflate = new DeflateStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream(expected);
            deflate.CopyTo(output);
            return output.ToArray();
        }
        catch
        {
            return null;
        }
    }

    private static void Unfilter(byte filter, byte[] row, byte[] prev)
    {
        var bpp = 4;
        for (var i = 0; i < row.Length; i++)
        {
            var left = i >= bpp ? row[i - bpp] : (byte)0;
            var up = prev[i];
            var upLeft = i >= bpp ? prev[i - bpp] : (byte)0;
            row[i] = filter switch
            {
                1 => (byte)(row[i] + left),
                2 => (byte)(row[i] + up),
                3 => (byte)(row[i] + ((left + up) >> 1)),
                4 => (byte)(row[i] + Paeth(left, up, upLeft)),
                _ => row[i],
            };
        }
    }

    private static byte Paeth(byte a, byte b, byte c)
    {
        var p = a + b - c;
        var pa = Math.Abs(p - a);
        var pb = Math.Abs(p - b);
        var pc = Math.Abs(p - c);
        if (pa <= pb && pa <= pc)
            return a;
        return pb <= pc ? b : c;
    }

    private static int ReadInt(byte[] data, int offset) =>
        data[offset] << 24 | data[offset + 1] << 16 | data[offset + 2] << 8 | data[offset + 3];
}
