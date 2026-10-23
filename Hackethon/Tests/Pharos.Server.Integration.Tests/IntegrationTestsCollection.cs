using Pharos.Server.Integration.Tests.Common;

namespace Pharos.Server.Integration.Tests;

[CollectionDefinition(nameof(IntegrationTestsCollection))]
public class IntegrationTestsCollection : ICollectionFixture<IntegrationTestsWaf>
{
}