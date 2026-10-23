using Tetris.Protocol.Pharos.Storage;

namespace Pharos.Server.Reagents.Application.Interfaces;

internal interface IInstrumentReagentStorage
{
    ReagentDrawer GetDrawer(string instrumentNumber);

    ReagentStorageUpdateResult UpdateDrawer(string instrumentNumber, ReagentDrawer drawer);

    IReadOnlyList<Cartridge> GetCartridges(string instrumentNumber);

    ReagentStorageUpdateResult UpdateCartridges(string instrumentNumber, IReadOnlyCollection<Cartridge> cartridges, bool fullList);

    IReadOnlyList<CarrierInformation> GetCarriers(string instrumentNumber);

    ReagentStorageUpdateResult UpdateCarriers(string instrumentNumber, IReadOnlyCollection<CarrierInformation> carriers, bool fullList);

    IReadOnlyList<DilutionPlateStack> GetDilutionPlatesDrawer(string instrumentNumber);

    bool UpdateDilutionPlatesDrawer(string instrumentNumber, IReadOnlyCollection<DilutionPlateStack> plates, bool fullList);

    IReadOnlyList<DilutionPlateInStation> GetDilutionPlatesStation(string instrumentNumber);

    bool UpdateDilutionPlatesStation(string instrumentNumber, IReadOnlyCollection<DilutionPlateInStation> plates, bool fullList);

    IReadOnlyList<BottledReagent> GetBottles(string instrumentNumber);

    bool UpdateBottles(string instrumentNumber, IReadOnlyCollection<BottledReagent> bottles, bool fullList);

    IReadOnlyList<WasteContainer> GetLiquidWaste(string instrumentNumber);

    void UpdateLiquidWaste(string instrumentNumber, IReadOnlyCollection<WasteContainer> wasteContainers);

    IReadOnlyList<RinseContainer> GetRinseContainers(string instrumentNumber);

    void UpdateRinseContainers(string instrumentNumber, IReadOnlyCollection<RinseContainer> rinseContainers);

    IReadOnlyList<WasteContainer> GetSolidWaste(string instrumentNumber);

    void UpdateSolidWaste(string instrumentNumber, IReadOnlyCollection<WasteContainer> solidWasteContainer);
}

internal class ReagentStorageUpdateResult
{
    public bool DrawerChanged { get; set; }
    public bool InternalStorageChanged { get; set; }
    public bool HasErrors { get; set; }
    public bool InternalStorageFull { get; set; }
}
