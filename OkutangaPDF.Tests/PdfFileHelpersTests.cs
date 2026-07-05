using OkutangaPDF.Helpers;

namespace OkutangaPDF.Tests;

[TestClass]
public sealed class PdfFileHelpersTests
{
    [TestMethod]
    [DataRow(0L, "0 B")]
    [DataRow(512L, "512 B")]
    [DataRow(1023L, "1023 B")]
    [DataRow(1024L, "1 KB")]
    [DataRow(1536L, "1.5 KB")]
    [DataRow(1048576L, "1 MB")]
    [DataRow(30L * 1024 * 1024, "30 MB")]
    public void FormatFileSize_ReturnsExpected(long bytes, string expected)
    {
        Assert.AreEqual(expected, PdfFileHelpers.FormatFileSize(bytes));
    }

    [TestMethod]
    public void SanitizeFileName_ReplacesInvalidCharacters()
    {
        var result = PdfFileHelpers.SanitizeFileName("relatório:2024.pdf");
        Assert.IsFalse(result.Contains(':', StringComparison.Ordinal));
        StringAssert.EndsWith(result, ".pdf");
    }

    [TestMethod]
    public void SanitizeFileName_EmptyName_DefaultsToDocumentoPdf()
    {
        Assert.AreEqual("documento.pdf", PdfFileHelpers.SanitizeFileName(null));
        Assert.AreEqual("documento.pdf", PdfFileHelpers.SanitizeFileName("   "));
    }

    [TestMethod]
    public void SanitizeFileName_AddsPdfExtensionWhenMissing()
    {
        Assert.AreEqual("ficheiro.pdf", PdfFileHelpers.SanitizeFileName("ficheiro"));
    }
}
