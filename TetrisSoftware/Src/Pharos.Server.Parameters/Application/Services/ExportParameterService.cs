using System.Drawing;
using System.IO.Compression;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Infrastructure;
using Pharos.Server.Parameters.Application.Commands.ImportArticles;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Parameters.Domain;
using Method = Pharos.Server.Domain.Parameters.Methods.Method;

namespace Pharos.Server.Parameters.Application.Services;

internal interface IExportParameterService
{
    Task<Method> GetMethodForExport(int methodId, CancellationToken cancellationToken);
    Task<ArticleParameters> GetArticlesForExport(CancellationToken cancellationToken);
    void ExportArticles(ArticleParameters articleParameters, StreamWriter writer);
    void ExportMethod(Method method, List<PhadiaQualityControlTest> qualityControls, StreamWriter writer);
    void ExportTests(Method method, StreamWriter writer);
    Task<MemoryStream> ExportParametersAsZip(int[] methodIds, bool exportPackage, CancellationToken cancellationToken);
}

internal class ExportParameterService(IParametersDbContext dbContext, IFileWriter fileWriter) : IExportParameterService
{
    public async Task<Method> GetMethodForExport(int methodId, CancellationToken cancellationToken)
    {
        var method = await dbContext.Methods
            .Where(m => m.Id == methodId)
            .Include(m => m.AnalyticalRun)
            .Include(m => m.Calculation)
            .Include(m => m.CalibrationCurve)
            .ThenInclude(c => c.CalibrationPoints)
            .Include(m => m.ClassTableBelows)
            .Include(m => m.ClassTables)
            .Include(m => m.ConcentrationUnits)
            .Include(m => m.CurveControls)
            .Include(m => m.Presentation)
            .Include(m => m.Reagents)
            .Include(m => m.Reporting)
            .Include(m => m.Tests)
            .AsSplitQuery()
            .FirstAsync(cancellationToken)
            .ConfigureAwait(false);

        return method;
    }

    public async Task<ArticleParameters> GetArticlesForExport(CancellationToken cancellationToken)
    {
        var articles = await dbContext.Articles
            .AsQueryable()
            .Include(x => x.CartridgeCells)
            .OrderBy(x => x.ArticleNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var markets = await dbContext.IntendedMarkets
            .Where(x => !IntendedMarket.PredefinedMarketsIds.Contains(x.Id))
            .AsQueryable()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var packages = await dbContext.Packages
            .AsQueryable()
            .Include(x => x.PackageItems)
            .OrderBy(x => x.PackageBarcode)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var result = new ArticleParameters
        {
            Version = await dbContext.ArticlesVersion.Select(x => x.VersionNumber).FirstAsync(cancellationToken)
        };

        result.Articles.AddRange(articles);
        result.Markets.AddRange(markets);
        result.Packages.AddRange(packages);
        return result;
    }

    private async Task<ParameterUpdatePackageVersion?> GetParameterUpdatePackageVersionForExport(CancellationToken cancellationToken)
    {
        return await dbContext.ParameterUpdatePackageVersion
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<List<Panel>> GetPanelsForExport(CancellationToken cancellationToken)
    {
        return await dbContext.Panels
            .AsNoTracking()
            .Include(x => x.Tests)
            .ThenInclude(x => x.MethodTest)
            .ThenInclude(x => x.Method)
            .OrderBy(x => x.PanelName)
            .AsSplitQuery()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public void ExportArticles(ArticleParameters articleParameters, StreamWriter writer)
    {
        writer.AddSection("FILE");
        writer.AddParameter("Type", MethodUpdateConstants.ArticlesFileType);
        writer.AddParameter("FormatVersion", MethodUpdateConstants.ArticlesFileFormatVersion);
        writer.AddParameter("Version", articleParameters.Version);

        if (articleParameters.Markets.Count > 0)
        {
            writer.AddSection("MARKET");
            foreach (var market in articleParameters.Markets)
            {
                writer.Write(market.Id);
                writer.AddParameterAtPos(market.Name, true);
            }
        }

        writer.AddSection("ARTICLE");
        foreach (var article in articleParameters.Articles)
        {
            writer.Write(article.ArticleNumber);
            writer.AddParameterAtPos((int)article.ArticleType);
            writer.AddParameterAtPos(article.FullName);
            writer.AddParameterAtPos(article.Quantity);
            writer.AddParameterAtPos(article.MethodName);
            writer.AddParameterAtPos(article.Identity);
            writer.AddParameterAtPos(article.MaxDaysInInstrument);
            writer.AddParameterAtPos(article.MaxHoursInInstrument);
            writer.AddParameterAtPos(article.NumberOfCells);
            writer.AddParameterAtPos(article.MaxPiercedHoles);
            writer.AddParameterAtPos(article.FriendlyName);
            writer.AddParameterAtPos(article.Text1);
            writer.AddParameterAtPos(article.Text2);
            writer.AddParameterAtPos(article.Text3);
            writer.AddParameterAtPos(article.Text4);
            writer.AddParameterAtPos(article.Shape == ArticleIndicatorShape.None ? "" : ((int)article.Shape).ToString());
            writer.AddParameterAtPos(article.Color1 == null ? "" : ColorTranslator.ToHtml(article.Color1.Value));
            writer.AddParameterAtPos(article.Color2 == null ? "" : ColorTranslator.ToHtml(article.Color2.Value));
            writer.AddParameterAtPos(article.Color3 == null ? "" : ColorTranslator.ToHtml(article.Color3.Value));
            writer.AddParameterAtPos(article.Color4 == null ? "" : ColorTranslator.ToHtml(article.Color4.Value));
            writer.WriteLine(string.Empty);
        }

        writer.AddSection("CARTRIDGE");
        var cartridges = articleParameters.Articles.Where(x => x.CartridgeCells is { Count: > 0 }).ToList();
        foreach (var cartridge in cartridges)
        {
            if (cartridge.CartridgeCells == null)
            {
                continue;
            }

            foreach (var cell in cartridge.CartridgeCells)
            {
                writer.Write(cartridge.ArticleNumber);
                writer.AddParameterAtPos(cell.CellPosition);
                writer.AddParameterAtPos(cell.CellIdentity, true);
            }
        }

        writer.AddSection("PACKAGE");
        foreach (var package in articleParameters.Packages)
        {
            foreach (var item in package.PackageItems)
            {
                writer.Write(package.PackageBarcode);
                writer.AddParameterAtPos(package.OrderingNumber);
                writer.AddParameterAtPos(package.PackageFullName);
                writer.AddParameterAtPos(item.Article.ArticleNumber);
                writer.AddParameterAtPos(item.ArticleAmount);
                writer.AddParameterAtPos(package.IntendedMarketId, true);
            }
        }

        writer.AddSection("END");
    }

    private static void ExportVersion(ParameterUpdatePackageVersion parameterUpdatePackageVersion, StreamWriter writer)
    {
        writer.AddSection("FILE");
        writer.AddParameter("Type", MethodUpdateConstants.VersionFileType);
        writer.AddParameter("FormatVersion", MethodUpdateConstants.VersionFileFormatVersion);
        writer.AddParameter("Version", parameterUpdatePackageVersion.VersionNumber);
    }

    private static void ExportPanels(IList<Panel> panels, StreamWriter writer)
    {
        writer.AddSection("FILE");
        writer.AddParameter("Type", MethodUpdateConstants.PanelsFileType);
        writer.AddParameter("FormatVersion", MethodUpdateConstants.PanelsFileFormatVersion);
        writer.AddParameter("Comment", "Test panels");
        writer.AddParameter("Version", 1);

        writer.AddSection("TESTPANELS");
        foreach (var panel in panels)
        {
            foreach (var test in panel.Tests.OrderBy(x => x.SortOrder))
            {
                writer.Write(panel.PanelName);
                writer.AddParameterAtPos(panel.PanelLongName);
                writer.AddParameterAtPos(test.MethodTest.TestName);
                writer.AddParameterAtPos(test.MethodTest.Method.MethodName);
                writer.AddParameterAtPos(test.Replicates);
                writer.AddParameterAtPos(panel.GroupedPanel, true);
            }
        }

        writer.AddSection("END");
    }

    public void ExportMethod(Method method, List<PhadiaQualityControlTest> qualityControls, StreamWriter writer)
    {
        writer.AddSection("FILE");
        writer.AddParameter("Type", MethodUpdateConstants.MethodFileType);
        writer.AddParameter("FormatVersion", MethodUpdateConstants.MethodFileFormatVersion);
        writer.AddParameter("Version", method.MethodVersion);

        writer.AddSection("METHOD");
        writer.AddParameter("Name", method.FullMethodName);
        writer.AddParameter("AbbreviatedName", method.MethodName);
        writer.AddParameter("MethodVersion", method.MethodVersion);
        writer.AddParameter("TemplateFlag", method.IsTemplate);
        writer.AddParameter("Default", !method.IsUserDefined);
        writer.AddParameter("SequenceNumber", (int)method.Technology);
        writer.AddParameter("InstrumentDilutionLocked", method.PreventDilutionFactorsBelowDefault);
        writer.AddParameter("AllocationOrder", (int)method.CalibratorPipettingOrder);
        writer.AddParameter("CalibratorPiercingOrder", (int)method.CalibratorPipettingOrder);
        writer.AddParameter("SortOrder", method.SortOrder);
        writer.AddParameter("DefaultConcentrationFactor", method.DefaultConcentrationFactor, 5);
        writer.AddParameter("SpecialMethod", method.IsSpecialMethod);
        writer.AddParameter("RUO", method.IsRuo);
        writer.AddParameter("Allow50percentWash", method.Allow50PercentWash);
        writer.AddParameter("AllowDilutionInPipette", method.AllowDilutionInPipette);

        writer.AddSection("REAGENTS");
        writer.AddParameter("Sample", method.Reagents.SingleOrDefault(r => r.ReagentType == ReagentType.Sample)!.Volume);
        writer.AddParameter("Conjugate", method.Reagents.SingleOrDefault(r => r.ReagentType == ReagentType.Conjugate)!.Volume, false);
        writer.AddParameterAtPos(method.Reagents.SingleOrDefault(r => r.ReagentType == ReagentType.Conjugate)!.DefaultReagentIdentity, true);
        writer.AddParameter("DevSol", method.Reagents.SingleOrDefault(r => r.ReagentType == ReagentType.DevelopmentSolution)!.Volume);
        writer.AddParameter("StopSol", method.Reagents.SingleOrDefault(r => r.ReagentType == ReagentType.StopSolution)!.Volume);
        writer.AddParameter("WashSolConc", method.Reagents.SingleOrDefault(r => r.ReagentType == ReagentType.WashSolutionConcentrate)!.DefaultReagentIdentity);
        writer.AddParameter("WashSolAdd", method.Reagents.SingleOrDefault(r => r.ReagentType == ReagentType.WashAdditive)!.DefaultReagentIdentity);

        writer.AddSection("CALCONTROL");
        foreach (var point in method.CurveControls.OrderBy(x => x.ExpectedConcentration))
        {
            writer.AddParameter("Point", point.Active, false);
            writer.AddParameterAtPos(point.CurveControlName);
            writer.AddParameterAtPos(point.MethodTest.TestName);
            writer.AddParameterAtPos(point.Replicates);
            writer.AddParameterAtPos(point.ExpectedConcentration, 3);
            writer.AddParameterAtPos(point.ExpectedStdDev, 3);
            writer.AddParameterAtPos(point.InnerStdDevLimit, 2);
            writer.AddParameterAtPos(point.OuterStdDevLimit, 2);
            writer.AddParameterAtPos(point.AdjustmentForAlfaBeta1Beta2, 5, true);
        }

        writer.AddSection("CALCURVE");

        writer.AddParameter("TestName", method.CalibrationCurve.MethodTest.TestName);
        writer.AddParameter("AgeUntilWarning", method.CalibrationCurve.DefaultMaxNumberOfDays);
        writer.AddParameter("MaxBadReplicates", method.CalibrationCurve.MaxBadReplicates);
        writer.AddParameter("ReportLowRUZeroPoint", method.CalibrationCurve.ReportLowRuForZeroCalibrators);
        writer.AddParameter("QuotientIndex", method.CalibrationCurve.QuotientIndex);
        writer.AddParameter("ExcludeLowestInMeanDiff", method.CalibrationCurve.ExcludeLowestCalibratorFromMeanDiff);

        foreach (var point in method.CalibrationCurve.CalibrationPoints.OrderBy(x => x.ExpectedConcentration))
        {
            writer.AddParameter("Point", point.CalibratorName, false);
            writer.AddParameterAtPos(point.ExpectedConcentration, 3);
            writer.AddParameterAtPos(point.Replicates);
            writer.AddParameterAtPos(point.AdjustmentForAlfaBeta1Beta2, 5);
            writer.AddParameterAtPos(point.IdealResponse, 1);
            writer.AddParameterAtPos(point.ExpectedStdDev, 1);
            writer.AddParameterAtPos(point.UpperResponseLimit, 1);
            writer.AddParameterAtPos(point.LowerResponseLimit, 1);
            writer.AddParameterAtPos(point.StdDevPrecisionLimit, 2);
            writer.AddParameterAtPos(point.StdDevLimit, 2);
            writer.AddParameterAtPos(point.UpperLogarithmicLimit, 3);
            writer.AddParameterAtPos(point.LowerLogarithmicLimit, 3, true);
        }

        writer.AddSection("BLANK");
        writer.AddParameter("Subtract", method.SubtractBlank);

        writer.AddSection("PRESENTATION");
        writer.AddParameter("XaxisText", method.Presentation.XAxisText);
        writer.AddParameter("XaxisUnit", method.Presentation.XAxisUnit);
        writer.AddParameter("XaxisScale", (int)method.Presentation.XAxisScale);
        writer.AddParameter("YaxisText", method.Presentation.YAxisText);
        writer.AddParameter("YaxisUnit", method.Presentation.YAxisUnit);
        writer.AddParameter("YaxisScale", (int)method.Presentation.YAxisScale);
        writer.AddParameter("FixedYScale", method.Presentation.FixedYAxis, false);
        writer.AddParameterAtPos(method.Presentation.FixedYAxisLowerLimit, 1);
        writer.AddParameterAtPos(method.Presentation.FixedYAxisUpperLimit, 1, true);

        writer.AddSection("CALCULATION");
        writer.AddParameter("Type", (int)method.Calculation.CalculationType);
        writer.AddParameter("NormalWeight", (int)method.Calculation.NormalWeight);
        writer.AddParameter("PredictedVariance", method.Calculation.PredictedVarianceA, 6, false);
        writer.AddParameterAtPos(method.Calculation.PredictedVarianceB, 6, true);
        writer.AddParameter("RobustWeight", (int)method.Calculation.RobustWeight);
        writer.AddParameter("MinCoeffCorr", method.Calculation.MinCoefficientOfCorrelation, 5);
        writer.AddParameter("ZeroPoint", method.Calculation.CalibratorStartingPoint, false);
        writer.AddParameterAtPos(method.Calculation.CalibratorsForLeastSquareCalc, true);
        writer.AddParameter("LowestConc", method.Reporting.DefaultMinConcentrationToReport, 3);

        writer.AddSection("CUTOFF");
        writer.AddParameter("TextBelow", method.Reporting.DefaultTextBelowCutOff);
        writer.AddParameter("TextAbove", method.Reporting.DefaultTextAboveCutOff);

        writer.AddSection("REFERENCECURVE");
        writer.AddParameter("ConcUnit", GetConcentrationUnits(method.ConcentrationUnits));
        writer.AddParameter("QuantBelow", method.Reporting.DefaultTextQuantitativeBelow);
        writer.AddParameter("UseCalculatedTextQuantitiveAbove", method.Reporting.DefaultUseCalculatedTextQuantitativeAbove);
        writer.AddParameter("QuantAbove", method.Reporting.DefaultTextQuantitativeAbove);
        writer.AddParameter("QuantDoubtful", method.Reporting.DefaultTextQuantitativeDoubtful);

        writer.AddParameter("TextBelow", method.ClassTableBelows.SingleOrDefault(x => x.ClassTableId == 1)?.DefaultTextBelowConcentration ?? "");
        if (method.ClassTables.Where(x => x.ClassTableId == 1).IsNullOrEmpty())
        {
            writer.AddParameter("Class", "");
        }

        //CLASS SECTIONS
        GetClassSections(writer, method.ClassTables, method.ClassTableBelows);

        // TODO: check the method parameters spec
        //writer.AddSection("UNKNOWNS");
        //writer.AddParameter("CVlimit", cvLimit, 1);

        writer.AddSection("INTERNALQC");
        foreach (var point in qualityControls.OrderBy(x => x.SortOrder))
        {
            writer.AddParameter("Point", point.QualityControl.Name, false);
            writer.AddParameterAtPos(point.Test.TestName);
            writer.AddParameterAtPos(point.PreDilutionFactor);
            writer.AddParameterAtPos((int)point.QualityControlType);
            writer.AddParameterAtPos(point.InstrumentDilutionFactor, true);
        }

        writer.AddSection("ANALYTICALRUN");
        writer.AddParameter("MaxAnalyticalRunSize", method.AnalyticalRun.DefaultMaxAnalyticalRunSize);
        writer.AddParameter("AllowMonthlyCalibration", method.AnalyticalRun.DefaultAllowMonthlyCalibration);
        writer.AddParameter("ConnectCarrierAndConjugateLot", method.AnalyticalRun.RunCalibrationOnCarrierLotChange);
    }

    public void ExportTests(Method method, StreamWriter writer)
    {
        writer.AddSection("FILE");
        writer.AddParameter("Type", MethodUpdateConstants.TestFileType);
        writer.AddParameter("FormatVersion", MethodUpdateConstants.TestFileFormatVersion);
        writer.AddParameter("Version", method.MethodVersion);

        writer.AddSection("INFORMATION");
        writer.AddParameter("Method", method.MethodName);

        writer.AddSection("TESTS");
        foreach (var test in method.Tests)
        {
            writer.Write(test.TestName + "=");
            writer.Write(test.FullTestName);
            writer.AddParameterAtPos(test.DefaultReplicates);
            writer.AddParameterAtPos(test.DefaultReportConcentration);
            writer.AddParameterAtPos(test.DefaultReportClassId);
            writer.AddParameterAtPos(test.DefaultReportCutOff);
            writer.AddParameterAtPos((int)test.DefaultReportQuotient);
            writer.AddParameterAtPos(test.DefaultMethodConcentrationUnitId - 1); //The unit ids are saved 1-10 i db, but listed as 0-9 in the import files. 
            writer.AddParameterAtPos(test.HighestConcentrationToReport, 3);
            writer.AddParameterAtPos(test.NumberOfDecimalsForConcentrationBelow10);
            writer.AddParameterAtPos(test.UseLotSpecificCode);
            writer.AddParameterAtPos(test.DefaultDilutionFactor);
            writer.AddParameterAtPos(test.DefaultCutOffConcentration, 3);
            writer.AddParameterAtPos(test.DefaultLabTestName);
            writer.AddParameterAtPos((int)test.DefaultDilutionMethod);
            writer.AddParameterAtPos(test.DiluentIdentity);
            writer.AddParameterAtPos(test.DefaultEmphasizeOnReports);
            writer.AddParameterAtPos(test.DefaultTextQuantitativeBelowForDilutedTest); // avoid "---" output
            writer.AddParameterAtPos(test.DefaultTextQuantitativeAboveForDilutedTest); // avoid "---" output
            writer.AddParameterAtPos(test.ReportConcentrationAboveCurveFlag);
            writer.AddParameterAtPos(test.ReportConcentrationBelowCurveFlag);
            writer.AddParameterAtPos(test.NumberOfDecimalsForConcentrationAbove100);
            writer.AddParameterAtPos(test.NumberOfDecimalsForConcentrationBetween10And100);
            writer.AddParameterAtPos(test.MixOfAllergens);
            writer.AddParameterAtPos(test.Priority?.OrderInPriorityGroupA);
            writer.AddParameterAtPos(test.Priority?.OrderInPriorityGroupB);
            writer.AddParameterAtPos(test.IterativeAlfaBeta1Beta2Flag);
            writer.AddParameterAtPos(test.AdjustmentForAlfaBeta1Beta2, 5);
            writer.AddParameterAtPos(test.Priority?.IncludeInPriorityGroupA);
            writer.AddParameterAtPos(test.Priority?.IncludeInPriorityGroupB);
            writer.AddParameterAtPos(test.SpecialMethodName);
            writer.AddParameterAtPos(test.Disabled);
            writer.AddParameterAtPos(test.ConcentrationFactor, 5);
            writer.AddParameterAtPos(test.LowerDetectionLimit, 3);
            writer.AddParameterAtPos(test.DevelopmentSolutionIdentity);
            writer.AddParameterAtPos(test.IsRuo, true);
        }
    }

    public async Task<MemoryStream> ExportParametersAsZip(int[] methodIds, bool exportPackage, CancellationToken cancellationToken)
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDirectory);

        if (exportPackage)
        {
            methodIds = await dbContext.Methods.Select(x => x.Id).ToArrayAsync(cancellationToken);
        }

        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        foreach (var methodId in methodIds)
        {
            var method = await GetMethodForExport(methodId, cancellationToken);
            var methodName = method.MethodName;

            // Create and save temporary files
            await using (var methodFileWriter = new StreamWriter(Path.Combine(tempDirectory, methodName + ".mth"), false, encoding))
            {
                var qualityControls = await dbContext.PhadiaQualityControlTests
                    .Include(x => x.QualityControl)
                    .Include(x => x.Test)
                    .Where(qc => qc.Test.MethodId == methodId)
                    .AsNoTracking().ToListAsync(cancellationToken);

                ExportMethod(method, qualityControls, methodFileWriter);
            }

            await using (var testsFileWriter = new StreamWriter(Path.Combine(tempDirectory, methodName + ".tst"), false, encoding))
            {
                ExportTests(method, testsFileWriter);
            }
        }

        var articleParameters = await GetArticlesForExport(cancellationToken);

        await using (var articlesFileWriter = new StreamWriter(Path.Combine(tempDirectory, "articles.arf"), false, encoding))
        {
            ExportArticles(articleParameters, articlesFileWriter);
        }

        if (exportPackage)
        {
            var packageVersion = await GetParameterUpdatePackageVersionForExport(cancellationToken);
            if (packageVersion is not null)
            {
                await using var versionFileWriter = new StreamWriter(Path.Combine(tempDirectory, "version.txt"), false, encoding);
                ExportVersion(packageVersion, versionFileWriter);
            }

            var panels = await GetPanelsForExport(cancellationToken);
            await using var panelsFileWriter = new StreamWriter(Path.Combine(tempDirectory, "TestPanels.tpf"), false, encoding);
            ExportPanels(panels, panelsFileWriter);
        }

        //Add description file
        var description = "This is an exported package.";
        var descriptionFileStream = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(description));
        await fileWriter.WriteAsync("description.txt", tempDirectory, descriptionFileStream, cancellationToken);

        //Compress to zip in temp directory
        var tempZipDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempZipDirectory);

        var parameterUpdateZip = Path.Combine(tempZipDirectory, "ParameterUpdatePackageTemp.zip");
        ZipFile.CreateFromDirectory(tempDirectory, parameterUpdateZip);

        var stream = await File.ReadAllBytesAsync(parameterUpdateZip, cancellationToken);

        //Delete temporary directories
        Directory.Delete(tempDirectory, true);
        Directory.Delete(tempZipDirectory, true);

        return new MemoryStream(stream);
    }

    private static string GetConcentrationUnits(IList<MethodConcentrationUnit> methodMethodConcentrationUnits)
    {
        var units = methodMethodConcentrationUnits
            .Where(x => !x.ConcentrationUnit.IsNullOrEmpty())
            .OrderBy(x => x.UnitId)
            .Select(x => x.ConcentrationUnit);
        return string.Join(";", units);
    }

    private static void GetClassSections(StreamWriter writer, IList<MethodClassTable> methodMethodClassTables, IList<MethodClassTableBelow> methodMethodClassTableBelows)
    {
        var uniqueIds = methodMethodClassTables.Select(x => x.ClassTableId).Distinct();

        foreach (var id in uniqueIds)
        {
            var classRows = methodMethodClassTables
                .Where(x => x.ClassTableId == id)
                .OrderBy(x => x.ClassTableRowId)
                .ToList();

            if (id != 1)
            {
                writer.AddSection("CLASS" + id);
                writer.AddParameter("TextBelow", methodMethodClassTableBelows.SingleOrDefault(x => x.ClassTableId == id)?.DefaultTextBelowConcentration ?? "");
            }

            foreach (var row in classRows)
            {
                writer.AddParameter("Class", row.Concentration, 3, false);
                writer.AddParameterAtPos(row.DefaultTextAboveConcentration, true);
            }
        }

    }
}
