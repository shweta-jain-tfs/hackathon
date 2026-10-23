using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Sample;

/// <summary>
/// Lists available gel separation layer detection modes.
/// </summary>
/// <remarks>
/// Do not modify numbers for the existing enum entries (they are persisted).
/// </remarks>
public enum GelSeparationLayerDetection
{
    [DisplayNameLocal("GelSeparationLayerDetection_OnNoError")]
    OnNoError = 0,

    [DisplayNameLocal("GelSeparationLayerDetection_OnError")]
    OnError = 1,

    [DisplayNameLocal("GelSeparationLayerDetection_Off")]
    DetectionOff = 2
}