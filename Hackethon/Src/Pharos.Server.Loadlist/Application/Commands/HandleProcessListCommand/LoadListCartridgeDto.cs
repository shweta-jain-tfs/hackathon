namespace Pharos.Server.LoadList.Application.Commands.HandleProcessListCommand;

public class LoadListCartridgeDto() : LoadListItemBase(aggregateIntoArticle: true)
{
    public List<string?> Texts { get; set; } = default!;
}