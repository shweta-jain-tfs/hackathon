using Tetris.Protocol.Pharos.Storage;

namespace Pharos.Server.Application.LoadList;

public interface IStorageToLoadListKeyService
{
    string GetKey(CarrierInformation item);
    Task<string> GetKey(Cartridge item);
    Task<string> GetKey(BottledReagent item);
}