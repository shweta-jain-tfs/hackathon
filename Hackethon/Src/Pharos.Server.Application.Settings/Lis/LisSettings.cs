using System.Net;
using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Lis;

[RootConfigSection(sortOrder: 40)]
[DisplayNameLocal("LisSettings_LIS")]
public class LisSettings : IConfigSectionWithInitialValues
{
    [CopyFunction]
    [DisplayNameLocal("LisSettings_LisChannels")]

    [PageListMaxItems(10)]
    [PageListMinItems(1)]
    public List<LisChannel> LisChannels { get; set; } = [];

    public void InitializeConfigSection(bool isDevelopment)
    {
        // Use only in development
        if (isDevelopment)
        {
            LisChannels.Add(new LisChannel
            {
                Name = "Seeded ASTM 1001",
                Other = new LisOtherSettings
                {
                    ResetStatisticsAtMidnight = true
                },
                IsEnabled = true,
                ProtocolType = LisProtocolType.Astm,
                AstmSettings = new AstmSettings
                {
                    CommunicationSettings = new AstmCommunicationSettings
                    {
                        CommunicationMode = AstmCommunicationMode.Tcp,
                        EnqCompetitionTimeout = 1,
                        TcpSettings = new AstmCommunicationTcpSettings
                        {
                            SocketType = SocketType.Client,
                            IpAddress = IPAddress.Loopback.ToString(),
                            TcpPort = 1001,
                            TcpReconnectionInterval = 10000
                        },
                        Rs232Settings = new AstmCommunicationRs232Settings
                        {
                            Port = 1
                        }
                    }
                },
                ImportSettings = new LisImportSettings
                {
                    AskForMissingRequestsInstrumentScan = true
                },
            });
        }
    }
}