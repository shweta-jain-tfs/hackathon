using Pharos.Server.LoadList.Domain;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos.Consumables;

namespace Pharos.Server.LoadList.Application.Interfaces;

internal interface ILoadListKeyService
{
    string GetKey(MissingConsumable item);
    string GetKey(LoadListConfiguration item);
    string GetKey(ArticleType articleType, string? articleIdentity, string? methodName);
    Task<string> GetKey(EnumReagentType reagentType, string reagentBarcode, string reagentUdiBarcode);
}