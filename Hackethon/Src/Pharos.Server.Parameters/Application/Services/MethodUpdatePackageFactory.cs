using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Services;
internal class MethodUpdatePackageFactory : IMethodUpdatePackageFactory
{
    public IMethodUpdatePackage Create(string filePath, string? originalFileName)
    {
        return new MethodUpdatePackage(filePath, originalFileName);
    }
}
