﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ImageMagitek.Codec;
using ImageMagitek.Colors;
using ImageMagitek.Project;

namespace ImageMagitek
{
    /// <summary>
    /// Mode for the Arranger
    /// SequentialArrangers are for simple sequential file access
    /// ScatteredArrangers are capable of accessing many files, file offsets, palettes, and codecs in a single arranger
    /// MemoryArrangers are used as a scratchpad (currently unimplemented)
    /// </summary>
    public enum ArrangerMode { Sequential = 0, Scattered, Memory };

    /// <summary>
    /// Layout of graphics for the arranger
    /// Each layout directs Arranger element selection and Arranger cloning to perform differently
    /// Tiled will snap selection rectangles to tile boundaries
    /// Single will snap selection rectangles to pixel boundaries
    /// </summary>
    public enum ArrangerLayout { Tiled = 0, Single };

    /// <summary>
    /// Specifies how the pixels' colors are determined for the graphic
    /// Indexed graphics have their full color determined by a palette
    /// Direct graphics have their full color determined by the pixel image data alone
    /// </summary>
    public enum PixelColorType { Indexed = 0, Direct }

    /// <summary>
    /// Move operations for sequential arrangers
    /// </summary>
    public enum ArrangerMoveType { ByteDown = 0, ByteUp, RowDown, RowUp, ColRight, ColLeft, PageDown, PageUp, Home, End, Absolute };

    /// <summary>
    /// Arranger base class for graphical screen elements
    /// </summary>
    public abstract class Arranger : IProjectResource
    {
        /// <summary>
        /// Individual Elements that compose the Arranger
        /// </summary>
        protected ArrangerElement[,] ElementGrid { get; set; }

        /// <summary>
        /// Gets the size of the entire Arranger in Element coordinates
        /// </summary>
        public Size ArrangerElementSize { get; protected set; }

        /// <summary>
        /// Gets the Size of the entire Arranger in pixel coordinates
        /// </summary>
        public Size ArrangerPixelSize { get => new Size(ArrangerElementSize.Width * ElementPixelSize.Width, ArrangerElementSize.Height * ElementPixelSize.Height); }

        /// <summary>
        /// Gets the size of an individual Element in pixels
        /// </summary>
        public Size ElementPixelSize { get; protected set; }

        /// <summary>
        /// Gets the Mode of the Arranger
        /// </summary>
        public ArrangerMode Mode { get; protected set; }

        /// <summary>
        /// Gets the ArrangerLayout of the Arranger
        /// </summary>
        public ArrangerLayout Layout { get; protected set; }

        /// <summary>
        /// Gets the ColorType of the Arranger
        /// </summary>
        public PixelColorType ColorType { get; protected set; }

        public string Name { get; set; }

        public bool CanContainChildResources => false;

        public abstract bool ShouldBeSerialized { get; set; }

        public abstract void Resize(int arrangerWidth, int arrangerHeight);
        public abstract void Resize(int arrangerWidth, int arrangerHeight, Func<int, int, ArrangerElement> elementFactory);

        private readonly BlankDirectCodec _blankDirectCodec = new BlankDirectCodec();
        private readonly BlankIndexedCodec _blankIndexedCodec = new BlankIndexedCodec();

        /// <summary>
        /// Clones the Arranger
        /// </summary>
        /// <returns></returns>
        public virtual Arranger CloneArranger()
        {
            if (Layout == ArrangerLayout.Tiled || Layout == ArrangerLayout.Single)
                return CloneArranger(0, 0, ArrangerPixelSize.Width, ArrangerPixelSize.Height);
            else
                throw new NotSupportedException($"{nameof(CloneArranger)} with {nameof(ArrangerLayout)} '{Layout}' is not supported");
        }

        /// <summary>
        /// Clones a subsection of the Arranger
        /// </summary>
        /// <param name="pixelX">Left edge of Arranger in pixel coordinates</param>
        /// <param name="pixelY">Top edge of Arranger in pixel coordinates</param>
        /// <param name="width">Width of Arranger in pixels</param>
        /// <param name="height">Height of Arranger in pixels</param>
        /// <returns></returns>
        public virtual Arranger CloneArranger(int pixelX, int pixelY, int width, int height)
        {
            if (pixelX < 0 || pixelX + width > ArrangerPixelSize.Width || pixelY < 0 || pixelY + height > ArrangerPixelSize.Height)
                throw new ArgumentOutOfRangeException($"{nameof(CloneArranger)} parameters ({nameof(pixelX)}: {pixelX}, {nameof(pixelY)}: {pixelY}, {nameof(width)}: {width}, {nameof(height)}: {height})" +
                    $" were outside of the bounds of arranger '{Name}' of size (width: {ArrangerPixelSize.Width}, height: {ArrangerPixelSize.Height})");

            if (Layout == ArrangerLayout.Single)
            {
                if (pixelX != 0 || pixelY != 0 || width != ArrangerPixelSize.Width || height != ArrangerPixelSize.Height)
                    throw new InvalidOperationException($"{nameof(CloneArranger)} of an Arranger with ArrangerLayout of Single must have the same dimensions as the original");
                return CloneArrangerCore(pixelX, pixelY, width, height);
            }

            return CloneArrangerCore(pixelX, pixelY, width, height);
        }

        protected abstract Arranger CloneArrangerCore(int posX, int posY, int width, int height);

        /// <summary>
        /// Sets Element to a position in the Arranger ElementGrid
        /// </summary>
        /// <param name="element">Element to be placed into the ElementGrid</param>
        /// <param name="posX">x-coordinate in Element coordinates</param>
        /// <param name="posY">y-coordinate in Element coordinates</param>
        public virtual void SetElement(in ArrangerElement element, int posX, int posY)
        {
            if (ElementGrid is null)
                throw new NullReferenceException($"{nameof(SetElement)} property '{nameof(ElementGrid)}' was null");

            if (posX >= ArrangerElementSize.Width || posY >= ArrangerElementSize.Height)
                throw new ArgumentOutOfRangeException($"{nameof(SetElement)} parameter was out of range: ({posX}, {posY})");

            if (element.Codec is object)
                if (element.Codec.ColorType != ColorType)
                    throw new ArgumentException($"{nameof(SetElement)} parameter '{nameof(element)}' did not match the Arranger's {nameof(PixelColorType)}");

            //if (ColorType == PixelColorType.Indexed && element.Palette is null && element.DataFile is object)
            //    throw new ArgumentException($"{nameof(SetElement)} parameter '{nameof(element)}' does not contain a palette");

            var relocatedElement = element.WithLocation(posX * ElementPixelSize.Width, posY * ElementPixelSize.Height);
            ElementGrid[posX, posY] = relocatedElement;
        }

        /// <summary>
        /// Resets the Element to a default state at the given position in the Arranger ElementGrid
        /// </summary>
        /// <param name="posX"></param>
        /// <param name="posY"></param>
        public virtual void ResetElement(int posX, int posY)
        {
            if (ElementGrid is null)
                throw new NullReferenceException($"{nameof(ResetElement)} property '{nameof(ElementGrid)}' was null");

            if (posX >= ArrangerElementSize.Width || posY >= ArrangerElementSize.Height)
                throw new ArgumentOutOfRangeException($"{nameof(ResetElement)} parameter was out of range: ({posX}, {posY})");

            var el = GetElement(posX, posY);

            if (ColorType == PixelColorType.Indexed)
                el = el.WithTarget(null, 0, _blankIndexedCodec, el.Palette);
            else if (ColorType == PixelColorType.Direct)
                el = el = el.WithTarget(null, 0, _blankDirectCodec, null);

            SetElement(el, posX, posY);
        }

        /// <summary>
        /// Gets an Element from a position in the Arranger ElementGrid in Element coordinates
        /// </summary>
        /// <param name="posX">x-coordinate in Element coordinates</param>
        /// <param name="posY">y-coordinate in Element coordinates</param>
        /// <returns></returns>
        public ArrangerElement GetElement(int posX, int posY)
        {
            if (ElementGrid is null)
                throw new NullReferenceException($"{nameof(GetElement)} property '{nameof(ElementGrid)}' was null");

            if (posX >= ArrangerElementSize.Width || posY >= ArrangerElementSize.Height)
                throw new ArgumentOutOfRangeException($"{nameof(GetElement)} parameter was out of range: ({posX}, {posY})");

            return ElementGrid[posX, posY];
        }

        /// <summary>
        /// Gets an Element from a position in the Arranger in pixel coordinates
        /// </summary>
        /// <param name="pixelX">x-coordinate in pixel coordinates</param>
        /// <param name="pixelY">x-coordinate in pixel coordinates</param>
        public ArrangerElement GetElementAtPixel(int pixelX, int pixelY)
        {
            if (ElementGrid is null)
                throw new NullReferenceException($"{nameof(GetElementAtPixel)} property '{nameof(ElementGrid)}' was null");

            if (pixelX >= ArrangerPixelSize.Width || pixelY >= ArrangerPixelSize.Height)
                throw new ArgumentOutOfRangeException($"{nameof(GetElementAtPixel)} parameter was out of range: ({pixelX}, {pixelY})");

            return ElementGrid[pixelX / ElementPixelSize.Width, pixelY / ElementPixelSize.Height];
        }

        /// <summary>
        /// Returns the enumeration of all Elements in the grid in a left-to-right, row-by-row order
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ArrangerElement> EnumerateElements() =>
            EnumerateElements(0, 0, ArrangerElementSize.Width, ArrangerElementSize.Height);

        /// <summary>
        /// Returns the enumeration of a subsection of Elements in the grid in a left-to-right, row-by-row order
        /// </summary>
        /// <param name="elemX">Starting x-coordinate in element coordinates</param>
        /// <param name="elemY">Starting y-coordinate in element coordinates</param>
        /// <param name="width">Number of elements to enumerate in x-direction</param>
        /// <param name="height">Number of elements to enumerate in y-direction</param>
        /// <returns></returns>
        public IEnumerable<ArrangerElement> EnumerateElements(int elemX, int elemY, int width, int height)
        {
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    yield return ElementGrid[x+elemX, y+elemY];
        }

        /// <summary>
        /// Returns the enumeration of a subsection of Elements in the grid in a left-to-right, row-by-row order
        /// </summary>
        /// <param name="x">Starting x-coordinate in pixel coordinates</param>
        /// <param name="y">Starting y-coordinate in pixel coordinates</param>
        /// <param name="width">Width of range in pixels</param>
        /// <param name="height">Height of range in pixels</param>
        /// <returns></returns>
        public IEnumerable<ArrangerElement> EnumerateElementsByPixel(int pixelX, int pixelY, int width, int height)
        {
            if (width <= 0 || height <= 0)
                yield break;

            int elemX = pixelX / ElementPixelSize.Width;
            int elemY = pixelY / ElementPixelSize.Height;
            int elemX2 = (pixelX + width) / ElementPixelSize.Width;
            int elemY2 = (pixelY + height) / ElementPixelSize.Height;

            for (int y = elemY; y < elemY2; y++)
                for (int x = elemX; x < elemX2; x++)
                    yield return GetElement(x, y);
        }

        /// <summary>
        /// Returns the set of distinct Palettes contained by the Arranger's Elements
        /// </summary>
        /// <returns></returns>
        public HashSet<Palette> GetReferencedPalettes()
        {
            return EnumerateElements()
                .Select(x => x.Palette)
                .OfType<Palette>()
                .Distinct()
                .ToHashSet();
        }

        /// <summary>
        /// Returns the set of distinct Codecs contained by the Arranger's Elements
        /// </summary>
        /// <returns></returns>
        public HashSet<IGraphicsCodec> GetReferencedCodecs()
        {
            return EnumerateElements()
                .Select(x => x.Codec)
                .Where(x => !(x is BlankIndexedCodec) && !(x is BlankDirectCodec ))
                .Distinct()
                .ToHashSet();
        }

        public abstract IEnumerable<IProjectResource> LinkedResources { get; }

        public bool UnlinkResource(IProjectResource resource)
        {
            if (resource is Palette palette)
                return UnlinkPalette(palette);
            else if (resource is DataFile df)
                return UnlinkDataFile(df);
            else
                return false;
        }

        private bool UnlinkPalette(Palette palette)
        {
            var isModified = false;

            for (int y = 0; y < ArrangerElementSize.Height; y++)
            {
                for (int x = 0; x < ArrangerElementSize.Width; x++)
                {
                    var el = GetElement(x, y);
                    if (ReferenceEquals(palette, el.Palette))
                    {
                        SetElement(el.WithPalette(default), x, y);
                        isModified = true;
                    }
                }
            }

            return isModified;
        }

        private bool UnlinkDataFile(DataFile dataFile)
        {
            var isModified = false;

            for (int y = 0; y < ArrangerElementSize.Height; y++)
            {
                for (int x = 0; x < ArrangerElementSize.Width; x++)
                {
                    var el = GetElement(x, y);
                    if (ReferenceEquals(dataFile, el.DataFile))
                    {
                        SetElement(el.WithFile(default, 0), x, y);
                        isModified = true;
                    }
                }
            }

            return isModified;
        }
    }
}
