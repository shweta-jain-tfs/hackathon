using MediatR;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Infrastructure;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Queries;
internal record GetMethodUpdatePackageDescription(Guid PackageFileId) : IRequest<string>;

internal class GetMethodUpdatePackageDescriptionHandler(
    ITempFileStorage tempFileStorage,
    IMethodUpdatePackageFactory packageFactory) : IRequestHandler<GetMethodUpdatePackageDescription, string>
{
    public async Task<string> Handle(GetMethodUpdatePackageDescription request, CancellationToken cancellationToken)
    {
        var filePath = tempFileStorage.GetFilePath(request.PackageFileId);
        if (filePath == null)
        {
            throw new NotFoundException("Cannot find the uploaded method update package file.");
        }

        using var package = packageFactory.Create(filePath, tempFileStorage.GetFileName(request.PackageFileId));
        return await package.GetDescription();
    }
}
