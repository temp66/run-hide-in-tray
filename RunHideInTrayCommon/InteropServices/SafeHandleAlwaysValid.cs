using System.Runtime.InteropServices;

namespace InteropServices;

// Do not call `SetHandleInvalid` or `Dangerous*`
public abstract class SafeHandleAlwaysValid : SafeHandle
{
    protected SafeHandleAlwaysValid() : base(0, true) { }

    public sealed override bool IsInvalid => false;
}
