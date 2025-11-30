using InteropServices;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.JobObjects;
using Windows.Win32.System.Threading;

using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Windows;

public class ProcessDescendantsMonitor : IDisposable
{
    SafeFileHandle _jobObject;
    nuint _completionKey;
    SafeFileHandle _completionPort;
    SafeProcessHandle _rootProcess;

    public event EventHandler? AllExited;
    public event EventHandler<Win32Exception>? Faulted;

    bool _disposed = false;

    public ProcessDescendantsMonitor(string exec)
    {
        try
        {
            _jobObject = PInvoke.CreateJobObject(null, null);
            if (_jobObject.IsInvalid)
                throw Win32Error.CreateExceptionFromLastError(nameof(PInvoke.CreateJobObject));

            _completionKey = (nuint)_jobObject.DangerousGetHandle();

            using SafeFileHandle invalidHandle = new(HANDLE.INVALID_HANDLE_VALUE, true);
            _completionPort = PInvoke.CreateIoCompletionPort(invalidHandle, null, _completionKey, 1);
            if (_completionPort.IsInvalid)
                throw Win32Error.CreateExceptionFromLastError(nameof(PInvoke.CreateIoCompletionPort));

            JOBOBJECT_ASSOCIATE_COMPLETION_PORT jobObjectAssociateCompletionPort;
            unsafe
            {
                jobObjectAssociateCompletionPort = new()
                {
                    CompletionKey = (void*)_completionKey,
                    CompletionPort = (HANDLE)_completionPort.DangerousGetHandle(),
                };
            }
            if (!PInvoke.SetInformationJobObject(
                _jobObject,
                JOBOBJECTINFOCLASS.JobObjectAssociateCompletionPortInformation,
                Cast.AsBytes(ref jobObjectAssociateCompletionPort)
            ))
                throw Win32Error.CreateExceptionFromLastError(nameof(PInvoke.SetInformationJobObject));

            // JOBOBJECT_EXTENDED_LIMIT_INFORMATION jobObjectExtendedLimitInformation = new()
            // {
            //     BasicLimitInformation = new()
            //     {
            //         LimitFlags = JOB_OBJECT_LIMIT.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE,
            //     },
            // };
            // if (!PInvoke.SetInformationJobObject(
            //     _jobObject,
            //     JOBOBJECTINFOCLASS.JobObjectExtendedLimitInformation,
            //     Cast.AsBytes(ref jobObjectExtendedLimitInformation)
            // ))
            //     throw Win32Error.CreateExceptionFromLastError(nameof(PInvoke.SetInformationJobObject));

            Span<char> execSpan = $"{exec}\0".ToCharArray();
            STARTUPINFOW startupInfo = new()
            {
                cb = (uint)Marshal.SizeOf<STARTUPINFOW>(),
            };
            PROCESS_INFORMATION processInformation;
            bool createProcessResult;
            unsafe
            {
                createProcessResult = PInvoke.CreateProcess(
                    null, ref execSpan,
                    null, null, false,
                    PROCESS_CREATION_FLAGS.CREATE_SUSPENDED,
                    null, null,
                    startupInfo, out processInformation
                );
            }
            if (!createProcessResult)
                throw Win32Error.CreateExceptionFromLastError(nameof(PInvoke.CreateProcess));
            _rootProcess = new(processInformation.hProcess, true);
            using SafeFileHandle thread = new(processInformation.hThread, true);

            if (!PInvoke.AssignProcessToJobObject(_jobObject, _rootProcess))
                throw Win32Error.CreateExceptionFromLastError(nameof(PInvoke.AssignProcessToJobObject));

            if (PInvoke.ResumeThread(thread) == uint.MaxValue)
                throw Win32Error.CreateExceptionFromLastError(nameof(PInvoke.ResumeThread));
        }
        catch
        {
            Dispose();
            throw;
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
        {
            _rootProcess?.Dispose();
            _completionPort?.Dispose();
            _jobObject?.Dispose();
        }
        _disposed = true;
    }

    public Task WaitForAllProcessesAsync(ISynchronizeInvoke sychronizingObject)
    {
        return Task.Run(() => WaitForAllProcesses(sychronizingObject));
    }

    // https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-jobobject_associate_completion_port#remarks
    // Caveat: Notifications are not guaranteed.
    void WaitForAllProcesses(ISynchronizeInvoke synchronizingObject)
    {
        while (true)
        {
            uint messageIdentifier;
            nuint completionKey;
            bool getQueuedCompletionStatusResult;
            unsafe
            {
                getQueuedCompletionStatusResult = PInvoke.GetQueuedCompletionStatus(
                    _completionPort,
                    out messageIdentifier,
                    out completionKey,
                    out NativeOverlapped* overlapped,
                    PInvoke.INFINITE
                );
            }
            if (!getQueuedCompletionStatusResult)
            {
                if (Faulted is not null)
                    _ = synchronizingObject.BeginInvoke(Faulted, [
                        this,
                        Win32Error.CreateExceptionFromLastError(nameof(PInvoke.GetQueuedCompletionStatus))
                    ]);
                return;
            }

            if (messageIdentifier == PInvoke.JOB_OBJECT_MSG_ACTIVE_PROCESS_ZERO && completionKey == _completionKey)
            {
                if (AllExited is not null)
                    _ = synchronizingObject.BeginInvoke(AllExited, [this, EventArgs.Empty]);
                break;
            }
        }
    }

    public IEnumerable<Process> GetProcessList()
    {
        using MemoryManager memoryManager = new((nuint)JOBOBJECT_BASIC_PROCESS_ID_LIST.SizeOf(1));
        ref var jobObjectBasicProcessIdList = ref Unsafe.NullRef<JOBOBJECT_BASIC_PROCESS_ID_LIST>();
        while (true)
        {
            unsafe
            {
                jobObjectBasicProcessIdList = ref Unsafe.AsRef<JOBOBJECT_BASIC_PROCESS_ID_LIST>(memoryManager.Ptr);
            }
            Span<byte> jobObjectBasicProcessIdListSpan;
            unsafe
            {
                jobObjectBasicProcessIdListSpan = new(memoryManager.Ptr, (int)memoryManager.Size);
            }
            bool queryInformationJobObjectResult = PInvoke.QueryInformationJobObject(
                _jobObject,
                JOBOBJECTINFOCLASS.JobObjectBasicProcessIdList,
                jobObjectBasicProcessIdListSpan
            );
            if (!queryInformationJobObjectResult)
            {
                int error = Marshal.GetLastPInvokeError();
                if ((WIN32_ERROR)error != WIN32_ERROR.ERROR_MORE_DATA)
                    throw Win32Error.CreateExceptionFromError(nameof(PInvoke.QueryInformationJobObject), error);
            }

            if (
                !queryInformationJobObjectResult
                || jobObjectBasicProcessIdList.NumberOfProcessIdsInList < jobObjectBasicProcessIdList.NumberOfAssignedProcesses
            )
                memoryManager.Realloc((nuint)JOBOBJECT_BASIC_PROCESS_ID_LIST.SizeOf((int)jobObjectBasicProcessIdList.NumberOfAssignedProcesses));
            else
                break;
        }
        uint n = jobObjectBasicProcessIdList.NumberOfProcessIdsInList;
        Debug.Assert(n == jobObjectBasicProcessIdList.NumberOfAssignedProcesses);

        // In case `WaitForAllProcesses` is not notified
        if (n == 0)
            AllExited?.Invoke(this, EventArgs.Empty);

        nuint[] processIdList = jobObjectBasicProcessIdList.ProcessIdList.AsSpan((int)n).ToArray();

        foreach (nuint processId in processIdList)
        {
            Process process;
            try
            {
                process = System.Diagnostics.Process.GetProcessById((int)processId);
            }
            catch (ArgumentException)
            {
                continue;
            }
            try
            {
                // In case the process has exited and `processId` is recycled in this gap
                // Process ID cannot be recycled if there is a handle open to the process.
                if (!PInvoke.IsProcessInJob(process.SafeHandle, _jobObject, out BOOL isProcessInJob))
                    throw Win32Error.CreateExceptionFromLastError(nameof(PInvoke.IsProcessInJob));
                if (!isProcessInJob)
                    continue;
            }
            catch
            {
                process.Dispose();
                throw;
            }
            yield return process;
        }
    }

    public void KillAllProcesses()
    {
        // With `CloseHandle`, no notifications will be sent.
        // _jobObject.Close();

        if (!PInvoke.TerminateJobObject(_jobObject, 1))
            throw Win32Error.CreateExceptionFromLastError(nameof(PInvoke.TerminateJobObject));
    }

    public int GetRootProcessExitCode()
    {
        if (!PInvoke.GetExitCodeProcess(_rootProcess, out uint exitCode))
            throw Win32Error.CreateExceptionFromLastError(nameof(PInvoke.GetExitCodeProcess));
        return (int)exitCode;
    }
}
