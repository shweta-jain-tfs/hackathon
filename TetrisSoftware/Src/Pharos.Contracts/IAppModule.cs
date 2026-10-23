using System.Reflection;

namespace Pharos.Contracts;

/// <summary>
/// Represents an abstraction for an application module (both client- and server-side).
/// </summary>
public interface IAppModule
{
    /// <summary>
    /// Gets module assembly.
    /// </summary>
    Assembly Assembly { get; }
}