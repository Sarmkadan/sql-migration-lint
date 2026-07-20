using System;
using System.CommandLine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using SqlMigrationLint;

namespace SqlMigrationLint;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var pathArgument = new Argument<DirectoryInfo>("path", "The path to the migrations folder.")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        pathArgument.SetDefaultValue(new DirectoryInfo("."));

        var failOnOption = new Option<LintFindingSeverity>("--fail-on", "The severity level at which to exit with error.")
        {
            Arity = ArgumentArity.ExactlyOne
        };
        failOnOption.SetDefaultValue(LintFindingSeverity.Error);

        var jsonOption = new Option<bool>("--json", "Output findings in JSON format.");

        var ignoreOption = new Option<string[]>("--ignore", "Rule IDs to ignore.")
        {
            AllowMultipleArgumentsPerToken = true
        };

        var onlyLatestOption = new Option<int?>("--only-latest", "Lint only the last n migrations.");

        // New format option – supports "text" (default) and "json"
        var formatOption = new Option<string>(
            "--format",
            () => "text",
            "Output format. Supported values: text, json.");

        var rootCommand = new RootCommand("Lint EF Core migrations for dangerous operations.");
        rootCommand.AddArgument(pathArgument);
        rootCommand.AddOption(failOnOption);
        rootCommand.AddOption(jsonOption);
        rootCommand.AddOption(ignoreOption);
        rootCommand.AddOption(onlyLatestOption);
        rootCommand.AddOption(formatOption);

        rootCommand.SetHandler(async (path, failOnSeverity, json, ignore, onlyLatest, format) =>
        {
            var migrationsFolder = Path.Combine(path.FullName, "Migrations");
            if (!Directory.Exists(migrationsFolder))
            {
                Console.Error.WriteLine($"Migrations folder not found: {migrationsFolder}");
                Environment.Exit(1);
            }

            var migrationFiles = Directory.EnumerateFiles(migrationsFolder, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
                .Where(f => !Path.GetFileName(f).Contains("Snapshot", StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => Path.GetFileName(f))
                .ToArray();

            if (onlyLatest.HasValue)
            {
                migrationFiles = migrationFiles.TakeLast(onlyLatest.Value).ToArray();
            }

            var findings = new List<LintFinding>();
            var rules = new List<ILintRule>();
            rules.AddRange(DestructiveOperationRules.All);
            rules.AddRange(LockHeavyOperationRules.All);
            rules.Add(EmptyDownRule.Instance);

            int migrationsScanned = 0;

            foreach (var file in migrationFiles)
            {
                var migrationFile = MigrationFile.TryParse(file);
                if (migrationFile is null) continue;

                migrationsScanned++;

                var sqlOperation = new SqlOperation
                {
                    File = file,
                    Line = 1,
                    Sql = migrationFile.UpBody ?? string.Empty
                };

                foreach (var rule in rules)
                {
                    if (ignore?.Contains(rule.Name) == true) continue;

                    if (rule.AppliesTo(sqlOperation))
                    {
                        var result = rule.Evaluate(sqlOperation);
                        if (result is not null)
                            findings.Add(result);
                    }
                }
            }

            bool hasBlockers = findings.Any(f => f.Severity == LintSeverity.Blocker);
            RiskLevel maxRisk = RiskLevel.None;

            foreach (var f in findings)
            {
                var level = f.Severity switch
                {
                    LintSeverity.Blocker => RiskLevel.Blocker,
                    LintSeverity.Danger => RiskLevel.Danger,
                    LintSeverity.Warning => RiskLevel.Warning,
                    _ => RiskLevel.None
                };

                if (level > maxRisk)
                    maxRisk = level;
            }

            var report = new LintReport(findings, migrationsScanned, hasBlockers, maxRisk);

            // Output handling
            if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
            {
                // Full report in JSON format
                var jsonReport = JsonReportWriter.WriteReport(report, indented: true);
                Console.WriteLine(jsonReport);
            }
            else if (json)
            {
                // Legacy simple findings JSON
                var options = new JsonSerializerOptions { WriteIndented = true };
                Console.WriteLine(JsonSerializer.Serialize(findings, options));
            }
            else
            {
                // Human‑readable console output
                foreach (var finding in findings)
                {
                    Console.ForegroundColor = finding.Severity switch
                    {
                        LintSeverity.Blocker => ConsoleColor.Red,
                        LintSeverity.Danger => ConsoleColor.DarkYellow,
                        LintSeverity.Warning => ConsoleColor.Yellow,
                        _ => ConsoleColor.White
                    };
                    Console.WriteLine($"[{finding.Severity}] {finding.RuleName}: {finding.Message} ({finding.File}:{finding.Line})");
                    Console.ResetColor();
                }
            }

            var highestFindingSeverity = findings.Any()
                ? findings.Max(f => MapToFindingSeverity(f.Severity))
                : (LintFindingSeverity?)null;

            if (highestFindingSeverity.HasValue && highestFindingSeverity.Value >= failOnSeverity)
            {
                Environment.Exit(1);
            }
        }, pathArgument, failOnOption, jsonOption, ignoreOption, onlyLatestOption, formatOption);

        return await rootCommand.InvokeAsync(args);
    }

    private static LintFindingSeverity MapToFindingSeverity(LintSeverity severity) => severity switch
    {
        LintSeverity.Blocker => LintFindingSeverity.Error,
        LintSeverity.Danger => LintFindingSeverity.Error,
        LintSeverity.Warning => LintFindingSeverity.Warning,
        _ => LintFindingSeverity.Info
    };
}
