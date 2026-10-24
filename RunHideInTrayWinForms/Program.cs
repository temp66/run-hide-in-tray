extern alias RunHideInTrayCommon;

using RunHideInTrayCommon::RunHideInTrayCommon;
using RunHideInTrayCommon::RunHideInTrayCommon.Windows;
using RunHideInTrayWinForms.Diagnostics;

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
        using ProcessDescendantsMonitor processDescendantsMonitor = new(config.Exec);
        int? exitCode = null;
        processDescendantsMonitor.AllExited += (sender, eventArgs) =>
        {
            Application.Exit();
            exitCode = ((ProcessDescendantsMonitor)sender!).GetRootProcessExitCode();
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

        using TrayController trayController = new(config, synchronizingObject, processDescendantsMonitor);
        Application.Run();
        return exitCode!.Value;
    }
}
