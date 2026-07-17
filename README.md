## GitHubAnnotationsWriterJsonExtensions

`GitHubAnnotationsWriterJsonExtensions` provides System.Text.Json serialization and deserialization extensions for `LintFinding` objects, enabling easy conversion between C# objects and JSON strings. These methods support both single findings and collections, with configurable formatting options and robust error handling.

Example usage:

```csharp
using System;
using System.IO;
using System.Text.Json;
using SqlMigrationLint;

class Program
{
    static void Main()
    {
        // Create a lint finding
        var finding = new LintFinding
        {
            File = "0001_AddUserEmail.cs",
            Line = 42,
            Message = "Adding non-nullable column to existing table",
            Severity = LintSeverity.Warning,
            RuleName = "NonNullableColumnOnExistingTable",
            Risk = 3
        };

        // Serialize to compact JSON
        string json = finding.ToJson();
        Console.WriteLine("Compact JSON:");
        Console.WriteLine(json);

        // Serialize to indented JSON
        string indentedJson = finding.ToJson(indented: true);
        Console.WriteLine("\nIndented JSON:");
        Console.WriteLine(indentedJson);

        // Deserialize back to object
        var deserialized = GitHubAnnotationsWriterJsonExtensions.FromJson(json);
        if (deserialized != null)
        {
            Console.WriteLine($"\nDeserialized: {deserialized.Message}");
        }

        // Try-parse with error handling
        if (GitHubAnnotationsWriterJsonExtensions.TryFromJson(json, out var parsedFinding))
        {
            Console.WriteLine($"TryFromJson succeeded: {parsedFinding?.Message}");
        }

        // Work with collections
        var findings = new List<LintFinding> { finding };
        string collectionJson = findings.ToJson();
        Console.WriteLine($"\nCollection JSON: {collectionJson}");

        var deserializedList = GitHubAnnotationsWriterJsonExtensions.FromJsonToList(collectionJson);
        if (deserializedList != null)
        {
            Console.WriteLine($"Deserialized list count: {deserializedList.Count}");
        }

        // Try-parse collection
        if (GitHubAnnotationsWriterJsonExtensions.TryFromJson(collectionJson, out var parsedList))
        {
            Console.WriteLine($"TryFromJson collection succeeded: {parsedList?.Count} items");
        }
    }
}
```