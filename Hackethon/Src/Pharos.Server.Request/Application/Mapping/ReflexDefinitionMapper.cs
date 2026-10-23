using Pharos.Server.Domain.Reflex;
using Pharos.Server.Request.Domain;
using Riok.Mapperly.Abstractions;

namespace Pharos.Server.Request.Application.Mapping;

[Mapper]
internal static partial class ReflexDefinitionMapper
{
    [MapperIgnoreTarget(nameof(ReflexDefinition.Id))]
    [MapperIgnoreTarget(nameof(ReflexDefinition.TestOrderId))]
    [MapperIgnoreTarget(nameof(ReflexDefinition.DomainEvents))]
    public static partial ReflexDefinition CloneReflexDefinition(IReflexDefinition source);

    public static List<ReflexDefinition> CloneReflexDefinitions(List<IReflexDefinition> source)
    {
        return source.Select(CloneReflexDefinition).ToList();
    }

    [MapperIgnoreTarget(nameof(ReflexTest.Id))]
    [MapperIgnoreTarget(nameof(ReflexTest.DomainEvents))]
    private static partial ReflexTest CloneReflexTest(ReflexTest source);
}
