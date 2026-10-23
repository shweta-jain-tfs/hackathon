using Pharos.Server.Lis.Application.Interfaces;

namespace Pharos.Server.Lis.HL7.Application.Interfaces;

/// <summary>
/// Defines an abstract HL7-protocol message.
/// </summary>
internal interface IHL7Message : ILisMessage
{
    /// <summary>
    /// Gets the collection of segments that compose the message.
    /// </summary>
    public IList<IHL7Segment> Segments { get; }
}
