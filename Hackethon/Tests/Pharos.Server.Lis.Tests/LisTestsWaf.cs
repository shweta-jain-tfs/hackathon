using Pharos.Server.Integration.Tests.Common;

namespace Pharos.Server.Lis.Tests;

public class LisTestsWaf : DatabaseTestsWaf
{
    protected override string ConnectionStringName => "Lis";
}
