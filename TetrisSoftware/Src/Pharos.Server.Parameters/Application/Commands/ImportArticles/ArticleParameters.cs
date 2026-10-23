using Pharos.Server.Domain.Parameters;

namespace Pharos.Server.Parameters.Application.Commands.ImportArticles;

internal class ArticleParameters
{
    public int Version;
    public List<Article> Articles = new();
    public List<IntendedMarket> Markets = new();
    public List<Package> Packages = new();
}