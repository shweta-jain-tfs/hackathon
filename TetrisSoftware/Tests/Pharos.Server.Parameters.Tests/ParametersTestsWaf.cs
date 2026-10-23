using Pharos.Server.Integration.Tests.Common;

namespace Pharos.Server.Parameters;

public class ParametersTestsWaf : DatabaseTestsWaf
{
    protected override string ConnectionStringName => "Parameters";
}
