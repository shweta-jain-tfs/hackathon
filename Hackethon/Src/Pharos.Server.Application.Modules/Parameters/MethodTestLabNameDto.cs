namespace Pharos.Server.Application.Parameters;
/// <summary>
/// Data Transfer Object representing a laboratory test name associated with a method test.
/// </summary>
public class MethodTestLabNameDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the laboratory name.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the laboratory's name associated with the method test.
    /// </summary>
    public string TestName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the listing order of the laboratory test name, used when multiple names exist for one method test.
    /// </summary>
    public int SortOrder { get; set; }
}
