using System.ComponentModel.DataAnnotations;

namespace Pharos.Server.SystemUpdate.Domain;

internal sealed record UpdateMediumState(
    [property: MaxLength(2)]
    string DriveName);