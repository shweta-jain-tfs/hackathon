namespace Pharos.Common.Web.Components.Reagent;
public enum ReagentStatus
{
    NotIdentified = 1,
    Identified = 2
}

public enum ReagentError
{
    InternalStorageFull = 1,
    UnreadableBarcode = 2,
    TransferNotPossible = 3,
    UnknownArticle = 4,
    DilutionPlateNotDetected = 5,
    DilutionPlateNotPickedUp = 6,
    UnloadRequested = 7,
    ReagentLoaderErrorUnknownObject = 8,
    NeedEmptyLoader = 9,
    CloseDrawer = 10
}
