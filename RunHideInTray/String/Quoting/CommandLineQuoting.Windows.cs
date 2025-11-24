namespace String.Quoting;

internal static partial class CommandLineQuoting
{
    public static string Quoted(IEnumerable<string> args)
    {
        return string.Join(' ', [QuotedFileName(args.First()), .. args.Skip(1).Select(QuotedArgument)]);
    }

    public static string QuotedFileName(string fileName)
    {
        if (fileName.Contains('"'))
            throw new ApplicationException("The argv[0] argument cannot include a double quote.");
        if (fileName == string.Empty || fileName.Any(char.IsWhiteSpace))
            return $"""
                "{fileName}"
                """;
        return fileName;
    }
}
