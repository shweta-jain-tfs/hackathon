namespace Pharos.Server.Instrument.Infrastructure.Grpc;

internal class GrpcSettings
{
    public const string GrpcSettingsSectionName = "Grpc";

    public class MainServiceSettings
    {
        public int Port { get; set; } = 9876;
        public bool UseHttps { get; set; }
        public bool RequireClientCertificate { get; set; }
    }


    public class PairingServiceSettings
    {
        public int Port { get; set; } = 9875;
        public int PairingModeTimeoutSeconds { get; set; } = 120;

        /// <summary>
        /// Set this setting to true to allow repeated full pairing workflow even
        /// when the instrument certificate is already trusted. For testing purposes.
        /// </summary>
        public bool AllowRepeatedPairing { get; set; }
    }

    public MainServiceSettings MainService { get; set; } = new MainServiceSettings();
    public PairingServiceSettings PairingService { get; set; } = new PairingServiceSettings();
}
