using CustomAction.CustomActionLogic.Interfaces.AppSettings;
using CustomAction.CustomActionLogic.Interfaces.Database;
using CustomAction.Wrappers.Interfaces;

namespace CustomAction.CustomActionLogic.Database;
internal class DatabaseInstanceFactory : IDatabaseInstanceFactory
{
    private readonly ISessionWrapper _session;
    private readonly IAppSettingsReader _appSettingsReader;
    private DatabaseInstance _databaseInstance;

    public DatabaseInstanceFactory(ISessionWrapper session,
        IAppSettingsReader appSettingsReader)
    {
        _session = session;
        _appSettingsReader = appSettingsReader;
    }
    public IDatabaseInstance CreateInstance()
    {
        var connectionString = _appSettingsReader.GetPharosDbConnectionString();

        return _databaseInstance ??= new DatabaseInstance(new DbConnectionFactory(_session), new DbCommandFactory(), _session, connectionString);
    }
}
