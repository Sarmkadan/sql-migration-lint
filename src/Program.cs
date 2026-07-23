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

        // New format option – supports "text" (default) and "json"
        var formatOption = new Option<string>(
            "--format",
            () => "text",
            "Output format. Supported values: text, json.");

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

            var config = configPath != null ? LintConfig.Load(configPath) : null;

            var linter = MigrationLinter.CreateDefaultWithGlobalRules(configPath);
            var report = linter.Lint(path.FullName);

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
                Console.WriteLine(JsonSerializer.Serialize(report.Findings, options));
            }
            else
            {
                // Human‑readable console output
                ConsoleReportWriter.WriteReport(report);
            }

            // Compute and return the appropriate exit code
            var exitCode = report.ComputeExitCode(failOnSeverity);
            Environment.Exit(exitCode);
        }, pathArgument, failOnOption, jsonOption, ignoreOption, onlyLatestOption, formatOption, configOption);

        return rootCommand.InvokeAsync(args).GetAwaiter().GetResult();
    }
}