using Pharos.Server.Domain.Enums;
using Pharos.Server.Sample.Domain;

namespace Pharos.Server.Sample.Application.Interfaces;

internal interface IInstrumentSampleStorage
{
    SampleTube? GetTube(string tubeId);
    List<SampleTube> GetTubesBySampleId(string sampleId, bool includeTubesWithError = false);
    List<SampleTube> GetTubesBySampleIds(IReadOnlySet<string> sampleIds, bool includeTubesWithError = false);
    void AddOrUpdateTube(SampleTube tube, Func<SampleTube, SampleTube> updateTubeFunc);
    void RemoveTube(SampleTube tube);
    IQueryable<SampleTube> TubesAsQueryable(string instrumentNumber);

    SampleDrawer GetDrawer(string instrumentNumber, DrawerType drawerType);
    void UpdateDrawer(string instrumentNumber, SampleDrawer drawer);

    SampleTray? GetTray(string trayId);
    bool UpdateTraysSummary(string instrumentNumber);
    bool IsTrayInAnyInstrument(string trayId);
    bool UpdateTrayAsUnknownObject(string trayId);

    List<SampleTray> GetTrays(string? instrumentNumber = null, DrawerType? drawerType = null);

    void Clear(string instrumentNumber);
}