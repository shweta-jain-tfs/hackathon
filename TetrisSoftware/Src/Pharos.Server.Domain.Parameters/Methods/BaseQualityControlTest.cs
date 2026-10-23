using Pharos.Server.Domain.Common;
using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Domain.Parameters.Methods;

#pragma warning disable CS8618
public abstract class BaseQualityControlTest : BaseEntity
{
    public bool Enabled { get; set; }
    public QualityControlType QualityControlType { get; set; }
    public int PreDilutionFactor { get; set; }
    public int InstrumentDilutionFactor { get; set; }
    public int Replicates { get; set; }
    public double ReplicateCvLimit { get; set; }
    public double MaxCv { get; set; }
    public int NumberOfSd { get; set; }
    public int NumberOfTests { get; set; }
    public bool ApplyAcceptanceRange { get; set; }
    public bool UseAcceptanceRange { get; set; }
    public bool UseLotQualification { get; set; }

    public QualityControlSystemType SystemType { get; set; }

    public int TestId { get; set; }
    public virtual MethodTest Test { get; set; }
    public virtual IList<MethodQualityControlLot> Lots { get; set; }
    public virtual QualityControlSchedule Schedule { get; set; }

    public int QualityControlId { get; set; }
    public virtual BaseQualityControl QualityControl { get; set; }
}
