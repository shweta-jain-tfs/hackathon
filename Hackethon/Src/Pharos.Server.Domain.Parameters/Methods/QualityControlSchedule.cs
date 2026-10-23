using Pharos.Server.Domain.Common;

namespace Pharos.Server.Domain.Parameters.Methods;

#pragma warning disable CS8618
public class QualityControlSchedule : BaseEntity
{
    public bool FirstInRun { get; set; }
    public bool LastInRun { get; set; }
    public bool AfterCalibration { get; set; }
    public Periodically Periodically { get; set; }
    public int StartAfter { get; set; } //number of tests or minutes
    public int RunEvery { get; set; } //number of tests or minutes

    #region Navigation properties

    public virtual BaseQualityControlTest QualityControlTest { get; set; }
    public int QualityControlTestId { get; set; }

    #endregion

    public static QualityControlSchedule Default()
    {
        return new QualityControlSchedule
        {
            FirstInRun = false,
            LastInRun = false,
            AfterCalibration = false,
            Periodically = Periodically.No,
            StartAfter = 0,
            RunEvery = 1
        };
    }
}

public enum Periodically
{
    No = 0,
    YesByNumberOfTests,
    YesByTime
}