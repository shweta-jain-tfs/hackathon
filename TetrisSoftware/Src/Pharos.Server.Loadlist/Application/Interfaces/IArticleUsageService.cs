using Pharos.Server.Application.Parameters;
using Pharos.Server.LoadList.Domain;

namespace Pharos.Server.LoadList.Application.Interfaces;

public interface IArticleUsageService
{
    Task<ArticleUsage> GetInformation(string instrumentNumber, string articleName, ArticleType articleType, string? methodName, CancellationToken cancellationToken);
    Task<QualityControlTypeDto> GetQualityControlType(string qualityControlName, CancellationToken cancellationToken);
}