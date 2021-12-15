﻿using System;

namespace ImageMagitek;

/// <summary>
/// Struct used to store a file address that does not start on a byte-aligned address
/// </summary>
public readonly struct BitAddress : IEquatable<BitAddress>
{
    /// <summary>
    /// Portion of offset in bytes
    /// </summary>
    public long ByteOffset { get; }

    /// <summary>
    /// Number of bits to skip after ByteOffset
    /// Valid range is 0-7 inclusive
    /// A zero value would result in a byte-aligned address
    /// </summary>
    public int BitOffset { get; }

    /// <summary>
    /// Full offset in number of bits
    /// </summary>
    public long Offset => ByteOffset * 8 + BitOffset;

    public BitAddress(long byteOffset, int bitOffset)
    {
        if (bitOffset > 7)
            throw new ArgumentOutOfRangeException($"{nameof(BitAddress)}: {nameof(bitOffset)} {bitOffset} is out of range");

        ByteOffset = byteOffset;
        BitOffset = bitOffset;
    }

    /// <summary>
    /// Construct a new FileBitAddress from the number of bits to the address
    /// </summary>
    /// <param name="bits"></param>
    public BitAddress(long bits)
    {
        ByteOffset = bits / 8;
        BitOffset = (int)(bits % 8);
    }

    public bool Equals(BitAddress other) =>
        ByteOffset == other.ByteOffset && BitOffset == other.BitOffset;

    public override bool Equals(object obj) =>
        Equals((BitAddress)obj);

    public override int GetHashCode() => HashCode.Combine(BitOffset, ByteOffset);

    public static BitAddress Zero => new BitAddress(0, 0);

    public static bool operator ==(BitAddress lhs, BitAddress rhs) =>
        lhs.Equals(rhs);

    public static bool operator !=(BitAddress lhs, BitAddress rhs) =>
        !lhs.Equals(rhs);

    /// <summary>
    /// Adds two FileBitAddress objects and returns the result
    /// </summary>
    /// <param name="lhs"></param>
    /// <param name="rhs"></param>
    /// <returns></returns>
    public static BitAddress operator +(BitAddress lhs, BitAddress rhs)
    {
        long bits = lhs.Offset + rhs.Offset;
        return new BitAddress(bits);
    }

    /// <summary>
    /// Adds a specified number of bits to a FileBitAddress object
    /// </summary>
    /// <param name="lhs"></param>
    /// <param name="offset">Number of bits to advance the address</param>
    /// <returns></returns>
    public static BitAddress operator +(BitAddress lhs, long offset)
    {
        long bits = lhs.Offset + offset;
        return new BitAddress(bits);
    }

    /// <summary>
    /// Subtracts two FileBitAddress objects and returns the result
    /// </summary>
    /// <param name="lhs"></param>
    /// <param name="rhs"></param>
    /// <returns></returns>
    public static BitAddress operator -(BitAddress lhs, BitAddress rhs)
    {
        long bits = lhs.Offset - rhs.Offset;

        return new BitAddress(bits);
    }

    /// <summary>
    /// Subtracts a number of bits from a FileBitAddress object
    /// </summary>
    /// <param name="lhs"></param>
    /// <param name="offset">Offset in number of bits</param>
    /// <returns></returns>
    public static BitAddress operator -(BitAddress lhs, long offset)
    {
        long retbits = lhs.Offset - offset;

        return new BitAddress(retbits);
    }

    public static bool operator <(BitAddress lhs, BitAddress rhs)
    {
        return lhs.Offset < rhs.Offset;
    }

    public static bool operator <=(BitAddress lhs, BitAddress rhs)
    {
        return lhs.Offset <= rhs.Offset;
    }

    public static bool operator >(BitAddress lhs, BitAddress rhs)
    {
        return lhs.Offset > rhs.Offset;
    }
    public static bool operator >=(BitAddress lhs, BitAddress rhs)
    {
        return lhs.Offset >= rhs.Offset;
    }
}