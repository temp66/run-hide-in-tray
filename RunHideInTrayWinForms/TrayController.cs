extern alias RunHideInTrayCommon;

using RunHideInTrayCommon::RunHideInTrayCommon;
using RunHideInTrayCommon::String;
using RunHideInTrayCommon::Windows;
using RunHideInTrayWinForms.Diagnostics;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;

namespace RunHideInTrayWinForms;

internal class TrayController : IDisposable
{
    const int NotifyIconTextMaxLength = 127;

    NotifyIcon _notifyIcon;
    ContextMenuStrip _contextMenuStrip;
    ToolStripMenuItem _hideMenuItem;
    ToolStripMenuItem _showMenuItem;
    ToolStripSeparator _toolStripSeparator = new();
    ToolStripMenuItem _exitMenuItem;

    ProcessDescendantsMonitor _processDescendantsMonitor;
    BindingList<HWND> _hiddenWindowHandleList = [];

    bool _disposed = false;

    public TrayController(Config config, ProcessDescendantsMonitor processDescendantsMonitor)
    {
        _hideMenuItem = new("Hide", null, (sender, eventArgs) => HideWindows());

        _showMenuItem = new("Show", null, (sender, eventArgs) => ShowWindows())
        {
            Enabled = false,
        };
        _hiddenWindowHandleList.ListChanged += (sender, eventArgs) => _showMenuItem.Enabled = _hiddenWindowHandleList.Count != 0;

        _exitMenuItem = new("Exit", null, OnExitMenuItemClick);

        _contextMenuStrip = new()
        {
            Font = SystemFonts.IconTitleFont,
            AllowTransparency = true,
        };
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        _contextMenuStrip.Items.AddRange(_hideMenuItem, _showMenuItem, _toolStripSeparator, _exitMenuItem);

        _notifyIcon = new()
        {
            ContextMenuStrip = _contextMenuStrip,
            Text = Truncating.Ellipsis(config.Title, NotifyIconTextMaxLength),
            Icon = config.Icon,
            Visible = true,
        };
        _notifyIcon.MouseClick += (sender, eventArgs) =>
        {
            if (eventArgs.Button == MouseButtons.Left)
                ToggleWindowsVisibility();
        };

        _processDescendantsMonitor = processDescendantsMonitor;

        if (config.HideOnStartTimeout >= 0)
            HideWindows(config.HideOnStartTimeout);
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
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        if (disposing)
        {
            _notifyIcon.Dispose();
            _contextMenuStrip.Dispose();
        }
        _disposed = true;
    }

    void OnExitMenuItemClick(object? sender, EventArgs eventArgs)
    {
        ShowWindows();

        bool closedAny = false;
        try
        {
            _processDescendantsMonitor.EnumerateProcesses((process) =>
            {
                bool result;
                try
                {
                    result = process.CloseMainWindow();
                }
                catch (InvalidOperationException)
                {
                    return;
                }
                closedAny |= result;
            });
        }
        catch (Win32Exception ex)
        {
            ExceptionReporter.ShowMessageBox(ex);
            return;
        }
        if (closedAny)
            return;

        DialogResult dialogResult = MessageBox.Show(
            """
            Failed to close main window: The process does not have a main window or the main window is disabled (for example if a modal dialog is being shown).
            Try killing the process?
            """,
            ApplicationInfo.Name,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning
        );
        if (dialogResult == DialogResult.No)
            return;

        try
        {
            _processDescendantsMonitor.KillAllProcesses();
        }
        catch (Win32Exception ex)
        {
            ExceptionReporter.ShowMessageBox(ex);
        }
    }

    void ToggleWindowsVisibility()
    {
        if (_showMenuItem.Enabled)
            ShowWindows();
        else
            HideWindows();
    }

    void HideWindows(int waitTimeout = 0)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        do
        {
            try
            {
                _processDescendantsMonitor.EnumerateProcesses((process) =>
                {
                    try
                    {
                        process.WaitForInputIdle((int)long.Max(waitTimeout - stopwatch.ElapsedMilliseconds, 0));
                    }
                    catch (InvalidOperationException)
                    {
                        // In the case of consoles, `Process.MainWindowHandle` may still be valid.
                        // https://stackoverflow.com/questions/65528642/callback-for-console-window-closing-of-other-process-assuming-the-hwnd-of-the#comment115854186_65528642
                        // https://github.com/microsoft/terminal/issues/11823#issuecomment-979185489
                        // return;
                    }
                    HWND mainWindowHandle;
                    try
                    {
                        mainWindowHandle = (HWND)process.MainWindowHandle;
                    }
                    catch (InvalidOperationException)
                    {
                        return;
                    }
                    if (mainWindowHandle == HWND.Null)
                        return;

                    PInvoke.ShowWindow(mainWindowHandle, SHOW_WINDOW_CMD.SW_HIDE);
                    // When invoked by a GUI event, here the foreground window likely belongs to explorer.
                    // When invoked during startup, here the foreground window likely belongs to `process`.
                    // It is not easy to return to previous foreground window.
                    // https://stackoverflow.com/questions/621998/restoring-window-focus-back-to-previous-owner
                    // https://stackoverflow.com/questions/1041532/prevent-system-tray-icon-from-stealing-focus-when-clicked
                    // https://stackoverflow.com/questions/13659970/how-to-focus-on-last-activated-program/13660585#13660585
                    // https://stackoverflow.com/questions/55484749/how-to-prevent-the-current-window-from-losing-focus-when-clicking-a-system-tray
                    // https://learn.microsoft.com/en-us/answers/questions/966217/how-to-get-the-hwnd-of-the-next-window-that-will-b
                    // This requires the message loop to have already started. Use `keybd_event` instead.
                    // SendKeys.Send("%{ESC}");
                    _hiddenWindowHandleList.Add(mainWindowHandle);
                });
            }
            catch (Win32Exception ex)
            {
                ExceptionReporter.ShowMessageBox(ex);
                return;
            }

            if (_showMenuItem.Enabled)
                break;
            Thread.Sleep(10);
        }
        while (stopwatch.ElapsedMilliseconds < waitTimeout);
    }

    void ShowWindows()
    {
        foreach (HWND hiddenWindowHandle in _hiddenWindowHandleList)
        {
            PInvoke.ShowWindow(hiddenWindowHandle, SHOW_WINDOW_CMD.SW_SHOW);
            if (PInvoke.IsIconic(hiddenWindowHandle))
                PInvoke.OpenIcon(hiddenWindowHandle);
            else
                PInvoke.SetForegroundWindow(hiddenWindowHandle);
        }
        _hiddenWindowHandleList.Clear();
    }

    void OnUserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs eventArgs)
    {
        if (eventArgs.Category == UserPreferenceCategory.Window)
            _contextMenuStrip.Font = SystemFonts.IconTitleFont;
    }
}
