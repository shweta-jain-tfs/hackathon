using Pharos.Server.Domain.Parameters;

namespace Pharos.Server.Parameters.Application.Parsers.ValueTypeParsers;

internal class ArticleParser(List<Article> articles) : Parser<Article>
{
    public override ParserResult<Article> GetValue(string value)
    {
        value = value.Trim().ToUpper();

        var article = articles.FirstOrDefault(x => x.ArticleNumber == value);
        if (article == null)
        {
            return new ParserResult<Article>();
        }

        return new ParserResult<Article>(article);
    }
}