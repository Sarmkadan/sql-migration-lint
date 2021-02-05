using System;
using System.Text.RegularExpressions;

namespace SqlMigrationLint
{
    public class MigrationFile
    {
        public string FilePath { get; }
        public string MigrationName { get; }
        public string[] Lines { get; }
        public string UpBody { get; }
        public string DownBody { get; }

        private MigrationFile(string filePath, string migrationName, string[] lines, string upBody, string downBody)
        {
            FilePath = filePath;
            MigrationName = migrationName;
            Lines = lines;
            UpBody = upBody;
            DownBody = downBody;
        }

        public static MigrationFile? TryParse(string filePath)
        {
            string[] lines = System.IO.File.ReadAllLines(filePath);
            string migrationName = null;
            string upBody = null;
            string downBody = null;

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

        private static bool IsMigration(string[] lines, out string migrationName)
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
