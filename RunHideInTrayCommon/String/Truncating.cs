using System.Diagnostics;

namespace String;

public static class Truncating
{
    public static string Ellipsis(string s, int maxLength)
    {
        Debug.Assert(maxLength >= 3);
        if (s.Length <= maxLength)
            return s;
        return $"{s[..(maxLength - 3)]}...";
    }
}
