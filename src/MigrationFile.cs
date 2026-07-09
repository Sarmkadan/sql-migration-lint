using System;
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
        /// Tries to parse a migration file from the specified file path.
        /// </summary>
        /// <param name="filePath">The file path of the migration file.</param>
        /// <returns>A <see cref="MigrationFile"/> instance if the file is a valid migration, otherwise null.</returns>
        public static MigrationFile? TryParse(string filePath)
        {
            string[] lines = System.IO.File.ReadAllLines(filePath);
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
                if (lines[i].Contains("Up(MigrationBuilder builder)"))
                {
                    upStartIndex = i;
                    for (int j = i + 1; j < lines.Length; j++)
                    {
                        if (lines[j].Trim().StartsWith("}"))
                        {
                            upEndIndex = j;
                            break;
                        }
                    }
                }
                else if (lines[i].Contains("Down(MigrationBuilder builder)"))
                {
                    downStartIndex = i;
                    for (int j = i + 1; j < lines.Length; j++)
                    {
                        if (lines[j].Trim().StartsWith("}"))
                        {
                            downEndIndex = j;
                            break;
                        }
                    }
                }
            }

            if (upStartIndex != -1 && upEndIndex != -1)
                upBody = string.Join(Environment.NewLine, lines, upStartIndex + 1, upEndIndex - upStartIndex - 1);

            if (downStartIndex != -1 && downEndIndex != -1)
                downBody = string.Join(Environment.NewLine, lines, downStartIndex + 1, downEndIndex - downStartIndex - 1);

            return new MigrationFile(filePath, migrationName, lines, upBody, downBody);
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
