using System.IO.Abstractions;
using Pharos.Server.SystemUpdate.Application.Interfaces;

namespace Pharos.Server.SystemUpdate.Application.Services;

internal class UpdateMediumValidator : IUpdateMediumValidator
{
    private readonly IFileSystem _fs;
    private const string _updateFolder = "Updates";

    public UpdateMediumValidator(IFileSystem fs)
    {
        _fs = fs;
    }

    public bool IsValidUpdateMedium(string driveName)
    {
        var updatePath = Path.Combine(driveName, _updateFolder);

        // For now, we simply check for the presence of the update folder.
        return _fs.Directory.Exists(updatePath);
    }
}
