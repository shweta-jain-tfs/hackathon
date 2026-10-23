namespace Pharos.Server.Lis.Application.Interfaces;

/// <summary>
/// Defines an abstract LIS-protocol specific message.
/// </summary>
public interface ILisMessage
{
    /// <summary>
    /// Get the LIS message as a string. This is later converted to a byte[] and split up to frames to be sent to LIS.
    /// </summary>
    /// <param name="redactSensitiveData">Whether to redact sensitive data in the message, used for logging purposes.</param>
    /// <returns>The LIS message as a string</returns>
    string GetMessageString(bool redactSensitiveData = false);
}