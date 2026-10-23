using Pharos.Server.Integration.Tests.Common;

namespace Pharos.Server.Sample.Tests;

public class SampleTestsWaf : DatabaseTestsWaf
{
    protected override string ConnectionStringName => "Sample";
}
