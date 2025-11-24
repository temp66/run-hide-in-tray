using System.Runtime.InteropServices;

namespace InteropServices;

// Do not call `SetHandleInvalid` or `Dangerous*`
internal abstract class SafeHandleAlwaysValid : SafeHandle
{
    protected SafeHandleAlwaysValid() : base(0, true) { }

    public sealed override bool IsInvalid => false;
}
