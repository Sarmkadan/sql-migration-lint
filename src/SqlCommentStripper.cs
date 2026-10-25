using System;
using System.Text;

namespace SqlMigrationLint;

/// <summary>
/// Removes SQL <c>--</c> line comments and <c>/* */</c> block comments from migration text so that rules do not match
/// statements that are commented out.
/// <para>
/// Each removed character is replaced by a space and line breaks are kept, so the output has the same length and the
/// same line structure as the input; offsets and line numbers computed on the stripped text remain valid for the raw text.
/// </para>
/// <para>
/// Only single-quoted SQL string literals are honoured, so <c>--</c> or <c>/*</c> inside <c>'...'</c> is kept. Double
/// quotes are deliberately not treated as delimiters: EF Core Up bodies wrap their SQL in C# string literals, and
/// <c>"</c> is also the identifier quote in SQL, so treating it as a delimiter would hide the comments inside
/// <c>migrationBuilder.Sql("...")</c> calls. Block comments nest, as they do in PostgreSQL and SQL Server.
/// </para>
/// </summary>
public static class SqlCommentStripper
{
    /// <summary>
    /// Returns <paramref name="sql"/> with SQL line and block comments replaced by spaces.
    /// </summary>
    /// <param name="sql">The SQL text to strip; null is treated as empty.</param>
    /// <returns>The text with comments blanked out, of the same length as the input.</returns>
    public static string Strip(string? sql)
    {
        if (string.IsNullOrEmpty(sql))
        {
            return string.Empty;
        }

        var result = new StringBuilder(sql.Length);
        int i = 0;
        while (i < sql.Length)
        {
            char c = sql[i];
            if (c == '\'')
            {
                int end = FindClosingQuote(sql, i);
                result.Append(sql, i, end - i + 1);
                i = end + 1;
            }
            else if (c == '-' && PeekIs(sql, i + 1, '-'))
            {
                int end = IndexOfLineBreak(sql, i);
                AppendBlanked(result, sql, i, end);
                i = end;
            }
            else if (c == '/' && PeekIs(sql, i + 1, '*'))
            {
                int end = FindBlockCommentEnd(sql, i);
                AppendBlanked(result, sql, i, end);
                i = end;
            }
            else
            {
                result.Append(c);
                i++;
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Returns the index of the quote that closes the literal starting at <paramref name="start"/>, honouring the
    /// doubled-quote escape (<c>''</c>). An unterminated literal runs to the end of the text.
    /// </summary>
    private static int FindClosingQuote(string sql, int start)
    {
        char quote = sql[start];
        int j = start + 1;
        while (j < sql.Length)
        {
            if (sql[j] == quote)
            {
                if (PeekIs(sql, j + 1, quote))
                {
                    j += 2;
                    continue;
                }

                return j;
            }

            j++;
        }

        return sql.Length - 1;
    }

    /// <summary>
    /// Returns the exclusive end index of the block comment starting at <paramref name="start"/>, counting nested
    /// <c>/*</c> openers. An unterminated comment runs to the end of the text.
    /// </summary>
    private static int FindBlockCommentEnd(string sql, int start)
    {
        int depth = 1;
        int j = start + 2;
        while (j < sql.Length && depth > 0)
        {
            if (sql[j] == '/' && PeekIs(sql, j + 1, '*'))
            {
                depth++;
                j += 2;
            }
            else if (sql[j] == '*' && PeekIs(sql, j + 1, '/'))
            {
                depth--;
                j += 2;
            }
            else
            {
                j++;
            }
        }

        return Math.Min(j, sql.Length);
    }

    /// <summary>
    /// Returns the index of the line break ending the line that contains <paramref name="start"/>, or the text length
    /// when the line is the last one.
    /// </summary>
    private static int IndexOfLineBreak(string sql, int start)
    {
        int index = sql.IndexOfAny(new[] { '\r', '\n' }, start);
        return index < 0 ? sql.Length : index;
    }

    /// <summary>
    /// Appends the range <c>[start, end)</c> with every character replaced by a space, except line breaks which are kept.
    /// </summary>
    private static void AppendBlanked(StringBuilder result, string sql, int start, int end)
    {
        for (int k = start; k < end; k++)
        {
            char c = sql[k];
            result.Append(c == '\r' || c == '\n' ? c : ' ');
        }
    }

    private static bool PeekIs(string sql, int index, char expected) =>
        index < sql.Length && sql[index] == expected;
}
