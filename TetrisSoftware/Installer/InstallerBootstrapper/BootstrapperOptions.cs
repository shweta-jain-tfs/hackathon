namespace InstallerBootstrapper;

internal class BootstrapperOptions
{
    public bool SecureEnclavesEnabled { get; set; } = true;
    public bool TestRollback { get; set; }
    public bool ShowHelp { get; set; }
    public bool ParseError { get; set; }
}