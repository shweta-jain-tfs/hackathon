namespace Pharos.Server.Application.Reagents;

public interface IInstrumentReagentStorageUtilization
{
    int GetAvailablePositions(string instrumentNumber, ReagentStorageType storageType);
}

public enum ReagentStorageType
{
    Carrier,
    Cartridge,
    DilutionPlate
}