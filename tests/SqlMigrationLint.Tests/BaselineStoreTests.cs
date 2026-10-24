using System;
using System.IO;
using System.Text.Json;
using Xunit;

namespace SqlMigrationLint.Tests;

public class BaselineStoreTests
{
    [Fact]
    public void LoadBaseline_MissingFile_ThrowsBaselineStoreException()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");
        var ex = Assert.Throws<BaselineStoreException>(() => BaselineStore.LoadBaseline(path));
        Assert.Equal(path, ex.FilePath);
        Assert.IsType<FileNotFoundException>(ex.InnerException);
    }

    [Fact]
    public void LoadBaseline_EmptyFile_ThrowsBaselineStoreException()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");
        File.WriteAllText(path, string.Empty);
        var ex = Assert.Throws<BaselineStoreException>(() => BaselineStore.LoadBaseline(path));
        Assert.Equal(path, ex.FilePath);
        Assert.IsType<JsonException>(ex.InnerException);
    }

    [Fact]
    public void LoadBaseline_MalformedJson_ThrowsBaselineStoreException()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");
        File.WriteAllText(path, "{ invalid json }");
        var ex = Assert.Throws<BaselineStoreException>(() => BaselineStore.LoadBaseline(path));
        Assert.Equal(path, ex.FilePath);
        Assert.IsType<JsonException>(ex.InnerException);
    }

    [Fact]
    public void LoadBaseline_WhitespaceOnlyFile_ThrowsBaselineStoreException()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");
        File.WriteAllText(path, "   \n  ");
        var ex = Assert.Throws<BaselineStoreException>(() => BaselineStore.LoadBaseline(path));
        Assert.Equal(path, ex.FilePath);
        Assert.IsType<JsonException>(ex.InnerException);
    }

    [Fact]
    public void LoadBaseline_DirectoryPath_ThrowsBaselineStoreExceptionNamingDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        try
        {
            var ex = Assert.Throws<BaselineStoreException>(() => BaselineStore.LoadBaseline(path));
            Assert.Equal(path, ex.FilePath);
            Assert.Contains("is a directory", ex.Message);
            Assert.IsType<IOException>(ex.InnerException);
        }
        finally
        {
            Directory.Delete(path);
        }
    }

    [Fact]
    public void WriteBaseline_DirectoryPath_ThrowsIOExceptionNamingDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        try
        {
            var report = new LintReport(Array.Empty<LintFinding>(), 0, false, RiskLevel.None);

            var ex = Assert.Throws<IOException>(() => BaselineStore.WriteBaseline(path, report));
            Assert.Contains("is a directory", ex.Message);
        }
        finally
        {
            Directory.Delete(path);
        }
    }
}
