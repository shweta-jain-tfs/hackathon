using System.Text.Json;

namespace Pharos.Client.Common.Models;

public partial class UsersClient
{
    partial void Initialize()
    {
        _instanceSettings = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
    }
}