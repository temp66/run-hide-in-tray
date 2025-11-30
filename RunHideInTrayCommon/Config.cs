using System.Drawing;

namespace RunHideInTrayCommon;

public class Config : IDisposable
{
    public const int IconLargestSize = 256;

    public required bool elevate;

    public required int HideOnStartTimeout;

    public required string Title;

    public required Icon Icon { get; init; }

    public required string Exec;

    bool _disposed = false;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;
        if (disposing)
            Icon.Dispose();
        _disposed = true;
    }
}
