using System.ComponentModel.DataAnnotations;
using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Lis;

/// <summary>
/// Represents a collection of settings for a Laboratory Information System (LIS) connection.
/// The channel is not a LIS connection itself, but connections are created using channel settings.
/// </summary>
public class LisChannel
{
    /// <summary>
    /// Gets or sets the channel display name.
    /// </summary>
    [MaxLength(50)]
    [Required]
    [Unique]
    [DisplayNameLocal("LisSettings_LisChannel_LisNameColumnHeader")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the LIS channel is enabled.
    /// When enabled, the connection will be active. When disabled, the connection will be closed.
    /// </summary>
    [TableColumn]
    [ValueAfterCopy(false)]
    [DisplayNameLocal("LisSettings_LisChannel_IsEnabled")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets data import settings.
    /// </summary>
    [DisplayNameLocal("LisSettings_LisChannel_ImportSettings")]
    public LisImportSettings ImportSettings { get; set; } = new();

    /// <summary>
    /// Gets or sets data export settings.
    /// </summary>
    [DisplayNameLocal("LisSettings_LisChannel_ExportSettings")]
    public LisExportSettings ExportSettings { get; set; } = new();


    [DisplayNameLocal("LisSettings_LisChannel_Other")]
    public LisOtherSettings Other { get; set; } = new();

    /// <summary>
    /// Gets or sets the LIS protocol type for the connection.
    /// </summary>
    [TableColumn]
    [DisplayNameLocal("LisSettings_LisChannel_LisColumnHeaderProtocolType")]
    public LisProtocolType ProtocolType
    {
        get;
        set
        {
            field = value;
            AstmSettings = null;
            DataCapSettings = null;
            HL7Settings = null;

            switch (field)
            {
                case LisProtocolType.Astm:
                    AstmSettings = new AstmSettings();
                    break;

                case LisProtocolType.HL7:
                    HL7Settings = new HL7Settings();
                    break;

                case LisProtocolType.DataCap:
                    DataCapSettings = new DataCapSettings();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }
    } = LisProtocolType.Astm;

    [SettingsGroup]
    [DisplayNameLocal("LisSettings_LisChannel_LisAstmSettings")]
    [DependsFrom(nameof(ProtocolType), nameof(LisProtocolType.Astm), DependencyType.Hide)]
    public AstmSettings? AstmSettings { get; set; }

    [SettingsGroup]
    [DisplayNameLocal("LisSettings_LisChannel_LisHL7Settings")]
    [DependsFrom(nameof(ProtocolType), nameof(LisProtocolType.HL7), DependencyType.Hide)]
    public HL7Settings? HL7Settings { get; set; }

    [DisplayNameLocal("LisSettings_LisChannel_LisDataCapSettings")]
    [DependsFrom(nameof(ProtocolType), nameof(LisProtocolType.DataCap), DependencyType.Hide)]
    public DataCapSettings? DataCapSettings { get; set; }

    [DisplayNameLocal("LisSettings_LisChannel_DataCapVariant")]
    [DependsFrom(nameof(ProtocolType), nameof(LisProtocolType.DataCap), DependencyType.Hide)]
    public DataCapVariant DataCapVariant { get; set; } = DataCapVariant.OnlineAutomatic;
}

public enum DataCapVariant
{
    [DisplayNameLocal("DataCapVariant_OnlineAutomatic")]
    OnlineAutomatic = 0,
    [DisplayNameLocal("DataCapVariant_OnlineManual")]
    OnlineManual = 1,
    [DisplayNameLocal("DataCapVariant_TcpIpFtp")]
    TcpIpFtp = 2
}
