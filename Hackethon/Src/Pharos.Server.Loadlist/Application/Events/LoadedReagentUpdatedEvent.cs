using System.Drawing;
using Microsoft.EntityFrameworkCore;
using Pharos.Server.Application.Parameters;
using Pharos.Server.LoadList.Domain;

namespace Pharos.Server.LoadList.Application.Events;

internal record LoadedReagentUpdatedEvent(string InstrumentNumber, List<LastLoadedReagent> ChangedOrUpdatedItems) : INotification;

internal class LoadedReagentUpdatedEventHandler : INotificationHandler<LoadedReagentUpdatedEvent>
{
    private readonly ILoadListDbContext _dbContext;
    private readonly ILoadListKeyService _keyService;
    private readonly IArticlesService _articlesService;
    private readonly IPublisher _publisher;

    public LoadedReagentUpdatedEventHandler(ILoadListDbContext dbContext, ILoadListKeyService keyService, IArticlesService articlesService, IPublisher publisher)
    {
        _dbContext = dbContext;
        _keyService = keyService;
        _articlesService = articlesService;
        _publisher = publisher;
    }

    public async Task Handle(LoadedReagentUpdatedEvent notification, CancellationToken cancellationToken)
    {
        var loadListConfigurations = await _dbContext.LoadListConfigurations.Where(x => x.InstrumentSerial == notification.InstrumentNumber).ToListAsync(cancellationToken);

        var updatedConfigurationIds = new List<int>();

        foreach (var loadListConfiguration in loadListConfigurations)
        {
            var key = _keyService.GetKey(loadListConfiguration);
            var changedOrUpdatedItem = notification.ChangedOrUpdatedItems.FirstOrDefault(x => x.ReagentKey == key);
            if (changedOrUpdatedItem == null)
            {
                continue;
            }

            if (loadListConfiguration.ArticleNumber == changedOrUpdatedItem.ArticleNumber)
            {
                continue;
            }

            updatedConfigurationIds.Add(loadListConfiguration.Id);

            var article = await _articlesService.GetArticle(changedOrUpdatedItem.ArticleNumber);
            if (article != null)
            {
                SetValues(loadListConfiguration, article);
            }
        }

        if (_dbContext.HasChanges())
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _publisher.Publish(new LoadListConfigurationUpdatedEvent(updatedConfigurationIds), cancellationToken);
        }
    }

    private static void SetValues(LoadListConfiguration config, Article article)
    {
        if (article.Color1 != null)
        {
            config.Color1 = ColorTranslator.ToHtml(article.Color1.Value);
        }

        if (article.Color2 != null)
        {
            config.Color2 = ColorTranslator.ToHtml(article.Color2.Value);
        }

        if (article.Color3 != null)
        {
            config.Color3 = ColorTranslator.ToHtml(article.Color3.Value);
        }

        if (article.Color4 != null)
        {
            config.Color4 = ColorTranslator.ToHtml(article.Color4.Value);
        }

        config.ArticleNumber = article.ArticleNumber;
        config.ArticleType = article.ArticleType;
        config.Text1 = article.Text1;
        config.Text2 = article.Text2;
        config.Text3 = article.Text3;
        config.Text4 = article.Text4;
        config.Shape = article.Shape;
        config.Quantity = article.Quantity;
        config.FriendlyName = article.FriendlyName ?? config.Identity;
        config.FullName = article.FullName;
        config.Unit = article.UnitOfMeasure;
    }
}