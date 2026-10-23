using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Lis;

public class AstmCommunicationSettings
{
    [DisplayNameLocal("AstmCommunicationSettings_CommunicationMode")]
    public AstmCommunicationMode CommunicationMode { get; set; } = AstmCommunicationMode.Tcp;

    [DisplayNameLocal("AstmCommunicationSettings_TcpSettings")]
    [DependsFrom(nameof(CommunicationMode), nameof(AstmCommunicationMode.Tcp), DependencyType.Hide)]
    public AstmCommunicationTcpSettings TcpSettings { get; set; } = new();

    [DisplayNameLocal("AstmCommunicationSettings_Rs232Settings")]
    [DependsFrom(nameof(CommunicationMode), nameof(AstmCommunicationMode.Rs232), DependencyType.Hide)]
    public AstmCommunicationRs232Settings Rs232Settings { get; set; } = new();

    [DisplayNameLocal("AstmCommunicationSettings_UseOnlyEtx")]
    public bool UseOnlyEtx { get; set; }

    [DisplayNameLocal("AstmCommunicationSettings_EtxOnlyNormalFrameBlockSize")]
    [DependsFrom(nameof(UseOnlyEtx), "True", DependencyType.Disable)]
    public bool EtxOnlyNormalFrameBlockSize { get; set; } = true;

    [DisplayNameLocal("AstmCommunicationSettings_EtxOnlySplitLargeRecords")]
    [DependsFrom([nameof(UseOnlyEtx), nameof(EtxOnlyNormalFrameBlockSize)], ["True", "False"], DependencyType.Disable)]
    public bool EtxOnlySplitLargeRecords { get; set; }

    /// <summary>
    /// Gets or sets the delay between request information records in seconds.
    /// </summary>
    [DisplayNameLocal("AstmCommunicationSettings_DelayBetweenRequestInformationRecords")]
    [SettingsRange(0, 1200)]
    public int DelayBetweenRequestInformationRecords { get; set; } = 0;

    /// <summary>
    /// Gets or sets the timeout for ENQ competition resolution in seconds.
    /// </summary>
    [DisplayNameLocal("AstmCommunicationSettings_EnqCompetitionTimeout")]
    [SettingsRange(1, 60)]
    public int EnqCompetitionTimeout { get; set; } = 1;

    [DisplayNameLocal("AstmCommunicationSettings_UseLegacyChecksumCalculation")]
    public bool UseLegacyChecksumCalculation { get; set; }
}

public enum AstmCommunicationMode
{
    [DisplayNameLocal("AstmCommunicationSettings_AstmCommunicationMode_Tcp")]
    Tcp = 0,

    [DisplayNameLocal("AstmCommunicationSettings_AstmCommunicationMode_Rs232")]
    Rs232 = 1
}