using CustomAction.CustomActionLogic;
using CustomAction.CustomActionLogic.Appsettings;
using CustomAction.CustomActionLogic.Database;
using CustomAction.Wrappers;
using WixToolset.Dtf.WindowsInstaller;

namespace CustomAction
{
    public class CustomActions
    {
        [CustomAction]
        public static ActionResult AddWindowsServiceAccountAsSqlLogin(Session session)
        {
            var databaseActions = CreateDatabaseActions(session);

            return databaseActions.AddWindowsServiceAccountAsSqlLogin();
        }

        [CustomAction]
        public static ActionResult BackupDatabase(Session session)
        {
            var databaseActions = CreateDatabaseActions(session);

            return databaseActions.BackupDatabase();
        }

        [CustomAction]
        public static ActionResult RestoreDatabaseBackup(Session session)
        {
            var databaseActions = CreateDatabaseActions(session);

            return databaseActions.RestoreDatabaseBackup();
        }

        private static DatabaseActions CreateDatabaseActions(Session session)
        {
            var sessionWrapper = new SessionWrapper(session);
            var fileSystem = new FileSystem();

            var appsettingsReader = new AppSettingsReader(sessionWrapper, fileSystem, new AppSettingsLocator(session.CustomActionData));
            var dbInstanceFactory = new DatabaseInstanceFactory(sessionWrapper, appsettingsReader);

            return new DatabaseActions(
                sessionWrapper,
                fileSystem,
                new LocalEnvironment(),
                dbInstanceFactory,
                new Date(),
                new ServiceAccountOptions(session.CustomActionData));
        }

        [CustomAction]
        public static ActionResult UpdateAppSettings(Session session)
        {
            var sessionWrapper = new SessionWrapper(session);
            var fileSystem = new FileSystem();
            var appsettingsUpdater = new AppSettingsUpdater(sessionWrapper, fileSystem, new AppSettingsLocator(session.CustomActionData), new AppSettingsOverrideOptions(session.CustomActionData));

            var appsettingsActions = new AppSettingsActions(sessionWrapper, appsettingsUpdater);

            return appsettingsActions.UpdateAppSettings();
        }

        [CustomAction]
        public static ActionResult EnableSecureEnclaveIfDisabled(Session session)
        {
            var databaseActions = CreateDatabaseActions(session);
            return databaseActions.EnableSecureEnclaveIfDisabled();
        }

        [CustomAction]
        public static ActionResult TestRollbackFailInstallation(Session session)
        {
            session.Log("TestRollbackFailInstallation: Deliberately failing installation to test rollback capabilities");
            return ActionResult.Failure;
        }

        [CustomAction]
        public static ActionResult RunInitDb(Session session)
        {
            var sessionWrapper = new SessionWrapper(session);
            var fileSystem = new FileSystem();
            var processRunner = new ProcessRunner();

            var actions = new ApplicationInitializationActions(sessionWrapper, fileSystem, processRunner);

            var installFolder = session.CustomActionData["INSTALLFOLDER"];
            return actions.RunInitDb(installFolder);
        }
    }
}
