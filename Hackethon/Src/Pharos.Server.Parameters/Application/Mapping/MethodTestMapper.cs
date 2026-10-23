using Pharos.Server.Application.Parameters;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Queries.GetMethodsAndTest;
using Riok.Mapperly.Abstractions;

namespace Pharos.Server.Parameters.Application.Mapping;

[Mapper]
internal static partial class MethodTestMapper
{
    [MapProperty([nameof(MethodTest.Method), nameof(Method.MethodName)], nameof(MethodTestDetailsDto.MethodName))]
    [MapProperty([nameof(MethodTest.DefaultConcentrationUnit), nameof(MethodConcentrationUnit.ConcentrationUnit)], nameof(MethodTestDetailsDto.DefaultConcentrationUnit))]
    [MapProperty([nameof(MethodTest.Method), nameof(Method.PreventDilutionFactorsBelowDefault)], nameof(MethodTestDetailsDto.PreventDilutionFactorsBelowDefault))]
    [MapProperty([nameof(MethodTest.Method), nameof(Method.Technology)], nameof(MethodTestDetailsDto.Technology))]
    [MapProperty([nameof(MethodTest.Method), nameof(Method.ConcentrationUnits)], nameof(MethodTestDetailsDto.ConcentrationUnits), Use = nameof(MapConcentrationUnits))]
    [MapPropertyFromSource(nameof(MethodTestDetailsDto.MethodTestLabNames), Use = nameof(MapMethodTestLabNames))]
    [MapperIgnoreTarget(nameof(MethodTestDetailsDto.ClassTables))]
    [MapperIgnoreTarget(nameof(MethodTestDetailsDto.QueriedAt))]
    [MapperIgnoreTarget(nameof(MethodTestDetailsDto.LaboratoryTestNamesDuplicatedAcrossMethods))]
    public static partial MethodTestDetailsDto ToMethodTestDetailsDto(this MethodTest source);

    private static List<MethodConcentrationUnitOptionDto> MapConcentrationUnits(IList<MethodConcentrationUnit> source)
    {
        return source
            .OrderBy(x => x.UnitId)
            .Select(ToMethodConcentrationUnitOptionDto)
            .ToList();
    }

    private static List<MethodTestLabNameDto> MapMethodTestLabNames(MethodTest source)
    {
        if (source.LabTestNames.Count == 0)
        {
            return
            [
                new MethodTestLabNameDto
                {
                    TestName = source.DefaultLabTestName,
                    SortOrder = 1
                }
            ];
        }

        return source.LabTestNames
            .OrderBy(x => x.SortOrder)
            .Select(ToMethodTestLabNameDto)
            .ToList();
    }

    private static partial MethodConcentrationUnitOptionDto ToMethodConcentrationUnitOptionDto(MethodConcentrationUnit source);

    private static partial MethodTestLabNameDto ToMethodTestLabNameDto(MethodTestLabName source);
}
