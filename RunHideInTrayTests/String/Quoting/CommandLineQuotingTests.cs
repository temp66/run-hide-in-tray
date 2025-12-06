namespace String.Quoting;

[TestClass]
public sealed partial class CommandLineQuotingTests
{
    [TestMethod]
    [DynamicData(nameof(QuotedArgumentTestData))]
    public void QuotedArgumentTest(string quoted, string original)
    {
        Assert.AreEqual(quoted, CommandLineQuoting.QuotedArgument(original));
    }

    static IEnumerable<object[]> QuotedArgumentTestData()
    {
        string quoted;

        yield return new object[]
        {
            """
            ""
            """,
            string.Empty,
        };

        quoted = """
            \\\\n\\
            """;
        yield return new object[] { quoted, quoted };

        quoted = """
            \a\b\c\t
            """;
        yield return new object[] { quoted, quoted };

        yield return new object[]
        {
            """
            " a b    c
            d "
            """,
            """
             a b    c
            d 
            """,
        };

        yield return new object[]
        {
            """
            "\\\\a \\"
            """,
            """
            \\\\a \
            """,
        };

        yield return new object[]
        {
            """
            "\""
            """,
            """
            "
            """,
        };

        yield return new object[]
        {
            """
            "\\\""
            """,
            """
            \"
            """,
        };

        yield return new object[]
        {
            """
            "\" a"
            """,
            """
            " a
            """,
        };
    }
}
