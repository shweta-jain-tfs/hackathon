using Pharos.Server.Settings.Application.Commands;
using Pharos.Server.Settings.Application.Metadata;

namespace Pharos.Server.Settings.Application.Validation;

/// <summary>
/// Validates PageList max items constraints for add, copy, and import operations.
/// </summary>
public interface IPageListMaxItemsValidator
{
    /// <summary>
    /// Validates if a new item can be added to a PageList based on max items constraint.
    /// Used by copy operations.
    /// </summary>
    /// <param name="pageListKey">The key of the PageList to validate.</param>
    /// <param name="rootPageLabel">The root page label for loading metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result indicating if adding is allowed.</returns>
    Task<PageListMaxItemsValidationResult> ValidateCanAddAsync(
        string pageListKey,
        string rootPageLabel,
        CancellationToken cancellationToken);

    /// <summary>
    /// Validates max items constraints for all PageLists in a page against existing settings.
    /// Used by add/update operations.
    /// </summary>
    /// <param name="page">The page containing PageLists to validate.</param>
    /// <param name="existingSettingKeys">All existing setting keys from the database.</param>
    /// <returns>Validation result with any constraint violations.</returns>
    PageListMaxItemsValidationResult ValidateExistingPageLists(
        Page page,
        IEnumerable<string> existingSettingKeys);

    /// <summary>
    /// Validates max items constraints for all PageLists in a page during import.
    /// Used by import operations to validate against the imported data structure.
    /// </summary>
    /// <param name="page">The page containing PageLists with imported entries.</param>
    /// <returns>Validation result with any constraint violations.</returns>
    PageListMaxItemsValidationResult ValidateImportedPageLists(Page page);
}

/// <summary>
/// Result of PageList max items validation.
/// </summary>
public record PageListMaxItemsValidationResult
{
    /// <summary>
    /// Indicates if the validation passed (true) or failed (false).
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// The current count of items in the PageList (for single PageList validation).
    /// </summary>
    public int CurrentCount { get; init; }

    /// <summary>
    /// The maximum allowed items for the PageList (null if no limit).
    /// </summary>
    public int? MaxItems { get; init; }

    /// <summary>
    /// User-friendly error message for single PageList validation failures.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// List of detailed validation errors for multiple PageList validation.
    /// </summary>
    public List<FailedSettingImportError> Errors { get; init; } = [];

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static PageListMaxItemsValidationResult Success(int currentCount = 0, int? maxItems = null)
        => new() { IsValid = true, CurrentCount = currentCount, MaxItems = maxItems };

    /// <summary>
    /// Creates a failed validation result for a single PageList.
    /// </summary>
    public static PageListMaxItemsValidationResult Failure(string errorMessage, int currentCount, int? maxItems)
        => new() { IsValid = false, ErrorMessage = errorMessage, CurrentCount = currentCount, MaxItems = maxItems };

    /// <summary>
    /// Creates a failed validation result with multiple errors.
    /// </summary>
    public static PageListMaxItemsValidationResult Failure(List<FailedSettingImportError> errors)
        => new() { IsValid = false, Errors = errors };
}
