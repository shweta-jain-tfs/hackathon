using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Application.AuditTrail.Enums
{
    public enum EventDataFields
    {
        // Authentication and Authorization
        [AllowedType(typeof(Guid))]
        UserId,

        [AllowedType(typeof(string))]
        Username,

        [AllowedType(typeof(string))]
        DisplayName,

        [AllowedType(typeof(string))]
        RoleId,

        [AllowedType(typeof(string))]
        RoleName,

        [AllowedType(typeof(string))]
        Pin,

        [AllowedType(typeof(int))]
        MinimumLength,

        [AllowedType(typeof(string))]
        RequireDigit,

        [AllowedType(typeof(string))]
        RequireLowercase,

        [AllowedType(typeof(string))]
        RequireNonAlphanumeric,

        [AllowedType(typeof(string))]
        RequireUppercase,

        [AllowedType(typeof(List<string>))]
        Permissions,
        // TODO: More to be added

        // Patient's personal and health information
        [AllowedType(typeof(string))]
        PatientId,

        [AllowedType(typeof(string))]
        PatientNumber,

        [AllowedType(typeof(string))]
        PatientName,

        [AllowedType(typeof(DateOnly))]
        BirthDate,

        [AllowedType(typeof(int))]
        Age,

        [AllowedType(typeof(Gender))]
        Gender,

        [AllowedType(typeof(string))]
        Address,

        [AllowedType(typeof(string))]
        Diagnosis,

        [AllowedType(typeof(string))]
        Comments,

        [AllowedType(typeof(string))]
        ViewerUserId,

        [AllowedType(typeof(string))]
        ViewerUsername,
        // TODO: More to be added

        // Test Result
        [AllowedType(typeof(string))]
        TestResultId,

        [AllowedType(typeof(string))]
        SampleId,

        [AllowedType(typeof(string))]
        SampleDatabaseId,

        [AllowedType(typeof(string))]
        Test,

        [AllowedType(typeof(string))]
        Method,

        [AllowedType(typeof(string))]
        Response,

        [AllowedType(typeof(string))]
        Concentration,

        [AllowedType(typeof(string))]
        Class,

        [AllowedType(typeof(string))]
        Replicates,

        [AllowedType(typeof(string))]
        Dilution,

        [AllowedType(typeof(string))]
        PreDilution,

        [AllowedType(typeof(string))]
        InstrumentDilution,

        [AllowedType(typeof(DateTimeOffset))]
        MeasurementTime,

        [AllowedType(typeof(int))]
        ErrorCode,

        [AllowedType(typeof(string))]
        ErrorDescription,

        [AllowedType(typeof(string))]
        ApproverName,

        [AllowedType(typeof(string))]
        ApproverId,
        // TODO: More to be added

        // Error and failure audit
        [AllowedType(typeof(string))]
        InstrumentId,

        [AllowedType(typeof(string))]
        InstrumentName,
        // TODO: More to be added

        // Settings
        [AllowedType(typeof(Dictionary<string, string>))]
        ModifiedSettings,

        // Instrument - Sample Drawer Events
        [AllowedType(typeof(string))]
        InstrumentSerialNumber,

        [AllowedType(typeof(string))]
        DrawerType,

        [AllowedType(typeof(string))]
        UnlockMethod,

        [AllowedType(typeof(DateTimeOffset))]
        EventTime,

        // Sample Tray Position Events
        [AllowedType(typeof(int))]
        TubePosition,

        [AllowedType(typeof(string))]
        TubeBarcode,

        [AllowedType(typeof(string))]
        TrayId,

        // TODO: More to be added
    }
}