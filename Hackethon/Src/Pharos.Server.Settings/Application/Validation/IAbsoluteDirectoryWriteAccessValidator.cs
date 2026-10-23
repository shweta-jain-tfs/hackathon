namespace Pharos.Server.Settings.Application.Validation;

/// <summary>
/// Validates that the server can write to an absolute directory path setting.
/// </summary>
public interface IAbsoluteDirectoryWriteAccessValidator
{
    /// <summary>
    /// Validates write access for the supplied directory path.
    /// </summary>
    /// <param name="path">The directory path to validate.</param>
    /// <returns>Validation result indicating if the path is writable.</returns>
    AbsoluteDirectoryWriteAccessValidationResult Validate(string? path);
}

/// <summary>
/// Result of absolute directory write access validation.
/// </summary>
public record AbsoluteDirectoryWriteAccessValidationResult
{
    /// <summary>
    /// Indicates if the validation passed (true) or failed (false).
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// The path after environment variables have been expanded.
    /// </summary>
    public string? ExpandedPath { get; init; }

    /// <summary>
    /// Error message for validation failures.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static AbsoluteDirectoryWriteAccessValidationResult Success(string? expandedPath = null)
        => new() { IsValid = true, ExpandedPath = expandedPath };

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    public static AbsoluteDirectoryWriteAccessValidationResult Failure(string? expandedPath, string errorMessage)
        => new() { IsValid = false, ExpandedPath = expandedPath, ErrorMessage = errorMessage };
}
