using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace Pharos.Server.Parameters.FileComparer;

internal static class ParameterFileComparer
{
    private const string _noValue = "does_not_matter";

    public static void CheckMethodFilesEqual(string expectedFilePath, string actualFilePath, bool emptyValuesAsZeros = false, bool ignoreTrailingZeroesInDecimals = false)
    {
        using var expected = new FileStream(expectedFilePath, FileMode.Open, FileAccess.Read);
        using var actual = new FileStream(actualFilePath, FileMode.Open, FileAccess.Read);
        var expectedData = ReadMethodFileValues(expected);
        var actualData = ReadMethodFileValues(actual);
        CheckMethodFilesEqual(expectedData, actualData, emptyValuesAsZeros, ignoreTrailingZeroesInDecimals);
    }

    private static Dictionary<string, string?> ReadMethodFileValues(Stream stream)
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        using var reader = new StreamReader(stream);
        var sectionPrefix = string.Empty;

        while (reader.Peek() != -1)
        {
            var rawLine = reader.ReadLine()!; // Since Peak didn't return -1, stream hasn't ended.
            var line = rawLine.Trim();

            // Ignore blank lines
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }
            // Ignore comments
            if (line[0] is ';' or '#' or '/')
            {
                continue;
            }
            // [Section:header]
            if (line[0] == '[' && line[line.Length - 1] == ']')
            {
                // remove the brackets
                sectionPrefix = string.Concat(line.AsSpan(1, line.Length - 2).Trim(), ConfigurationPath.KeyDelimiter);
                continue;
            }

            // key = value OR "value"
            var separator = line.IndexOf('=');
            string key;
            string value;
            if (separator < 0)
            {
                // support values without keys
                key = line;
                value = _noValue;
            }
            else
            {
                key = sectionPrefix + line.Substring(0, separator).Trim();
                value = line.Substring(separator + 1).Trim();
            }

            // Remove quotes
            if (value.Length > 1 && value[0] == '"' && value[value.Length - 1] == '"')
            {
                value = value.Substring(1, value.Length - 2);
            }

            if (data.ContainsKey(key))
            {
                // make the key unique
                var index = 2;
                while (data.ContainsKey(key + index))
                {
                    index++;
                }
                key += index;
            }

            data.Add(key, value);
        }

        return data;
    }

    private static void CheckMethodFilesEqual(Dictionary<string, string?> expected, Dictionary<string, string?> actual, bool emptyValuesAsZeros = false, bool ignoreTrailingZeroesInDecimals = false)
    {
        if (expected.Count != actual.Count)
        {
            throw new InvalidOperationException("Both files must contain the same number of parameters.");
        }

        foreach (var actualEntry in actual)
        {
            var actualValue = actualEntry.Value ?? "";

            if (actualValue == _noValue)
            {
                // this is a table record, not a normal ini entry
                var matchingEntry = expected.FirstOrDefault(x => AreArraysEqual(x.Key, actualEntry.Key, emptyValuesAsZeros, ignoreTrailingZeroesInDecimals));
                if (matchingEntry.Equals(default(KeyValuePair<string, string?>)))
                {
                    throw new InvalidOperationException($"No matching row was found for '{actualEntry.Key}'.");
                }
            }
            else
            {
                var key = actualEntry.Key;
                if (!expected.TryGetValue(key, out var expectedValue))
                {
                    throw new InvalidOperationException("Both files must contain the same set of parameters.");
                }
                expectedValue ??= "";
                if (expectedValue.Contains(';'))
                {
                    CheckArraysAreEqual(key, expectedValue, actualValue, emptyValuesAsZeros, ignoreTrailingZeroesInDecimals);
                    continue;
                }
                if (AreValuesEqual(expectedValue, actualValue, emptyValuesAsZeros, ignoreTrailingZeroesInDecimals))
                {
                    continue;
                }
                throw new InvalidOperationException($"The expected value for the key '{key}' is '{expectedValue}' but the actual value is '{actualValue}'.");
            }
        }
    }

    private static bool AreArraysEqual(string expected, string actual, bool emptyValuesAsZeros = false, bool ignoreTrailingZeroesInDecimals = false)
    {
        var expectedValues = expected.Split(';');
        var actualValues = actual.Split(';');
        if (expectedValues.Length != actualValues.Length)
        {
            return false;
        }

        for (var i = 0; i < expectedValues.Length; i++)
        {
            if (AreValuesEqual(expectedValues[i], actualValues[i], emptyValuesAsZeros, ignoreTrailingZeroesInDecimals))
            {
                continue;
            }
            return false;
        }
        return true;
    }

    private static void CheckArraysAreEqual(string key, string expected, string actual, bool emptyValuesAsZeros = false, bool ignoreTrailingZeroesInDecimals = false)
    {
        var expectedValues = expected.Split(';');
        var actualValues = actual.Split(';');
        if (expectedValues.Length != actualValues.Length)
        {
            throw new InvalidOperationException($"Array values for the '{key}' key should have the same length.");
        }

        for (var i = 0; i < expectedValues.Length; i++)
        {
            if (AreValuesEqual(expectedValues[i], actualValues[i], emptyValuesAsZeros, ignoreTrailingZeroesInDecimals))
            {
                continue;
            }
            throw new InvalidOperationException($"The expected value for '{key}[{i}]' is '{expectedValues[i]}' but the actual value is '{actualValues[i]}'.");
        }
    }

    private static bool AreValuesEqual(string value1, string value2, bool emptyValuesAsZeros = false, bool ignoreTrailingZeroesInDecimals = false)
    {
        if (string.Equals(value1, value2, StringComparison.Ordinal))
        {
            return true;
        }

        decimal decimalValue1, decimalValue2;
        var formatProvider = CultureInfo.InvariantCulture;

        if (emptyValuesAsZeros)
        {
            if (string.IsNullOrEmpty(value1))
            {
                if (decimal.TryParse(value2, formatProvider, out decimalValue2))
                {
                    return decimalValue2 == 0;
                }
                return false;
            }
            if (string.IsNullOrEmpty(value2))
            {
                if (decimal.TryParse(value1, formatProvider, out decimalValue1))
                {
                    return decimalValue1 == 0;
                }
                return false;
            }
        }

        if (ignoreTrailingZeroesInDecimals && decimal.TryParse(value1, formatProvider, out decimalValue1) && decimal.TryParse(value2, formatProvider, out decimalValue2))
        {
            return decimalValue1 == decimalValue2;
        }
        return false;
    }
}
