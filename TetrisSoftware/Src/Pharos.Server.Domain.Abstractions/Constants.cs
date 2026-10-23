namespace Pharos.Server.Domain;

public static class Constants
{
    public const int ArticleBarcodeLength = 14;
    public const int ArticleUdiLength = 43;
    public const int InstrumentSerialNumberLength = 20;
    public const int InstrumentDisplayNameLength = 8;
    public const int InstrumentSoftwareVersionLength = 10;
    public const int MethodNameLength = 4;
    public const int LabTestNameLength = 10;
    public const int LotNumberLength = 5;
    public const int QualityControlNameLength = 16;
    public const int QualityControlBarcodeFormatNameLength = 15;
    public const int QualityControlIdOnBarcodeLength = 32;
    public const int SampleIdMaxLength = 32;
    public const int TubeBarcodeMaxLength = 32;
    public const int TestNameLength = 5;
    public const int TrayIdLength = 4;
    public const int OperatorCommentLength = 40;
    public const int ArticleNumberLength = 3;
    public const int ArticleIdentityLength = 16;

    // Sample
    public const int MaxSampleTubesInTray = 50;
    public const int MaxTraysInSampleDrawer = 4;
    public const int TubeIdMaxLength = 64;
    public const int SamplesInternalStorageMaxCap = 40;
    public const int InternalStoragePositionsReservedForQc = 5;

    // Result
    public const int ResultClassLength = 8;
    public const int ResultCutOffLength = 8;
    public const int ResponseAsTextLength = 7;
    public const int ConcentrationAsTextLength = 10;

    // Request
    public const int MaxReplicatesPerTest = 50;
    public const int MaxPredilutionFactor = 1000;

    // Deck Reagent
    public const int MaxReagentLoaders = 4;
    public const int MaxDilutionPlateStack = 4;
    public const int MaxReagentCompartments = 12;
    public const int MaxReagentCarriersInCompartment = 6;
    public const int MaxReagentCarrierUdi = 43;
    public const int MaxReagentLoaderPosition = 72;
    public const int MaxDilutionPlateNumber = 5;

    // Requestor
    public const int RequestorIdLength = 32;
    public const int NameLength = 75;
    public const int AddressLength = 150;
    public const int CommentLength = 80;
    public const int ZipCodeLength = 12;
    public const int OrderingPhysicianCodeLength = 32;
    public const int PhoneNumberLength = 20;
    public const int ShortcutCodeLength = 10;

    // Reflex
    public const int ReflexNameLength = 64;
    public const int ClassCompareValueLength = 64;
    public const int CutOffCompareValueLength = 64;

    //Inventory
    public const int MaxCarrierStoragePositions = 630;
    public const int MaxCartridgeStoragePositionsFront = 17;
    public const int MaxCartridgeStoragePositionsRear = 56;
    public const int MaxBottlesStoragePosition = 8;
    public const int MaxDilutionPlateWells = 96;

    // Audit Trail
    public const int EventDataLength = 64000;
    public const int RecordNoteLength = 255;
    public const int EventIdLength = 55;
    public const int HashLength = 256;
    public const int UsernameLength = 50;
    public const int AuditTrailCommentLength = 1000;

    //Bottles
    public const int MaxQuantityStopSolution = 3000;
    public const int MaxQuantityWashAdditive = 1000;
    public const int MaxQuantityWashConcentrateA = 3000;
    public const int MaxQuantityWashConcentrateB = 1000;
    public const int BottleCutoffQuantityForSize = 1200;

    //Liquids
    public const int DefaultWasteLevel = 0;
    public const int DefaultRinseLevel = 100;
    public const int DefaultCapDispenseWarningHours = 1;

    //Patient
    public const int PatientNameLength = 75;
    public const int PatientNumberLength = 12;
    public const int MedicalRecordNumberLength = 12;
    public const int PatientIdLength = 75;
    public const int PatientAddressLength = 150;
    public const int PatientZipCodeLength = 12;
    public const int PatientDiagnosisLength = 200;
    public const int PatientCommentLength = 1000;

    // LabCommunity
    public const int LabCommunityLaboratoryCodeLength = 6;
    public const int LabCommunityUsernameLength = 50;
    public const int LabCommunityPasswordLength = 50;
    public const int ExportItemLastErrorLength = 512;

    //Liquids and Waste
    public const int MaxLiquidWasteContainers = 2;
    public const int MaxRinseContainers = 2;
}