using MediatR;

namespace Pharos.Server.Application.Lis;

/// <summary>
/// This command is sent every time when Pharos receives a message with test orders from LIS.
/// </summary>
public sealed class ProcessLisTestOrdersCommand : IRequest<bool>
{
    public int LisChannelId { get; init; }

    /// <summary>
    /// The list of patients with samples and test order. See remarks for more information.
    /// </summary>
    /// <remarks>
    /// Every <see cref="LisPatient"/> object must represent a unique (for this request) patient.
    /// Every patient must have be linked with, at least, one sample and one test order.
    /// Test results that do not have patient information are combined under s single patient object.
    /// </remarks>
    public IList<LisPatient> Patients { get; init; } = [];
}