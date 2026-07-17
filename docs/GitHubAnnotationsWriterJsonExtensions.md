# GitHubAnnotationsWriterJsonExtensions

Provides JSON serialization and deserialization helpers for `LintFinding` instances used when emitting GitHub Actions annotations from the SQL Migration Linter.

## API

### `public static string ToJson(this LintFinding finding)`
Serializes a single `LintFinding` to a JSON string suitable for GitHub Actions annotation output.  
- **Parameters**  
  - `finding`: The `LintFinding` instance to serialize.  
- **Return value**  
  - A JSON‑encoded string representing the finding.  
- **Exceptions**  
  - Throws `ArgumentNullException` if `finding` is `null`.  
  - Throws `JsonException` if serialization fails for any reason.

### `public static LintFinding? FromJson(this string json)`
Deserializes a JSON string into a single `LintFinding`.  
- **Parameters**  
  - `json`: The JSON string produced by `ToJson`.  
- **Return value**  
  - The deserialized `LintFinding`, or `null` if `json` is `null` or cannot be parsed.  
- **Exceptions**  
  - Throws `JsonException` if `json` is not `null` but contains malformed JSON that prevents deserialization.

### `public static bool TryFromJson(this string json, out LintFinding? finding)`
Attempts to deserialize a JSON string into a single `LintFinding` without throwing exceptions.  
- **Parameters**  
  - `json`: The JSON string to parse.  
  - `finding`: When the method returns `true`, contains the deserialized `LintFinding`; otherwise `null`.  
- **Return value**  
  - `true` if `json` was successfully parsed; otherwise `false`.  
- **Exceptions**  
  - None; all error conditions are reported via the return value.

### `public static string ToJson(this IReadOnlyList<LintFinding> findings)`
Serializes a collection of `LintFinding` instances to a JSON array string.  
- **Parameters**  
  - `findings`: The list of findings to serialize.  
- **Return value**  
  - A JSON‑encoded array string representing the findings.  
- **Exceptions**  
  - Throws `ArgumentNullException` if `findings` is `null`.  
  - Throws `JsonException` if serialization fails.

### `public static IReadOnlyList<LintFinding>? FromJsonToList(this string json)`
Deserializes a JSON array string into a read‑only list of `LintFinding` instances.  
- **Parameters**  
  - `json`: The JSON array string produced by the collection `ToJson` overload.  
- **Return value**  
  - A read‑only list of `LintFinding` objects, or `null` if `json` is `null` or cannot be parsed into a list.  
- **Exceptions**  
  - Throws `JsonException` if `json` is not `null` but contains malformed JSON that prevents deserialization into a list.

### `public static bool TryFromJson(this string json, out IReadOnlyList<LintFinding>? findings)`
Attempts to deserialize a JSON array string into a read‑only list of `LintFinding` instances without throwing exceptions.  
- **Parameters**  
  - `json`: The JSON array string to parse.  
  - `findings`: When the method returns `true`, contains the deserialized list; otherwise `null`.  
- **Return value**  
  - `true` if `json` was successfully parsed into a list; otherwise `false`.  
- **Exceptions**  
  - None; all error conditions are reported via the return value.

## Usage

### Serializing a single finding for GitHub Actions
```csharp
LintFinding finding = new LintFinding
{
    RuleId = "MG001",
    Message = "Unapplied migration detected",
    FilePath = "Migrations/20230901_AddTable.sql",
    Line = 12,
    Column = 5,
    EndLine = 12,
    EndColumn = 20,
    Severity = LintSeverity.Error
};

string json = finding.ToJson();
// Output the JSON to the GitHub Actions workflow log
Console.WriteLine(json);
```

### Deserializing a list of findings from a stored JSON blob
```csharp
string storedJson = File.ReadAllText("lint-findings.json");

if (storedJson.TryFromJson(out IReadOnlyList<LintFinding>? findings) && findings != null)
{
    foreach (var f in findings)
    {
        Console.WriteLine($"{f.Severity}: {f.Message}");
    }
}
else
{
    Console.Error.WriteLine("Failed to parse lint findings JSON.");
}
```

## Notes
- All methods are stateless and thread‑safe; they rely only on their input parameters and the thread‑safe `System.Text.Json` serializer internally.  
- Passing `null` for the input object or list results in an `ArgumentNullException` for the `ToJson` overloads; the `FromJson`/`TryFromJson` overloads treat a `null` JSON string as a missing value and return `null` or `false` accordingly.  
- Invalid JSON (e.g., malformed syntax, mismatched types) causes the throwing variants to raise a `JsonException`; the `TryFromJson` variants return `false` and set the output parameter to `null`.  
- The JSON format produced is a compact representation; no indentation or formatting guarantees are made beyond valid JSON.  
- These helpers are intended for internal use by the linter’s GitHub annotation writer; they do not perform any validation of the `LintFinding` content beyond serialization.
