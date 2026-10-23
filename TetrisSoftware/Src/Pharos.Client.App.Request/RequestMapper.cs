using Riok.Mapperly.Abstractions;

namespace Pharos.Client.App.Request;

[Mapper]
public static partial class RequestMapper
{
    public static partial CreateRequestCommand ToCreateRequestCommand(this RequestDetails source);

    [MapProperty(nameof(RequestDetails.QueriedAt), nameof(UpdateRequestCommand.RequestDetailsQueriedAt))]
    public static partial UpdateRequestCommand ToUpdateRequestCommand(this RequestDetails source);

    [MapProperty(nameof(RequestTest.ReflexDefinitions), nameof(NewUpdatedTest.ReflexDefinitionIds), Use = nameof(ToReflexDefinitionIds))]
    private static partial NewUpdatedTest? ToNewUpdatedTest(RequestTest? source);

    public static partial List<RequestsController_VolumeCalculationTestOrder> ToVolumeCalculationTests(this IEnumerable<RequestTest> source);

    [MapProperty(nameof(RequestTest.TestSortOrder), nameof(RequestsController_VolumeCalculationTestOrder.ExecutionOrder))]
    [MapProperty(nameof(RequestTest.Uid), nameof(RequestsController_VolumeCalculationTestOrder.TestOrderId))]
#pragma warning disable IDE0051
    private static partial RequestsController_VolumeCalculationTestOrder ToVolumeCalculationTest(this RequestTest source);
#pragma warning restore IDE0051

#pragma warning disable CA1859
    private static ICollection<int> ToReflexDefinitionIds(ICollection<RequestReflexDefinition> source)
#pragma warning restore CA1859
    {
        return source.Select(rd => rd.Id).ToList();
    }
}