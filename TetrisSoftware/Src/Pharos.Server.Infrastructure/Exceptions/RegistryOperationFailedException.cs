namespace Pharos.Server.Infrastructure.Exceptions;

/// <summary>
/// Exception that occurs when certificate operations fail in a way that prevents the application from starting
/// </summary>
internal class RegistryOperationFailedException : Exception
{
    public RegistryOperationFailedException(string message) : base(message) { }
    public RegistryOperationFailedException(string message, Exception innerException) : base(message, innerException) { }
}