using System;
using System.IO;
using System.Text.RegularExpressions;

namespace SqlMigrationLint
{
    /// <summary>
    /// Represents a SQL migration file.
    /// </summary>
    public class MigrationFile
    {
        /// <summary>
        /// Gets the file path of the migration file.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// Gets the name of the migration.
        /// </summary>
        public string MigrationName { get; }

        /// <summary>
        /// Gets the lines of the migration file.
        /// </summary>
        public string[] Lines { get; }

        /// <summary>
        /// Gets the Up body of the migration, or null if no Up method body could be located.
        /// </summary>
        public string? UpBody { get; }

        /// <summary>
        /// Gets the Down body of the migration, or null if no Down method body could be located.
        /// </summary>
        public string? DownBody { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MigrationFile"/> class.
        /// </summary>
        /// <param name="filePath">The file path of the migration file.</param>
        /// <param name="migrationName">The name of the migration.</param>
        /// <param name="lines">The lines of the migration file.</param>
        /// <param name="upBody">The Up body of the migration.</param>
        /// <param name="downBody">The Down body of the migration.</param>
        [System.Text.Json.Serialization.JsonConstructor]
        public MigrationFile(string filePath, string migrationName, string[] lines, string? upBody, string? downBody)
        {
            FilePath = filePath;
            MigrationName = migrationName;
            Lines = lines;
            UpBody = upBody;
            DownBody = downBody;
        }

        /// <summary>
        /// Parses a migration file from the specified file path, throwing exceptions for invalid arguments or file access errors.
        /// </summary>
        /// <param name="filePath">The file path of the migration file.</param>
        /// <returns>A <see cref="MigrationFile"/> instance if the file is a valid migration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is empty or whitespace.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown when the directory does not exist.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown when access to the file is denied.</exception>
        /// <exception cref="IOException">Thrown when the file cannot be read.</exception>
        /// <exception cref="FormatException">Thrown when the file is not a valid migration file.</exception>
        public static MigrationFile Parse(string filePath)
        {
            ArgumentNullException.ThrowIfNull(filePath);

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path cannot be empty or whitespace.", nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Migration file not found.", filePath);
            }

            string[] lines;
            try
            {
                lines = File.ReadAllLines(filePath);
            }
            catch (DirectoryNotFoundException ex)
            {
                throw new DirectoryNotFoundException("Migration file directory not found.", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new UnauthorizedAccessException("Access to migration file is denied.", ex);
            }
            catch (IOException ex)
            {
                throw new IOException("Failed to read migration file.", ex);
            }

            string? migrationName;
            string? upBody = null;
            string? downBody = null;

            if (!IsMigration(lines, out migrationName))
            {
                throw new FormatException("The specified file is not a valid migration file.");
            }

            int upStartIndex = -1;
            int upEndIndex = -1;
            int downStartIndex = -1;
            int downEndIndex = -1;

            for (int i = 0; i < lines.Length; i++)
            {
                // EF Core scaffolds "void Up(MigrationBuilder migrationBuilder)"; match any parameter name.
                if (Regex.IsMatch(lines[i], @"\bUp\s*\(\s*MigrationBuilder\b"))
                {
                    upStartIndex = i;
                    upEndIndex = FindMethodEnd(lines, i);
                }
                else if (Regex.IsMatch(lines[i], @"\bDown\s*\(\s*MigrationBuilder\b"))
                {
                    downStartIndex = i;
                    downEndIndex = FindMethodEnd(lines, i);
                }
            }

            if (upStartIndex != -1 && upEndIndex != -1)
                upBody = ExtractBody(lines, upStartIndex, upEndIndex);

            if (downStartIndex != -1 && downEndIndex != -1)
                downBody = ExtractBody(lines, downStartIndex, downEndIndex);

            return new MigrationFile(filePath, migrationName!, lines, upBody, downBody);
        }

        /// <summary>
        /// Tries to parse a migration file from the specified file path.
        /// </summary>
        /// <param name="filePath">The file path of the migration file.</param>
        /// <returns>A <see cref="MigrationFile"/> instance if the file is a valid migration, otherwise null.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is empty or whitespace.</exception>
        public static MigrationFile? TryParse(string filePath)
        {
            ArgumentNullException.ThrowIfNull(filePath);

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path cannot be empty or whitespace.", nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                return null;
            }

            try
            {
                string[] lines = File.ReadAllLines(filePath);
                string? migrationName;
                string? upBody = null;
                string? downBody = null;

                if (!IsMigration(lines, out migrationName))
                    return null;

                int upStartIndex = -1;
                int upEndIndex = -1;
                int downStartIndex = -1;
                int downEndIndex = -1;

                for (int i = 0; i < lines.Length; i++)
                {
                    // EF Core scaffolds "void Up(MigrationBuilder migrationBuilder)"; match any parameter name.
                    if (Regex.IsMatch(lines[i], @"\bUp\s*\(\s*MigrationBuilder\b"))
                    {
                        upStartIndex = i;
                        upEndIndex = FindMethodEnd(lines, i);
                    }
                    else if (Regex.IsMatch(lines[i], @"\bDown\s*\(\s*MigrationBuilder\b"))
                    {
                        downStartIndex = i;
                        downEndIndex = FindMethodEnd(lines, i);
                    }
                }

                if (upStartIndex != -1 && upEndIndex != -1)
                    upBody = ExtractBody(lines, upStartIndex, upEndIndex);

                if (downStartIndex != -1 && downEndIndex != -1)
                    downBody = ExtractBody(lines, downStartIndex, downEndIndex);

                return new MigrationFile(filePath, migrationName!, lines, upBody, downBody);
            }
            catch (DirectoryNotFoundException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
            catch (IOException)
            {
                return null;
            }
        }

        /// <summary>
        /// Extracts the method body between the signature line and the closing brace,
        /// excluding a standalone opening-brace line so that an empty method yields
        /// an empty (whitespace-only) body.
        /// </summary>
        private static string ExtractBody(string[] lines, int startIndex, int endIndex)
        {
            int bodyStart = startIndex + 1;
            if (bodyStart < endIndex && lines[bodyStart].Trim() == "{")
                bodyStart++;

            return string.Join(Environment.NewLine, lines, bodyStart, Math.Max(0, endIndex - bodyStart));
        }

        /// <summary>
        /// Finds the line index of the closing brace that ends the method starting at
        /// <paramref name="startIndex"/>, using brace-depth tracking so nested blocks
        /// (e.g. <c>table => new { ... }</c> lambdas inside CreateTable) do not
        /// terminate the body early.
        /// </summary>
        /// <param name="lines">All lines of the file.</param>
        /// <param name="startIndex">Index of the method signature line.</param>
        /// <returns>The index of the method's closing brace line, or -1 if not found.</returns>
        private static int FindMethodEnd(string[] lines, int startIndex)
        {
            int depth = 0;
            bool opened = false;

            for (int i = startIndex; i < lines.Length; i++)
            {
                foreach (char c in lines[i])
                {
                    if (c == '{')
                    {
                        depth++;
                        opened = true;
                    }
                    else if (c == '}')
                    {
                        depth--;
                        if (opened && depth == 0)
                            return i;
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// Checks if the specified lines represent a migration.
        /// </summary>
        /// <param name="lines">The lines to check.</param>
        /// <param name="migrationName">The name of the migration.</param>
        /// <returns>True if the lines represent a migration, otherwise false.</returns>
        private static bool IsMigration(string[] lines, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? migrationName)
        {
            migrationName = null;
            foreach (string line in lines)
            {
                if (line.Contains(": Migration") || line.Contains("partial class") && line.Contains("Migration"))
                {
                    var match = Regex.Match(line, @"partial class (\w+)");
                    if (match.Success)
                    {
                        migrationName = match.Groups[1].Value;
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
