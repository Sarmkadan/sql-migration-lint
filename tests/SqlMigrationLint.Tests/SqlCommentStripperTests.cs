using System.Linq;
using SqlMigrationLint;
using Xunit;

namespace SqlMigrationLint.Tests;

/// <summary>
/// Unit tests for <see cref="SqlCommentStripper"/>: comments are blanked, literals are kept, and layout is preserved.
/// </summary>
public sealed class SqlCommentStripperTests
{
    [Fact]
    public void Strip_NullInput_ReturnsEmptyString()
    {
        Assert.Equal(string.Empty, SqlCommentStripper.Strip(null));
    }

    [Fact]
    public void Strip_LineComment_BlanksCommentAndKeepsLineBreak()
    {
        var result = SqlCommentStripper.Strip("-- DROP TABLE \"Orders\";\nSELECT 1;");

        Assert.Equal(new string(' ', 23) + "\nSELECT 1;", result);
        Assert.DoesNotContain("DROP", result);
    }

    [Fact]
    public void Strip_BlockComment_BlanksCommentContent()
    {
        var result = SqlCommentStripper.Strip("/* DROP TABLE \"Orders\"; */SELECT 1;");

        Assert.DoesNotContain("DROP", result);
        Assert.EndsWith("SELECT 1;", result);
    }

    [Fact]
    public void Strip_MultiLineBlockComment_KeepsNewlinesSoLineNumbersStayValid()
    {
        var input = "/*\nDROP TABLE x;\n*/\nUPDATE t SET a = 1;";

        var result = SqlCommentStripper.Strip(input);

        Assert.Equal(input.Length, result.Length);
        Assert.Equal(input.Count(c => c == '\n'), result.Count(c => c == '\n'));
        Assert.EndsWith("UPDATE t SET a = 1;", result);
    }

    [Fact]
    public void Strip_NestedBlockComment_RemovesWholeOuterComment()
    {
        var result = SqlCommentStripper.Strip("/* outer /* inner */ DROP TABLE x; */ SELECT 1;");

        Assert.DoesNotContain("DROP", result);
        Assert.EndsWith("SELECT 1;", result);
    }

    [Fact]
    public void Strip_UnterminatedBlockComment_RunsToEndOfText()
    {
        var result = SqlCommentStripper.Strip("SELECT 1; /* DROP TABLE x;");

        Assert.Equal("SELECT 1; " + new string(' ', 16), result);
    }

    [Fact]
    public void Strip_CommentMarkersInsideStringLiteral_AreKept()
    {
        const string input = "INSERT INTO t VALUES ('-- not a comment', '/* nor this */');";

        Assert.Equal(input, SqlCommentStripper.Strip(input));
    }

    [Fact]
    public void Strip_EscapedQuoteInsideLiteral_DoesNotEndLiteralEarly()
    {
        // The '' inside the literal is an escaped quote, so the -- after the literal is still inside the string.
        const string input = "SELECT 'it''s -- still text'; -- real comment";

        var result = SqlCommentStripper.Strip(input);

        Assert.StartsWith("SELECT 'it''s -- still text';", result);
        Assert.DoesNotContain("real comment", result);
    }

    [Fact]
    public void Strip_OutputLengthAlwaysMatchesInput()
    {
        const string input = "-- a\n/* b */ UPDATE t SET x = 'y' -- c\r\nDELETE FROM t;";

        Assert.Equal(input.Length, SqlCommentStripper.Strip(input).Length);
    }
}
