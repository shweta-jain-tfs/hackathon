namespace Pharos.Contracts.Security;

/// <summary>
/// Provides a set of predefined role names used for authorization within the application.
/// </summary>
public static class PredefinedRoles
{
    /// <summary>
    /// Gets an array containing all predefined role names.
    /// </summary>
    public static readonly string[] All =
    [
        Operator,
        Administrator,
        SeniorOperator,
        LabAssistant
    ];

    /// <summary>
    /// The role name for lab assistants.
    /// </summary>
    public const string LabAssistant = "lab_assistant";

    /// <summary>
    /// The role name for operators.
    /// </summary>
    public const string Operator = "operator";

    /// <summary>
    /// The role name for administrators.
    /// </summary>
    public const string Administrator = "administrator";

    /// <summary>
    /// The role name for senior operators.
    /// </summary>
    public const string SeniorOperator = "senior_operator";
}
