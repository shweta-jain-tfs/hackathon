namespace Pharos.Server.Parameters.Application.Interfaces;

internal interface IDilutionValidator
{
    Task Validate(int testId, int preDilutionFactor, int instrumentDilutionFactor);
}