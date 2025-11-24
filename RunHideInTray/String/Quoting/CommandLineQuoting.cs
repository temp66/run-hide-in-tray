// According to https://github.com/dotnet/runtime/blob/f48fbe9594b1b6ca9ffd829f65afbb39784b9d12/src/libraries/System.Private.CoreLib/src/System/Environment.cs#L129

using System.Text;

namespace String.Quoting;

internal static partial class CommandLineQuoting
{
    public static string QuotedArgument(string arg)
    {
        if (arg.Length == 0)
            return """
                ""
                """;
        if (!(arg.Any(char.IsWhiteSpace) || arg.Contains('"')))
            return arg;
        return $"""
            "{QuotedArgumentInner(arg)}"
            """;
    }

    static string QuotedArgumentInner(string arg)
    {
        StringBuilder stringBuilder = new();
        int idx = 0;
        while (idx < arg.Length)
            switch (arg[idx])
            {
                case '"':
                    stringBuilder.Append("""
                        \"
                        """);
                    ++idx;
                    break;
                case '\\':
                    int idx_ = idx;
                    while (++idx_ < arg.Length && arg[idx_] == '\\') ;
                    if (idx_ == arg.Length || arg[idx_] == '"')
                        stringBuilder.Append('\\', 2 * (idx_ - idx));
                    else
                        stringBuilder.Append('\\', idx_ - idx);
                    idx = idx_;
                    break;
                default:
                    stringBuilder.Append(arg[idx++]);
                    break;
            }
        return stringBuilder.ToString();
    }
}
