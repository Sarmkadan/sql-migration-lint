using System;
using System.Linq;
using SqlMigrationLint;

namespace SqlMigrationLint.Tests;

/// <summary>
/// Guards against the JSON-extension code generator re-processing its own output and emitting
/// recursively-named helper types (for example "FooJsonExtensionsJsonExtensions").
/// </summary>
public static class JsonExtensionsCodegenTests
{
    /// <summary>
    /// Verifies that no public type exported by the <see cref="MigrationOperation"/> assembly has a
    /// name ending in "JsonExtensionsJsonExtensions", which would indicate the *JsonExtensions
    /// generator ran on a class it had already generated.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when one or more recursively generated JSON-extension types are found in the assembly.
    /// </exception>
    public static void NoPublicType_HasRecursivelyGeneratedJsonExtensionsName()
    {
        var offendingTypes = typeof(MigrationOperation).Assembly
            .GetExportedTypes()
            .Where(type => type.Name.EndsWith("JsonExtensionsJsonExtensions", StringComparison.Ordinal))
            .Select(type => type.FullName)
            .ToArray();

        if (offendingTypes.Length != 0)
        {
            throw new InvalidOperationException(
                $"Found recursively generated JSON-extension type(s): {string.Join(", ", offendingTypes)}");
        }
    }
}
