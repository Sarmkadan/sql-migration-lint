using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace SqlMigrationLint
{
    /// <summary>
    /// Provides extension methods for <see cref="MigrationFile"/> to simplify common migration file operations.
    /// </summary>
    public static class MigrationFileExtensions
    {
        /// <summary>
        /// Gets the number of lines in the migration file.
        /// </summary>
        /// <param name="file">The migration file.</param>
        /// <returns>The number of lines.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="file"/> is null.</exception>
        public static int GetLineCount(this MigrationFile file)
        {
            ArgumentNullException.ThrowIfNull(file);
            return file.Lines.Length;
        }

        /// <summary>
        /// Gets the number of lines in the Up body.
        /// </summary>
        /// <param name="file">The migration file.</param>
        /// <returns>The number of lines in the Up body, or 0 if Up body is null.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="file"/> is null.</exception>
        public static int GetUpBodyLineCount(this MigrationFile file)
        {
            ArgumentNullException.ThrowIfNull(file);
            return file.UpBody?.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length ?? 0;
        }

        /// <summary>
        /// Gets the number of lines in the Down body.
        /// </summary>
        /// <param name="file">The migration file.</param>
        /// <returns>The number of lines in the Down body, or 0 if Down body is null.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="file"/> is null.</exception>
        public static int GetDownBodyLineCount(this MigrationFile file)
        {
            ArgumentNullException.ThrowIfNull(file);
            return file.DownBody?.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length ?? 0;
        }

        /// <summary>
        /// Gets all SQL statements from the Up body.
        /// </summary>
        /// <param name="file">The migration file.</param>
        /// <returns>An enumerable of SQL statements, or empty if Up body is null.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="file"/> is null.</exception>
        public static IEnumerable<string> GetUpSqlStatements(this MigrationFile file)
        {
            ArgumentNullException.ThrowIfNull(file);
            return GetSqlStatements(file.UpBody);
        }

        /// <summary>
        /// Gets all SQL statements from the Down body.
        /// </summary>
        /// <param name="file">The migration file.</param>
        /// <returns>An enumerable of SQL statements, or empty if Down body is null.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="file"/> is null.</exception>
        public static IEnumerable<string> GetDownSqlStatements(this MigrationFile file)
        {
            ArgumentNullException.ThrowIfNull(file);
            return GetSqlStatements(file.DownBody);
        }

        /// <summary>
        /// Gets all SQL statements from the migration file (both Up and Down).
        /// </summary>
        /// <param name="file">The migration file.</param>
        /// <returns>An enumerable of all SQL statements in the migration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="file"/> is null.</exception>
        public static IEnumerable<string> GetAllSqlStatements(this MigrationFile file)
        {
            ArgumentNullException.ThrowIfNull(file);
            return GetUpSqlStatements(file).Concat(GetDownSqlStatements(file));
        }

        /// <summary>
        /// Gets the file name without extension.
        /// </summary>
        /// <param name="file">The migration file.</param>
        /// <returns>The file name without extension.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="file"/> is null.</exception>
        public static string GetFileNameWithoutExtension(this MigrationFile file)
        {
            ArgumentNullException.ThrowIfNull(file);
            return Path.GetFileNameWithoutExtension(file.FilePath);
        }

        /// <summary>
        /// Gets the directory name containing the migration file.
        /// </summary>
        /// <param name="file">The migration file.</param>
        /// <returns>The directory name.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="file"/> is null.</exception>
        public static string GetDirectoryName(this MigrationFile file)
        {
            ArgumentNullException.ThrowIfNull(file);
            return Path.GetDirectoryName(file.FilePath) ?? string.Empty;
        }

        /// <summary>
        /// Checks if the migration has an Up body.
        /// </summary>
        /// <param name="file">The migration file.</param>
        /// <returns>True if the Up body is not null or empty; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="file"/> is null.</exception>
        public static bool HasUpBody(this MigrationFile file)
        {
            ArgumentNullException.ThrowIfNull(file);
            return !string.IsNullOrEmpty(file.UpBody);
        }

        /// <summary>
        /// Checks if the migration has a Down body.
        /// </summary>
        /// <param name="file">The migration file.</param>
        /// <returns>True if the Down body is not null or empty; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="file"/> is null.</exception>
        public static bool HasDownBody(this MigrationFile file)
        {
            ArgumentNullException.ThrowIfNull(file);
            return !string.IsNullOrEmpty(file.DownBody);
        }

        /// <summary>
        /// Gets the total number of SQL statements in the migration.
        /// </summary>
        /// <param name="file">The migration file.</param>
        /// <returns>The total count of SQL statements.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="file"/> is null.</exception>
        public static int GetTotalSqlStatementCount(this MigrationFile file)
        {
            ArgumentNullException.ThrowIfNull(file);
            return GetUpSqlStatements(file).Count() + GetDownSqlStatements(file).Count();
        }

        /// <summary>
        /// Gets the size of the migration file in bytes.
        /// </summary>
        /// <param name="file">The migration file.</param>
        /// <returns>The file size in bytes.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="file"/> is null.</exception>
        /// <exception cref="IOException">Thrown when the file cannot be accessed.</exception>
        public static long GetFileSize(this MigrationFile file)
        {
            ArgumentNullException.ThrowIfNull(file);
            return File.Exists(file.FilePath) ? new FileInfo(file.FilePath).Length : 0L;
        }

        /// <summary>
        /// Gets the number of comments in the Up body.
        /// </summary>
        /// <param name="file">The migration file.</param>
        /// <returns>The number of comment lines in the Up body.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="file"/> is null.</exception>
        public static int GetUpBodyCommentCount(this MigrationFile file)
        {
            ArgumentNullException.ThrowIfNull(file);
            return CountComments(file.UpBody);
        }

        /// <summary>
        /// Gets the number of comments in the Down body.
        /// </summary>
        /// <param name="file">The migration file.</param>
        /// <returns>The number of comment lines in the Down body.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="file"/> is null.</exception>
        public static int GetDownBodyCommentCount(this MigrationFile file)
        {
            ArgumentNullException.ThrowIfNull(file);
            return CountComments(file.DownBody);
        }

        /// <summary>
        /// Gets the number of SQL keywords in the Up body.
        /// </summary>
        /// <param name="file">The migration file.</param>
        /// <param name="keywords">The set of SQL keywords to search for.</param>
        /// <returns>The number of SQL keywords found in the Up body.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="file"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="keywords"/> is null.</exception>
        public static int GetUpBodyKeywordCount(this MigrationFile file, IReadOnlySet<string> keywords)
        {
            ArgumentNullException.ThrowIfNull(file);
            ArgumentNullException.ThrowIfNull(keywords);
            return CountKeywords(file.UpBody, keywords);
        }

        /// <summary>
        /// Gets the number of SQL keywords in the Down body.
        /// </summary>
        /// <param name="file">The migration file.</param>
        /// <param name="keywords">The set of SQL keywords to search for.</param>
        /// <returns>The number of SQL keywords found in the Down body.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="file"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="keywords"/> is null.</exception>
        public static int GetDownBodyKeywordCount(this MigrationFile file, IReadOnlySet<string> keywords)
        {
            ArgumentNullException.ThrowIfNull(file);
            ArgumentNullException.ThrowIfNull(keywords);
            return CountKeywords(file.DownBody, keywords);
        }

        private static IEnumerable<string> GetSqlStatements(string? body)
        {
            ArgumentNullException.ThrowIfNull(body);

            if (string.IsNullOrEmpty(body))
            {
                return Array.Empty<string>();
            }

            // Split by semicolon and remove empty entries
            return body.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s));
        }

        private static int CountComments(string? body)
        {
            if (string.IsNullOrEmpty(body))
            {
                return 0;
            }

            return body.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Count(line => line.Trim().StartsWith("--", StringComparison.Ordinal) ||
                              line.Trim().StartsWith("/*", StringComparison.Ordinal));
        }

        private static int CountKeywords(string? body, IReadOnlySet<string> keywords)
        {
            ArgumentNullException.ThrowIfNull(keywords);

            if (string.IsNullOrEmpty(body) || keywords.Count == 0)
            {
                return 0;
            }

            var bodyText = body.ToUpperInvariant();
            return keywords.Count(keyword => bodyText.Contains(keyword, StringComparison.Ordinal));
        }
    }
}