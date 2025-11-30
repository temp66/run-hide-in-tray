namespace RunHideInTrayConsole.Diagnostics;

internal enum ExitCode
{
    Success = 0,
    // 1 is used by `System.CommandLine`.
    InvalidFileName = 2,
    ErrorStartProcess = 3,
}
