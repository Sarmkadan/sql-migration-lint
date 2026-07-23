using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SqlMigrationLint.JsonSerialization;

/// <summary>
/// Source-generated <see cref="JsonSerializerContext"/> covering the DTOs and rule configuration
/// types used throughout the linter, replacing the reflection-based serialization previously
/// duplicated across a number of per-type <c>*JsonExtensions</c> classes.
/// </summary>
/// <remarks>
/// Using a single source-generated context keeps naming policy and null-handling consistent in
/// exactly one place, avoids reflection at serialization time, and keeps the assembly friendly to
/// trimming and native AOT publishing.
/// </remarks>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false)]
[JsonSerializable(typeof(LintFinding))]
[JsonSerializable(typeof(IReadOnlyList<LintFinding>))]
[JsonSerializable(typeof(LintReport))]
[JsonSerializable(typeof(LintConfig))]
[JsonSerializable(typeof(RuleSeverityConfig))]
[JsonSerializable(typeof(Dictionary<string, RuleSeverityConfig>))]
[JsonSerializable(typeof(MigrationFile))]
[JsonSerializable(typeof(NamingConventionRule))]
[JsonSerializable(typeof(MissingDownMigrationRule))]
[JsonSerializable(typeof(IReadOnlyList<string>))]
public partial class LintJsonContext : JsonSerializerContext
{
}
