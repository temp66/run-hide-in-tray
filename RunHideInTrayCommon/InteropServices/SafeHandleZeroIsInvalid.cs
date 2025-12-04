using System.Runtime.InteropServices;

namespace InteropServices;

internal abstract class SafeHandleZeroIsInvalid : SafeHandle
{
    protected SafeHandleZeroIsInvalid(bool ownsHandle) : base(0, ownsHandle)
    {
    }

    public override bool IsInvalid => handle == 0;
}
