using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Sample.Domain;

internal class SampleTray : BaseEntityWithoutId
{
    [MaxLength(Constants.TrayIdLength)]
    public string Id { get; set; } = default!;

    [MaxLength(Constants.InstrumentSerialNumberLength)]
    public string? InstrumentNumber { get; set; }

    public DrawerType? Drawer { get; set; }

    public int? TrayPosition { get; set; }

    /// <summary>
    /// The entity's last modification timestamp.
    /// </summary>
    public DateTimeOffset? LastModified { get; set; }


    [NotMapped]
    public SampleTrayError Error { get; set; }

    [NotMapped]
    public int FreePositions { get; set; }

    [NotMapped]
    public int TubeErrorCount { get; set; }

    #region Navigation properties

    /// <summary>
    /// The navigation property for the sample tray positions
    /// </summary>
    public IList<SampleTube> SampleTubes { get; set; } = new List<SampleTube>();

    #endregion

    public static bool IsValidTrayId(string trayId)
    {
        if (string.IsNullOrEmpty(trayId) || trayId.Length != Constants.TrayIdLength || !int.TryParse(trayId, out _))
        {
            return false;
        }

        return true;
    }
}