namespace RunHideInTray.Diagnostics;

internal static class ExceptionReporter
{
    public static void ToConsole(Exception ex)
    {
        Console.Error.WriteLine(ex);
    }

    public static void ToConsole(string prefix, Exception ex)
    {
        Console.Error.WriteLine($"{prefix}:\n{ex}");
    }

    public static void ShowMessageBox(Exception ex)
    {
        MessageBox.Show(ex.ToString(), nameof(RunHideInTray), MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
