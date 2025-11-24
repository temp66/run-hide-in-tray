using RunHideInTray.Diagnostics;
using Windows;

using System.CommandLine;
using System.ComponentModel;
using System.Diagnostics;
using Windows.Win32;

namespace RunHideInTray;

internal static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static int Main(string[] args)
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        Application.SetColorMode(SystemColorMode.System);

        ConsoleGetter consoleGetter;
        try
        {
            consoleGetter = new();
        }
        catch (Win32Exception ex)
        {
            ExceptionReporter.ShowMessageBox(ex);
            return (int)ExitCode.ErrorAllocConsole;
        }
        using (consoleGetter)
        {
            // Do not know why, but no longer needed
            // // https://stackoverflow.com/questions/7572995/how-can-i-get-winforms-to-stop-silently-ignoring-unhandled-exceptions
            // AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
            // {
            //     ExceptionReporter.ToConsole((Exception)eventArgs.ExceptionObject);
            // };

            Option<int> hideOnStartTimeoutOption = new("--hide-on-start-timeout")
            {
                Description = """
                    Try hiding window on start. Specify the wait timeout in milliseconds.
                    Negative value disables hiding on start.
                    Zero enables hiding on start but does not wait.
                    """,
                DefaultValueFactory = (argumentResult) => -1,
            };
            Option<string> titleOption = new("--title")
            {
                Description = $"Title for system tray icon. [default: {nameof(RunHideInTray)} - <exec>...]",
            };
            Option<FileInfo> iconOption = new("--icon")
            {
                Description = "Path to the icon for system tray icon.",
            };
            iconOption.AcceptExistingOnly();
            Argument<string[]> execArgument = new("exec")
            {
                Description = "The executable to run, and possibly arguments to it",
                Arity = ArgumentArity.OneOrMore,
            };
            RootCommand rootCommand = new("Run the executable, and hide the window of it in system tray.")
            {
                hideOnStartTimeoutOption,
                titleOption,
                iconOption,
                execArgument,
            };

            rootCommand.SetAction((parseResult) =>
            {
                Config config;
                try
                {
                    config = new(
                        hideOnStartTimeout: parseResult.GetValue(hideOnStartTimeoutOption),
                        title: parseResult.GetValue(titleOption),
                        iconFileInfo: parseResult.GetValue(iconOption),
                        exec: parseResult.GetRequiredValue(execArgument)
                    );
                }
                catch (ApplicationException ex)
                {
                    ExceptionReporter.ToConsole("In <exec>...", ex);
                    return (int)ExitCode.InvalidFileName;
                }
                using (config)
                    return Main_(config, consoleGetter);
            });
            return rootCommand.Parse(args).Invoke();
        }
    }

    static int Main_(Config config, ConsoleGetter consoleGetter)
    {
        // Do not run message loop betweeen `processDescendantsMonitor` constructor and `Application.Run`.
        // Pitfall: `MessageBox.Show` runs a message loop.
        using Control synchronizingObject = new();
        // https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.control.invokerequired?view=windowsdesktop-9.0#remarks
        _ = synchronizingObject.Handle;
        ProcessDescendantsMonitor processDescendantsMonitor;
        try
        {
            processDescendantsMonitor = new(config.Exec, synchronizingObject);
        }
        catch (Win32Exception ex)
        {
            ExceptionReporter.ToConsole(ex);
            return (int)(ex.Data["FunctionName"] switch
            {
                nameof(PInvoke.CreateJobObject) => ExitCode.ErrorCreateJobObject,
                nameof(PInvoke.CreateIoCompletionPort) => ExitCode.ErrorCreateIoCompletionPort,
                nameof(PInvoke.SetInformationJobObject) => ExitCode.ErrorSetInformationJobObject,
                nameof(PInvoke.CreateProcess) => ExitCode.ErrorCreateProcess,
                nameof(PInvoke.AssignProcessToJobObject) => ExitCode.ErrorAssignProcessToJobObject,
                nameof(PInvoke.ResumeThread) => ExitCode.ErrorResumeThread,
                _ => throw new UnreachableException(),
            });
        }
        using (processDescendantsMonitor)
        {
            processDescendantsMonitor.AllExited += (sender, eventArgs) => Application.Exit();
            processDescendantsMonitor.Faulted += (sender, eventArgs) =>
            {
                ExceptionReporter.ShowMessageBox(eventArgs);
                Application.Exit();
            };
            // `Task.Dispose` may throw.
            /* using */
            Task waitForAllProcesses = processDescendantsMonitor.WaitForAllProcessesAsync();
            using TrayController trayController = new(config, processDescendantsMonitor);

            // Not working if run through `dotnet run`
            consoleGetter.Dispose();
            Application.Run();
        }
        return (int)ExitCode.Success;
    }
}
