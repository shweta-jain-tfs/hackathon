using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Application.SystemErrors;

public static class ReflexPharosErrors
{
    public static CreatePharosErrorCommand ReflexDefinitionCreated(string reflexDefinitionName)
    {
        return Create(PharosErrorCodes.ReflexDefinitionCreated, reflexDefinitionName);
    }

    public static CreatePharosErrorCommand ReflexDefinitionDeleted(string reflexDefinitionName)
    {
        return Create(PharosErrorCodes.ReflexDefinitionDeleted, reflexDefinitionName);
    }

    public static CreatePharosErrorCommand AutomaticFollowUpTestsAdded(
        string reflexDefinitionName,
        string sampleId,
        string requestId,
        string triggerTest,
        IEnumerable<string> followUpTests)
    {
        return FollowUpTests(
            PharosErrorCodes.AutomaticFollowUpTestsAdded,
            reflexDefinitionName,
            sampleId,
            requestId,
            triggerTest,
            followUpTests);
    }

    public static CreatePharosErrorCommand ManualFollowUpTestsAdded(
        string reflexDefinitionName,
        string sampleId,
        string requestId,
        string triggerTest,
        IEnumerable<string> followUpTests)
    {
        return FollowUpTests(
            PharosErrorCodes.ManualFollowUpTestsAdded,
            reflexDefinitionName,
            sampleId,
            requestId,
            triggerTest,
            followUpTests);
    }

    public static CreatePharosErrorCommand ManualFollowUpTestsDiscarded(
        string reflexDefinitionName,
        string sampleId,
        string requestId,
        string triggerTest,
        IEnumerable<string> followUpTests)
    {
        return FollowUpTests(
            PharosErrorCodes.ManualFollowUpTestsDiscarded,
            reflexDefinitionName,
            sampleId,
            requestId,
            triggerTest,
            followUpTests);
    }

    private static CreatePharosErrorCommand Create(int errorCode, params string[] parameters)
    {
        return new CreatePharosErrorCommand(
            errorCode,
            ErrorClassification.Information,
            InstrumentNumber: null,
            Parameters: [.. parameters],
            AllowMultiple: true);
    }

    private static CreatePharosErrorCommand FollowUpTests(
        int errorCode,
        string reflexDefinitionName,
        string sampleId,
        string requestId,
        string triggerTest,
        IEnumerable<string> followUpTests)
    {
        return Create(
            errorCode,
            reflexDefinitionName,
            sampleId,
            requestId,
            triggerTest,
            string.Join(", ", followUpTests));
    }
}
