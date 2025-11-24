namespace String;

[TestClass]
public sealed class TruncatingTests
{
    [TestMethod]
    [DynamicData(nameof(EllipsisTestData))]
    public void EllipsisTest(string original, int maxLength, string truncated)
    {
        Assert.AreEqual(truncated, Truncating.Ellipsis(original, maxLength));
    }

    static IEnumerable<object[]> EllipsisTestData()
    {
        string original;

        original = "abcde";
        yield return new object[] { original, 5, original };

        yield return new object[] { original, 6, original };

        yield return new object[] { original, 3, "..." };

        yield return new object[] { original, 4, "a..." };
    }
}
