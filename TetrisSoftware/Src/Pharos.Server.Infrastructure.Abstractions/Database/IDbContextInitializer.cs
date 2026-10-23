namespace Pharos.Server.Infrastructure.Database;

public enum RecreateObjectsMode
{
    Never,
    MigrationsPending,
    MigrationsPendingForBaseline
}

public interface IDbContextInitializer
{
    Task InitializeAsync(RecreateObjectsMode recreateMode);

    Task SeedAsync();
}
