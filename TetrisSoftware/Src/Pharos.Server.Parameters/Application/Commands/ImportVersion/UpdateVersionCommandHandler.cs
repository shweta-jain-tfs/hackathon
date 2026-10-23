using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Parameters.Domain;

namespace Pharos.Server.Parameters.Application.Commands.ImportVersion;

internal class UpdateVersionCommandHandler(IParametersDbContext dbContext)
{
    public async Task Handle(ParameterUpdatePackageVersion parameterUpdatePackageVersion)
    {
        var newVersion = parameterUpdatePackageVersion.VersionNumber;
        var version = await dbContext.ParameterUpdatePackageVersion.FirstOrDefaultAsync();
        if (version != null)
        {
            if (version.VersionNumber > newVersion)
            {
                throw new InvalidOperationException($"Parameter Update Package version {newVersion} in the package is not higher than existing version {version.VersionNumber}.");
            }

            dbContext.Entry(version).State = EntityState.Deleted;
        }

        dbContext.ParameterUpdatePackageVersion.Add(new ParameterUpdatePackageVersion
        {
            VersionNumber = newVersion,
            ImportDate = DateTimeOffset.Now
        });
        await dbContext.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
    }
}