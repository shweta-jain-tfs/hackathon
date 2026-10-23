namespace Pharos.Server.LoadList.Application.Interfaces;

public interface ILoadListConfigurationInitializerService
{
    /// <summary>
    /// Initializes (or syncs) all load list configurations for an instrument.
    /// </summary>
    Task InitializeForInstrument(string instrumentSerial, CancellationToken cancellationToken);

    Task InitializeQcForInstrument(string instSerialNumber, CancellationToken cancellationToken);
}