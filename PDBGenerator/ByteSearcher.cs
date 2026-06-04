using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86; // Or System.Runtime.Intrinsics.Arm for ARM64

/// <summary>
/// Represents a difference found between two byte arrays.
/// </summary>
/// <param name="Index">The byte index where the difference occurs.</param>
/// <param name="Description">A description of the difference.</param>
public record ByteDifference(int Index, string Description);

public static class ByteSearcher
{
    public static List<int> FindByteIndices(byte[] data, byte target)
    {
        var indices = new List<int>();
        int i = 0;
        int length = data.Length;

        // Use SIMD if supported by the hardware (processes 32 bytes at a time)
        if (Vector256.IsHardwareAccelerated && length >= Vector256<byte>.Count)
        {
            int vectorSize = Vector256<byte>.Count;
            int loopLimit = length - vectorSize;
            Vector256<byte> targetVector = Vector256.Create(target);

            for (; i <= loopLimit; i += vectorSize)
            {
                // Load 32 bytes from memory
                var currentVector = Vector256.LoadUnsafe(ref data[i]);

                // Compare all 32 bytes with the target simultaneously
                var matchVector = Vector256.Equals(currentVector, targetVector);

                // Extract a 32-bit bitmask where each set bit corresponds to a matching index
                uint mask = matchVector.ExtractMostSignificantBits();

                if (mask != 0)
                {
                    // Extract match offsets using BitOperations.TrailingZeroCount
                    int baseIndex = i;
                    while (mask != 0)
                    {
                        int bitIndex = System.Numerics.BitOperations.TrailingZeroCount(mask);
                        indices.Add(baseIndex + bitIndex);
                        mask &= mask - 1; // Clear the lowest set bit
                    }
                }
            }
        }

        // Clean up the remaining bytes (or fallback if SIMD isn't accelerated)
        for (; i < length; i++)
        {
            if (data[i] == target)
            {
                indices.Add(i);
            }
        }

        return indices;
    }
    public static List<ByteDifference> CompareByteArrays(byte[] array1, byte[] array2)
    {
        List<ByteDifference> differences = new();

        if (array1.Length != array2.Length)
        {
            differences.Add(new(0, "Array lengths differ: array1={array1.Length}, array2={array2.Length}"));
            return differences;
        }

        if (!Avx2.IsSupported)
        {
            // Fallback to standard comparison if AVX2 is not available
            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] != array2[i])
                {
                    differences.Add(new(i, $"Byte mismatch: array1[{i}]={array1[i]}, array2[{i}]={array2[i]}"));
                }
            }
            return differences;
        }

        int vectorSize = Vector256<byte>.Count; // 32 bytes
        int vectorIterations = array1.Length / vectorSize;

        // Compare full vectors
        for (int i = 0; i < vectorIterations; i++)
        {
            Vector256<byte> v1 = Vector256.Create(array1, i * vectorSize);
            Vector256<byte> v2 = Vector256.Create(array2, i * vectorSize);

            Vector256<byte> compared = Avx2.CompareEqual(v1, v2);
            uint mask = compared.ExtractMostSignificantBits();

            // If any byte differs, mask will have 0s
            if (mask != unchecked((uint)0xFFFFFFFF))
            {
                // Extract mismatch offsets
                uint mismatchMask = ~mask & 0xFFFFFFFF;
                int baseIndex = i * vectorSize;

                while (mismatchMask != 0)
                {
                    int bitIndex = System.Numerics.BitOperations.TrailingZeroCount(mismatchMask);
                    int index = baseIndex + bitIndex;
                    differences.Add(new(index, $"Byte mismatch: array1[{index}]={array1[index]}, array2[{index}]={array2[index]}"));
                    mismatchMask &= mismatchMask - 1; // Clear the lowest set bit
                }
            }
        }

        // Compare remaining bytes (if length is not divisible by 32)
        int remainder = array1.Length % vectorSize;
        for (int i = array1.Length - remainder; i < array1.Length; i++)
        {
            if (array1[i] != array2[i])
            {
                differences.Add(new(i, $"Byte mismatch: array1[{i}]={array1[i]}, array2[{i}]={array2[i]}"));
            }
        }

        return differences;
    }
}
