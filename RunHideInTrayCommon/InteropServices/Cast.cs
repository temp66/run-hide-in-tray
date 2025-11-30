using System.Runtime.InteropServices;

namespace InteropServices;

internal static class Cast
{
    public static Span<byte> AsBytes<T>(ref T data) where T : struct
    {
        return MemoryMarshal.AsBytes(new Span<T>(ref data));
    }
}
