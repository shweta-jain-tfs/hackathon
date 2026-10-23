using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Quality;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Quality.Application.Mapping;

namespace Pharos.Server.Quality.Application.Services;

public record QcManufacturingInfo(double? RangeMin, double? RangeMax, double? ExpectedConcentration, ManufacturingLimitCheck LimitCheck);

internal class QcCartridgeInfoService(
    IMethodsService methodsService,
    IArticlesService articlesService,
    IQualityControlService qualityControlService,
    ISender sender,
    ILogger<QcCartridgeInfoService> logger) : IQcCartridgeInfoService
{
    public async Task<QcCartridgeInformationDto?> ParseQcDataMatrixAsync(string dataMatrix, CancellationToken cancellationToken)
    {
        if (!QcDataMatrixBarcode.TryParse(dataMatrix, out var parsedQcData) || parsedQcData == null)
        {
            return null;
        }

        // Build lot number from article ID and lot number from QC data matrix
        var lotNumber = $"{parsedQcData.ArticleId}{parsedQcData.LotNumber}";

        var article = await articlesService.GetArticle(parsedQcData.ArticleId);

        if (article != null)
        {
            var qcCartridgeInformation = article.ToQcCartridgeInformationDto();

            var methodId = await methodsService.GetMethod(qcCartridgeInformation.MethodName ?? string.Empty, m => m.Id);

            // Populate tests with concentration units from method parameters using the article's method name
            var testsWithUnits = await PopulateTestDetailsAsync(parsedQcData.Tests, qcCartridgeInformation.MethodName, parsedQcData.ArticleId, cancellationToken).ConfigureAwait(false);

            qcCartridgeInformation = qcCartridgeInformation with
            {
                ArticleId = parsedQcData.ArticleId,
                LotNumber = lotNumber,
                DisplayLotNumber = parsedQcData.LotNumber,
                Version = parsedQcData.Version,
                MethodId = methodId,
                Tests = testsWithUnits,
            };

            var isNewlyRegistered = await RegisterQcRangesAsync(qcCartridgeInformation, parsedQcData.ArticleId, cancellationToken).ConfigureAwait(false);

            qcCartridgeInformation = qcCartridgeInformation with
            {
                IsNewlyRegistered = isNewlyRegistered
            };

            return qcCartridgeInformation;
        }

        return null;
    }

    /// <summary>
    /// Populates QC test information with concentration units from method parameters.
    /// Uses IMethodsService.GetMethodTestByLabNames() to look up units by test name.
    /// When methodName is provided, the search is scoped to that specific method for better efficiency.
    /// </summary>
    private async Task<IReadOnlyList<QcCartridgeTestInfo>> PopulateTestDetailsAsync(IReadOnlyList<QcCartridgeTestInfo> tests, string? methodName, string articleNumber, CancellationToken cancellationToken)
    {
        if (tests.Count == 0)
        {
            return tests;
        }

        var updatedTests = new List<QcCartridgeTestInfo>(tests.Count);

        var qualityControlTests = await qualityControlService.GetPhadiaQualityControlTests(articleNumber, cancellationToken);

        foreach (var test in tests)
        {
            var unit = await GetConcentrationUnitAsync(test.TestName, methodName).ConfigureAwait(false);
            var qualityControlTest = qualityControlTests.FirstOrDefault(t => t.TestName == test.TestName);

            updatedTests.Add(test with
            {
                Unit = unit ?? string.Empty,
                QcTestId = qualityControlTest?.Id,
                QcTestEnabled = qualityControlTest?.Enabled ?? false
            });
        }

        return updatedTests;
    }

    /// <summary>
    /// Gets the concentration unit for a test by searching methods.
    /// When methodName is provided, searches within that specific method for better performance.
    /// When methodName is null, searches all methods.
    /// </summary>
    private async Task<string?> GetConcentrationUnitAsync(string testName, string? methodName)
    {
        try
        {
            var methodTest = await methodsService.GetMethodTestByLabNameOrTestName(methodName, testName, x => new { x.Method.MethodName, x.TestName });

            if (methodTest == null)
            {
                return null;
            }

            return await methodsService.GetMethodTestConcentrationUnit(methodTest.MethodName, methodTest.TestName).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to get concentration unit for test: {TestName}", testName);
            return null;
        }
    }

    /// <summary>
    /// Registers QC ranges in the database if they don't already exist.
    /// </summary>
    /// <param name="qcCartridgeInfo">The QC cartridge information with test ranges</param>
    /// <param name="articleNumber">The article number associated with the QC cartridge information</param>
    /// <param name="cancellationToken"></param>
    /// <returns>True if ranges were newly registered; false if they already existed</returns>
    private async Task<bool> RegisterQcRangesAsync(QcCartridgeInformationDto qcCartridgeInfo, string? articleNumber, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(articleNumber))
        {
            return false;
        }

        var isNewlyRegistered = true;

        foreach (var test in qcCartridgeInfo.Tests)
        {
            if (test.QcTestId != null)
            {
                var qcManufacturingInfo = ConvertLimitsToManufacturingLimitCheck(test.Limits);

                var qcTestExists = await qualityControlService.CheckIfQcLotExistsForTest((int)test.QcTestId, qcCartridgeInfo.LotNumber, cancellationToken).ConfigureAwait(false);

                if (qcTestExists)
                {
                    var command = new UpdatePhadiaQCLotManufacturingCommand
                    {
                        Id = test.QcTestId.Value,
                        LotNumber = qcCartridgeInfo.LotNumber,
                        ManufacturingRangeMin = qcManufacturingInfo.RangeMin,
                        ManufacturingRangeMax = qcManufacturingInfo.RangeMax,
                        ManufacturingExpectedConcentration = qcManufacturingInfo.ExpectedConcentration,
                    };

                    await sender.Send(command, cancellationToken).ConfigureAwait(false);

                    isNewlyRegistered = false;
                }
                else
                {
                    var command = new CreatePhadiaQualityControlLotCommand
                    {
                        QualityControlId = test.QcTestId.Value,
                        LotType = LotType.AcceptanceRange,
                        LotNumber = qcCartridgeInfo.LotNumber
                    };

                    await sender.Send(command, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        return isNewlyRegistered;
    }

    /// <summary>
    /// Converts QC Data Matrix limits to database range format.
    /// Maps QC cartridge limit types to RangeMin/RangeMax with exclude flags.
    /// </summary>
    private static QcManufacturingInfo ConvertLimitsToManufacturingLimitCheck(IReadOnlyList<QcCartridgeTestLimit> limits)
    {
        if (limits is null || limits.Count == 0)
        {
            return new QcManufacturingInfo(null, null, null, ManufacturingLimitCheck.None);
        }

        QcCartridgeTestLimit? lessThan = null;
        QcCartridgeTestLimit? greaterThan = null;
        QcCartridgeTestLimit? equalTo = null;

        foreach (var limit in limits)
        {
            switch (limit.LimitType)
            {
                case QcCartridgeLimitType.LessThan:
                case QcCartridgeLimitType.LessThanOrEqual:
                    lessThan ??= limit;
                    break;

                case QcCartridgeLimitType.GreaterThan:
                case QcCartridgeLimitType.GreaterThanOrEqual:
                    greaterThan ??= limit;
                    break;

                case QcCartridgeLimitType.Equal:
                    equalTo ??= limit;
                    break;
            }
        }

        return new QcManufacturingInfo(greaterThan?.LimitValue, lessThan?.LimitValue, equalTo?.LimitValue,
            DetermineLimitCheck(lessThan?.LimitType, greaterThan?.LimitType));
    }

    private static ManufacturingLimitCheck DetermineLimitCheck(QcCartridgeLimitType? lessThanType, QcCartridgeLimitType? greaterThanType)
    {
        var hasUpper = lessThanType.HasValue;
        var hasLower = greaterThanType.HasValue;

        if (!hasUpper && !hasLower)
        {
            return ManufacturingLimitCheck.LessThan_GreaterThan;
        }

        if (hasUpper && !hasLower)
        {
            return lessThanType == QcCartridgeLimitType.LessThan ? ManufacturingLimitCheck.LessThan : ManufacturingLimitCheck.LessThanOrEqual;
        }

        if (!hasUpper && hasLower)
        {
            return greaterThanType == QcCartridgeLimitType.GreaterThan ? ManufacturingLimitCheck.GreaterThan : ManufacturingLimitCheck.GreaterThanOrEqual;
        }

        var upperInclusive = lessThanType == QcCartridgeLimitType.LessThanOrEqual;
        var lowerInclusive = greaterThanType == QcCartridgeLimitType.GreaterThanOrEqual;

        return (upperInclusive, lowerInclusive) switch
        {
            (false, false) => ManufacturingLimitCheck.LessThan_GreaterThan,
            (false, true) => ManufacturingLimitCheck.LessThan_GreaterThanOrEqual,
            (true, false) => ManufacturingLimitCheck.LessThanOrEqual_GreaterThan,
            (true, true) => ManufacturingLimitCheck.LessThanOrEqual_GreaterThanOrEqual,
        };
    }
}
