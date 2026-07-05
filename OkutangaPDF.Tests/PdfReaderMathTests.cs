using OkutangaPDF.Helpers;

namespace OkutangaPDF.Tests;

[TestClass]
public sealed class PdfReaderMathTests
{
    [TestMethod]
    [DataRow(0.1, 0.5)]
    [DataRow(1.0, 1.0)]
    [DataRow(5.0, 3.0)]
    public void ClampScale_ClampsToRange(double input, double expected)
    {
        Assert.AreEqual(expected, PdfReaderMath.ClampScale(input));
    }

    [TestMethod]
    [DataRow(1, 1)]
    [DataRow(5, 5)]
    [DataRow(500, 200)]
    public void ClampRecentLimit_ClampsToRange(int input, int expected)
    {
        Assert.AreEqual(expected, PdfReaderMath.ClampRecentLimit(input));
    }

    [TestMethod]
    [DataRow(5, 0, 1)]
    [DataRow(5, 10, 5)]
    [DataRow(-3, 10, 1)]
    [DataRow(99, 10, 10)]
    public void ClampPage_WithPageCount(int page, int pageCount, int expected)
    {
        Assert.AreEqual(expected, PdfReaderMath.ClampPage(page, pageCount));
    }

    [TestMethod]
    public void ClampPage_ZeroPageCount_ReturnsOneWithoutThrowing()
    {
        Assert.AreEqual(1, PdfReaderMath.ClampPage(99, 0));
    }
}
