using Microsoft.EntityFrameworkCore.ChangeTracking;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Parameters.Domain;

namespace Pharos.Server.Parameters.Application.Interfaces;

internal interface IParametersDbContext : IDbContext
{
    public DbSet<Article> Articles { get; }
    public DbSet<ArticlesVersion> ArticlesVersion { get; }

    public DbSet<IntendedMarket> IntendedMarkets { get; }
    public DbSet<Package> Packages { get; }
    public DbSet<PackageItem> PackageItems { get; }

    public DbSet<Method> Methods { get; }
    public DbSet<MethodTest> MethodTests { get; }
    public DbSet<MethodTestLabName> MethodTestLabNames { get; }
    public DbSet<MethodReagent> MethodReagents { get; }
    public DbSet<MethodCalibrationCurve> MethodCalibrationCurves { get; }
    public DbSet<MethodCalibrationPoint> MethodCalibrationPoints { get; }
    public DbSet<MethodCurveControl> MethodCurveControls { get; }
    public DbSet<MethodConcentrationUnit> MethodConcentrationUnits { get; }
    public DbSet<PhadiaQualityControl> PhadiaQualityControl { get; }
    public DbSet<PhadiaQualityControlTest> PhadiaQualityControlTests { get; }
    public DbSet<BaseQualityControlTest> QualityControlTests { get; }
    public DbSet<MethodQualityControlLot> MethodQualityControlLot { get; }
    public DbSet<MethodClassTable> MethodClassTables { get; }
    public DbSet<MethodClassTableBelow> MethodClassTableBelows { get; }


    public DbSet<LabQualityControl> LabQualityControls { get; }
    public DbSet<QualityControlBarcodeFormat> QualityControlBarcodeFormats { get; }
    public DbSet<LabQualityControlTest> LabQualityControlTests { get; }


    public DbSet<QualityControlSchedule> QualityControlSchedules { get; }


    public DbSet<Panel> Panels { get; }
    public DbSet<PanelTest> PanelTests { get; }

    public DbSet<ParameterUpdatePackageVersion> ParameterUpdatePackageVersion { get; }

    EntityEntry Entry(object entity);
}