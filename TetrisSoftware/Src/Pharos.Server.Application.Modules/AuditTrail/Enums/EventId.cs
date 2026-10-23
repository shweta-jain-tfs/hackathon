namespace Pharos.Server.Application.AuditTrail.Enums
{
    public enum EventId
    {
        // Authentication and Authorization
        UserLoggedIntoPharos = 1,
        UserLoggedOutPharos = 2,
        FailedLoginAttempt = 3,
        UserLoggedIntoServiceUi = 4,
        UserLoggedOutServiceUi = 5,
        NewUserAccountCreated = 6,
        PasswordComplexityReqChanged = 7,
        PasswordReset = 8,
        UserAccountModified = 9,
        UserAccountTempLockedOut = 10,
        UserAccountDisabled = 11,
        UserAccountPermanentlyDeleted = 12,
        RoleCreated = 13,
        RoleModified = 14,
        RoleDeleted = 15,
        // TODO: More to be added

        // Sample
        SampleCreated = 20,
        SampleModified = 21,
        SampleDeleted = 22,
        TestAdded = 23,
        TestRemoved = 24,
        ManualEntry = 25,
        // TODO: More to be added

        // Patient's personal and health information
        RecordCreated = 40,
        RecordModified = 41,
        RecordDeleted = 42,
        HealthInfoViewed = 43,
        // TODO: More to be added

        // System Events
        SettingsChanged = 50,
        // TODO: More to be added

        // Test Result
        ResultReceived = 70,
        ResultViewed = 71,
        ResultExported = 72,
        TestProcessingError = 73,
        ResultAutomaticallyApproved = 74,
        ResultApproved = 75,
        ResultDeleted = 76,
        ResultRejected = 77,
        ResultApprovalFlagReset = 78,
        // TODO: More to be added

        // Settings
        SettingsExported = 90,
        SettingsImported = 91,
        RequestsImported = 92,
        // TODO: More to be added

        // Instrument - Sample Drawer Events
        SampleDrawerUnlock = 110,
        SampleDrawerOpened = 111,
        SampleDrawerClosed = 112,
        SampleDrawerUnauthorizedUnlockAttempt = 113,
        // TODO: More to be added
    }
}
