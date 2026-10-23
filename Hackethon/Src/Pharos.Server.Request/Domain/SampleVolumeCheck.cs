namespace Pharos.Server.Request.Domain;

/// <summary>
/// Volume Check behavior for a sample. 
/// </summary>
/// <remarks>
/// Do not modify numbers for the existing enum entries (they are persisted).
/// </remarks>
public enum SampleVolumeCheck
{
    Off = 0,
    OnRunAccordingToTestOrder = 1,
    OnRunNoTestsWhenVolumeTooLow = 2
}