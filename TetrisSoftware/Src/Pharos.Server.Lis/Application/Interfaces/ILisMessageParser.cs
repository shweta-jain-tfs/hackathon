namespace Pharos.Server.Lis.Application.Interfaces;

public interface ILisMessageParser<out TIMessage> where TIMessage : ILisMessage
{
    /// <summary>
    /// Parses a protocol-specific LIS message.
    /// </summary>
    /// <param name="message">The string containing a protocol-specific LIS message.</param>
    /// <returns></returns>
    TIMessage ParseMessage(string message);
}
