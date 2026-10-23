namespace Pharos.Server.Application;

/// <summary>
/// Defines an accessor-service for getting the user associated with the executing the request.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the ID of the user associated with the executing the request.
    /// If it returns null, this means that the code execution started in the background
    /// (not as an HTTP request to the server API).
    /// </summary>
    Guid? UserId { get; }
}
