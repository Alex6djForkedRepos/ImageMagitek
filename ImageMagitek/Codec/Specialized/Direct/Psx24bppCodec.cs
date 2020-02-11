﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ImageMagitek.Colors;
using ImageMagitek.ExtensionMethods;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;

namespace ImageMagitek.Codec
{
    public class Psx24bppCodec : DirectCodec
    {
        public override string Name => "PSX 24bpp";
        public override int Width { get; } = 8;
        public override int Height { get; } = 8;
        public override ImageLayout Layout => ImageLayout.Single;
        public override int ColorDepth => 24;
        public override int StorageSize => Width * Height * 24;

        private BitStream _bitStream;

        public Psx24bppCodec(int width, int height)
        {
            Width = width;
            Height = height;

            _foreignBuffer = new byte[(StorageSize + 7) / 8];
            _nativeBuffer = new ColorRgba32[Width, Height];

            _bitStream = BitStream.OpenRead(_foreignBuffer, StorageSize);
        }

        public override ColorRgba32[,] DecodeElement(ArrangerElement el, ReadOnlySpan<byte> encodedBuffer)
        {
            if (encodedBuffer.Length * 8 < StorageSize)
                throw new ArgumentException(nameof(encodedBuffer));

            encodedBuffer.Slice(0, _foreignBuffer.Length).CopyTo(_foreignBuffer);
            _bitStream.SeekAbsolute(0);

            for (int y = 0; y < el.Height; y++)
            {
                for (int x = 0; x < el.Width; x++)
                {
                    byte r = _bitStream.ReadByte();
                    byte g = _bitStream.ReadByte();
                    byte b = _bitStream.ReadByte();

                    _nativeBuffer[x, y] = new ColorRgba32(r, g, b, 0xFF);
                }
            }

            return NativeBuffer;
        }

        public override ReadOnlySpan<byte> EncodeElement(ArrangerElement el, ColorRgba32[,] imageBuffer)
        {
            if (imageBuffer.GetLength(0) != Width || imageBuffer.GetLength(1) != Height)
                throw new ArgumentException(nameof(imageBuffer));

            var bs = BitStream.OpenWrite(StorageSize, 8);

            for (int y = 0; y < el.Height; y++)
            {
                for (int x = 0; x < el.Width; x++)
                {
                    var imageColor = imageBuffer[x, y];
                    bs.WriteByte(imageColor.R);
                    bs.WriteByte(imageColor.G);
                    bs.WriteByte(imageColor.B);
                }
            }

            return bs.Data;
        }
    }
}
