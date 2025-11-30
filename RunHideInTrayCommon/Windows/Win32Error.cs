using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Windows;

internal static class Win32Error
{
    public static Win32Exception CreateExceptionFromError(string functionName, int error)
    {
        Win32Exception ex = new(error, $"{functionName}: {Marshal.GetPInvokeErrorMessage(error)}");
        ex.Data["FunctionName"] = functionName;
        return ex;
    }

    public static Win32Exception CreateExceptionFromLastError(string functionName)
    {
        return CreateExceptionFromError(functionName, Marshal.GetLastPInvokeError());
    }
}
