using MediatR;
using Microsoft.Extensions.Configuration;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Parameters.Domain;

namespace Pharos.Server.Parameters.Infrastructure.Database;

internal class ParametersDbContext(
    IConfiguration configuration,
    IAuditableEntitySaveChangesInterceptor auditableEntitySaveChangesInterceptor,
    IMediator? mediator) : BaseDbContext<ParametersDbContext>(configuration, auditableEntitySaveChangesInterceptor, mediator), IParametersDbContext
{
    public const string Schema = "Parameters";

    protected override string DefaultSchema => Schema;

    public DbSet<Article> Articles => Set<Article>();
    public DbSet<ArticlesVersion> ArticlesVersion => Set<ArticlesVersion>();
    public DbSet<IntendedMarket> IntendedMarkets => Set<IntendedMarket>();
    public DbSet<Package> Packages => Set<Package>();
    public DbSet<PackageItem> PackageItems => Set<PackageItem>();

    public DbSet<Method> Methods => Set<Method>();
    public DbSet<MethodTest> MethodTests => Set<MethodTest>();
    public DbSet<MethodTestLabName> MethodTestLabNames => Set<MethodTestLabName>();
    public DbSet<MethodReagent> MethodReagents => Set<MethodReagent>();
    public DbSet<MethodCalibrationCurve> MethodCalibrationCurves => Set<MethodCalibrationCurve>();
    public DbSet<MethodCalibrationPoint> MethodCalibrationPoints => Set<MethodCalibrationPoint>();
    public DbSet<MethodCurveControl> MethodCurveControls => Set<MethodCurveControl>();
    public DbSet<MethodConcentrationUnit> MethodConcentrationUnits => Set<MethodConcentrationUnit>();
    public DbSet<PhadiaQualityControl> PhadiaQualityControl => Set<PhadiaQualityControl>();
    public DbSet<PhadiaQualityControlTest> PhadiaQualityControlTests => Set<PhadiaQualityControlTest>();
    public DbSet<BaseQualityControlTest> QualityControlTests => Set<BaseQualityControlTest>();
    public DbSet<MethodQualityControlLot> MethodQualityControlLot => Set<MethodQualityControlLot>();
    public DbSet<MethodClassTable> MethodClassTables => Set<MethodClassTable>();
    public DbSet<MethodClassTableBelow> MethodClassTableBelows => Set<MethodClassTableBelow>();


    public DbSet<LabQualityControl> LabQualityControls => Set<LabQualityControl>();
    public DbSet<QualityControlBarcodeFormat> QualityControlBarcodeFormats => Set<QualityControlBarcodeFormat>();
    public DbSet<LabQualityControlTest> LabQualityControlTests => Set<LabQualityControlTest>();


    public DbSet<QualityControlSchedule> QualityControlSchedules => Set<QualityControlSchedule>();

    public DbSet<Panel> Panels => Set<Panel>();
    public DbSet<PanelTest> PanelTests => Set<PanelTest>();

    public DbSet<ParameterUpdatePackageVersion> ParameterUpdatePackageVersion => Set<ParameterUpdatePackageVersion>();
}