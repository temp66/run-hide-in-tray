extern alias RunHideInTrayCommon;

using RunHideInTrayCommon::RunHideInTrayCommon;
using RunHideInTrayCommon::RunHideInTrayCommon.Json;
using RunHideInTrayCommon::Windows;
using RunHideInTrayWinForms.Diagnostics;

using RunHideInTrayCommon::Windows.Win32;

using System.ComponentModel;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;

namespace RunHideInTrayWinForms;

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

        // https://stackoverflow.com/questions/7572995/how-can-i-get-winforms-to-stop-silently-ignoring-unhandled-exceptions
        AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
        {
            ExceptionReporter.ShowMessageBox((Exception)eventArgs.ExceptionObject);
        };

        if (args.Length != 1)
            return (int)ExitCode.InvalidPipeName;
        NamedPipeClientStream pipeClientStream;
        try
        {
            pipeClientStream = new(".", args[0], PipeDirection.In);
        }
        catch (ArgumentException)
        {
            return (int)ExitCode.InvalidPipeName;
        }
        using (pipeClientStream)
        {
            try
            {
                pipeClientStream.Connect(1000);
            }
            catch (SystemException)
            {
                return (int)ExitCode.InvalidPipeName;
            }

            Config config;
            try
            {
                config = JsonSerializer.Deserialize(pipeClientStream, ConfigJsonSerializerContext.Default.Config)!;
            }
            catch (JsonException)
            {
                return (int)ExitCode.InvalidJson;
            }
            using (config)
                return Main_(config);
        }
    }

    static int Main_(Config config)
    {
        ProcessDescendantsMonitor processDescendantsMonitor;
        try
        {
            processDescendantsMonitor = new(config.Exec);
        }
        catch (Win32Exception ex)
        {
            ExceptionReporter.ShowMessageBox(ex);
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
            int? exitCode = null;
            processDescendantsMonitor.AllExited += (sender, eventArgs) =>
            {
                Application.Exit();
                try
                {
                    exitCode = ((ProcessDescendantsMonitor)sender!).GetRootProcessExitCode();
                }
                catch (Win32Exception ex)
                {
                    ExceptionReporter.ShowMessageBox(ex);
                    exitCode = (int)ExitCode.ErrorGetExitCodeProcess;
                }
            };
            processDescendantsMonitor.Faulted += (sender, eventArgs) =>
            {
                ExceptionReporter.ShowMessageBox(eventArgs);
                Application.Exit();
                exitCode = (int)ExitCode.ErrorGetQueuedCompletionStatus;
            };
            // Do not run message loops betweeen `ProcessDescendantsMonitor.WaitForAllProcessesAsync` and `Application.Run`.
            // Pitfall: `MessageBox.Show` runs a message loop.
            using Control synchronizingObject = new();
            // https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.control.invokerequired?view=windowsdesktop-10.0#remarks
            _ = synchronizingObject.Handle;
            // https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.dispose?view=net-10.0#system-threading-tasks-task-dispose
            // `Task.Dispose` may throw.
            /* using Task waitForAllProcesses */ _ = processDescendantsMonitor.WaitForAllProcessesAsync(synchronizingObject);

            using TrayController trayController = new(config, processDescendantsMonitor);
            Application.Run();
            return (int)exitCode!;
        }
    }
}
