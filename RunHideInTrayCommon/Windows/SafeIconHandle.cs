using InteropServices;

using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;

namespace RunHideInTrayCommon.Windows;

internal class SafeIconHandle : SafeHandleZeroIsInvalid
{
    public SafeIconHandle() : base(true)
    {
    }

    public SafeIconHandle(nint hIcon) : base(true)
    {
        SetHandle(hIcon);
    }

    protected override bool ReleaseHandle()
    {
        return PInvoke.DestroyIcon((HICON)handle);
    }
}
