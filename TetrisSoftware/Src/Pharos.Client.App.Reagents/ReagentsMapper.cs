using Pharos.Client.Common.Models;
using Pharos.Common.Web.Components.Reagent;
using Riok.Mapperly.Abstractions;

namespace Pharos.Client.App.Reagents;

[Mapper(RequiredEnumMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class ReagentsMapper
{
    public static BottleStatus? ToBottleStatus(this ReagentPositionStatus source)
    {
        return source is ReagentPositionStatus.InUse or ReagentPositionStatus.Empty
            ? source.ToLegendBottleStatus()
            : null;
    }

    public static LiquidBottleStatus? ToLiquidBottleStatus(this LiquidContainer source)
    {
        return source.Status == ReagentPositionStatus.Disabled
            ? source.Status.ToLegendLiquidBottleStatus()
            : source.InUse
                ? LiquidBottleStatus.InUse
                : null;
    }

    private static partial BottleStatus? ToLegendBottleStatus(this ReagentPositionStatus source);

    private static partial LiquidBottleStatus? ToLegendLiquidBottleStatus(this ReagentPositionStatus source);
}
