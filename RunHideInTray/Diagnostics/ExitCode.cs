namespace RunHideInTray.Diagnostics;

internal enum ExitCode
{
    Success = 0,
    // 1 is used by `System.CommandLine`.
    ErrorAllocConsole = 2,
    InvalidFileName = 3,
    ErrorCreateJobObject = 4,
    ErrorCreateIoCompletionPort = 5,
    ErrorSetInformationJobObject = 6,
    ErrorCreateProcess = 7,
    ErrorAssignProcessToJobObject = 8,
    ErrorResumeThread = 9,
}
