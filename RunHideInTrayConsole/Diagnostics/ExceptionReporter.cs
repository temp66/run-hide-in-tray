namespace RunHideInTrayConsole.Diagnostics;

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
}
