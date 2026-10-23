using Pharos.Server.Domain.Reflex;
using Pharos.Server.Request.Domain;
using Riok.Mapperly.Abstractions;

namespace Pharos.Server.Request.Application.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
internal static partial class ReflexTestMapper
{
    [MapperIgnoreSource(nameof(ReflexTest.Id))]
    [MapperIgnoreSource(nameof(ReflexTest.DomainEvents))]
    public static partial TestOrder ToTestOrder(this ReflexTest source);
}
