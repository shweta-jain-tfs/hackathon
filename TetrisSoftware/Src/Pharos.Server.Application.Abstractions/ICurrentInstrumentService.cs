namespace Pharos.Server.Application;

/// <summary>
/// Defines an accessor-service for getting the instrument associated with executing the request.
/// </summary>
public interface ICurrentInstrumentService
{
    /// <summary>
    /// Gets the ID of the instrument associated with executing the request.
    /// If the return value is null this means that the request was made from a desktop or a mobile client.
    /// </summary>
    int? InstrumentId { get; }

    /// <summary>
    /// Gets the serial number of the instrument associated with executing the request.
    /// If the return value is null this means that the request was made from a desktop or a mobile client.
    /// </summary>
    string? InstrumentNumber { get; }
}
