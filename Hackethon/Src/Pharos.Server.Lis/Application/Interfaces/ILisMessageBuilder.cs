using Pharos.Server.Application.Lis;
using Pharos.Server.Application.Settings.Lis;

namespace Pharos.Server.Lis.Application.Interfaces;

/// <summary>
/// Represents an abstraction of the message builder for specific LIS communication protocol.
/// </summary>
public interface ILisMessageBuilder
{
    /// <summary>
    /// Builds protocol-specific message for requesting test orders from LIS for a specified list of samples.
    /// </summary>
    ILisMessage BuildRequestMessage(LisChannel lisChannel, IReadOnlyCollection<string> sampleIds);

    /// <summary>
    /// Builds protocol-specific message for sending test results back to LIS.
    /// </summary>
    /// <param name="lisChannel"></param>
    /// <param name="patients">
    /// The list of patients with samples and test results. See remarks for more information.
    /// </param>
    /// <remarks>
    /// Every <see cref="LisPatient"/> object must represent a unique (for this request) patient.
    /// Every patient must have be linked with, at least, one sample and one test result.
    /// Test results that do not have patient information are combined under s single patient object.
    /// </remarks>
    /// <returns>A "ready-to-send" string representation of the LIS message.</returns>
    ILisMessage BuildSampleTestResultMessage(LisChannel lisChannel, IReadOnlyList<LisPatient> patients);

    /// <summary>
    /// Builds protocol-specific message for sending quality control test results to LIS.
    /// </summary>
    /// <param name="lisChannel">The LIS channel configuration.</param>
    /// <param name="qcTestResult">The quality control test result.</param>
    /// <returns>A "ready-to-send" string representation of the LIS message.</returns>
    ILisMessage BuildQualityControlTestResultMessage(LisChannel lisChannel, LisQualityControlTestResult qcTestResult);

    /// <summary>
    /// Builds protocol-specific message for sending an acknowledgement back to LIS.
    /// </summary>
    /// <param name="lisChannel">The LIS channel configuration.</param>
    /// <param name="messageMetadata">The metadata of the message being acknowledged.</param>
    /// <param name="success">Indicates whether the acknowledgement represents a successful processing.</param>
    /// <returns>A "ready-to-send" string representation of the LIS acknowledgement message.</returns>
    ILisMessage? BuildAcknowledgementMessage(LisChannel lisChannel, Dictionary<string, string> messageMetadata, bool success);
}