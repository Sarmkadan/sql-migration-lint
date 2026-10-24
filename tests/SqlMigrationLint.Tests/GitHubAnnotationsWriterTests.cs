using System.Collections.Generic;
using System.IO;
using Xunit;

namespace SqlMigrationLint.Tests;

public class GitHubAnnotationsWriterTests
{
    private static string Write(params LintFinding[] findings)
    {
        var report = new LintReport(findings, migrationsScanned: 1, hasBlockers: false, maxRisk: RiskLevel.Warning);
        using var writer = new StringWriter();

        new GitHubAnnotationsWriter().WriteReport(report, writer);

        return writer.ToString().TrimEnd('\r', '\n');
    }

    [Fact]
    public void WriteReport_MessageWithNewlineAndDoubleColon_EscapesWorkflowCommandSyntax()
    {
        var finding = new LintFinding("drop-table", LintSeverity.Warning, "first\n::error::forged", "V1.cs", 1);

        var output = Write(finding);

        Assert.Equal("::warning file=V1.cs,line=1::first%0A%3A%3Aerror%3A%3Aforged", output);
    }

    [Fact]
    public void WriteReport_FileWithNewlineCommaAndDoubleColon_EscapesFileProperty()
    {
        var finding = new LintFinding("drop-table", LintSeverity.Warning, "ok", "db/evil\n::set-env,name=X.cs", 1);

        var output = Write(finding);

        Assert.Equal("::warning file=db/evil%0A%3A%3Aset-env%2Cname=X.cs,line=1::ok", output);
    }

    [Fact]
    public void WriteReport_MessageWithPercentEncodedNewline_DoesNotDecodeIt()
    {
        var finding = new LintFinding("drop-table", LintSeverity.Blocker, "literal %0A marker", "V1.cs", 1);

        var output = Write(finding);

        Assert.Equal("::error file=V1.cs,line=1::literal %250A marker", output);
    }

    [Fact]
    public void WriteReport_InjectedFindings_EmitsExactlyOneLinePerFinding()
    {
        var findings = new List<LintFinding>
        {
            new("drop-table", LintSeverity.Warning, "a\r\n::error::forged", "V1.cs", 1),
            new("drop-table", LintSeverity.Warning, "b\n::add-mask::secret", "V2.cs", 1)
        };

        var output = Write(findings.ToArray());

        Assert.Equal(2, output.Split('\n').Length);
    }
}
