namespace Pharos.Server.Domain.Enums;

public enum ReagentPositionStatus
{
    // Maintain the order of this enum to support the sorting in the UI.
    AspirationError,
    Available,
    Disabled,
    CartridgeStorageFull,
    DilutionPlateNotDetected,
    DilutionPlateNotPickedUp,
    DisconnectedBottle,
    DispenseError,
    Empty,
    Expired,
    Identified,
    InternalStorageFull,
    InUse,
    MissingBottle,
    NotIdentified,
    Reserved,
    ReagentLoaderErrorUnknownObject,
    TransferNotPossible,
    UndetectedVolume,
    Unloading,
    UnloadRequested,
    UnexpectedVolume,
    UnknownArticle,
    UnknownBottle,
    UnknownCarrier,
    UnknownCartridge,
    UnreadableBarcode,
    UnreadableOcr
}