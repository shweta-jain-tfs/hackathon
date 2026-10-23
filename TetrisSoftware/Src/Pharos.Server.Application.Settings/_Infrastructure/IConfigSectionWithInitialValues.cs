using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings;

/// <summary>
/// Implement this interface in a class marked by <see cref="RootConfigSectionAttribute"/>
/// to provide initial settings values for either the production or development environment.
/// </summary>
public interface IConfigSectionWithInitialValues
{
    /// <summary>
    /// Fills the config section object with initial settings.
    /// </summary>
    /// <param name="isDevelopment">Indicates whether the initial settings should be for the development environment.</param>
    void InitializeConfigSection(bool isDevelopment);
}