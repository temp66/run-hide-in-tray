using RunHideInTrayCommon;
using RunHideInTrayConsole;
using RunHideInTrayConsole.Diagnostics;

using System.CommandLine;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;

namespace RunHideInTrayConsole;

internal class Program
{
    // `Process.UseShellExecute` requires STA.
    [STAThread]
    static int Main(string[] args)
    {
        Option<bool> elevateOption = new("--elevate")
        {
            Description = $"""
                Run {ApplicationInfo.Name} as administrator.
                Required if <exec>... will run as administrator but currently {ApplicationInfo.Name} is not.
                """,            
        };
        Option<int> hideOnStartTimeoutOption = new("--hide-on-start-timeout")
        {
            Description = """
                Try to hide window on start. Specify the wait timeout in milliseconds.
                Negative value disables hiding on start.
                Zero enables hiding on start but does not wait.
                """,
            DefaultValueFactory = (argumentResult) => -1,
        };
        Option<string> titleOption = new("--title")
        {
            Description = $"Title for system tray icon. [default: {ApplicationInfo.Name} - <exec>...]",
        };
        Option<FileInfo> iconOption = new("--icon")
        {
            Description = "Path to the icon for system tray icon.",
        };
        iconOption.AcceptExistingOnly();
        Argument<string[]> execArgument = new("exec")
        {
            Description = "The executable to run, and possibly arguments to it.",
            Arity = ArgumentArity.OneOrMore,
        };
        RootCommand rootCommand = new("Run the executable, and hide the window of it in system tray.")
        {
            elevateOption,
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
                config = Config.Create(
                    elevate: parseResult.GetValue(elevateOption),
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
                return Main_(config);
        });
        return rootCommand.Parse(args).Invoke();
    }

    static int Main_(Config config)
    {
        string pipeName = Guid.NewGuid().ToString();
        using NamedPipeServerStream pipeServerStream = new(pipeName, PipeDirection.Out);
        ProcessStartInfo processStartInfo = new(ApplicationInfo.WinFormsExePath, pipeName)
        {
            UseShellExecute = true,
        };
        if (config.Elevate)
            processStartInfo.Verb = "runas";
        Process? process;
        try
        {
            process = Process.Start(processStartInfo);
        }
        catch (SystemException ex)
        {
            ExceptionReporter.ToConsole(ex);
            return (int)ExitCode.ErrorStartProcess;
        }
        if (process is null)
            return (int)ExitCode.ErrorStartProcess;
        using (process)
        {
            pipeServerStream.WaitForConnection();

            JsonSerializer.Serialize(pipeServerStream, config, ConfigJsonSerializerContext.Default.Config);
            return (int)ExitCode.Success;
        }
    }
}
