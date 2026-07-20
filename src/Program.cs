using System.CommandLine;
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

        var rootCommand = new RootCommand("Lint EF Core migrations for dangerous operations.");
        rootCommand.AddArgument(pathArgument);
        rootCommand.AddOption(failOnOption);
        rootCommand.AddOption(jsonOption);
        rootCommand.AddOption(ignoreOption);
        rootCommand.AddOption(onlyLatestOption);

        rootCommand.SetHandler(async (path, failOnSeverity, json, ignore, onlyLatest) =>
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

            foreach (var file in migrationFiles)
            {
                var migrationFile = MigrationFile.TryParse(file);
                if (migrationFile is null) continue;

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

            var maxRisk = findings.Count > 0 
                ? findings.Max(f => f.Severity switch {
                    LintSeverity.Blocker => RiskLevel.Blocker,
                    LintSeverity.Danger => RiskLevel.Danger,
                    LintSeverity.Warning => RiskLevel.Warning,
                    _ => RiskLevel.None
                }) 
                : RiskLevel.None;

            if (json)
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                Console.WriteLine(JsonSerializer.Serialize(findings, options));
            }
            else
            {
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
        }, pathArgument, failOnOption, jsonOption, ignoreOption, onlyLatestOption);

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
