using System.Globalization;
using System.IO.Compression;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Parameters.Application.Commands.ImportArticles;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Parameters.Application.Services;
using Pharos.Server.Parameters.FileComparer;

namespace Pharos.Server.Parameters.Application.Commands.ImportMethodsAndTests;

[Collection(nameof(ParametersTestsCollection))]
public class ImportMethodsAndTestsCommandTests(ParametersTestsWaf factory) : DatabaseTestBase<ParametersTestsWaf>(factory)
{
    [Fact]
    public Task ImportMethodWithTestsToEmptyDatabase()
    {
        return ImportAndExportFilesWithChecks(["sIgE.mth", "sIgE.tst"]);
    }

    [Fact]
    public Task RepeatedImportOfMethod()
    {
        return ImportAndExportFilesWithChecks(["sIgE.mth", "sIgE.tst"], repeatedImport: true);
    }

    [Fact]
    public async Task UpdateExistingMethodWithoutTests()
    {
        await ImportAndExportFilesWithChecks(["sIgE.mth", "sIgE.tst"]);
        await ImportAndExportFilesWithChecks(["sIgE_modified.mth"]);
    }

    [Fact]
    public async Task UpdateTestsForExistingMethod()
    {
        await ImportAndExportFilesWithChecks(["sIgE.mth", "sIgE.tst"]);
        await ImportAndExportFilesWithChecks(["sIgE_modified.tst"]);
    }

    [Trait("ProductRequirement", "PR_2587")]
    [Trait("ModuleRequirement", "MR_5950")]
    [Fact]
    public async Task Reimport_ShouldNotOverrideMethodNameLis_WhenMethodAlreadyHasValue()
    {
        await ImportAndExportFilesWithChecks(["sIgE.mth", "sIgE.tst"]);

        // set a custom MethodNameLis value in the database
        const string customMethodNameLis = "Custom Lis Id";
        var dbContext = ServiceProvider.GetRequiredService<IParametersDbContext>();
        var importedMethod = await dbContext.Methods.FirstAsync(TestContext.Current.CancellationToken);
        importedMethod.MethodNameLis = customMethodNameLis;
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await ImportAndExportFilesWithChecks(["sIgE_modified.mth"]);

        var reloadedMethod = await dbContext.Methods.AsNoTracking().FirstAsync(x => x.Id == importedMethod.Id, TestContext.Current.CancellationToken);
        reloadedMethod.MethodNameLis.Should().Be(customMethodNameLis);
    }

    [Fact]
    public async Task FailedMethodFileRollsBackImport()
    {
        await ImportAndExportFilesWithChecks(["sIgE.mth"], ["sIgA_parse_error.mth"]);
    }

    [Fact]
    public async Task FailedArticlesFileRollsBackImport()
    {
        await ImportAndExportFilesWithChecks(["sIgE.mth"], ["article_error.arf"]);
    }

    [Trait("ProductRequirement", "PR_2588")]
    [Trait("ModuleRequirement", "MR_5959")]
    [Fact]
    public async Task Reimport_WithMultipleLaboratoryNamesForATest_ShouldSucceed()
    {
        await ImportAndExportFilesWithChecks(["sIgE.mth", "sIgE.tst"]);

        var dbContext = ServiceProvider.GetRequiredService<IParametersDbContext>();
        var methodTest = await dbContext.MethodTests
            .Include(x => x.LabTestNames)
            .FirstAsync(TestContext.Current.CancellationToken);

        dbContext.MethodTestLabNames.Add(new MethodTestLabName
        {
            MethodTestId = methodTest.Id,
            TestName = "LAB2",
            SortOrder = 2
        });
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var dir = DataDrivenTestHelper.GetTestDataFilesPath(GetType());
        var packageMock = MockMethodUpdatePackage(dir, "sIgE.mth", "sIgE.tst");

        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            await FluentActions.Invoking(() => sender.Send(new ImportMethodsAndTestsCommand(packageMock.Object)))
                .Should()
                .NotThrowAsync();
        }

        var reloadedLabNames = await dbContext.MethodTestLabNames
            .Where(x => x.MethodTestId == methodTest.Id)
            .OrderBy(x => x.SortOrder)
            .Select(x => x.TestName)
            .ToListAsync(TestContext.Current.CancellationToken);

        reloadedLabNames.Should().Equal(methodTest.DefaultLabTestName, "LAB2");
    }

    [Trait("ProductRequirement", "PR_2588")]
    [Trait("ModuleRequirement", "MR_5954")]
    [Fact]
    public async Task ExportTests_ShouldUseDefaultValues_WhenUserOverridesExist()
    {
        await ImportAndExportFilesWithChecks(["sIgE.mth", "sIgE.tst"]);

        var dbContext = ServiceProvider.GetRequiredService<IParametersDbContext>();
        var methodTest = await dbContext.MethodTests.FirstAsync(TestContext.Current.CancellationToken);
        methodTest.UserReplicates = methodTest.DefaultReplicates + 1;
        methodTest.UserDilutionFactor = methodTest.DefaultDilutionFactor + 1;
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var dir = DataDrivenTestHelper.GetTestDataFilesPath(GetType());
        var expectedFile = Path.Combine(dir, "sIgE.tst");
        var actualFile = Path.ChangeExtension(Path.GetTempFileName(), ".tst");

        try
        {
            await using var exportScope = ServiceProvider.CreateAsyncScope();
            var exportService = exportScope.ServiceProvider.GetRequiredService<IExportParameterService>();
            var importedMethod = await exportService.GetMethodForExport(methodTest.MethodId, TestContext.Current.CancellationToken);
            var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

            await using (var writer = new StreamWriter(actualFile, false, encoding))
            {
                exportService.ExportTests(importedMethod, writer);
            }

            ParameterFileComparer.CheckMethodFilesEqual(expectedFile, actualFile, true, true);
        }
        finally
        {
            if (File.Exists(actualFile))
            {
                File.Delete(actualFile);
            }
        }
    }

    [Fact]
    [Trait("ProductRequirement", "PR_342")]
    [Trait("ProductRequirement", "PR_2765")]
    public async Task Import_ShouldKeepNewNormalMethodAndTestsAvailable()
    {
        // Arrange
        var packageMock = MockMethodUpdatePackageWithSpecializedFlags(
            isRuo: false,
            isSpecialMethod: false,
            testIsRuo: false,
            specialMethodName: null);

        // Act
        var importedMethodId = await ImportPackageAndGetMethodId(packageMock);

        // Assert
        var dbContext = ServiceProvider.GetRequiredService<IParametersDbContext>();
        var method = await dbContext.Methods
            .Include(x => x.Tests)
            .FirstAsync(x => x.Id == importedMethodId, TestContext.Current.CancellationToken);

        method.IsSpecializedMethodActivated.Should().BeFalse();
        method.Enabled.Should().BeTrue();
        method.Tests.Should().OnlyContain(x => !x.IsSpecializedTestActivated && x.Active);
    }

    [Theory]
    [Trait("ProductRequirement", "PR_342")]
    [Trait("ProductRequirement", "PR_2765")]
    [InlineData(true, false, false)]
    [InlineData(false, true, true)]
    public async Task Import_ShouldApplyAvailabilityDefaultsForNewSpecializedMethod(
        bool isRuo,
        bool isSpecialMethod,
        bool expectedTestActive)
    {
        // Arrange
        var packageMock = MockMethodUpdatePackageWithSpecializedFlags(
            isRuo: isRuo,
            isSpecialMethod: isSpecialMethod,
            testIsRuo: false,
            specialMethodName: null);

        // Act
        var importedMethodId = await ImportPackageAndGetMethodId(packageMock);

        // Assert
        var dbContext = ServiceProvider.GetRequiredService<IParametersDbContext>();
        var method = await dbContext.Methods
            .Include(x => x.Tests)
            .FirstAsync(x => x.Id == importedMethodId, TestContext.Current.CancellationToken);

        method.IsSpecializedMethodActivated.Should().BeFalse();
        method.Enabled.Should().BeFalse();
        method.Tests.Should().OnlyContain(x => x.Active == expectedTestActive);
    }

    [Theory]
    [Trait("ProductRequirement", "PR_342")]
    [Trait("ProductRequirement", "PR_2765")]
    [InlineData(true, null)]
    [InlineData(false, "CEGA")]
    public async Task Import_ShouldDisableNewSpecializedTest(
        bool isRuo,
        string? specialMethodName)
    {
        // Arrange
        var packageMock = MockMethodUpdatePackageWithSpecializedFlags(
            isRuo: false,
            isSpecialMethod: false,
            testIsRuo: isRuo,
            specialMethodName: specialMethodName);

        // Act
        var importedMethodId = await ImportPackageAndGetMethodId(packageMock);

        // Assert
        var dbContext = ServiceProvider.GetRequiredService<IParametersDbContext>();
        var test = await dbContext.MethodTests
            .FirstAsync(x => x.MethodId == importedMethodId && x.TestName == "a_IgE", TestContext.Current.CancellationToken);

        test.IsSpecializedTestActivated.Should().BeFalse();
        test.Active.Should().BeFalse();
    }

    [Theory]
    [Trait("ProductRequirement", "PR_342")]
    [Trait("ProductRequirement", "PR_2765")]
    [InlineData(true, false, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, true)]
    [InlineData(false, true, false)]
    public async Task Reimport_ShouldRetainSpecializedMethodAvailability(
        bool isRuo,
        bool isSpecialMethod,
        bool isActivated)
    {
        // Arrange
        var packageMock = MockMethodUpdatePackageWithSpecializedFlags(
            isRuo: isRuo,
            isSpecialMethod: isSpecialMethod,
            testIsRuo: false,
            specialMethodName: null);
        var importedMethodId = await ImportPackageAndGetMethodId(packageMock);

        var dbContext = ServiceProvider.GetRequiredService<IParametersDbContext>();
        var method = await dbContext.Methods.FirstAsync(x => x.Id == importedMethodId, TestContext.Current.CancellationToken);
        method.IsSpecializedMethodActivated = isActivated;
        method.Enabled = isActivated;
        await dbContext.SaveChangesAsync(CancellationToken.None);

        // Act
        await ImportPackage(packageMock);

        // Assert
        await using var assertScope = ServiceProvider.CreateAsyncScope();
        var assertDbContext = assertScope.ServiceProvider.GetRequiredService<IParametersDbContext>();
        var reimportedMethod = await assertDbContext.Methods
            .AsNoTracking()
            .FirstAsync(x => x.Id == importedMethodId, TestContext.Current.CancellationToken);

        reimportedMethod.IsRuo.Should().Be(isRuo);
        reimportedMethod.IsSpecialMethod.Should().Be(isSpecialMethod);
        reimportedMethod.IsSpecializedMethodActivated.Should().Be(isActivated);
        reimportedMethod.Enabled.Should().Be(isActivated);
    }

    [Theory]
    [Trait("ProductRequirement", "PR_342")]
    [Trait("ProductRequirement", "PR_2765")]
    [InlineData(true, null, true)]
    [InlineData(true, null, false)]
    [InlineData(false, "CEGA", true)]
    [InlineData(false, "CEGA", false)]
    public async Task Reimport_ShouldRetainSpecializedTestAvailability(
        bool isRuo,
        string? specialMethodName,
        bool isActivated)
    {
        // Arrange
        var packageMock = MockMethodUpdatePackageWithSpecializedFlags(
            isRuo: false,
            isSpecialMethod: false,
            testIsRuo: isRuo,
            specialMethodName: specialMethodName);
        var importedMethodId = await ImportPackageAndGetMethodId(packageMock);

        var dbContext = ServiceProvider.GetRequiredService<IParametersDbContext>();
        var methodTest = await dbContext.MethodTests.FirstAsync(x => x.MethodId == importedMethodId && x.TestName == "a_IgE", TestContext.Current.CancellationToken);
        methodTest.IsSpecializedTestActivated = isActivated;
        methodTest.Active = isActivated;
        await dbContext.SaveChangesAsync(CancellationToken.None);

        // Act
        await ImportPackage(packageMock);

        // Assert
        await using var assertScope = ServiceProvider.CreateAsyncScope();
        var assertDbContext = assertScope.ServiceProvider.GetRequiredService<IParametersDbContext>();
        var reimportedTest = await assertDbContext.MethodTests
            .AsNoTracking()
            .FirstAsync(x => x.MethodId == importedMethodId && x.TestName == "a_IgE", TestContext.Current.CancellationToken);

        reimportedTest.IsRuo.Should().Be(isRuo);
        reimportedTest.SpecialMethodName.Should().Be(specialMethodName);
        reimportedTest.IsSpecializedTestActivated.Should().Be(isActivated);
        reimportedTest.Active.Should().Be(isActivated);
    }

    [Fact]
    public async Task PackageExport_ShouldIncludeExpectedPackageFile()
    {
        await ImportArticlesForPackageExport();
        var importedMethodId = await ImportPackageAndGetMethodId(["sIgE.mth", "sIgE.tst"]);

        var dbContext = ServiceProvider.GetRequiredService<IParametersDbContext>();
        var methodTests = await dbContext.MethodTests
            .Where(x => x.MethodId == importedMethodId)
            .OrderBy(x => x.TestName)
            .Take(2)
            .ToListAsync(TestContext.Current.CancellationToken);

        methodTests.Should().HaveCount(2);

        dbContext.Panels.Add(new Panel
        {
            PanelName = "AlphaPanel",
            PanelLongName = "Alpha Panel",
            GroupedPanel = true,
            Tests =
            [
                new PanelTest { MethodTestId = methodTests[1].Id, Replicates = 3, SortOrder = 2 },
                new PanelTest { MethodTestId = methodTests[0].Id, Replicates = 2, SortOrder = 1 }
            ]
        });

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var exportDirectory = await ExportPackageAndExtractAsync();

        try
        {
            File.Exists(Path.Combine(exportDirectory, "version.txt")).Should().BeTrue();
            File.Exists(Path.Combine(exportDirectory, "TestPanels.tpf")).Should().BeTrue();
            File.Exists(Path.Combine(exportDirectory, "description.txt")).Should().BeTrue();
            File.Exists(Path.Combine(exportDirectory, "articles.arf")).Should().BeTrue();
            Directory.GetFiles(exportDirectory, "*.mth").Should().NotBeEmpty();
            Directory.GetFiles(exportDirectory, "*.tst").Should().NotBeEmpty();
        }
        finally
        {
            DeleteDirectoryIfExists(exportDirectory);
        }
    }

    [Fact]
    public async Task PackageExport_ShouldWriteVersionFromDatabase()
    {
        await ImportArticlesForPackageExport();
        await ImportPackageAndGetMethodId(["sIgE.mth", "sIgE.tst"]);

        const int expectedVersion = 12345;
        var dbContext = ServiceProvider.GetRequiredService<IParametersDbContext>();
        var version = await dbContext.ParameterUpdatePackageVersion.FirstAsync(TestContext.Current.CancellationToken);
        dbContext.ParameterUpdatePackageVersion.Remove(version);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        dbContext.ParameterUpdatePackageVersion.Add(new Domain.ParameterUpdatePackageVersion
        {
            VersionNumber = expectedVersion
        });
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var exportDirectory = await ExportPackageAndExtractAsync();

        try
        {
            var versionFile = await File.ReadAllLinesAsync(Path.Combine(exportDirectory, "version.txt"), TestContext.Current.CancellationToken);

            versionFile.Should().Equal(
                "[FILE]",
                "Type=Phadia E UniCAP VERSION FILE",
                "FormatVersion=1.0",
                $"Version={expectedVersion}");
        }
        finally
        {
            DeleteDirectoryIfExists(exportDirectory);
        }
    }

    [Fact]
    public async Task PackageExport_ShouldWritePanelRowsInExpectedOrder()
    {
        await ImportArticlesForPackageExport();
        var importedMethodId = await ImportPackageAndGetMethodId(["sIgE.mth", "sIgE.tst"]);

        var dbContext = ServiceProvider.GetRequiredService<IParametersDbContext>();
        var methodTests = await dbContext.MethodTests
            .Include(x => x.Method)
            .Where(x => x.MethodId == importedMethodId)
            .OrderBy(x => x.TestName)
            .Take(3)
            .ToListAsync(TestContext.Current.CancellationToken);

        methodTests.Should().HaveCount(3);

        dbContext.Panels.AddRange(
            new Panel
            {
                PanelName = "BetaPanel",
                PanelLongName = "Beta Panel",
                GroupedPanel = false,
                Tests =
                [
                    new PanelTest { MethodTestId = methodTests[2].Id, Replicates = 7, SortOrder = 2 },
                    new PanelTest { MethodTestId = methodTests[1].Id, Replicates = 5, SortOrder = 1 }
                ]
            },
            new Panel
            {
                PanelName = "AlphaPanel",
                PanelLongName = "Alpha Panel",
                GroupedPanel = true,
                Tests =
                [
                    new PanelTest { MethodTestId = methodTests[0].Id, Replicates = 4, SortOrder = 1 }
                ]
            });

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var exportDirectory = await ExportPackageAndExtractAsync();

        try
        {
            var panelFile = await File.ReadAllLinesAsync(Path.Combine(exportDirectory, "TestPanels.tpf"), TestContext.Current.CancellationToken);

            panelFile.Should().ContainInConsecutiveOrder(
                "[FILE]",
                "Type=Phadia E UniCAP TESTPANELS FILE",
                "FormatVersion=1.0",
                "Comment=Test panels",
                "Version=1",
                "[TESTPANELS]");

            var testPanelLines = panelFile
                .SkipWhile(x => x != "[TESTPANELS]")
                .Skip(1)
                .TakeWhile(x => x != "[END]")
                .ToList();

            testPanelLines.Should().Equal(
                $"AlphaPanel;Alpha Panel;{methodTests[0].TestName};{methodTests[0].Method.MethodName};4;1",
                $"BetaPanel;Beta Panel;{methodTests[1].TestName};{methodTests[1].Method.MethodName};5;0",
                $"BetaPanel;Beta Panel;{methodTests[2].TestName};{methodTests[2].Method.MethodName};7;0");
        }
        finally
        {
            DeleteDirectoryIfExists(exportDirectory);
        }
    }

    private async Task<int> ImportPackageAndGetMethodId(params string[] files)
    {
        var dir = DataDrivenTestHelper.GetTestDataFilesPath(GetType());
        var packageMock = MockMethodUpdatePackage(dir, files);
        return await ImportPackageAndGetMethodId(packageMock);
    }

    private async Task<int> ImportPackageAndGetMethodId(Mock<IMethodUpdatePackage> packageMock)
    {
        var packageFactory = new Mock<IMethodUpdatePackageFactory>();
        packageFactory.Setup(f => f.Create(It.IsAny<string>(), It.IsAny<string?>()))
            .Returns((string _, string? _) => packageMock.Object);

        var dbContext = ServiceProvider.GetRequiredService<IParametersDbContext>();
        var sender = ServiceProvider.GetRequiredService<ISender>();
        var logger = ServiceProvider.GetRequiredService<ILogger<ImportMethodUpdatePackageCommand>>();

        var handler = new ImportMethodUpdatePackageCommandHandler(packageFactory.Object, dbContext, sender, new Mock<IPublisher>().Object, logger);
        var success = await handler.Handle(new ImportMethodUpdatePackageCommand("", ""), TestContext.Current.CancellationToken);

        Assert.True(success);

        return await dbContext.Methods.Select(x => x.Id).FirstAsync(TestContext.Current.CancellationToken);
    }

    private async Task ImportPackage(Mock<IMethodUpdatePackage> packageMock)
    {
        await using var scope = ServiceProvider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var success = await sender.Send(new ImportMethodsAndTestsCommand(packageMock.Object));

        success.Should().BeTrue();
    }

    private async Task ImportAndExportFilesWithChecks(string[] successfulFiles, string[]? errorFiles = null, bool repeatedImport = false)
    {
        var files = successfulFiles;
        if (errorFiles != null)
        {
            files = successfulFiles.Concat(errorFiles).ToArray();
        }

        var packageFactory = new Mock<IMethodUpdatePackageFactory>();
        var dir = DataDrivenTestHelper.GetTestDataFilesPath(GetType());
        var packageMock = MockMethodUpdatePackage(dir, files);
        packageFactory.Setup(f => f.Create(It.IsAny<string>(), It.IsAny<string?>()))
            .Returns((string _, string? _) => packageMock.Object);

        var dbContext = ServiceProvider.GetRequiredService<IParametersDbContext>();
        var sender = ServiceProvider.GetRequiredService<ISender>();
        var logger = ServiceProvider.GetRequiredService<ILogger<ImportMethodUpdatePackageCommand>>();

        var handler = new ImportMethodUpdatePackageCommandHandler(packageFactory.Object, dbContext, sender, new Mock<IPublisher>().Object, logger);
        var success = await handler.Handle(new ImportMethodUpdatePackageCommand("", ""), TestContext.Current.CancellationToken);
        if (errorFiles is { Length: > 0 })
        {
            Assert.False(success);
            Assert.False(await dbContext.Methods.AnyAsync(TestContext.Current.CancellationToken));
            return;
        }

        Assert.True(success);

        if (repeatedImport)
        {
            // create a new scope for repeated import
            await using var scope = ServiceProvider.CreateAsyncScope();
            var sender2 = scope.ServiceProvider.GetRequiredService<ISender>();
            await sender2.Send(new ImportMethodsAndTestsCommand(packageMock.Object));
        }

        // get imported method from the database
        var importedMethodId = await dbContext.Methods.Select(x => x.Id).FirstAsync(TestContext.Current.CancellationToken);
        var importedMethodFromDatabase = await dbContext.Methods.AsNoTracking().FirstAsync(x => x.Id == importedMethodId, TestContext.Current.CancellationToken);
        importedMethodFromDatabase.Reporting.DefaultMinConcentrationToReport.Should().Be(GetExpectedLowestConcentration(dir, successfulFiles));

        // use a new scope to have a "clean slate"
        await using var exportScope = ServiceProvider.CreateAsyncScope();
        var exportService = exportScope.ServiceProvider.GetRequiredService<IExportParameterService>();
        var importedMethod = await exportService.GetMethodForExport(importedMethodId, TestContext.Current.CancellationToken);

        // generate export files and compare with original
        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        foreach (var file in successfulFiles)
        {
            var fileExt = Path.GetExtension(file);
            var actualFile = Path.Combine(dir, $"{Path.GetFileNameWithoutExtension(file)}_actual{fileExt}");
            try
            {
                await using (var writer = new StreamWriter(actualFile, false, encoding))
                {
                    if (fileExt == ".mth")
                    {
                        var qualityControls = await dbContext.PhadiaQualityControlTests
                            .Include(qc => qc.QualityControl)
                            .Include(qc => qc.Test)
                            .Where(qc => qc.Test.MethodId == importedMethodId)
                            .AsNoTracking()
                            .ToListAsync(TestContext.Current.CancellationToken);
                        exportService.ExportMethod(importedMethod, qualityControls, writer);
                    }
                    else if (fileExt == ".tst")
                    {
                        exportService.ExportTests(importedMethod, writer);
                    }
                    else
                    {
                        throw new NotSupportedException($"File extension {fileExt} is not supported.");
                    }
                }

                var expectedFile = Path.Combine(dir, file);
                ParameterFileComparer.CheckMethodFilesEqual(expectedFile, actualFile, true, true);
            }
            finally
            {
                if (File.Exists(actualFile))
                {
                    File.Delete(actualFile);
                }
            }
        }
    }

    private async Task<string> ExportPackageAndExtractAsync()
    {
        await using var exportScope = ServiceProvider.CreateAsyncScope();
        var exportService = exportScope.ServiceProvider.GetRequiredService<IExportParameterService>();
        await using var zipStream = await exportService.ExportParametersAsZip([], true, TestContext.Current.CancellationToken);

        var exportDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(exportDirectory);

        zipStream.Position = 0;
        ZipFile.ExtractToDirectory(zipStream, exportDirectory);

        return exportDirectory;
    }

    private async Task ImportArticlesForPackageExport()
    {
        var dbContextInitializer = ParametersModule.Instance.GetDbContextInitializer(ServiceProvider);
        await dbContextInitializer!.InitializeAsync(RecreateObjectsMode.Never);

        var dir = DataDrivenTestHelper.GetTestDataFilesPath(typeof(ImportArticlesCommandTests));
        var packageMock = MockMethodUpdatePackage(dir, "article.arf");
        var sender = ServiceProvider.GetRequiredService<ISender>();
        var success = await sender.Send(new ImportArticlesCommand(packageMock.Object));

        success.Should().BeTrue();
    }

    private static void DeleteDirectoryIfExists(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }
    }

    private static double GetExpectedLowestConcentration(string dir, string[] successfulFiles)
    {
        var methodFile = successfulFiles.LastOrDefault(x => string.Equals(Path.GetExtension(x), ".mth", StringComparison.OrdinalIgnoreCase));
        if (methodFile == null)
        {
            return 0;
        }

        var lowestConcentrationLine = File.ReadLines(Path.Combine(dir, methodFile))
            .Single(x => x.StartsWith("LowestConc=", StringComparison.Ordinal));

        return double.Parse(lowestConcentrationLine["LowestConc=".Length..], CultureInfo.InvariantCulture);
    }

    private static Mock<IMethodUpdatePackage> MockMethodUpdatePackage(string dir, params string[] files)
    {
        var packageFiles = new List<string>(files) { "version.txt" };
        var packageMock = new Mock<IMethodUpdatePackage>();

        packageMock.Setup(m => m.GetFileNames())
            .Returns(packageFiles);

        foreach (var file in packageFiles)
        {
            packageMock.Setup(m => m.GetFileContent(file))
                .Returns(() => File.ReadAllBytesAsync(Path.Combine(dir, file)));
        }
        return packageMock;
    }

    private Mock<IMethodUpdatePackage> MockMethodUpdatePackageWithSpecializedFlags(
        bool isRuo,
        bool isSpecialMethod,
        bool testIsRuo,
        string? specialMethodName)
    {
        var dir = DataDrivenTestHelper.GetTestDataFilesPath(GetType());
        var methodFile = File.ReadAllText(Path.Combine(dir, "sIgE.mth"));
        methodFile = methodFile
            .Replace("SpecialMethod=0", $"SpecialMethod={Convert.ToInt32(isSpecialMethod)}", StringComparison.Ordinal)
            .Replace("RUO=0", $"RUO={Convert.ToInt32(isRuo)}", StringComparison.Ordinal);

        var testFile = SetTestParameter(
            File.ReadAllText(Path.Combine(dir, "sIgE.tst")),
            "a_IgE",
            29,
            specialMethodName ?? string.Empty);
        testFile = SetTestParameter(testFile, "a_IgE", 34, Convert.ToInt32(testIsRuo).ToString(CultureInfo.InvariantCulture));

        return MockMethodUpdatePackage(
            new Dictionary<string, string>
            {
                ["sIgE.mth"] = methodFile,
                ["sIgE.tst"] = testFile,
                ["version.txt"] = File.ReadAllText(Path.Combine(dir, "version.txt"))
            });
    }

    private static string SetTestParameter(string content, string testName, int parameterIndex, string value)
    {
        var lines = content.Split(["\r\n", "\n"], StringSplitOptions.None);
        for (var i = 0; i < lines.Length; i++)
        {
            if (!lines[i].StartsWith($"{testName}=", StringComparison.Ordinal))
            {
                continue;
            }

            var values = lines[i][(testName.Length + 1)..].Split(';');
            values[parameterIndex] = value;
            lines[i] = $"{testName}={string.Join(';', values)}";
            break;
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static Mock<IMethodUpdatePackage> MockMethodUpdatePackage(Dictionary<string, string> files)
    {
        var packageMock = new Mock<IMethodUpdatePackage>();
        packageMock.Setup(m => m.GetFileNames())
            .Returns(files.Keys.ToList());

        foreach (var file in files)
        {
            packageMock.Setup(m => m.GetFileContent(file.Key))
                .ReturnsAsync(Encoding.UTF8.GetBytes(file.Value));
        }

        return packageMock;
    }
}
