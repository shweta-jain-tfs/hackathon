using Pharos.Server.Integration.Tests.Common;

namespace Pharos.Server.Request;

public class RequestTestsWaf : DatabaseTestsWaf
{
    protected override string ConnectionStringName => "Request";
}
