using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Commands.MethodTestDetails;
using Pharos.Server.Parameters.Application.Queries.GetMethodsAndTest;
using Riok.Mapperly.Abstractions;

namespace Pharos.Server.Parameters.Application.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
internal static partial class MethodTestUpdateMapper
{
    [MapProperty(nameof(UpdateMethodTestDetailsCommand.Replicates), nameof(MethodTest.UserReplicates))]
    [MapProperty(nameof(UpdateMethodTestDetailsCommand.DilutionMethod), nameof(MethodTest.UserDilutionMethod))]
    [MapProperty(nameof(UpdateMethodTestDetailsCommand.DilutionFactor), nameof(MethodTest.UserDilutionFactor))]
    [MapProperty(nameof(UpdateMethodTestDetailsCommand.ReportConcentration), nameof(MethodTest.UserReportConcentration))]
    [MapProperty(nameof(UpdateMethodTestDetailsCommand.MethodConcentrationUnitId), nameof(MethodTest.UserMethodConcentrationUnitId))]
    [MapProperty(nameof(UpdateMethodTestDetailsCommand.ReportCutOff), nameof(MethodTest.UserReportCutOff))]
    [MapProperty(nameof(UpdateMethodTestDetailsCommand.CutOffConcentration), nameof(MethodTest.UserCutOffConcentration))]
    [MapProperty(nameof(UpdateMethodTestDetailsCommand.ReportQuotient), nameof(MethodTest.UserReportQuotient))]
    [MapProperty(nameof(UpdateMethodTestDetailsCommand.TextQuantitativeAbove), nameof(MethodTest.TextQuantitativeAbove))]
    [MapProperty(nameof(UpdateMethodTestDetailsCommand.TextQuantitativeBelow), nameof(MethodTest.TextQuantitativeBelow))]
    [MapProperty(nameof(UpdateMethodTestDetailsCommand.TextQuantitativeBelowForDilutedTest), nameof(MethodTest.UserTextQuantitativeBelowForDilutedTest))]
    [MapProperty(nameof(UpdateMethodTestDetailsCommand.TextQuantitativeAboveForDilutedTest), nameof(MethodTest.UserTextQuantitativeAboveForDilutedTest))]
    [MapperIgnoreSource(nameof(UpdateMethodTestDetailsCommand.Id))]
    [MapperIgnoreSource(nameof(UpdateMethodTestDetailsCommand.ReportClass))]
    [MapperIgnoreSource(nameof(UpdateMethodTestDetailsCommand.ReportClassId))]
    [MapperIgnoreSource(nameof(UpdateMethodTestDetailsCommand.Priority))]
    [MapperIgnoreSource(nameof(UpdateMethodTestDetailsCommand.QueriedAt))]
    [MapperIgnoreSource(nameof(UpdateMethodTestDetailsCommand.MethodTestLabNames))]
    public static partial void UpdateMethodTest(this UpdateMethodTestDetailsCommand source, MethodTest target);

    public static partial void UpdateMethodTestPriority(this MethodTestPriorityDto source, MethodTestPriority target);
}
