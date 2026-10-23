using Pharos.Server.Domain.Parameters;
using Riok.Mapperly.Abstractions;
using Tetris.Protocol.Instrument.Articles;

namespace Pharos.Server.Instrument.Application.Mapping;


[Mapper]
internal static partial class ArticleMapper
{
    [MapProperty(nameof(Article.MaxDaysInInstrument), nameof(Carrier.MaxDaysOfUse))]
    [MapProperty(nameof(Article.Quantity), nameof(Carrier.PackageSize))]
    [MapProperty(nameof(Article.Identity), nameof(Carrier.TestName))]
    [MapperIgnoreTarget(nameof(Carrier.UseExpiredReagents))]
    [MapperIgnoreTarget(nameof(Carrier.WasteTypeWhenEmpty))]
    [MapperIgnoreTarget(nameof(Carrier.WasteTypeWhenNotEmpty))]
    public static partial Carrier ToCarrier(Article source);

    [MapProperty(nameof(Article.Identity), nameof(Conjugate.ConjugateName))]
    [MapProperty(nameof(Article.MaxDaysInInstrument), nameof(Conjugate.MaxDaysOfUse))]
    [MapProperty(nameof(Article.MaxHoursInInstrument), nameof(Conjugate.MaxHoursOfUseAfterOpen))]
    [MapProperty(nameof(Article.Quantity), nameof(Conjugate.QuantityPerCell))]
    [MapperIgnoreTarget(nameof(Conjugate.UseExpiredReagents))]
    [MapperIgnoreTarget(nameof(Conjugate.WasteTypeWhenEmpty))]
    [MapperIgnoreTarget(nameof(Conjugate.WasteTypeWhenNotEmpty))]
    public static partial Conjugate ToConjugate(Article source);

    [MapProperty(nameof(Article.CartridgeCells), nameof(CalCcCartridge.Cells))]
    [MapProperty(nameof(Article.MaxDaysInInstrument), nameof(CalCcCartridge.MaxDaysOfUse))]
    [MapProperty(nameof(Article.Quantity), nameof(CalCcCartridge.TotalSets))]
    [MapperIgnoreTarget(nameof(CalCcCartridge.UseExpiredReagents))]
    [MapperIgnoreTarget(nameof(CalCcCartridge.WasteTypeWhenEmpty))]
    [MapperIgnoreTarget(nameof(CalCcCartridge.WasteTypeWhenNotEmpty))]
    public static partial CalCcCartridge ToCalCcCartridge(Article source);

    [MapProperty(nameof(CartridgeCellIdentity.CellIdentity), nameof(CalCcCartridgeCell.PointName))]
    private static partial CalCcCartridgeCell ToCalCcCartridgeCell(CartridgeCellIdentity source);

    [MapProperty(nameof(Article.MaxDaysInInstrument), nameof(QualityControl.MaxDaysOfUse))]
    [MapProperty(nameof(Article.MaxHoursInInstrument), nameof(QualityControl.MaxHoursOfUseAfterOpen))]
    [MapProperty(nameof(Article.Quantity), nameof(QualityControl.QuantityPerCell))]
    [MapProperty(nameof(Article.Identity), nameof(QualityControl.QualityControlName))]
    [MapperIgnoreTarget(nameof(QualityControl.UseExpiredReagents))]
    [MapperIgnoreTarget(nameof(QualityControl.WasteTypeWhenEmpty))]
    [MapperIgnoreTarget(nameof(QualityControl.WasteTypeWhenNotEmpty))]
    public static partial QualityControl ToQualityControl(Article source);

    [MapProperty(nameof(Article.MaxDaysInInstrument), nameof(Development.MaxDaysOfUse))]
    [MapProperty(nameof(Article.MaxHoursInInstrument), nameof(Development.MaxHoursOfUseAfterOpen))]
    [MapProperty(nameof(Article.Quantity), nameof(Development.VolumePerCell))]
    [MapProperty(nameof(Article.Identity), nameof(Development.DevelopmentName))]
    [MapperIgnoreTarget(nameof(Development.UseExpiredReagents))]
    [MapperIgnoreTarget(nameof(Development.WasteTypeWhenEmpty))]
    [MapperIgnoreTarget(nameof(Development.WasteTypeWhenNotEmpty))]
    public static partial Development ToDevelopment(Article source);

    [MapProperty(nameof(Article.MaxPiercedHoles), nameof(Diluent.MaxPiercedHoles))]
    [MapProperty(nameof(Article.MaxDaysInInstrument), nameof(Diluent.MaxDaysOfUse))]
    [MapProperty(nameof(Article.MaxHoursInInstrument), nameof(Diluent.MaxHoursOfUseAfterOpen))]
    [MapProperty(nameof(Article.Quantity), nameof(Diluent.VolumePerCell))]
    [MapProperty(nameof(Article.Identity), nameof(Diluent.DiluentName))]
    [MapperIgnoreTarget(nameof(Diluent.UseExpiredReagents))]
    [MapperIgnoreTarget(nameof(Diluent.WasteTypeWhenEmpty))]
    [MapperIgnoreTarget(nameof(Diluent.WasteTypeWhenNotEmpty))]
    public static partial Diluent ToDiluent(Article source);

    [MapProperty(nameof(Article.MaxDaysInInstrument), nameof(StopSolution.MaxDaysOfUse))]
    [MapProperty(nameof(Article.Quantity), nameof(StopSolution.TotalVolume))]
    [MapperIgnoreTarget(nameof(StopSolution.UseExpiredReagents))]
    public static partial StopSolution ToStopSolution(Article source);

    [MapProperty(nameof(Article.MaxDaysInInstrument), nameof(WashConcentrate.MaxDaysOfUse))]
    [MapProperty(nameof(Article.Quantity), nameof(WashConcentrate.TotalVolume))]
    [MapProperty(nameof(Article.Identity), nameof(WashConcentrate.WashConcentrateName))]
    [MapperIgnoreTarget(nameof(WashConcentrate.UseExpiredReagents))]
    public static partial WashConcentrate ToWashConcentrate(Article source);

    [MapProperty(nameof(Article.MaxDaysInInstrument), nameof(WashAdditive.MaxDaysOfUse))]
    [MapProperty(nameof(Article.Quantity), nameof(WashAdditive.TotalVolume))]
    [MapProperty(nameof(Article.Identity), nameof(WashAdditive.WashAdditiveName))]
    [MapperIgnoreTarget(nameof(WashAdditive.UseExpiredReagents))]
    public static partial WashAdditive ToWashAdditive(Article source);

    [MapProperty(nameof(Article.MaxDaysInInstrument), nameof(MaintenanceConcentrate.MaxDaysOfUse))]
    [MapProperty(nameof(Article.Quantity), nameof(MaintenanceConcentrate.TotalVolume))]
    [MapperIgnoreTarget(nameof(MaintenanceConcentrate.UseExpiredReagents))]
    public static partial MaintenanceConcentrate ToMaintenanceConcentrate(Article source);

    [MapProperty(nameof(Article.Quantity), nameof(FluoroC.TotalVolume))]
    [MapperIgnoreTarget(nameof(FluoroC.UseExpiredReagents))]
    public static partial FluoroC ToFluoroC(Article source);

    [MapProperty(nameof(Article.Quantity), nameof(QualityControlTube.TotalDoses))]
    [MapProperty(nameof(Article.Identity), nameof(QualityControlTube.QualityControlName))]
    [MapProperty(nameof(Article.MaxDaysInInstrument), nameof(QualityControlTube.MaxDaysOfUse))]
    [MapperIgnoreTarget(nameof(QualityControlTube.UseExpiredReagents))]
    public static partial QualityControlTube ToQualityControlTube(Article source);
}
