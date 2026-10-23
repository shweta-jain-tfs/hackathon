namespace Pharos.Server.SystemUpdate.Application.Interfaces;

internal interface IUpdateMediumValidator
{
    bool IsValidUpdateMedium(string driveName);
}
