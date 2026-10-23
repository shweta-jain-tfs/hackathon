using Pharos.Server.Domain.Entities;

namespace Pharos.Server.Application.Request;

/// <summary>
/// Service for formatting sample IDs based on application settings.
/// This allows for consistent formatting of sample IDs across the application, and enables features such as reusing sample IDs with a date suffix if configured in the settings.
/// </summary>
public interface ISampleIdFormatter
{
    /// <summary>
    /// Formats the specified request identifier as a string representation suitable for display or logging.
    /// The formatting may include the creation date, depending on the application settings.
    /// If the setting to reuse sample IDs is enabled, the formatted string will include the sample ID followed by a date suffix.
    /// If the setting is disabled, only the sample ID will be included in the formatted output.
    /// </summary>
    /// <param name="sampleId">The unique identifier of the sample to include in the formatted string. Cannot be null.</param>
    /// <param name="created">The date and time when the sample was created. The value is included in the formatted output.</param>
    /// <returns>A string representation of the specified request identifier.</returns>
    string Format(string sampleId, DateTimeOffset? created);

    /// <summary>
    /// Formats the specified request identifier as a string representation suitable for display or logging.
    /// The formatting may include the creation date, depending on the application settings.
    /// If the setting to reuse sample IDs is enabled, the formatted string will include the sample ID followed by a date suffix.
    /// If the setting is disabled, only the sample ID will be included in the formatted output.
    /// </summary>
    /// <param name="requestUid">The unique identifier of the request to format. Cannot be null.</param>
    /// <returns>A string representation of the specified request identifier.</returns>
    string Format(RequestUid requestUid);
}