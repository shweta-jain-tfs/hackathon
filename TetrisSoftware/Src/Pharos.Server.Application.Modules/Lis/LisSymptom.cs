using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Application.Lis;

public record LisSymptom
{
    public SymptomType SymptomType { get; init; }
}