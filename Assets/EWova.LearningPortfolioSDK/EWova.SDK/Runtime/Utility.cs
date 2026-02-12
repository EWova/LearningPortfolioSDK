using System;

using UnityEngine;
using UnityEngine.Experimental.Rendering;

public static class Utility
{
    private static readonly string[] SizeSuffixes = { "Byte", "KB", "MB", "GB", "TB", "PB", "EB" };
    private const long KB = 1024;

    public struct MemorySize
    {
        public static readonly MemorySize Zero = default;
        public long NativeSize;
        public long GraphicsSize;
        public readonly long RuntimeEstimateSize => NativeSize + GraphicsSize;
        public MemorySize(long nativeSize, long graphicsSize) : this()
        {
            NativeSize = nativeSize;
            GraphicsSize = graphicsSize;
        }

    }
    public enum MemorySizeType
    {
        NativeSize,
        GraphicsSize
    }

    public static string ToHumanReadableSize(this in long size)
    {
        if (size <= 0)
            return "0Byte";

        int magnitude = (int)Math.Log(size, KB);
        double adjustedSize = size / Math.Pow(KB, magnitude);

        return $"{Math.Round(adjustedSize, 2):##.##}{SizeSuffixes[magnitude]}";
    }
    public static string ToHumanReadableSize(this in ulong size)
    {
        if (size == ulong.MinValue)
            return "0Byte";

        int magnitude = (int)Math.Log(size, KB);
        double adjustedSize = size / Math.Pow(KB, magnitude);

        return $"{Math.Round(adjustedSize, 2):##.##}{SizeSuffixes[magnitude]}";
    }


    public static string ToHumanReadableSize(this byte[] size)
    {
        return ToHumanReadableSize(size.LongLength);
    }

    public static float[] ConvertFrom16Bit(this byte[] array)
    {
        const int SizeOf = sizeof(short);//2

        if (array.Length % SizeOf != 0)
            throw new ArgumentException("The length of the byte array is not a multiple of target byte size.");

        int convSize = array.Length / SizeOf;
        float[] floatArr = new float[convSize];

        const float Max = short.MaxValue;
        for (int i = 0; i < convSize; i++)
            floatArr[i] = BitConverter.ToInt16(array, i * SizeOf) / Max;

        return floatArr;
    }
    public static float[] ConvertFrom16BitUnsigned(this byte[] array)
    {
        const int SizeOf = sizeof(ushort);//2

        if (array.Length % SizeOf != 0)
            throw new ArgumentException("The length of the byte array is not a multiple of target byte size.");

        int convSize = array.Length / SizeOf;
        float[] floatArr = new float[convSize];

        const float Max = ushort.MaxValue;
        for (int i = 0; i < convSize; i++)
            floatArr[i] = BitConverter.ToUInt16(array, i * SizeOf) / Max;

        return floatArr;
    }
    public static float[] ConvertFrom32Bit(this byte[] array)
    {
        const int SizeOf = sizeof(float);//4

        if (array.Length % SizeOf != 0)
            throw new ArgumentException("The length of the byte array is not a multiple of target byte size.");

        int convSize = array.Length / SizeOf;
        float[] floatArr = new float[convSize];

        for (int i = 0; i < convSize; i++)
            floatArr[i] = BitConverter.ToSingle(array, i * SizeOf);

        return floatArr;
    }

    public static long CalcUnityObjectNativeSizeAny<T>(this T obj) where T : UnityEngine.Object
            => obj switch
            {
                Texture2D Texture2D => Texture2D.CalcUnityObjectNativeSize().RuntimeEstimateSize,
                Mesh Mesh => Mesh.CalcUnityObjectNativeSize(),
                AudioClip AudioClip => AudioClip.CalcUnityObjectNativeSize(),
                _ => 0L
            };

    public static MemorySize CalcUnityObjectNativeSize(this Texture2D obj)
    {
        if (obj == null)
            return MemorySize.Zero;

        return new MemorySize()
        {
            GraphicsSize = CalcUnityObjectNativeSizeTexture(obj.width, obj.height, obj.mipmapCount, obj.graphicsFormat, MemorySizeType.GraphicsSize),
            NativeSize = !obj.isReadable ? 0L : CalcUnityObjectNativeSizeTexture(obj.width, obj.height, obj.mipmapCount, obj.graphicsFormat, MemorySizeType.NativeSize)
        };
    }

    public static long CalcUnityObjectNativeSize(this Mesh obj)
    {
        if (obj == null)
            return 0L;

        if (!obj.isReadable)
            return 0L;

        if (obj.vertexCount <= 0)
            return 0L;

        long totalByte = 0L;

        bool IsNotNullNotEmpty(Array list) => list != null && list.Length > 0;

        if (IsNotNullNotEmpty(obj.vertices)) { totalByte += 12; }
        if (IsNotNullNotEmpty(obj.normals)) { totalByte += 12; }
        if (IsNotNullNotEmpty(obj.tangents)) { totalByte += 16; }
        if (IsNotNullNotEmpty(obj.uv)) { totalByte += 8; }
        if (IsNotNullNotEmpty(obj.uv2)) { totalByte += 8; }
        if (IsNotNullNotEmpty(obj.uv3)) { totalByte += 8; }
        if (IsNotNullNotEmpty(obj.uv4)) { totalByte += 8; }
        if (IsNotNullNotEmpty(obj.uv5)) { totalByte += 8; }
        if (IsNotNullNotEmpty(obj.uv6)) { totalByte += 8; }
        if (IsNotNullNotEmpty(obj.uv7)) { totalByte += 8; }
        if (IsNotNullNotEmpty(obj.uv8)) { totalByte += 8; }
        if (IsNotNullNotEmpty(obj.colors)) { totalByte += 16; }
        if (IsNotNullNotEmpty(obj.triangles)) { totalByte += 4; }
        if (IsNotNullNotEmpty(obj.boneWeights)) { totalByte += 20; }
        totalByte = totalByte * obj.vertexCount;

        for (int i = 0; i < obj.subMeshCount; i++)
        {
            UnityEngine.Rendering.SubMeshDescriptor subMesh = obj.GetSubMesh(i);
            totalByte += subMesh.indexCount * 4;
        }

        if (obj.isReadable) totalByte *= 2;

        return totalByte;
    }

    public static long CalcUnityObjectNativeSize(this AudioClip obj)
    {
        if (obj == null)
            return 0L;

        // 計算記憶體使用量 = 頻道數 * 取樣率 * 長度 * 每個樣本的大小 (假設PCM格式，每個樣本通常是2位元組)
        int channels = obj.channels;         // 頻道數
        int frequency = obj.frequency;       // 取樣率 (Hz)
        float length = obj.length;           // 長度 (秒)
        int sampleSize = 2;                  // 每個樣本大小 (假設PCM 16位 = 2 bytes)

        return (long)(channels * frequency * length * sampleSize);
    }

    public static long CalcUnityObjectNativeSizeTexture(int width, int height, int mipmapCount, GraphicsFormat format, MemorySizeType memorySizeType)
    {
        if (memorySizeType is MemorySizeType.GraphicsSize)
        {
            width = Mathf.NextPowerOfTwo(width);
            height = Mathf.NextPowerOfTwo(height);
        }
        
        long blockPixel = (width / GraphicsFormatUtility.GetBlockWidth(format)) * (height / GraphicsFormatUtility.GetBlockHeight(format));
        long blockBitsPerPixel = GraphicsFormatUtility.GetBlockSize(format);
        double mipmapFactor = mipmapCount <= 1 ? 1.0 : (4.0 / 3.0);
        //if (mipmapCount != 0) for (uint i = 0; i < mipmapCount; i++) mipmapFactor += mipmapFactor * 0.25f;

        return (long)(blockPixel * blockBitsPerPixel * mipmapFactor);
    }
}
