using Pharos.Server.Domain.Reflex;
using Pharos.Server.Reflex.Application.Queries;
using Pharos.Server.Reflex.Application.Queries.GetReflexDefinition;
using Pharos.Server.Reflex.Domain;
using Riok.Mapperly.Abstractions;

namespace Pharos.Server.Reflex.Application.Mapping;

[Mapper]
internal static partial class ReflexMapper
{
    [MapperIgnoreTarget(nameof(ReflexDefinitionDto.CommentCode))]
    [MapperIgnoreTarget(nameof(ReflexDefinitionDto.HasRequestorEnabled))]
    [MapperIgnoreTarget(nameof(ReflexDefinitionDto.QueriedAt))]
    public static partial ReflexDefinitionDto ToReflexDefinitionDto(this ReflexDefinition source);

    [MapperIgnoreTarget(nameof(ReflexConditionDto.ReportingConcentration))]
    [MapperIgnoreTarget(nameof(ReflexConditionDto.ReportingClass))]
    [MapperIgnoreTarget(nameof(ReflexConditionDto.ReportingQuotient))]
    [MapperIgnoreTarget(nameof(ReflexConditionDto.ReportingCutOff))]
    [MapperIgnoreTarget(nameof(ReflexConditionDto.ClassOptions))]
    [MapperIgnoreTarget(nameof(ReflexConditionDto.CutOffOptions))]
    public static partial ReflexConditionDto ToReflexConditionDto(this ReflexCondition source);

    [MapperIgnoreTarget(nameof(ReflexTestDto.TestFullName))]
    [MapperIgnoreTarget(nameof(ReflexTestDto.MethodFullName))]
    [MapperIgnoreSource(nameof(ReflexTest.DomainEvents))]
    public static partial ReflexTestDto ToReflexTestDto(this ReflexTest source);

    public static partial ReflexCondition ToReflexCondition(this ReflexConditionDto source);
}
