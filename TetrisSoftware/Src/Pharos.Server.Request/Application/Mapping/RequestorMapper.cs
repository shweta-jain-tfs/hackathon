using Pharos.Server.Application.Lis;
using Pharos.Server.Request.Domain;
using Riok.Mapperly.Abstractions;

namespace Pharos.Server.Request.Application.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
internal static partial class RequestorMapper
{
    [MapperIgnoreSource(nameof(LisRequestor.HasNonUniqueRequestorId))]
    public static partial void UpdateRequestor(LisRequestor source, Requestor target);
}
