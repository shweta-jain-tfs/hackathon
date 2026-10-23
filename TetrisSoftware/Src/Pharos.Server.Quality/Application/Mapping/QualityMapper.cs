using System.Drawing;
using Pharos.Server.Application.Quality;
using Pharos.Server.Domain.Parameters;
using Riok.Mapperly.Abstractions;

namespace Pharos.Server.Quality.Application.Mapping;

[Mapper]
internal static partial class QualityMapper
{
    [MapperIgnoreTarget(nameof(QcCartridgeInformationDto.ArticleId))]
    [MapperIgnoreTarget(nameof(QcCartridgeInformationDto.LotNumber))]
    [MapperIgnoreTarget(nameof(QcCartridgeInformationDto.DisplayLotNumber))]
    [MapperIgnoreTarget(nameof(QcCartridgeInformationDto.Version))]
    [MapperIgnoreTarget(nameof(QcCartridgeInformationDto.Tests))]
    [MapperIgnoreTarget(nameof(QcCartridgeInformationDto.MethodId))]
    [MapperIgnoreTarget(nameof(QcCartridgeInformationDto.IsNewlyRegistered))]
    public static partial QcCartridgeInformationDto ToQcCartridgeInformationDto(this Article source);

    private static string? ConvertColorToString(Color? source)
    {
        if (source == null)
        {
            return null;
        }
        return ColorTranslator.ToHtml(source.Value);
    }
}
