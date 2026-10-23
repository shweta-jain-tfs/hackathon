namespace Pharos.Server.Domain.Common;

/// <summary>
/// Used to indicate that a something is considered sensitive data and should be protected.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ProtectedDataAttribute : Attribute
{
}
