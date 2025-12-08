# RunHideInTray

A Windows Forms application for Windows 11.

Run an executable, hide the window of it, and provide hide/show/exit controls in system tray.

---

```
Description:
  Run an executable, and hide the window of it in system tray.

Usage:
  RunHideInTray <exec>... [options]

Arguments:
  <exec>  The executable to run, and possibly arguments to it.

Options:
  --elevate                                        Run RunHideInTray as administrator.
                                                   Required if <exec>... will run as administrator but currently RunHideInTray is not.
  --hide-on-start-timeout <hide-on-start-timeout>  Try to hide window on start. Specify the wait timeout in milliseconds.
                                                   Negative value disables hiding on start.
                                                   Zero enables hiding on start but does not wait. [default: -1]
  --title <title>                                  Title for system tray icon. [default: RunHideInTray - <exec>...]
  --icon <icon>                                    Path to the icon for system tray icon.
  -?, -h, --help                                   Show help and usage information
  --version                                        Show version information

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
  * Implemented using Win32 job object, there are some cases where RunHideInTray fails to know what processes are spawned, and fails to function.
    - Processes are created with Win32_Process.Create. This should be rare.
    - <exec>... delegates process creation to some other existing process and immediately exits.
      For instance, File Explorer, Visual Studio Code, and Microsoft Edge.

  The icon for system tray icon is picked in the following order:
  1. --icon option.
  2. The first icon of icons embedded in the first argument of <exec>..., if the argument is an absolute path that contains icon resources.
  3. A default generic application icon.

```

After a system tray icon appears, hide or show window through its context menu, or simply click on the icon to toggle window visibility.

## Notes

- On first run,
  - If you downloaded released executables instead of compiling from source and have some antivirus software on, you will likely encounter an error:

    ```
    System.ComponentModel.Win32Exception (1223): An error occurred trying to start process ... The operation was canceled by the user.
    ```

    Manually run RunHideInTrayWinForms.exe once may solve the issue.

  - The system tray icon may be hidden by default.

- The "Hide" menu item is always enabled, because new windows may show up, but the program only tracks which windows are hidden and is unaware of that.

- The "Show" menu item is enabled iff there are some windows hidden.

- Window handle (HWND) reusing is not taken care of.

  Reuse of HWND is unlikely according to https://stackoverflow.com/a/65617844 and https://www.fractolog.com/2025/09/hwnd-generation-and-reuse/.

  Besides, HWND is different from HANDLE. I doubt there is a proper way to detect or prevent reuse of HWND.

## Examples

- ```powershell
  RunHideInTray --elevate -- perfmon /sys
  ```

- ```powershell
  RunHideInTray --title "Robocopy Sync" -- conhost robocopy source destination /MIR /MOT:1 /SJ /SL /IT /IM /X /V /TS
  ```

- ```powershell
  RunHideInTray --hide-on-start-timeout 0 --title "App Log" -- conhost pwsh -Command "Get-Content app.log -Wait"
  ```

- https://github.com/UnblockNeteaseMusic/server

  1. Final directory structure:

     ```
     UnblockNeteaseMusic-server
     │  91001487.ico
     │  run-RunHideInTray.ps1
     │  run.ps1
     │
     └─UnblockNeteaseMusic
          app.js
     ```

  2. Clone that repository and setup as instructed. (You may have to install a root certificate.)

     The following steps take [直接使用 Repo 最新版本](https://github.com/UnblockNeteaseMusic/server/tree/47d6b1d918f8dbf6160b8fa07cd17a9480285005?tab=readme-ov-file#%E7%9B%B4%E6%8E%A5%E4%BD%BF%E7%94%A8-repo-%E6%9C%80%E6%96%B0%E7%89%88%E6%9C%AC) as an example.

  3. Convert the repository owner's avatar to ICO format. (Many online converters are available.)

  4. Create `run.ps1`.

     ```powershell
     cd "D:\UnblockNeteaseMusic-server"
     [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new()
     node UnblockNeteaseMusic\app.js
     ```

  5. Create `run-RunHideInTray.ps1`.
     
     ```powershell
     cd "D:\UnblockNeteaseMusic-server"
     RunHideInTray --hide-on-start-timeout 0 --title "UnblockNeteaseMusic" --icon 91001487.ico -- conhost pwsh -File run.ps1
     ```
  
  6. Create a shortcut to `run-RunHideInTray.ps1` as you like.
