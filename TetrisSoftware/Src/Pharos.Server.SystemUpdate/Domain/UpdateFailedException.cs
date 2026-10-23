namespace Pharos.Server.SystemUpdate.Domain;

/// <summary>
/// Represents an exception that is thrown when an update operation fails.
/// </summary>
internal class UpdateFailedException : Exception
{
    public UpdateFailedException(string message) : base(message) { }
    public UpdateFailedException(string message, Exception innerException) : base(message, innerException) { }
}
