using RunHideInTrayCommon;
using RunHideInTrayConsole;
using RunHideInTrayConsole.Diagnostics;

using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
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
        ApplicationConfiguration.Initialize();

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
        RootCommand rootCommand = new("Run an executable, and hide the window of it in system tray.")
        {
            elevateOption,
            hideOnStartTimeoutOption,
            titleOption,
            iconOption,
            execArgument,
        };
        foreach (Option option in rootCommand.Options)
            if (option is HelpOption helpOption)
                helpOption.Action = new CustomHelpAction((HelpAction)helpOption.Action!);

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

    class CustomHelpAction : SynchronousCommandLineAction
    {
        HelpAction _defaultHelpAction;

        public CustomHelpAction(HelpAction defaultHelpAction)
        {
            _defaultHelpAction = defaultHelpAction;
        }

        public override int Invoke(ParseResult parseResult)
        {
            int result = _defaultHelpAction.Invoke(parseResult);

            Console.WriteLine($"""
                Notes:
                  It is recommended to pass <exec>... last, after --.

                  <exec>... is passed to CreateProcess.
                  Proper quoting of arguments is required.
                  File name resolution (from CreateProcess documentation):
                    If the file name does not contain an extension, .exe is appended ... If the file name ends in a period (.) with no extension, or if the file name contains a path, .exe is not appended.
                    If the file name does not contain a directory path, the system searches for the executable file in the following sequence:
                    1. The directory from which the application loaded.
                    2. The current directory for the parent process.
                    3. The 32-bit Windows system directory ...
                    4. The 16-bit Windows system directory ...
                    5. The Windows directory ...
                    6. The directories that are listed in the PATH environment variable. Note that this function does not search the per-application path specified by the App Paths registry key ...
                  * Current working directory is inherited.
                  * Environment is inherited if not --elevate.
                  * Inheritable handles are not inherited.

                  * Support multiprocess, multi-window applications.
                  * Do not support Windows Terminal.
                    Many developers struggle with it. See the main issue: https://github.com/microsoft/terminal/issues/12464.
                    The default terminal application is likely Windows Terminal, and it will not work.
                    To work around, prepend <exec>... with conhost.
                  * Do not support UWP apps.
                  * Implemented using Win32 job object, there are some cases where {ApplicationInfo.Name} fails to know what processes are spawned, and fails to function.
                    - Processes are created with Win32_Process.Create. This should be rare.
                    - <exec>... delegates process creation to some other existing process and immediately exits.
                      For instance, File Explorer, Visual Studio Code, and Microsoft Edge.
                  
                  The icon for system tray icon is picked in the following order:
                  1. --icon option.
                  2. The first icon of icons embedded in the first argument of <exec>..., if the argument is an absolute path that contains icon resources.
                  3. A default generic application icon.

                """
            );

            return result;
        }
    }

    static int Main_(Config config)
    {
        string pipeName = Guid.NewGuid().ToString();
        using NamedPipeServerStream pipeServerStream = new(pipeName, PipeDirection.Out);
        ProcessStartInfo processStartInfo = new(Path.GetFullPath(ApplicationInfo.WinFormsExeRelativePath, AppContext.BaseDirectory), pipeName)
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
