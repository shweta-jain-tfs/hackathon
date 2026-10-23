using System.Drawing;
using FluentValidation;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Parameters.Application.Common;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Parameters.Application.Parsers;
using Pharos.Server.Parameters.Application.Parsers.ValueTypeParsers;
using Pharos.Server.Parameters.Domain;

namespace Pharos.Server.Parameters.Application.Commands.ImportArticles;

/// <summary>
/// Lists the names of the columns (with their indices) from the articles specification used by Pharos.
/// The names and the values are taken from the specification.
/// </summary>
internal enum ArticleField
{
    ArticleBarcode = 0,
    Type = 1,
    ArticleFullName = 2,
    Quantity = 3,
    MethodName = 4,
    ArticleIdentity = 5,
    MaxDaysInInstrument = 6,
    MaxHoursInInstrument = 7,
    NumberOfCartridgeCells = 8,
    MaxPiercedHoles = 9,
    FriendlyName = 10,
    Text1 = 11,
    Text2 = 12,
    Text3 = 13,
    Text4 = 14,
    Shape = 15,
    Color1 = 16,
    Color2 = 17,
    Color3 = 18,
    Color4 = 19
}

/// <summary>
/// Lists the names of the columns (with their indices) from the package specification used by Pharos.
/// The names and the values are taken from the specification.
/// </summary>
internal enum PackageField
{
    PackageBarcode = 0,
    ArticleNumber = 1,
    PackageFullName = 2,
    ArticleBarcode = 3,
    AmountInPackage = 4,
    IntendedMarket = 5
}

internal class ArticleParametersHandler(
    IParameterFileContentParser parameterFileContentParser,
    IParameterFileParser parameterFileParser,
    IValidator<Article> validatorArticle,
    IValidator<IntendedMarket> validatorMarket,
    IValidator<Package> validatorPackage)
    : ParameterHandler<ArticleParameters>(parameterFileContentParser, parameterFileParser)
{
    private string? _currentPackage;

    protected override ArticleParameters HandleFile(ParameterFile file)
    {
        var result = new ArticleParameters();

        var sectionParsers = new List<ParameterSectionParser<ArticleParameters>>
        {
            new("[FILE]",
            true,
            (_, parameters) => HandleFileSection(file.Name, parameters, MethodUpdateConstants.ArticlesFileType, MethodUpdateConstants.ArticlesFileFormatVersion, out result.Version), null),

            new("[MARKET]",
                false,
                null,
                new List<ParameterPropertyParserBase<ArticleParameters>>
                {
                    new PropertyObjectParser<ArticleParameters, IntendedMarket>(
                        "",
                        true,
                        [
                            new ParameterArrayValueParser<IntendedMarket, int>(
                                0,
                                true,
                                (model, value) => model.Id = value, new IntParser()),

                            new ParameterArrayValueParser<IntendedMarket, string>(
                                1,
                                true,
                                (model, value) => model.Name = value, new StringParser()),
                        ],
                        (mParent, mObject) => mParent.Markets.Add(mObject))
                }),

            new("[ARTICLE]",
            true,
            null,
            new List<ParameterPropertyParserBase<ArticleParameters>>
            {
                new PropertyObjectParser<ArticleParameters, Article>(
                    "",
                    true,
                    [
                        new ParameterArrayValueParser<Article, string>(
                            (int)ArticleField.ArticleBarcode,
                            true,
                            (model, value) => model.ArticleNumber = value, new StringParser()),

                        new ParameterArrayValueParser<Article, ArticleType>(
                            (int)ArticleField.Type,
                            true,
                            // TBD: Remove this compatibility normalization later. Tracked by Bug no. 37427.
                            (model, value) => model.ArticleType = ArticleTypeImportMapper.Normalize(value), new EnumParser<ArticleType>()),

                        new ParameterArrayValueParser<Article, string>(
                            (int)ArticleField.ArticleFullName,
                            false,
                            (model, value) => model.FullName = value, new StringParser()),

                        new ParameterArrayValueParser<Article, int>(
                            (int)ArticleField.Quantity,
                            true,
                            (model, value) => model.Quantity = value, new IntParser()),

                        new ParameterArrayValueParser<Article, string>(
                            (int)ArticleField.MethodName,
                            false,
                            (model, value) => model.MethodName = value, new StringParser()),

                        new ParameterArrayValueParser<Article, string>(
                            (int)ArticleField.ArticleIdentity,
                            false,
                            (model, value) => model.Identity = value, new StringParser()),

                        new ParameterArrayValueParser<Article, int?>(
                            (int)ArticleField.MaxDaysInInstrument,
                            false,
                            (model, value) => model.MaxDaysInInstrument = value, new NullableIntParser()),

                        new ParameterArrayValueParser<Article, int?>(
                            (int)ArticleField.MaxHoursInInstrument,
                            false,
                            (model, value) => model.MaxHoursInInstrument = value, new NullableIntParser()),

                        new ParameterArrayValueParser<Article, int?>(
                            (int)ArticleField.NumberOfCartridgeCells,
                            false,
                            (model, value) => model.NumberOfCells = value, new NullableIntParser()),

                        new ParameterArrayValueParser<Article, int?>(
                            (int)ArticleField.MaxPiercedHoles,
                            false,
                            (model, value) => model.MaxPiercedHoles = value, new NullableIntParser()),

                        new ParameterArrayValueParser<Article, string>(
                            (int)ArticleField.FriendlyName,
                            false,
                            (model, value) => model.FriendlyName = value, new StringParser()),

                        new ParameterArrayValueParser<Article, string>(
                            (int)ArticleField.Text1,
                            false,
                            (model, value) => model.Text1 = value, new StringParser()),

                        new ParameterArrayValueParser<Article, string>(
                            (int)ArticleField.Text2,
                            false,
                            (model, value) => model.Text2 = value, new StringParser()),

                        new ParameterArrayValueParser<Article, string>(
                            (int)ArticleField.Text3,
                            false,
                            (model, value) => model.Text3 = value, new StringParser()),

                        new ParameterArrayValueParser<Article, string>(
                            (int)ArticleField.Text4,
                            false,
                            (model, value) => model.Text4 = value, new StringParser()),

                        new ParameterArrayValueParser<Article, ArticleIndicatorShape>(
                            (int)ArticleField.Shape,
                            false,
                            (model, value) => model.Shape = value, new EnumParser<ArticleIndicatorShape>()),

                        new ParameterArrayValueParser<Article, Color?>(
                            (int)ArticleField.Color1,
                            false,
                            (model, value) => model.Color1 = value, new ColorParser()),

                        new ParameterArrayValueParser<Article, Color?>(
                            (int)ArticleField.Color2,
                            false,
                            (model, value) => model.Color2 = value, new ColorParser()),

                        new ParameterArrayValueParser<Article, Color?>(
                            (int)ArticleField.Color3,
                            false,
                            (model, value) => model.Color3 = value, new ColorParser()),

                        new ParameterArrayValueParser<Article, Color?>(
                            (int)ArticleField.Color4,
                            false,
                            (model, value) => model.Color4 = value, new ColorParser())
                    ],
                    (mParent, mObject) =>
                    {
                        mObject.CartridgeCells = new List<CartridgeCellIdentity>();
                        mParent.Articles.Add(mObject);
                    })
            }),

            new("[CARTRIDGE]",
            false,
            null,
            new List<ParameterPropertyParserBase<ArticleParameters>>
            {
                new PropertyObjectParser<ArticleParameters, CartridgeCellIdentity>(
                    "",
                    true,
                    [
                        new ParameterArrayValueParser<CartridgeCellIdentity, string>(
                            0,
                            true,
                            (model, value) => model.Article = new Article { ArticleNumber = value }, new StringParser()),

                        new ParameterArrayValueParser<CartridgeCellIdentity, int>(
                            1,
                            true,
                            (model, value) => model.CellPosition = value, new IntParser()),

                        new ParameterArrayValueParser<CartridgeCellIdentity, string>(
                            2,
                            false,
                            (model, value) => model.CellIdentity = value, new StringParser()),
                    ],
                    (mParent, mObject) =>
                    {
                        var article = mParent.Articles.FirstOrDefault(x => x.ArticleNumber == mObject.Article.ArticleNumber);
                        if (article == null)
                        {
                            throw new ValidationException($"The article '{mObject.Article.ArticleNumber}' is referenced in the [CARTRIDGE] section but not found in the [ARTICLES] section.");
                        }
                        mObject.Article = null!;
                        article.CartridgeCells?.Add(mObject);
                    })
            }),

            new("[PACKAGE]",
            true,
            null,
            new List<ParameterPropertyParserBase<ArticleParameters>>
            {
                new PropertyObjectParser<ArticleParameters, Package>(
                    "",
                    true,
                    [
                        new ParameterArrayValueParser<Package, string>(
                            (int)PackageField.PackageBarcode,
                            true,
                            (model, value) => model.PackageBarcode = value, new StringParser()),

                        new ParameterArrayValueParser<Package, string>(
                            (int)PackageField.ArticleNumber,
                            true,
                            (model, value) => model.OrderingNumber = value, new StringParser()),

                        new ParameterArrayValueParser<Package, string>(
                            (int)PackageField.PackageFullName,
                            false,
                            (model, value) => model.PackageFullName = value, new StringParser()),

                        new ParameterArrayValueParser<Package, int>(
                            (int)PackageField.IntendedMarket,
                            false,
                            (model, value) => model.IntendedMarketId = value, new IntParser())
                    ],
                    (mParent, mObject) =>
                    {
                        _currentPackage = mObject.PackageBarcode;
                        var p = mParent.Packages.Find(x => x.PackageBarcode == mObject.PackageBarcode);
                        if (p == null)
                        {
                            // TODO: Need to remove from parameter package AllCountries,
                            // for now map “International” and “All countries” from the file to “Worldwide.”
                            if(mObject.IntendedMarketId == 2)
                            {
                                mObject.IntendedMarketId = 0; 
                            }
                            if (!IntendedMarket.PredefinedMarketsIds.Contains(mObject.IntendedMarketId) && result.Markets.All(x => x.Id != mObject.IntendedMarketId))
                            {
                                throw new ValidationException($"The package '{mObject.PackageBarcode}' has '{mObject.IntendedMarketId}' as intended market ID, which is neither pre-defined not specified in in the [MARKET] section.");
                            }
                            mObject.PackageItems = new List<PackageItem>();
                            mParent.Packages.Add(mObject);
                        }
                    }),

                new PropertyObjectParser<ArticleParameters, PackageItem>(
                    "",
                    true,
                    [
                        new ParameterArrayValueParser<PackageItem, Article>(
                            (int)PackageField.ArticleBarcode,
                            true,
                            (model, value) => model.Article = value, new ArticleParser(result.Articles)),

                        new ParameterArrayValueParser<PackageItem, int>(
                            (int)PackageField.AmountInPackage,
                            true,
                            (model, value) => model.ArticleAmount = value, new IntParser())
                    ],
                    (mParent, mObject) =>
                    {
                        var pp = mParent.Packages.Find(x=> x.PackageBarcode == _currentPackage);
                        pp?.PackageItems.Add(mObject);
                    })
                }),
            new("[END]", true, null, null)
        };

        try
        {
            ParseFileWithSectionParsers(file, sectionParsers, result);
        }
        catch (Exception exception) when (exception is InvalidDataException or FormatException or OverflowException)
        {
            throw new ValidationException($"The article parameter file '{file.Name}' contains malformed data: {exception.Message}");
        }

        foreach (var article in result.Articles)
        {
            validatorArticle.ValidateAndThrow(article);
        }

        foreach (var market in result.Markets)
        {
            validatorMarket.ValidateAndThrow(market);
        }

        foreach (var package in result.Packages)
        {
            validatorPackage.ValidateAndThrow(package);
        }

        return result;
    }
}
