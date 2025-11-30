namespace RunHideInTrayWinForms.Diagnostics;

internal enum ExitCode
{
    InvalidPipeClientHandle = 1,
    InvalidJson = 2,
    ErrorCreateJobObject = 3,
    ErrorCreateIoCompletionPort = 4,
    ErrorSetInformationJobObject = 5,
    ErrorCreateProcess = 6,
    ErrorAssignProcessToJobObject = 7,
    ErrorResumeThread = 8,
    ErrorGetQueuedCompletionStatus = 9,
    ErrorGetExitCodeProcess = 10,
}
