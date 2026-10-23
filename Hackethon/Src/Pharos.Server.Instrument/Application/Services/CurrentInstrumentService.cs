using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application;
using Pharos.Server.Application.Instrument;

namespace Pharos.Server.Instrument.Application.Services;

internal sealed class CurrentInstrumentService(
#if !DEBUG
    Microsoft.Extensions.Options.IOptionsSnapshot<Pharos.Server.Application.Settings.RnD.RndSettings> rndSettings,
#endif
    IHttpContextAccessor httpContextAccessor,
    IInstrumentManager instrumentManager,
    ILogger<CurrentInstrumentService> logger) : ICurrentInstrumentService
{
    /// <summary>
    /// Request header used to override instrument detection.
    ///
    /// Value options:
    /// - Instrument serial number: resolves that instrument.
    /// - "desktop" (any casing): forces desktop mode (no instrument).
    ///
    /// Enabled in Debug builds, and in Release only when RndMode is enabled.
    /// </summary>
    internal const string _instrumentSerialHeader = "X-Instrument-Serial";

    public int? InstrumentId => CurrentInstrument?.Id;

    public string? InstrumentNumber => CurrentInstrument?.SerialNumber;

    private IInstrument? CurrentInstrument
    {
        get
        {
#if DEBUG
            var debugHeaderEnabled = true;
#else
            // Allows browser profiles or test clients to select the instrument explicitly.
            // This is useful when one server is connected to multiple instruments.
            // Example with ModHeader:
            // - Profile A sends "X-Instrument-Serial: SEED_001"
            // - Profile B sends "X-Instrument-Serial: SEED_002"
            // - Profile C sends "X-Instrument-Serial: desktop" to force desktop mode
            var debugHeaderEnabled = rndSettings.Value.RndMode;
#endif

            if (debugHeaderEnabled)
            {
                var debugSerial = httpContextAccessor.HttpContext?
                    .Request.Headers[_instrumentSerialHeader]
                    .FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(debugSerial))
                {
                    debugSerial = debugSerial.Trim();

                    if (string.Equals(debugSerial, "desktop", StringComparison.OrdinalIgnoreCase))
                    {
                        logger.LogDebug("Debug override: forcing desktop mode from header {Header}", _instrumentSerialHeader);
                        return null;
                    }

                    logger.LogDebug("Debug override: resolving instrument by serial '{Serial}' from header {Header}", debugSerial, _instrumentSerialHeader);
                    try { return instrumentManager.GetInstrument(debugSerial); }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Debug override: invalid serial '{Serial}' in header {Header}. Falling back to IP-based detection.", debugSerial, _instrumentSerialHeader);
                    }
                }
            }

            // Beware that, if there is a reverse proxy or load balancer before the server,
            // clientIpAddress below will be an IP address of the reverse proxy or load balancer.
            var instrumentIpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress;
            logger.LogDebug("HttpContext returned {Ip}", instrumentIpAddress);
            if (instrumentIpAddress == null)
            {
                logger.LogDebug("Instrument IP was null, returning null!");
                return null;
            }

            if (!string.IsNullOrEmpty(httpContextAccessor.HttpContext?.Request.Headers["X-Forwarded-For"]))
            {
                // This is a sanity check for the deployment configuration.
                // If the execution gets here you need to re-write this service in a more sophisticated way.
                // See https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-7.0
                throw new InvalidOperationException("Pharos is hosted behind a proxy or a load balancer. The automatic instrument detection will not work correctly.");
            }

            // search for a connected instrument
            logger.LogDebug("Getting the instrument for IP: {IP}", instrumentIpAddress);

            //var ipv4Address = instrumentIpAddress.MapToIPv4();
            return instrumentManager.FindInstrument(instrumentIpAddress);
        }
    }
}
