using System.Globalization;

namespace Pharos.Server.Result.Application.Services.ResultExportService;

internal static class ResultExportFileName
{
    /// <summary>
    /// Generates a file name for the result export based on the provided CsvInformation.
    /// </summary>
    /// <param name="information">The information used to generate the file name.</param>
    /// <returns>The generated file name.</returns>
    public static string GenerateFileName(CsvInformation information)
    {
        return GenerateFileName(information.StartDateTime, information.InstrumentName, information.CustomRunIdentity, information.MethodName);
    }

    /// <summary>
    /// Generates a file name for the result export based on the provided parameters.
    /// </summary>
    /// <param name="startDateTime">The start date and time of the first analytical run.</param>
    /// <param name="instrumentName">The name of the instrument used in the analytical run.</param>
    /// <param name="customRunIdentity">The custom run identity used to generate the result export.</param>
    /// <param name="methodName">The method name used in the analytical run.</param>
    /// <returns>The generated file name.</returns>
    public static string GenerateFileName(DateTimeOffset startDateTime, string? instrumentName, string? customRunIdentity, string? methodName)
    {
        // Initialize the parts list with the prefix "result_export"
        // Add the sanitized instrument name if it is not null or empty
        var parts = new List<string>
        {
            string.IsNullOrWhiteSpace(instrumentName) ? "result_export" : SanitizeFileName(instrumentName),
        };
        
        // Add the sanitized custom run identity if it is not null or empty
        if (!string.IsNullOrWhiteSpace(customRunIdentity))
        {
            parts.Add(SanitizeFileName(customRunIdentity));
        }
        // Otherwise, add the method name if it is not null or empty
        else if (!string.IsNullOrWhiteSpace(methodName))
        {
            parts.Add(methodName);
        }

        // Add the start date and time in the specified format
        parts.Add(startDateTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
        parts.Add(startDateTime.ToString("HHmmss", CultureInfo.InvariantCulture));

        // Join the parts with a hyphen and add the .csv extension
        return $"{string.Join('-', parts)}.csv";
    }

    /// <summary>
    /// Sanitizes the file name by removing invalid characters.
    /// </summary>
    /// <param name="name">The name to sanitize.</param>
    /// <returns>The sanitized name.</returns>
    private static string SanitizeFileName(string name)
    {
        var invalids = Path.GetInvalidFileNameChars();
        return string.Join("_", name.Split(invalids, StringSplitOptions.RemoveEmptyEntries));
    }
}
