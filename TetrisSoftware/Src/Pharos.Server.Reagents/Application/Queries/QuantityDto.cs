using Pharos.Server.Domain.Parameters;

namespace Pharos.Server.Reagents.Application.Queries;
public class QuantityDto
{
    public int Value { get; set; }
    public ArticleQuantityUnit Unit { get; set; }
}
