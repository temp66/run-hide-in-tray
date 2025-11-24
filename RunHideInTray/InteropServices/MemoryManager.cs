using System.Runtime.InteropServices;

namespace InteropServices;

internal class MemoryManager : SafeHandleAlwaysValid
{
    public unsafe void* Ptr { get; private set; }

    public nuint Size { get; private set; }

    public MemoryManager(nuint size)
    {
        unsafe
        {
            Ptr = NativeMemory.Alloc(size);
        }
        Size = size;
    }

    public void Realloc(nuint size)
    {
        unsafe
        {
            Ptr = NativeMemory.Realloc(Ptr, size);
        }
        Size = size;
    }

    protected override bool ReleaseHandle()
    {
        unsafe
        {
            NativeMemory.Free(Ptr);
        }
        return true;
    }
}
