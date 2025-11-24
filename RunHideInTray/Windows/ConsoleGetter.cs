using InteropServices;

using Windows.Win32;

namespace Windows;

internal class ConsoleGetter : SafeHandleAlwaysValid
{
    public ConsoleGetter()
    {
        try
        {
            ConsoleGetterShared.Add();
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    protected override bool ReleaseHandle() => ConsoleGetterShared.Remove();
}

file static class ConsoleGetterShared
{
    static int s_referenceCount;

    public static void Add()
    {
        if (s_referenceCount++ != 0)
            return;
        if (PInvoke.AttachConsole(PInvoke.ATTACH_PARENT_PROCESS))
            return;
        if (!PInvoke.AllocConsole())
            throw Win32Error.CreateExceptionFromLastError(nameof(PInvoke.AllocConsole));
    }

    public static bool Remove()
    {
        if (--s_referenceCount != 0)
            return true;
        return PInvoke.FreeConsole();
    }
}
