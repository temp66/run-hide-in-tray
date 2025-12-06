namespace String.Quoting;

public sealed partial class CommandLineQuotingTests
{
    [TestMethod]
    [DynamicData(nameof(QuotedFileNameTestData))]
    public void QuotedFileNameTest(string quoted, string original)
    {
        Assert.AreEqual(quoted, CommandLineQuoting.QuotedFileName(original));
    }

    static IEnumerable<object[]> QuotedFileNameTestData()
    {
        yield return new object[]
        {
            """
            "C:\Program Files"
            """,
            """
            C:\Program Files
            """,
        };

        yield return new object[]
        {
            """
            ""
            """,
            string.Empty,
        };

        yield return new object[] { "abc", "abc" };
    }

    [TestMethod]
    [DynamicData(nameof(QuotedFileNameThrowTestData))]
    public void QuotedFileNameThrowTest(string original)
    {
        Assert.ThrowsExactly<ApplicationException>(
            () => CommandLineQuoting.QuotedFileName(original),
            "The argv[0] argument cannot include a double quote."
        );
    }

    static IEnumerable<object[]> QuotedFileNameThrowTestData()
    {
        yield return new object[]
        {
            """
            a"b
            """
        };
    }

    [TestMethod]
    [DynamicData(nameof(QuotedTestData))]
    public void QuotedTest(string quoted, IEnumerable<string> original)
    {
        Assert.AreEqual(quoted, CommandLineQuoting.Quoted(original));
    }

    static IEnumerable<object[]> QuotedTestData()
    {
        yield return new object[]
        {
            """
            "" \\\\n\\ "\""
            """,
            new string[]
            {
                string.Empty,
                """
                \\\\n\\
                """,
                """
                "
                """,
            },
        };

        yield return new object[]
        {
            """
            "a \\"
            """,
            new string[]
            {
                """
                a \\
                """,
            },
        };
    }
}
