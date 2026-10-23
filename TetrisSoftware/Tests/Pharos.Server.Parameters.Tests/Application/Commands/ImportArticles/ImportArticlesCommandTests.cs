using System.Text;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Parameters.Application.Services;
using Pharos.Server.Parameters.FileComparer;

namespace Pharos.Server.Parameters.Application.Commands.ImportArticles;

[Collection(nameof(ParametersTestsCollection))]
public class ImportArticlesCommandTests(ParametersTestsWaf factory) : DatabaseTestBase<ParametersTestsWaf>(factory)
{
    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        // initialize pre-defined database parameters
        var dbContextInitializer = ParametersModule.Instance.GetDbContextInitializer(ServiceProvider);
        await dbContextInitializer!.InitializeAsync(RecreateObjectsMode.Never);
    }

    [Fact]
    public Task ImportArticlesToEmptyDatabase()
    {
        return ImportAndExportArticlesWithChecks(["article.arf"]);
    }

    [Fact]
    public Task RepeatedImportOfArticles()
    {
        return ImportAndExportArticlesWithChecks(["article.arf"], repeatedImport: true);
    }

    [Fact]
    public async Task LowerArticleVersionIsIgnored()
    {
        await ImportAndExportArticlesWithChecks(["article_min_v2.arf"]);
        await ImportAndExportArticlesWithChecks(["article_min_v1.arf"], customExpectedFile: "article_min_v2.arf");
    }

    [Fact]
    public async Task MultipleArticleFilesInOnePackage()
    {
        await ImportAndExportArticlesWithChecks(["article_min_v1.arf", "article_min_v1_extra.arf"], customExpectedFile: "article_multi_expected.arf", mustFail: false);
    }

    [Fact]
    public async Task MultipleArticleVersionsNotAllowedInOnePackage()
    {
        await ImportAndExportArticlesWithChecks(["article_min_v1.arf", "article_min_v2.arf"], mustFail: true);
    }

    [Fact]
    public async Task FailedFileParsing()
    {
        await ImportAndExportArticlesWithChecks(["article_error.arf"], mustFail: true);
    }

    [Fact]
    public async Task MalformedPackageFieldReturnsValidationError()
    {
        var dir = DataDrivenTestHelper.GetTestDataFilesPath(GetType());
        var packageMock = MockMethodUpdatePackage(dir, "article_malformed_package.arf");
        var sender = ServiceProvider.GetRequiredService<ISender>();

        var action = () => sender.Send(new ImportArticlesCommand(packageMock.Object));

        await action.Should().ThrowAsync<FluentValidation.ValidationException>()
            .WithMessage("*article_malformed_package.arf*contains malformed data*");
    }

    [Fact]
    public async Task ImportDeletesExistingArticle()
    {
        await ImportAndExportArticlesWithChecks(["article_min_v1.arf", "article_min_v1_extra.arf"], customExpectedFile: "article_multi_expected.arf");
        await ImportAndExportArticlesWithChecks(["article_delete_article.arf"], customExpectedFile: "article_min_v2.arf");
    }

    [Fact]
    public async Task ImportDeletesExistingPackage()
    {
        await ImportAndExportArticlesWithChecks(["article_min_v1.arf", "article_min_v1_extra.arf"], customExpectedFile: "article_multi_expected.arf");
        await ImportAndExportArticlesWithChecks(["article_delete_package.arf"], customExpectedFile: "article_delete_package_expected.arf");
    }

    private async Task ImportAndExportArticlesWithChecks(string[] articleFiles, string? customExpectedFile = null, bool repeatedImport = false, bool mustFail = false)
    {
        var dir = DataDrivenTestHelper.GetTestDataFilesPath(GetType());
        var packageMock = MockMethodUpdatePackage(dir, articleFiles);

        var sender = ServiceProvider.GetRequiredService<ISender>();
        bool result;
        try
        {
            result = await sender.Send(new ImportArticlesCommand(packageMock.Object));
        }
        catch
        {
            if (mustFail)
            {
                return;
            }
            throw;
        }
        result.Should().BeTrue();

        if (repeatedImport)
        {
            // create a new scope for repeated import
            await using var scope = ServiceProvider.CreateAsyncScope();
            var sender2 = scope.ServiceProvider.GetRequiredService<ISender>();
            result = await sender2.Send(new ImportArticlesCommand(packageMock.Object));
        }

        result.Should().BeTrue();

        // generate export files and compare with original
        if (customExpectedFile == null && articleFiles.Length > 1)
        {
            throw new ArgumentException("The test with multiple input files must specify th expected output file.");
        }

        var fileExt = Path.GetExtension(articleFiles[0]);
        var actualFile = Path.Combine(dir, $"{Path.GetFileNameWithoutExtension(articleFiles[0])}_actual{fileExt}");
        var normalizedExpectedFile = Path.Combine(dir, $"{Path.GetFileNameWithoutExtension(articleFiles[0])}_expected_normalized{fileExt}");
        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        try
        {
            await using (var writer = new StreamWriter(actualFile, false, encoding))
            {
                // get imported articles from the database
                // use a new scope to have a "clean slate"
                await using var scope = ServiceProvider.CreateAsyncScope();
                var exportService = scope.ServiceProvider.GetRequiredService<IExportParameterService>();

                // export to a file
                var articleParameters = await exportService.GetArticlesForExport(TestContext.Current.CancellationToken);
                exportService.ExportArticles(articleParameters, writer);
            }
            var expectedFile = customExpectedFile != null ? Path.Combine(dir, customExpectedFile) : Path.Combine(dir, articleFiles[0]);
            NormalizeLegacyWorldwideMarket(expectedFile, normalizedExpectedFile, encoding);
            ParameterFileComparer.CheckMethodFilesEqual(normalizedExpectedFile, actualFile, true, true);
        }
        finally
        {
            if (File.Exists(actualFile))
            {
                File.Delete(actualFile);
            }
            if (File.Exists(normalizedExpectedFile))
            {
                File.Delete(normalizedExpectedFile);
            }
        }
    }

    private static void NormalizeLegacyWorldwideMarket(string sourceFile, string destinationFile, Encoding encoding)
    {
        var inPackageSection = false;
        using var reader = new StreamReader(sourceFile, encoding, detectEncodingFromByteOrderMarks: true);
        using var writer = new StreamWriter(destinationFile, false, encoding);

        while (reader.ReadLine() is { } line)
        {
            var trimmedLine = line.Trim();

            if (trimmedLine.StartsWith('[') && trimmedLine.EndsWith(']'))
            {
                inPackageSection = string.Equals(trimmedLine, "[PACKAGE]", StringComparison.OrdinalIgnoreCase);
                writer.WriteLine(line);
                continue;
            }

            if (!inPackageSection || string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine[0] is ';' or '#' or '/')
            {
                writer.WriteLine(line);
                continue;
            }

            var values = line.Split(';');
            if (values.Length > 5 && values[5] == "2")
            {
                values[5] = "0";
                writer.WriteLine(string.Join(';', values));
                continue;
            }

            writer.WriteLine(line);
        }
    }

    private static Mock<IMethodUpdatePackage> MockMethodUpdatePackage(string dir, params string[] files)
    {
        var packageMock = new Mock<IMethodUpdatePackage>();

        packageMock.Setup(m => m.GetFileNames())
            .Returns(files);

        foreach (var file in files)
        {
            packageMock.Setup(m => m.GetFileContent(file))
                .Returns(() => File.ReadAllBytesAsync(Path.Combine(dir, file)));
        }
        return packageMock;
    }
}
