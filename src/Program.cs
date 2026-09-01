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
    private const string MigrationsFolderName = "Migrations";
    private const string TextFormatName = "text";
    private const string JsonFormatName = "json";
    private const string GitHubFormatName = "github";
    private const string DesignerFileSuffix = ".Designer.cs";
    private const string SnapshotFileNameFragment = "Snapshot";
    private const string CSharpFileSearchPattern = "*.cs";

    public static int Main(string[] args)
    {
        var pathArgument = new Argument<DirectoryInfo>("path", "The path to the migrations folder.")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        pathArgument.SetDefaultValue(new DirectoryInfo("."));

        var failOnOption = new Option<LintFindingSeverity>("--fail-on", "The severity level at which to exit with error. Allowed values: Info, Warning, Error (default: Error).")
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

        // New format option – supports "text" (default), "json", and "github"
        var formatOption = new Option<string>(
            "--format",
            () => TextFormatName,
            $"Output format. Supported values: {TextFormatName}, {JsonFormatName}, {GitHubFormatName}.");

        var configOption = new Option<string?>("--config", "Path to .sqlmigrationlint.json configuration file.");

        var rootCommand = new RootCommand("Lint EF Core migrations for dangerous operations.");
        rootCommand.AddArgument(pathArgument);
        rootCommand.AddOption(failOnOption);
        rootCommand.AddOption(jsonOption);
        rootCommand.AddOption(ignoreOption);
        rootCommand.AddOption(onlyLatestOption);
        rootCommand.AddOption(formatOption);
        rootCommand.AddOption(configOption);

        rootCommand.SetHandler((path, failOnSeverity, json, ignore, onlyLatest, format, configPath) =>
        {
            var migrationsFolder = Path.Combine(path.FullName, MigrationsFolderName);
            if (!Directory.Exists(migrationsFolder))
            {
                Console.Error.WriteLine($"Migrations folder not found: {migrationsFolder}");
                Environment.Exit(1);
            }

            var migrationFiles = Directory.EnumerateFiles(migrationsFolder, CSharpFileSearchPattern, SearchOption.AllDirectories)
                .Where(f => !f.EndsWith(DesignerFileSuffix, StringComparison.OrdinalIgnoreCase))
                .Where(f => !Path.GetFileName(f).Contains(SnapshotFileNameFragment, StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => Path.GetFileName(f))
                .ToArray();

            if (onlyLatest.HasValue)
            {
                migrationFiles = migrationFiles.TakeLast(onlyLatest.Value).ToArray();
            }

            var linter = MigrationLinter.CreateDefaultWithGlobalRules(configPath);
            var report = linter.Lint(path.FullName);

            var selectedFiles = migrationFiles.ToHashSet(StringComparer.Ordinal);
            var ignoredRules = (ignore ?? Array.Empty<string>()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var filteredFindings = report.Findings
                .Where(f => !onlyLatest.HasValue || (f.File is not null && selectedFiles.Contains(f.File)))
                .Where(f => !ignoredRules.Contains(f.RuleName))
                .ToArray();

            var maxRisk = filteredFindings
                .Select(f => f.Severity switch
                {
                    LintSeverity.Blocker => RiskLevel.Blocker,
                    LintSeverity.Danger => RiskLevel.Danger,
                    LintSeverity.Warning => RiskLevel.Warning,
                    _ => RiskLevel.None
                })
                .DefaultIfEmpty(RiskLevel.None)
                .Max();

            report = new LintReport(
                filteredFindings,
                onlyLatest.HasValue ? migrationFiles.Length : report.MigrationsScanned,
                filteredFindings.Any(f => f.Severity == LintSeverity.Blocker),
                maxRisk);

            var outputFormat = json ? JsonFormatName : format;

            // Choose writer based on format flag
            IReportWriter writer = outputFormat.ToLowerInvariant() switch
            {
                JsonFormatName => new JsonReportWriter(indented: true),
                GitHubFormatName => new GitHubAnnotationsWriter(),
                _ => new ConsoleReportWriterAdapter()
            };

            // Write the report to the console (or appropriate output)
            writer.WriteReport(report, Console.Out);

            // Compute and return the appropriate exit code
            var exitCode = report.ComputeExitCode(failOnSeverity);
            Environment.Exit(exitCode);
        }, pathArgument, failOnOption, jsonOption, ignoreOption, onlyLatestOption, formatOption, configOption);

        return rootCommand.InvokeAsync(args).GetAwaiter().GetResult();
    }
}
