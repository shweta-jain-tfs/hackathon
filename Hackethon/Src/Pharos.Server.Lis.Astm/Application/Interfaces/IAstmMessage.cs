namespace Pharos.Server.Lis.Astm.Application.Interfaces;

/// <summary>
///     Defines an abstract ASTM-protocol message.
/// </summary>
internal interface IAstmMessage : ILisMessage
{
    /// <summary>
    ///     Gets the collection of records that compose the message.
    /// </summary>
    public IList<IAstmRecord> Records { get; }
}