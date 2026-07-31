using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SqlMigrationLint;

public static class DestructiveOperationRulesJsonExtensions
{
    private static readonly Regex _jsonSizeRegex = new Regex(@"^(\d+)$");
    private static readonly Regex _jsonSizeExceedsMaxRegex = new Regex(@"^(\d+)$");

    // ... rest of the code remains the same ...
}
