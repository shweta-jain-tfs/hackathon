using Pharos.Server.Integration.Tests.Common;

namespace Pharos.Server.Reagents;
public class ReagentTestsWaf : DatabaseTestsWaf
{
    protected override string ConnectionStringName => "Reagent";
}
