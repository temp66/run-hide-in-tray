using RunHideInTray.Diagnostics;
using String.Quoting;

namespace RunHideInTray;

internal record class Config : IDisposable
{
    public const int IconLargestSize = 256;

    public int HideOnStartTimeout { get; init; }

    public string Title { get; init; }

    public readonly Icon Icon;

    public string Exec { get; init; }

    bool _disposed = false;

    public Config(int hideOnStartTimeout, string? title, FileInfo? iconFileInfo, string[] exec)
    {
        HideOnStartTimeout = hideOnStartTimeout;
        try
        {
            Exec = CommandLineQuoting.Quoted(exec);
        }
        catch
        {
            Dispose();
            throw;
        }
        Title = title ?? $"{nameof(RunHideInTray)} - {Exec}";
        FileStream? iconFileStream;
        try
        {
            iconFileStream = iconFileInfo?.OpenRead();
        }
        catch (SystemException ex)
        {
            ExceptionReporter.ToConsole("Failed to open icon file", ex);
            iconFileStream = null;
        }
        using (iconFileStream)
        {
            if (iconFileStream is null)
                Icon = SystemIcons.GetStockIcon(StockIconId.Application);
            else
                try
                {
                    Icon = new(iconFileStream, IconLargestSize, IconLargestSize);
                }
                catch (ArgumentException ex)
                {
                    ExceptionReporter.ToConsole("Failed to load icon from file", ex);
                    Icon = SystemIcons.GetStockIcon(StockIconId.Application);
                }
        }
    }

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
            Icon?.Dispose();
        _disposed = true;
    }
}
