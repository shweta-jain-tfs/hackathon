namespace Pharos.Server.Domain.Parameters;

/// <summary>
/// The shape of the article indicator in Pharos UI.
/// </summary>
/// <remarks>
/// IMPORTANT: numbers match the articles file in the parameters package. Do NOT change them.
/// </remarks>
public enum ArticleIndicatorShape
{
    /// <summary>
    /// No shape is defined.
    /// </summary>
    None = 0,

    /// <summary>
    /// Round shape (circle).
    /// </summary>
    Round = 1,

    /// <summary>
    /// Rectangular shape (box).
    /// </summary>
    Rectangular = 2,

    /// <summary>
    /// Slanted shape (rhombus).
    /// </summary>
    Slanted = 3,
}
