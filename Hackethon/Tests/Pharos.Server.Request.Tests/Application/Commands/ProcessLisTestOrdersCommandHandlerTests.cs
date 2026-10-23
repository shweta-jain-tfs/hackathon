using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Lis;
using Pharos.Server.Application.Result;
using Pharos.Server.Application.Security;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Domain.Reflex;
using Pharos.Server.Infrastructure;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Parameters.Application.Services;
using Pharos.Server.Request.Application.Events;
using Pharos.Server.Result.Domain;
using Quartz;
using TestOrder = Pharos.Server.Request.Domain.TestOrder;

namespace Pharos.Server.Request.Application.Commands;

/// <summary>
/// Integration tests for ProcessLisTestOrdersCommandHandlerTests.
/// Uses actual database with DatabaseTestBase pattern.
/// </summary>
[Collection(nameof(RequestTestsCollection))]
public sealed class ProcessLisTestOrdersCommandHandlerTests(RequestTestsWaf factory)
    : DatabaseTestBase<RequestTestsWaf>(factory)
{
    private IRequestDbContext _requestDbContext = null!;
    private IParametersDbContext _parametersDbContext = null!;
    private TestData _testData = null!;

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        _testData = CreateTestData();
        _requestDbContext = ServiceProvider.GetRequiredService<IRequestDbContext>();
        _parametersDbContext = ServiceProvider.GetRequiredService<IParametersDbContext>();

        // Seed the full dataset once for all tests
        await SeedParametersDatabase();
    }

    #region Tests - Panel Expansion Enabled

    [Fact]
    [Trait("ProductRequirement", "PR_1822")]
    [Trait("ModuleRequirement", "MR_5610")]
    [Trait("ModuleRequirement", "MR_5871")]
    public async Task HandleImportedTestOrder_OnePanelNameWithoutMethodName_PanelExpansionEnabled_ShouldExpandPanel()
    {
        // Arrange
        const string sampleId = "SAMPLE123";
        const string panelName = "Anca";

        var handler = CreateHandler();
        var notification = CreateNotification(sampleId, CreateTestOrder(testName: panelName, methodName: string.Empty, panelExpansionEnabled: true));

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert - Anca panel should expand to: mp/El-G, pr/El-G
        await VerifyRequestCreatedWithTestCount(sampleId, 2);
        await VerifyTestOrdersCreated(sampleId, ("mp", "El-G"), ("pr", "El-G"));
        var panelId = await GetPanelIdByName(panelName);
        await VerifyPanelAssignedForTests(sampleId, panelId, ("mp", "El-G"), ("pr", "El-G"));
        await VerifyTestOrderSettings(sampleId,
            ("mp", "El-G", 2, 5),
            ("pr", "El-G", 3, 10));
    }

    [Fact]
    public async Task HandleImportedTestOrder_PanelMemberWithoutLaboratoryName_PanelExpansionEnabled_ShouldSkipTestOrder()
    {
        // Arrange
        const string sampleId = "SAMPLE_INTERNAL_PANEL";
        const string panelName = "IntPanel";

        var handler = CreateHandler();
        var notification = CreateNotification(sampleId, CreateTestOrder(testName: panelName, methodName: string.Empty, panelExpansionEnabled: true));

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        await VerifyRequestCreatedWithTestCount(sampleId, 0);
    }

    [Fact]
    public async Task HandleImportedTestOrder_PanelMemberLaboratoryNameDiffersFromInternalName_ShouldExpandUsingLaboratoryName()
    {
        // Arrange
        const string sampleId = "SAMPLE_LAB_PANEL";
        const string panelName = "LabNamePanel";

        var handler = CreateHandler();
        var notification = CreateNotification(sampleId, CreateTestOrder(testName: panelName, methodName: string.Empty, panelExpansionEnabled: true));

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        await VerifyRequestCreatedWithTestCount(sampleId, 1);
        var testOrder = await _requestDbContext.TestOrders
            .AsNoTracking()
            .SingleAsync(t => t.Request.SampleId == sampleId, TestContext.Current.CancellationToken);
        testOrder.LabTestName.Should().Be("Mtw-1R");
        testOrder.TestName.Should().Be("Mtw1R");
        testOrder.MethodName.Should().Be("sIgE");
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5972")]
    public async Task HandleImportedTestOrder_DisabledPanel_PanelExpansionEnabled_ShouldNotExpandPanel()
    {
        // Arrange
        const string sampleId = "SAMPLE_DISABLED_EXP";
        await DisablePanel("Anca");

        var handler = CreateHandler();
        var notification = CreateNotification(sampleId,
            CreateTestOrder(testName: "Anca", methodName: string.Empty, panelExpansionEnabled: true),
            CreateTestOrder(testName: "IgE", methodName: "Im-F", panelExpansionEnabled: true));

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert - The valid individual test should be imported, but the disabled panel should not be expanded.
        await VerifyRequestCreatedWithTestCount(sampleId, 1);
        await VerifyTestOrdersCreated(sampleId, ("IgE", "Im-F"));
    }

    [Fact]
    [Trait("ProductRequirement", "PR_1822")]
    [Trait("ModuleRequirement", "MR_5610")]
    public async Task HandleImportedTestOrder_OnePanelNameWithMethodName_PanelExpansionEnabled_ShouldNotExpandPanelAndHandleAsIndividualTest()
    {
        // Arrange
        const string sampleId = "SAMPLE456";

        var handler = CreateHandler();
        var notification = CreateNotification(sampleId,
            CreateTestOrder(testName: "Anca", methodName: "El-G", panelExpansionEnabled: true));// Panel name with specific test

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert - Should be treated as individual test, not expanded 
        var resultTestOrder = await _requestDbContext.TestOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Request.SampleId == sampleId && t.TestName == "Anca" && t.MethodName == "El-G", TestContext.Current.CancellationToken);
        Assert.Null(resultTestOrder);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_1822")]
    [Trait("ModuleRequirement", "MR_5610")]
    public async Task HandleImportedTestOrder_MultiplePanelNamesWithoutMethodName_PanelExpansionEnabled_ShouldExpandAllPanels()
    {
        // Arrange
        const string sampleId = "SAMPLE789";

        var handler = CreateHandler();
        var notification = CreateNotification(sampleId,
            CreateTestOrder(testName: "Anca", methodName: string.Empty, panelExpansionEnabled: true),
            CreateTestOrder(testName: "Pcd", methodName: string.Empty, panelExpansionEnabled: true));

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert - Anca (2 tests) + Pcd (4 tests) = 6 total
        await VerifyRequestCreatedWithTestCount(sampleId, 6);
        var panelIdAnca = await GetPanelIdByName("Anca");
        var panelIdPcd = await GetPanelIdByName("Pcd");
        await VerifyTestOrdersCreated(sampleId,
             ("mp", "El-G"), ("pr", "El-G"),
             ("Acy", "El-A"), ("Agp", "El-A"), ("Gcy", "El-G"), ("Ggp", "El-G"));
        await VerifyPanelAssignedForTests(sampleId, panelIdAnca, ("mp", "El-G"), ("pr", "El-G"));
        await VerifyPanelAssignedForTests(sampleId, panelIdPcd, ("Acy", "El-A"), ("Agp", "El-A"), ("Gcy", "El-G"), ("Ggp", "El-G"));
    }

    [Fact]
    [Trait("ProductRequirement", "PR_1822")]
    [Trait("ModuleRequirement", "MR_5610")]
    public async Task HandleImportedTestOrder_PanelWithoutMethodNameAndIndividualTestWithMethodNameMixed_PanelExpansionEnabled_ShouldExpandPanelAndAddIndividualTests()
    {
        // Arrange
        const string sampleId = "SAMPLE999";

        var handler = CreateHandler();
        var notification = CreateNotification(sampleId,
            CreateTestOrder(testName: "Anca", methodName: string.Empty, panelExpansionEnabled: true),
            CreateTestOrder(testName: "IgE", methodName: "Im-F", panelExpansionEnabled: true));

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert - Anca (2 tests) + IgE (1 test) = 3 total
        await VerifyRequestCreatedWithTestCount(expectedSampleId: sampleId, expectedTestCount: 3);
        await VerifyTestOrdersCreated(sampleId,
            ("mp", "El-G"), ("pr", "El-G"), ("IgE", "Im-F"));
        var panelId = await GetPanelIdByName("Anca");
        await VerifyPanelAssignedForTests(sampleId, panelId, ("mp", "El-G"), ("pr", "El-G"));

    }

    #endregion

    #region Tests - Panel Recognition Not Used

    [Fact]
    [Trait("ProductRequirement", "PR_1797")]
    [Trait("ModuleRequirement", "MR_5611")]
    public async Task HandleImportedTestOrder_AutomaticPanelRecognitionNotUsed_ShouldNotPerformPanelRecognition()
    {
        // Arrange
        const string sampleId = "SAMPLE001";

        var handler = CreateHandler();

        // Send all tests from Anca panel (mp/El-G, pr/El-G) but with AutoPanelRecognition disabled
        var notification = CreateNotification(sampleId,
            CreateTestOrderWithPanelRecognition("mp", "El-G", false, false, false),
            CreateTestOrderWithPanelRecognition("pr", "El-G", false, false, false));

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert - All tests should exist but without panel assignment
        await VerifyRequestCreatedWithTestCount(sampleId, 2);
        await VerifyTestOrdersCreated(sampleId, ("mp", "El-G"), ("pr", "El-G"));
        await VerifyNoPanelAssignedForTests(sampleId, ("mp", "El-G"), ("pr", "El-G"));
    }

    [Fact]
    public async Task HandleImportedTestOrder_DisabledPanel_AutomaticPanelRecognitionEnabled_ShouldAssignPanel()
    {
        // Arrange
        const string sampleId = "SAMPLE_DISABLED_RECOG";
        await DisablePanel("Anca");

        var handler = CreateHandler();
        var notification = CreateNotification(sampleId,
            CreateTestOrderWithPanelRecognition("mp", "El-G", false, true, false),
            CreateTestOrderWithPanelRecognition("pr", "El-G", false, true, false));

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert - Panel recognition considers matching panels regardless of their enabled state.
        await VerifyRequestCreatedWithTestCount(sampleId, 2);
        await VerifyTestOrdersCreated(sampleId, ("mp", "El-G"), ("pr", "El-G"));
        var panelId = await GetPanelIdByName("Anca");
        await VerifyPanelAssignedForTests(sampleId, panelId, ("mp", "El-G"), ("pr", "El-G"));
    }

    #endregion

    #region Tests - Panel Recognition Only From One Message

    [Fact]
    [Trait("ProductRequirement", "PR_1797")]
    [Trait("ModuleRequirement", "MR_5611")]
    public async Task HandleImportedTestOrder_OnlyFromOneMessageMode_ShouldNotAssignPanelWhenPartialPanelTestsReceived()
    {
        // Arrange
        const string sampleId = "SAMPLE_003";

        var handler = CreateHandler();

        // Anca requires: mp/El-G, pr/El-G (2 tests)
        // eczem requires: d1/slgE, e1/slgE, e5/slgE, f1/slgE, f13/slgE, f2/slgE (6 tests)
        // Send complete Anca panel and partial eczem panel in one message
        var notification = CreateNotification(sampleId,
            CreateTestOrderWithPanelRecognition("pr", "El-G", false, true, false), // Anca partial (only 1 of 2)
            CreateTestOrderWithPanelRecognition("d1", "slgE", false, true, false), // eczem partial
            CreateTestOrderWithPanelRecognition("e1", "slgE", false, true, false)); // eczem partial (only 2 of 6)

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert - Anca incomplete, eczem incomplete
        await VerifyRequestCreatedWithTestCount(sampleId, 3);
        await VerifyTestOrdersCreated(sampleId,
            ("pr", "El-G"), ("d1", "slgE"), ("e1", "slgE"));

        await VerifyNoPanelAssignedForTests(sampleId, ("pr", "El-G"), ("d1", "slgE"), ("e1", "slgE"));
    }

    [Fact]
    [Trait("ProductRequirement", "PR_1797")]
    [Trait("ModuleRequirement", "MR_5611")]
    public async Task HandleImportedTestOrder_OnlyFromOneMessageMode_ShouldNotUsePreviouslyStoredUnassignedTests()
    {
        // Arrange
        const string sampleId = "SAMPLE004";

        var handler = CreateHandler();

        // Anca requires: mp/El-G, pr/El-G (2 tests)
        // eczem requires: d1/slgE, e1/slgE, e5/slgE, f1/slgE, f13/slgE, f2/slgE (6 tests)

        // First message: Send complete Anca and partial eczem
        var notification1 = CreateNotification(sampleId,
            CreateTestOrderWithPanelRecognition("mp", "El-G", false, true, false), // Anca complete
            CreateTestOrderWithPanelRecognition("pr", "El-G", false, true, false), // Anca complete
            CreateTestOrderWithPanelRecognition("e5", "slgE", false, true, false), // eczem partial
            CreateTestOrderWithPanelRecognition("f1", "slgE", false, true, false), // eczem partial
            CreateTestOrderWithPanelRecognition("f13", "slgE", false, true, false), // eczem partial
            CreateTestOrderWithPanelRecognition("f2", "slgE", false, true, false)); // eczem partial

        // Act - First message
        await handler.Handle(notification1, TestContext.Current.CancellationToken);

        // Assert after first message
        await VerifyRequestCreatedWithTestCount(sampleId, 6);
        // verify panel assigned for Anca
        var ancaPanelId = await GetPanelIdByName("Anca");
        await VerifyPanelAssignedForTests(sampleId, ancaPanelId, ("mp", "El-G"), ("pr", "El-G"));
        await VerifyNoPanelAssignedForTests(sampleId, ("e5", "slgE"), ("f1", "slgE"), ("f13", "slgE"), ("f2", "slgE"));

        // Arrange - Second message: Send remaining eczem test
        var notification2 = CreateNotification(sampleId,
            CreateTestOrderWithPanelRecognition("d1", "slgE", false, true, false), // eczem remaining test
            CreateTestOrderWithPanelRecognition("e1", "slgE", false, true, false)); // eczem remaining test

        // Act - Second message (reuse same handler)
        await handler.Handle(notification2, TestContext.Current.CancellationToken);

        // Assert after second message - eczem should NOT be recognized (only from one message mode)
        await VerifyRequestCreatedWithTestCount(sampleId, 8);
        await VerifyNoPanelAssignedForTests(sampleId, ("d1", "slgE"), ("e1", "slgE"), ("e5", "slgE"), ("f1", "slgE"), ("f13", "slgE"), ("f2", "slgE")); // eczem still not assigned
    }

    [Fact]
    [Trait("ProductRequirement", "PR_1797")]
    [Trait("ModuleRequirement", "MR_5611")]
    public async Task HandleImportedTestOrder_OnlyFromOneMessageMode_ShouldAssignPanelToTestWithReflexDefinition()
    {
        // Arrange
        const string sampleId = "SAMPLE_REFLEX_PANEL_ONE_MESSAGE";
        const string reflexDefinitionName = "ReflexPanelOneMessage";
        await SeedReflexDefinition(reflexDefinitionName);

        var notification = CreateNotification(sampleId,
            CreateTestOrderWithReflex(
                testName: "fx1",
                reflexName: reflexDefinitionName,
                suppressReflex: false,
                autoPanelRecognitionFromOneMessage: true),
            CreateTestOrderWithPanelRecognition("f4", "slgE", false, true, false));

        // Act
        await CreateHandler().Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        var panelId = await GetPanelIdByName("ReflexPanel");
        await VerifyPanelAssignedForTests(sampleId, panelId, ("fx1", "slgE"), ("f4", "slgE"));
        await VerifyReflexDefinitionPreserved(sampleId, reflexDefinitionName);
    }

    #endregion

    #region Tests - Panel Recognition From Multiple Messages

    [Fact]
    [Trait("ProductRequirement", "PR_1797")]
    [Trait("ModuleRequirement", "MR_5611")]
    public async Task HandleImportedTestOrder_FromMultipleMessagesMode_ShouldCombineUnassignedTestsAcrossMessages()
    {
        // Arrange
        const string sampleId = "SAMPLE_005";

        // Anca requires: mp/El-G, pr/El-G (2 tests)
        // eczem requires: d1/slgE, e1/slgE, e5/slgE, f1/slgE, f13/slgE, f2/slgE (6 tests)

        // First message: Send complete Anca and partial eczem
        var notification1 = CreateNotification(sampleId,
            CreateTestOrderWithPanelRecognition("mp", "El-G", false, false, true), // Anca complete
            CreateTestOrderWithPanelRecognition("pr", "El-G", false, false, true), // Anca complete
            CreateTestOrderWithPanelRecognition("e5", "slgE", false, false, true), // eczem partial
            CreateTestOrderWithPanelRecognition("f1", "slgE", false, false, true), // eczem partial
            CreateTestOrderWithPanelRecognition("f13", "slgE", false, false, true), // eczem partial
            CreateTestOrderWithPanelRecognition("f2", "slgE", false, false, true)); // eczem partial

        // Act - First message
        var handle = CreateHandler();
        await handle.Handle(notification1, TestContext.Current.CancellationToken);

        // Assert after first message
        await VerifyRequestCreatedWithTestCount(sampleId, 6);
        // verify panel assigned for Anca
        var ancaPanelId = await GetPanelIdByName("Anca");
        await VerifyPanelAssignedForTests(sampleId, ancaPanelId, ("mp", "El-G"), ("pr", "El-G"));
        await VerifyNoPanelAssignedForTests(sampleId, ("e5", "slgE"), ("f1", "slgE"), ("f13", "slgE"), ("f2", "slgE"));

        // Arrange - Second message: Send remaining eczem tests
        var notification2 = CreateNotification(sampleId,
            CreateTestOrderWithPanelRecognition("d1", "slgE", false, false, true), // eczem remaining test
            CreateTestOrderWithPanelRecognition("e1", "slgE", false, false, true)); // eczem remaining test

        // Act - Second message (new handler for second message)
        await handle.Handle(notification2, TestContext.Current.CancellationToken);

        // Assert after second message - eczem should be recognized (from Multiple Messages mode)
        await VerifyRequestCreatedWithTestCount(sampleId, 8);

        // Then verify panel assignment
        var eczemPanelId = await GetPanelIdByName("eczem");
        await VerifyPanelAssignedForTests(sampleId, ancaPanelId, ("mp", "El-G"), ("pr", "El-G")); // Anca remains assigned
        await VerifyPanelAssignedForTests(sampleId, eczemPanelId, ("d1", "slgE"), ("e1", "slgE"), ("e5", "slgE"), ("f1", "slgE"), ("f13", "slgE"), ("f2", "slgE")); // eczem now assigned
    }

    [Fact]
    [Trait("ProductRequirement", "PR_1798")]
    [Trait("ModuleRequirement", "MR_5867")]
    public async Task HandleImportedTestOrder_FromMultipleMessagesMode_ShouldAssignPanelToExistingTestWithReflexDefinition()
    {
        // Arrange
        const string sampleId = "SAMPLE_REFLEX_PANEL_MULTI";
        const string reflexDefinitionName = "ReflexPanelMultipleMessages";
        await SeedReflexDefinition(reflexDefinitionName);

        var triggerNotification = CreateNotification(sampleId,
            CreateTestOrderWithReflex(
                testName: "fx1",
                reflexName: reflexDefinitionName,
                suppressReflex: false,
                autoPanelRecognitionFromMultipleMessage: true));

        // Act - import the reflex trigger before the other panel member
        await CreateHandler().Handle(triggerNotification, TestContext.Current.CancellationToken);

        // Assert - the incomplete panel is not recognized and the reflex definition is retained
        await VerifyNoPanelAssignedForTests(sampleId, ("fx1", "slgE"));
        await VerifyReflexDefinitionPreserved(sampleId, reflexDefinitionName);

        // Act - import the remaining panel member in a later message
        var remainingPanelNotification = CreateNotification(sampleId,
            CreateTestOrderWithPanelRecognition("f4", "slgE", false, false, true));
        await CreateHandler().Handle(remainingPanelNotification, TestContext.Current.CancellationToken);

        // Assert
        var panelId = await GetPanelIdByName("ReflexPanel");
        await VerifyPanelAssignedForTests(sampleId, panelId, ("fx1", "slgE"), ("f4", "slgE"));
        await VerifyReflexDefinitionPreserved(sampleId, reflexDefinitionName);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_1797")]
    [Trait("ModuleRequirement", "MR_5611")]
    public async Task HandleImportedTestOrder_FromMultipleMessagesMode_ShouldOnlyConsiderNotStartedAndScheduledTestsForPanelRecognition()
    {
        // Arrange
        const string sampleId = "SAMPLE_MULTI_STATE";

        // ("paps1", ("Gb2", "El-G"), ("Gcl", "El-G"))
        // ("panas", ("ce", "El-G"), ("dn", "El-G"), ("jo", "El-G"))

        var existingRequest = new Domain.Request
        {
            SampleId = sampleId,
            Source = RequestSource.Lis,
            TestOrders = new List<TestOrder>
            {
                new()
                {
                    Uid = Guid.NewGuid(),
                    TestName = "Gb2",
                    MethodName = "El-G",
                    State = TestOrderState.Started,
                    TestPanelIds = []
                },
                new()
                {
                    Uid = Guid.NewGuid(),
                    TestName = "ce",
                    MethodName = "El-G",
                    State = TestOrderState.Scheduled,
                    TestPanelIds = []
                }
            }
        };

        _requestDbContext.Requests.Add(existingRequest);
        await _requestDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler();

        var notification = CreateNotification(sampleId,
            CreateTestOrderWithPanelRecognition("Gcl", "El-G", true, false, true),
            CreateTestOrderWithPanelRecognition("dn", "El-G", true, false, true),
            CreateTestOrderWithPanelRecognition("jo", "El-G", true, false, true));

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // verify
        var panasPanelId = await GetPanelIdByName("panas");
        await VerifyPanelAssignedForTests(sampleId, null, ("Gb2", "El-G"), ("Gcl", "El-G"));
        await VerifyPanelAssignedForTests(sampleId, panasPanelId, ("ce", "El-G"), ("dn", "El-G"), ("jo", "El-G"));
    }

    #endregion

    #region Tests - Panel Recognition Tie-Breaking Rules

    [Fact]
    [Trait("ProductRequirement", "PR_1797")]
    [Trait("ModuleRequirement", "MR_5611")]
    public async Task HandleImportedTestOrder_PanelRecognition_ShouldSelectPanelWithHighestNumberOfMatchingTests()
    {
        // Arrange
        const string sampleId = "SAMPLE_006";

        var handler = CreateHandler();

        // rhinc (9 tests): d1/slgE, e1/slgE, e5/slgE, f1/slgE, f13/slgE, f2/slgE, g6/slgE, t3/slgE, t9/slgE
        // wheez (10 tests): d1/slgE, e1/slgE, e5/slgE, f1/slgE, f13/slgE, f2/slgE, g6/slgE, t3/slgE, t9/slgE, w21/slgE
        // eczem (6 tests): d1/slgE, e1/slgE, e5/slgE, f1/slgE, f13/slgE, f2/slgE

        // Send all eczem tests - both eczem (6 tests) and rhinc (9 tests) and wheez (10 tests)
        // The larger panel (wheez with 10 tests) should be selected
        var notification = CreateNotification(sampleId,
            CreateTestOrderWithPanelRecognition("d1", "slgE", false, true, false),
            CreateTestOrderWithPanelRecognition("e1", "slgE", false, true, false),
            CreateTestOrderWithPanelRecognition("e5", "slgE", false, true, false),
            CreateTestOrderWithPanelRecognition("f1", "slgE", false, true, false),
            CreateTestOrderWithPanelRecognition("f13", "slgE", false, true, false),
            CreateTestOrderWithPanelRecognition("f2", "slgE", false, true, false),
            CreateTestOrderWithPanelRecognition("g6", "slgE", false, true, false),
            CreateTestOrderWithPanelRecognition("t3", "slgE", false, true, false),
            CreateTestOrderWithPanelRecognition("t9", "slgE", false, true, false),
            CreateTestOrderWithPanelRecognition("w21", "slgE", false, true, false));

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert - wheez should be selected (highest count = 10 tests)
        await VerifyRequestCreatedWithTestCount(sampleId, 10);
        var wheezPanelId = await GetPanelIdByName("wheez");
        await VerifyPanelAssignedForTests(sampleId, wheezPanelId,
            ("d1", "slgE"), ("e1", "slgE"), ("e5", "slgE"), ("f1", "slgE"), ("f13", "slgE"),
            ("f2", "slgE"), ("g6", "slgE"), ("t3", "slgE"), ("t9", "slgE"), ("w21", "slgE"));
    }

    [Fact]
    [Trait("ProductRequirement", "PR_1797")]
    [Trait("ModuleRequirement", "MR_5611")]
    public async Task HandleImportedTestOrder_PanelRecognition_ShouldSelectAlphabeticallyLowestPanelNameWhenTieOnMatchingTestCount()
    {
        // Arrange
        const string sampleId = "SAMPLE007";

        var handler = CreateHandler();

        // asthm (10 tests): d1/slgE, e1/slgE, e5/slgE, g6/slgE, i6/slgE, m2/slgE, t3/slgE, t9/slgE, w21/slgE, w6/slgE
        // rhina (10 tests): d1/slgE, e1/slgE, e5/slgE, g6/slgE, i6/slgE, m2/slgE, t3/slgE, t9/slgE, w21/slgE, w6/slgE
        // Both panels have exactly the same tests (10 tests each)
        // Alphabetically "asthm" comes before "rhina", so asthm should be selected

        var notification = CreateNotification(sampleId,
            CreateTestOrderWithPanelRecognition("d1", "slgE", false, true, false),
            CreateTestOrderWithPanelRecognition("e1", "slgE", false, true, false),
            CreateTestOrderWithPanelRecognition("e5", "slgE", false, true, false),
            CreateTestOrderWithPanelRecognition("g6", "slgE", false, true, false),
            CreateTestOrderWithPanelRecognition("i6", "slgE", false, true, false),
            CreateTestOrderWithPanelRecognition("m2", "slgE", false, true, false),
            CreateTestOrderWithPanelRecognition("t3", "slgE", false, true, false),
            CreateTestOrderWithPanelRecognition("t9", "slgE", false, true, false),
            CreateTestOrderWithPanelRecognition("w21", "slgE", false, true, false),
            CreateTestOrderWithPanelRecognition("w6", "slgE", false, true, false));

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert - asthm should be selected (alphabetically lowest wins in tie)
        await VerifyRequestCreatedWithTestCount(sampleId, 10);
        var asthmPanelId = await GetPanelIdByName("asthm");
        await VerifyPanelAssignedForTests(sampleId, asthmPanelId,
            ("d1", "slgE"), ("e1", "slgE"), ("e5", "slgE"), ("g6", "slgE"), ("i6", "slgE"),
            ("m2", "slgE"), ("t3", "slgE"), ("t9", "slgE"), ("w21", "slgE"), ("w6", "slgE"));
    }

    #endregion

    #region Tests - Patient Age Handling

    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5384")]
    public async Task HandleImportedSample_When_LisProvides_Age_Should_UseLisAge()
    {
        // Arrange
        const string sampleId = "SAMPLE_AGE1";
        var birthDate = new DateOnly(1990, 1, 15);
        var requestDate = new DateTimeOffset(2024, 1, 15, 0, 0, 0, TimeSpan.Zero);

        var handler = CreateHandlerWithPatientInfoEnabled();
        var notification = new ProcessLisTestOrdersCommand
        {
            LisChannelId = 1,
            Patients =
            [
                new LisPatient
                {
                    PracticePatientId = "PID001",
                    BirthDate = birthDate,
                    PatientAge = 35, // LIS provides age 35 (different from calculated 34)
                    Samples =
                    [
                        new LisSample
                        {
                            SampleId = sampleId,
                            RequestDate = requestDate,
                            TestOrders = [CreateTestOrder("IgE", "Im-F", false)]
                        }
                    ]
                }
            ]
        };

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        var request = await _requestDbContext.Requests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.SampleId == sampleId, TestContext.Current.CancellationToken);

        request.Should().NotBeNull();
        request.PatientAge.Should().Be(35, "because LIS-provided age should be used directly");
    }

    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5384")]
    public async Task HandleImportedSample_When_LisProvides_OnlyBirthDateAndSettingIsEnabled_Should_CalculateAge()
    {
        // Arrange
        const string sampleId = "SAMPLE_AGE2";
        var birthDate = new DateOnly(1990, 1, 15);
        var requestDate = new DateTimeOffset(2024, 1, 15, 0, 0, 0, TimeSpan.Zero);

        var handler = CreateHandlerWithPatientInfoEnabled();
        var notification = new ProcessLisTestOrdersCommand
        {
            LisChannelId = 1,
            Patients =
            [
                new LisPatient
                {
                    PracticePatientId = "PID002",
                    BirthDate = birthDate,
                    PatientAge = null,
                    CalculateAge = true,
                    Samples =
                    [
                        new LisSample
                        {
                            SampleId = sampleId,
                            RequestDate = requestDate,
                            TestOrders = [CreateTestOrder("IgE", "Im-F", false)]
                        }
                    ]
                }
            ]
        };

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        var request = await _requestDbContext.Requests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.SampleId == sampleId, TestContext.Current.CancellationToken);

        request.Should().NotBeNull();
        request.PatientAge.Should().Be(34, "because age should be calculated from birth date when LIS doesn't provide age");
    }


    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5384")]
    public async Task HandleImportedSample_When_LisProvides_OnlyBirthDateAndSettingIsDisabled_Should_NotCalculateAge()
    {
        // Arrange
        const string sampleId = "SAMPLE_AGE4";
        var birthDate = new DateOnly(1990, 1, 15);
        var requestDate = new DateTimeOffset(2024, 1, 15, 0, 0, 0, TimeSpan.Zero);

        var handler = CreateHandlerWithPatientInfoEnabled();
        var notification = new ProcessLisTestOrdersCommand
        {
            LisChannelId = 1,
            Patients =
            [
                new LisPatient
                {
                    PracticePatientId = "PID004",
                    BirthDate = birthDate,
                    PatientAge = null,
                    CalculateAge = false,
                    Samples =
                    [
                        new LisSample
                        {
                            SampleId = sampleId,
                            RequestDate = requestDate,
                            TestOrders = [CreateTestOrder("IgE", "Im-F", false)]
                        }
                    ]
                }
            ]
        };

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        var request = await _requestDbContext.Requests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.SampleId == sampleId, TestContext.Current.CancellationToken);

        request.Should().NotBeNull();
        request.PatientAge.Should().BeNull("because age calculation is disabled even though birth date is provided");
    }

    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5384")]
    public async Task HandleImportedSample_When_LisProvides_NoBirthDateNoAge_Should_SetAgeNull()
    {
        // Arrange
        const string sampleId = "SAMPLE_AGE3";
        var requestDate = new DateTimeOffset(2024, 1, 15, 0, 0, 0, TimeSpan.Zero);

        var handler = CreateHandlerWithPatientInfoEnabled();
        var notification = new ProcessLisTestOrdersCommand
        {
            LisChannelId = 1,
            Patients =
            [
                new LisPatient
                {
                    PracticePatientId = "PID003",
                    BirthDate = null,
                    PatientAge = null,
                    Samples =
                    [
                        new LisSample
                        {
                            SampleId = sampleId,
                            RequestDate = requestDate,
                            TestOrders = [CreateTestOrder("IgE", "Im-F", false)]
                        }
                    ]
                }
            ]
        };

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        var request = await _requestDbContext.Requests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.SampleId == sampleId, TestContext.Current.CancellationToken);

        request.Should().NotBeNull();
        request.PatientAge.Should().BeNull("because age cannot be calculated without birth date");
    }

    #endregion

    #region Tests - Instrument Dilution and Pre-Dilution Factors

    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5617")]
    public async Task HandleImportedTestOrder_ShouldAddDefaultInstrumentDilutionFactorAndReplicates()
    {
        const string sampleId = "SAMPLE_ImportSetting01";

        var handler = CreateHandler();
        var notification = CreateNotification(sampleId,
            new LisTestOrder
            {
                TestName = "IgE",
                MethodName = "Im-F",
                PanelExpansionEnabled = false,
                Action = LisTestOrderAction.NewTest,
                NumberOfReplicates = null,
                InstrumentDilutionFactor = null,
            });

        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert - Should use default values from methodTest
        await VerifyRequestCreatedWithTestCount(sampleId, 1);
        var testOrder = await _requestDbContext.TestOrders
            .AsNoTracking()
            .FirstAsync(t => t.Request.SampleId == sampleId && t.TestName == "IgE" && t.MethodName == "Im-F", TestContext.Current.CancellationToken);

        testOrder.InstrumentDilutionFactor.Should().Be(1, "Should use default instrument dilution factor when LIS value is null");
        testOrder.NumberOfReplicates.Should().Be(1, "Should use default replicates when LIS value is null");
    }

    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5617")]
    public async Task HandleImportedTestOrder_ShouldUseProvidedInstrumentDilutionFactorAndReplicates()
    {
        const string sampleId = "SAMPLE_ImportSetting02";

        var handler = CreateHandler();
        var notification = CreateNotification(sampleId,
            new LisTestOrder
            {
                TestName = "IgE",
                MethodName = "Im-F",
                PanelExpansionEnabled = false,
                Action = LisTestOrderAction.NewTest,
                NumberOfReplicates = 3,
                InstrumentDilutionFactor = 5
            });

        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert - Should use provided values from LIS
        await VerifyRequestCreatedWithTestCount(sampleId, 1);
        var testOrder = await _requestDbContext.TestOrders
            .AsNoTracking()
            .FirstAsync(t => t.Request.SampleId == sampleId && t.TestName == "IgE" && t.MethodName == "Im-F", TestContext.Current.CancellationToken);

        testOrder.InstrumentDilutionFactor.Should().Be(5, "Should use provided instrument dilution factor when LIS value is specified");
        testOrder.NumberOfReplicates.Should().Be(3, "Should use provided replicates when LIS value is specified");
    }

    #endregion

    #region Lis Test Sort Order Handling

    [Fact]
    [Trait("ProductRequirement", "PR_575")]
    [Trait("ModuleRequirement", "MR_5613")]
    public async Task HandleImportedTestOrder_SortOrderAssignedForTestBasedOnRequestPriorityOrderUseA()
    {
        // Arrange
        const string sampleId = "SAMPLE1";

        var existingRequest = new Domain.Request
        {
            SampleId = sampleId,
            Source = RequestSource.Lis,
            TestOrders = new List<TestOrder>
            {
                new()
                {
                    TestName = "mps",
                    MethodName = "El-G",
                    TestSortOrder = 1
                }
            }
        };

        _requestDbContext.Requests.Add(existingRequest);
        await _requestDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler();

        // Input LIS message:
        var notification = CreateNotification(sampleId,
            CreateTestOrderWithPriorityA("pr", "El-G", useRequestPriorityA: true, panelExpansionEnabled: true, forceUniqueLabTestName: true),
            CreateTestOrderWithPriorityA("mp", "El-G", useRequestPriorityA: true, panelExpansionEnabled: true, forceUniqueLabTestName: true),
            CreateTestOrderWithPriorityA("prs", "El-G", useRequestPriorityA: true, panelExpansionEnabled: true, forceUniqueLabTestName: true),
            CreateTestOrderWithPriorityA("mps", "El-G", useRequestPriorityA: true, panelExpansionEnabled: true, forceUniqueLabTestName: true),
            CreateTestOrderWithPriorityA("e1", "slgE", useRequestPriorityA: true, panelExpansionEnabled: true, forceUniqueLabTestName: true),
            CreateTestOrderWithPriorityA("e5", "slgE", useRequestPriorityA: true, panelExpansionEnabled: true, forceUniqueLabTestName: true),
            CreateTestOrderWithPriorityA("wheez", "", useRequestPriorityA: true, panelExpansionEnabled: true, forceUniqueLabTestName: true),
            CreateTestOrderWithPriorityA("ce", "El-G", useRequestPriorityA: true, panelExpansionEnabled: true, forceUniqueLabTestName: true));

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        await VerifyRequestCreatedWithTestCount(sampleId, 15);

        // Verify sort orders match expected output
        await VerifyTestOrderSortOrder(sampleId, new[]
        {
            ("e1", "slgE", 1),
            ("prs", "El-G", 2),
            ("mps", "El-G", 3),
            ("pr", "El-G", 4),
            ("mp", "El-G", 5),
            ("e5", "slgE", 6),
            ("d1", "slgE", 7),
            ("f1", "slgE", 8),
            ("f13", "slgE", 9),
            ("f2", "slgE", 10),
            ("g6", "slgE", 11),
            ("t3", "slgE", 12),
            ("t9", "slgE", 13),
            ("w21", "slgE", 14),
            ("ce", "El-G", 15)
        });
    }

    [Fact]
    [Trait("ProductRequirement", "PR_575")]
    [Trait("ModuleRequirement", "MR_5613")]
    public async Task HandleImportedTestOrder_SortOrderAssignedForTestBasedOnRequestPriorityOrderNone()
    {
        // Arrange
        const string sampleId = "SAMPLE2";

        var existingRequest = new Domain.Request
        {
            SampleId = sampleId,
            Source = RequestSource.Lis,
            TestOrders = new List<TestOrder>
            {
                new()
                {
                    TestName = "mps",
                    MethodName = "El-G",
                    TestSortOrder = 1
                }
            }
        };

        _requestDbContext.Requests.Add(existingRequest);
        await _requestDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler();

        // Input LIS message:
        var notification = CreateNotification(sampleId,
            CreateTestOrderWithPriorityA("pr", "El-G", useRequestPriorityA: false, panelExpansionEnabled: true, forceUniqueLabTestName: true),
            CreateTestOrderWithPriorityA("mp", "El-G", useRequestPriorityA: false, panelExpansionEnabled: true, forceUniqueLabTestName: true),
            CreateTestOrderWithPriorityA("prs", "El-G", useRequestPriorityA: false, panelExpansionEnabled: true, forceUniqueLabTestName: true),
            CreateTestOrderWithPriorityA("mps", "El-G", useRequestPriorityA: false, panelExpansionEnabled: true, forceUniqueLabTestName: true),
            CreateTestOrderWithPriorityA("e1", "slgE", useRequestPriorityA: false, panelExpansionEnabled: true, forceUniqueLabTestName: true),
            CreateTestOrderWithPriorityA("e5", "slgE", useRequestPriorityA: false, panelExpansionEnabled: true, forceUniqueLabTestName: true),
            CreateTestOrderWithPriorityA("wheez", "", useRequestPriorityA: false, panelExpansionEnabled: true, forceUniqueLabTestName: true),
            CreateTestOrderWithPriorityA("ce", "El-G", useRequestPriorityA: false, panelExpansionEnabled: true, forceUniqueLabTestName: true));

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        await VerifyRequestCreatedWithTestCount(sampleId, 15);

        // Verify sort orders match expected output
        await VerifyTestOrderSortOrder(sampleId, new[]
        {
            ("pr", "El-G", 1),
            ("mp", "El-G", 2),
            ("prs", "El-G", 3),
            ("mps", "El-G", 4),
            ("e1", "slgE", 5),
            ("e5", "slgE", 6),
            ("d1", "slgE", 7),
            ("f1", "slgE", 8),
            ("f13", "slgE", 9),
            ("f2", "slgE", 10),
            ("g6", "slgE", 11),
            ("t3", "slgE", 12),
            ("t9", "slgE", 13),
            ("w21", "slgE", 14),
            ("ce", "El-G", 15)
        });
    }


    #endregion

    #region TODO: Tests - Unique Laboratory tests

    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5599")]
    public async Task HandleImportedTestOrder_SettingForceUniqueLabTestNameEnabled_LabTestNameRecievedFromLis_ShouldAddTest()
    {
        // Arrange
        const string inputSampleId = "SAMPLE_LAB01";
        const string inputLabTestName = "bhy";
        const string inputMethodName = "";

        var handler = CreateHandler();
        var lisTestOrder = CreateTestOrderWithForceUniqueLabTestName(true, inputLabTestName, inputMethodName);
        var notification = CreateNotification(inputSampleId, lisTestOrder);

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert - Test should be added with correct mapping
        await VerifyRequestCreatedWithTestCount(inputSampleId, 1);
        var testOrder = await _requestDbContext.TestOrders
            .AsNoTracking()
            .FirstAsync(t => t.Request.SampleId == inputSampleId, TestContext.Current.CancellationToken);
        testOrder.LabTestName.Should().Be(inputLabTestName);
        testOrder.TestName.Should().Be("c1");
        testOrder.MethodName.Should().Be("slgE");
    }

    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5599")]
    public async Task HandleImportedTestOrder_SettingForceUniqueLabTestNameDisabled_LabTestNameRecievedFromLisWithoutMethodName_ShouldSkipTestOrder()
    {
        // Arrange
        const string inputSampleId = "SAMPLE_LAB02";
        const string inputLabTestName = "bhy";
        const string inputMethodName = "";

        var handler = CreateHandler();
        var lisTestOrder = CreateTestOrderWithForceUniqueLabTestName(false, inputLabTestName, inputMethodName);
        var notification = CreateNotification(inputSampleId, lisTestOrder);

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert - Test should be added with correct mapping
        await VerifyRequestCreatedWithTestCount(inputSampleId, 0);
    }

    [Theory]
    [InlineData(false, "sIgE")]
    [InlineData(true, "")]
    public async Task HandleImportedTestOrder_InternalTestNameDoesNotMatchConfiguredLabName_ShouldSkipTestOrder(bool forceUniqueLabTestName, string methodName)
    {
        // Arrange
        const string sampleId = "SAMPLE_LAB_MISMATCH";
        const string internalTestName = "Mtw1R";

        var handler = CreateHandler();
        var lisTestOrder = CreateTestOrderWithForceUniqueLabTestName(forceUniqueLabTestName, internalTestName, methodName);
        var notification = CreateNotification(sampleId, lisTestOrder);

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        await VerifyRequestCreatedWithTestCount(sampleId, 0);
    }

    [Theory]
    [InlineData(false, "sIgE")]
    [InlineData(true, "")]
    public async Task HandleImportedTestOrder_ConfiguredLabNameMatches_ShouldAddMappedTest(bool forceUniqueLabTestName, string methodName)
    {
        // Arrange
        const string sampleId = "SAMPLE_LAB_MATCH";
        const string laboratoryTestName = "Mtw-1R";

        var handler = CreateHandler();
        var lisTestOrder = CreateTestOrderWithForceUniqueLabTestName(forceUniqueLabTestName, laboratoryTestName, methodName);
        var notification = CreateNotification(sampleId, lisTestOrder);

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        await VerifyRequestCreatedWithTestCount(sampleId, 1);
        var testOrder = await _requestDbContext.TestOrders
            .AsNoTracking()
            .SingleAsync(t => t.Request.SampleId == sampleId, TestContext.Current.CancellationToken);
        testOrder.LabTestName.Should().Be(laboratoryTestName);
        testOrder.TestName.Should().Be("Mtw1R");
        testOrder.MethodName.Should().Be("sIgE");
    }

    #endregion

    #region Tests - PreDilutionFactor Validation

    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5871")]
    public async Task HandleImportedSample_WhenNewRequest_ShouldAllowAnyPreDilutionFactor()
    {
        // Arrange
        const string sampleId = "SAMPLE_PreDilution_New";
        const int preDilutionFactor = 5;

        var handler = CreateHandler();
        var notification = new ProcessLisTestOrdersCommand
        {
            LisChannelId = 1,
            Patients = [new LisPatient
            {
                Samples = [new LisSample
                {
                    SampleId = sampleId,
                    TestOrders = [new LisTestOrder
                    {
                        TestName = "IgE",
                        MethodName = "Im-F",
                        PreDilutionFactor = preDilutionFactor,
                        PanelExpansionEnabled = false,
                        Action = LisTestOrderAction.NewTest
                    }]
                }]
            }]
        };

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert - New request should be created successfully with any PreDilutionFactor
        await VerifyRequestCreatedWithTestCount(sampleId, 1);

        var request = await _requestDbContext.Requests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.SampleId == sampleId, TestContext.Current.CancellationToken);

        request.Should().NotBeNull();
        request.PreDilutionFactor.Should().Be(preDilutionFactor);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5871")]
    public async Task HandleImportedSample_WhenExistingRequestWithDifferentPreDilutionFactor_ShouldSkipRequestUpdate()
    {
        // Arrange
        const string sampleId = "SAMPLE_PreDilution_Existing";
        const int originalPreDilutionFactor = 2;
        const int differentPreDilutionFactor = 5;

        // First, create an existing request
        var existingRequest = new Domain.Request
        {
            SampleId = sampleId,
            PreDilutionFactor = originalPreDilutionFactor,
            TestOrders = new List<TestOrder>()
        };

        _requestDbContext.Requests.Add(existingRequest);
        await _requestDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler();
        var notification = new ProcessLisTestOrdersCommand
        {
            LisChannelId = 1,
            Patients = [new LisPatient
            {
                Samples = [new LisSample
                {
                    SampleId = sampleId,
                    TestOrders = [new LisTestOrder
                    {
                        TestName = "IgE",
                        MethodName = "Im-F",
                        PreDilutionFactor = differentPreDilutionFactor,
                        PanelExpansionEnabled = false,
                        Action = LisTestOrderAction.NewTest
                    }]
                }]
            }]
        };

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert - Request should not be updated, PreDilutionFactor should remain original
        var request = await _requestDbContext.Requests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.SampleId == sampleId, TestContext.Current.CancellationToken);

        request.Should().NotBeNull();
        request.PreDilutionFactor.Should().Be(originalPreDilutionFactor, "because request update should be skipped when PreDilutionFactor differs");

        // Test orders should not be added due to validation failure
        var testOrders = await _requestDbContext.TestOrders
            .AsNoTracking()
            .Where(t => t.Request.SampleId == sampleId)
            .ToListAsync(TestContext.Current.CancellationToken);

        testOrders.Should().BeEmpty("because the entire request update should be skipped due to PreDilutionFactor validation failure");
    }

    [Fact]
    public async Task HandleImportedSample_WhenCaseInsensitiveMatchExists_ShouldPreserveOriginalSampleId()
    {
        const string originalSampleId = "SAMPLE_CASING";
        var existingRequest = new Domain.Request
        {
            SampleId = originalSampleId,
            TestOrders = []
        };
        _requestDbContext.Requests.Add(existingRequest);
        await _requestDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var optionsCache = ServiceProvider.GetRequiredService<IOptionsMonitorCache<SamplesSettings>>();
        optionsCache.TryRemove(Options.DefaultName);
        optionsCache.TryAdd(Options.DefaultName, new SamplesSettings { CaseInsensitiveSampleId = true });

        try
        {
            var result = await CreateHandler().Handle(
                CreateNotification("sample_casing", CreateTestOrder("IgE", "Im-F", false)),
                TestContext.Current.CancellationToken);

            result.Should().BeTrue();
            var request = await _requestDbContext.Requests
                .AsNoTracking()
                .SingleAsync(x => x.Id == existingRequest.Id, TestContext.Current.CancellationToken);
            request.SampleId.Should().Be(originalSampleId);
        }
        finally
        {
            optionsCache.TryRemove(Options.DefaultName);
        }
    }

    #endregion

    #region Tests - LIS-Specified Reflex Follow-Up Tests

    [Fact]
    [Trait("ModuleRequirement", "MR_5697")]
    [Trait("ProductRequirement", "PR_1798")]
    public async Task HandleImportedTestOrder_WhenReflexNameMatchesButTriggerTestDiffers_ShouldNotAddTestOrder()
    {
        // Arrange
        const string sampleId = "SAMPLE_REFLEX_MISMATCH";
        const string reflexDefName = "Reflexdef";
        await SeedReflexDefinition(reflexDefName);

        var handler = CreateHandler();
        var notification = CreateNotification(sampleId,
            CreateTestOrderWithReflex(testName: "f20", reflexName: reflexDefName, suppressReflex: false),
            CreateTestOrderWithReflex(testName: "f21", reflexName: reflexDefName, suppressReflex: false));

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        var testOrders = await _requestDbContext.TestOrders
            .AsNoTracking()
            .Where(to => to.Request.SampleId == sampleId)
            .ToListAsync(TestContext.Current.CancellationToken);

        testOrders.Should().BeEmpty("because a reflex-named LIS test must match the configured condition test before it is imported as a trigger");
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5867")]
    [Trait("ProductRequirement", "PR_1798")]
    public async Task HandleImportedTestOrder_WhenReflexNameAndTriggerTestMatch_ShouldAttachReflexDefinition()
    {
        // Arrange
        const string sampleId = "SAMPLE_REFLEX_MATCH";
        const string reflexDefName = "Reflexdef";
        await SeedReflexDefinition(reflexDefName);

        var handler = CreateHandler();
        var notification = CreateNotification(sampleId,
            CreateTestOrderWithReflex(testName: "fx1", reflexName: reflexDefName, suppressReflex: false));

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        await VerifyRequestCreatedWithTestCount(sampleId, 1);
        await VerifyTestOrdersCreated(sampleId, ("fx1", "slgE"));

        var testOrder = await _requestDbContext.TestOrders
            .AsNoTracking()
            .Include(to => to.ReflexDefinitions!)
            .FirstAsync(to => to.Request.SampleId == sampleId && to.TestName == "fx1" && to.MethodName == "slgE", TestContext.Current.CancellationToken);

        testOrder.ReflexDefinitions.Should().HaveCount(1);
        testOrder.ReflexDefinitions!.First().Name.Should().Be(reflexDefName);
    }

    [Fact(Skip = "Needs rewrite")]
    public async Task HandleImportedTestOrder_ReflextestsWithIndependentTest_WhenApproved_CreateFollowUpTestAccordingtoSuppressReflex()
    {
        // PHASE 1: LIS Import - Create Trigger Test with Reflex Definition
        // Arrange
        const string sampleId = "SAMPLE_REFLEX_001";
        const string reflexDefName = "Reflexdef";
        await SeedReflexDefinition(reflexDefName);

        var handler = CreateHandler();
        var notification = CreateNotification(sampleId,
            CreateTestOrderWithReflex(testName: "fx1", reflexName: reflexDefName, suppressReflex: false),
            CreateTestOrderWithReflex(testName: "f20", reflexName: reflexDefName, suppressReflex: false),
            CreateTestOrderWithReflex(testName: "f21", reflexName: reflexDefName, suppressReflex: true),
            CreateTestOrderWithReflex(testName: "f4", reflexName: string.Empty, suppressReflex: false));

        // Act - Import test orders from LIS
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert - Verify Trigger test fx1 and independent test f4 are created, along with reflex definition
        await VerifyRequestCreatedWithTestCount(sampleId, 2);
        await VerifyTestOrdersCreated(sampleId, ("fx1", "slgE"), ("f4", "slgE"));

        // Assert - Verify reflex definition is attached with LIS-specified follow-up test f20
        var fx1TestOrder = await _requestDbContext.TestOrders
             .Include(to => to.ReflexDefinitions!)
             .ThenInclude(rd => rd.ReflexTests)
             .FirstAsync(to => to.Request.SampleId == sampleId && to.TestName == "fx1", TestContext.Current.CancellationToken);

        fx1TestOrder?.ReflexDefinitions.Should().HaveCount(1,
            "because one reflex definition should be attached to the trigger test");
        fx1TestOrder?.ReflexDefinitions?.First().Name.Should().Be(reflexDefName);

        var reflexTests = fx1TestOrder?.ReflexDefinitions?.First().ReflexTests;
        reflexTests.Should().Contain(rt => rt.TestName == "f20",
            "because LIS specified f20 as the follow-up test (replacing the configured f1 test)");

        var lisSpecifiedF20 = reflexTests.First(rt => rt.TestName == "f20");
        lisSpecifiedF20.NumberOfReplicates.Should().Be(2,
            "because LIS specified 2 replicates for the follow-up test");
        lisSpecifiedF20.InstrumentDilutionFactor.Should().Be(5,
            "because LIS specified dilution factor 5 for the follow-up test");

        // PHASE 2: Test Result Approval - Trigger Automatic Reflex
        // Arrange 
        var testResult = new SampleTestResult
        {
            TestOrderId = fx1TestOrder!.Uid,
            TestName = fx1TestOrder.TestName,
            MethodName = fx1TestOrder.MethodName,
            MeanCutOff = "Positive", // Meets the reflex condition (CutOff = "Positive")
            Status = TestResultStatus.Approved,
            ApprovalStatus = ResultApprovalStatus.Approved
        };
        var resultHandler = CreateTestResultStatusChangedHandler();

        // Act - Approve trigger test result
        await resultHandler.Handle(
            new TestResultStatusChangedEvent([testResult]),
            TestContext.Current.CancellationToken);

        // Assert - Verify follow-up test f20 was automatically created
        await VerifyRequestCreatedWithTestCount(sampleId, 3); // fx1 + f20 + f4
        await VerifyTestOrdersCreated(sampleId, ("f20", "slgE"));

        // Assert - Verify suppressed follow-up test f21 was NOT created
        var f21TestOrder = await _requestDbContext.TestOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(to => to.Request.SampleId == sampleId && to.TestName == "f21", TestContext.Current.CancellationToken);
        f21TestOrder.Should().BeNull();

        // Assert - Verify f20 has LIS-specified properties and is marked as reflex test
        var f20TestOrder = await _requestDbContext.TestOrders
            .AsNoTracking()
            .FirstAsync(to => to.Request.SampleId == sampleId && to.TestName == "f20", TestContext.Current.CancellationToken);
        f20TestOrder.IsReflexTest.Should().BeTrue();
    }

    #endregion

    #region Tests - Sample Volume Check

    [Fact]
    public async Task HandleImportedSample_WhenSampleVolumeCheckIsEnabled_ShouldApplyConfiguredDefaultToNewRequest()
    {
        const string sampleId = "QNS_DEFAULT";
        var handler = CreateHandler(new RequestSettings
        {
            SampleVolumeCheckSettings = new SampleVolumeCheckSettings
            {
                UseSampleVolumeCheck = true,
                SelectedSampleVolumeCheck = SettingsSampleVolumeCheck.OnRunNoTestsWhenVolumeTooLow
            }
        });

        await handler.Handle(CreateNotification(sampleId, CreateTestOrder("IgE", "Im-F", false)), TestContext.Current.CancellationToken);

        var request = await _requestDbContext.Requests.AsNoTracking()
            .SingleAsync(r => r.SampleId == sampleId, TestContext.Current.CancellationToken);

        request.SampleVolumeCheck.Should().Be(SampleVolumeCheck.OnRunNoTestsWhenVolumeTooLow);
    }

    [Fact]
    public async Task HandleImportedSample_WhenOnlyForImmunoCapIsEnabledAndNoImmunoCapTestExists_ShouldTurnSampleVolumeCheckOff()
    {
        const string sampleId = "QNS_NON_IMMUNOCAP";
        var handler = CreateHandler(new RequestSettings
        {
            SampleVolumeCheckSettings = new SampleVolumeCheckSettings
            {
                UseSampleVolumeCheck = true,
                OnlyForImmunoCap = true,
                SelectedSampleVolumeCheck = SettingsSampleVolumeCheck.OnRunAccordingToTestOrder
            }
        });

        await handler.Handle(CreateNotification(sampleId, CreateTestOrder("qEli", "Qn-E", false)), TestContext.Current.CancellationToken);

        var request = await _requestDbContext.Requests.AsNoTracking()
            .SingleAsync(r => r.SampleId == sampleId, TestContext.Current.CancellationToken);

        request.SampleVolumeCheck.Should().Be(SampleVolumeCheck.Off);
    }

    [Fact]
    public async Task HandleImportedSample_WhenOnlyForImmunoCapIsEnabledAndImmunoCapIgGTestExists_ShouldApplyConfiguredDefault()
    {
        const string sampleId = "QNS_IMMUNOCAP_IGG";
        var handler = CreateHandler(new RequestSettings
        {
            SampleVolumeCheckSettings = new SampleVolumeCheckSettings
            {
                UseSampleVolumeCheck = true,
                OnlyForImmunoCap = true,
                SelectedSampleVolumeCheck = SettingsSampleVolumeCheck.OnRunAccordingToTestOrder
            }
        });

        await handler.Handle(CreateNotification(sampleId, CreateTestOrder("qIgG", "Qn-G", false)), TestContext.Current.CancellationToken);

        var request = await _requestDbContext.Requests.AsNoTracking()
            .SingleAsync(r => r.SampleId == sampleId, TestContext.Current.CancellationToken);

        request.SampleVolumeCheck.Should().Be(SampleVolumeCheck.OnRunAccordingToTestOrder);
    }

    [Fact]
    public async Task HandleImportedSample_WhenRequestAlreadyExists_ShouldPreserveSampleVolumeCheck()
    {
        const string sampleId = "QNS_EXISTING";
        var existingRequest = new Domain.Request
        {
            SampleId = sampleId,
            Source = RequestSource.Lis,
            SampleVolumeCheck = SampleVolumeCheck.OnRunNoTestsWhenVolumeTooLow
        };
        _requestDbContext.Requests.Add(existingRequest);
        await _requestDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(new RequestSettings
        {
            SampleVolumeCheckSettings = new SampleVolumeCheckSettings
            {
                UseSampleVolumeCheck = true,
                SelectedSampleVolumeCheck = SettingsSampleVolumeCheck.Off
            }
        });

        await handler.Handle(CreateNotification(sampleId, CreateTestOrder("IgE", "Im-F", false)), TestContext.Current.CancellationToken);

        var request = await _requestDbContext.Requests.AsNoTracking()
            .SingleAsync(r => r.SampleId == sampleId, TestContext.Current.CancellationToken);

        request.SampleVolumeCheck.Should().Be(SampleVolumeCheck.OnRunNoTestsWhenVolumeTooLow);
    }

    #endregion

    #region Private Methods - Test Data Setup

    private static TestData CreateTestData() =>
        new TestDataBuilder()
            .WithPanel("Anca", ("mp", "El-G", 2, 5), ("pr", "El-G", 3, 10))
            .WithPanel("paps1", ("Gb2", "El-G"), ("Gcl", "El-G"))
            .WithPanel("panas", ("ce", "El-G"), ("dn", "El-G"), ("jo", "El-G"))
            .WithPanel("Pcd", ("Acy", "El-A"), ("Agp", "El-A"), ("Gcy", "El-G"), ("Ggp", "El-G"))
            .WithPanel("LabNamePanel", ("Mtw1R", "sIgE"))
            .WithPanel("IntPanel", ("iPt", "El-G"))
            .WithPanel("asthm", ("d1", "slgE"), ("e1", "slgE"), ("e5", "slgE"), ("g6", "slgE"), ("i6", "slgE"), ("m2", "slgE"), ("t3", "slgE"), ("t9", "slgE"), ("w21", "slgE"), ("w6", "slgE"))
            .WithPanel("eczem", ("d1", "slgE"), ("e1", "slgE"), ("e5", "slgE"), ("f1", "slgE"), ("f13", "slgE"), ("f2", "slgE"))
            .WithPanel("rhina", ("d1", "slgE"), ("e1", "slgE"), ("e5", "slgE"), ("g6", "slgE"), ("i6", "slgE"), ("m2", "slgE"), ("t3", "slgE"), ("t9", "slgE"), ("w21", "slgE"), ("w6", "slgE"))
            .WithPanel("rhinc", ("d1", "slgE"), ("e1", "slgE"), ("e5", "slgE"), ("f1", "slgE"), ("f13", "slgE"), ("f2", "slgE"), ("g6", "slgE"), ("t3", "slgE"), ("t9", "slgE"))
            .WithPanel("wheez", ("d1", "slgE"), ("e1", "slgE"), ("e5", "slgE"), ("f1", "slgE"), ("f13", "slgE"), ("f2", "slgE"), ("g6", "slgE"), ("t3", "slgE"), ("t9", "slgE"), ("w21", "slgE"))
            .WithPanel("ReflexPanel", ("fx1", "slgE"), ("f4", "slgE"))
            .WithIndividualTest("IgE", "Im-F")
            .WithIndividualTest("Ig", "El-G")  // Panel name can also be used as individual test
            .WithIndividualTest("qEli", "Qn-E", AssayTechnology.EliA)
            .WithIndividualTest("qIgG", "Qn-G", AssayTechnology.ImmunoCapIgG)
                                               // Add reflex tests
            .WithIndividualTest("fx1", "slgE")
            .WithIndividualTest("f20", "slgE")
            .WithIndividualTest("f21", "slgE")
            .WithIndividualTest("f4", "slgE")
            .WithIndividualTest("Gb2", "El-G")
            .WithIndividualTest("Gcl", "El-G")
            .WithIndividualTest("dn", "El-G")
            .WithIndividualTest("jo", "El-G")
            .WithIndividualTest("Acy", "El-A")
            .WithIndividualTest("Agp", "El-A")
            .WithIndividualTest("Gcy", "El-G")
            .WithIndividualTest("Ggp", "El-G")
            .WithIndividualTest("i6", "slgE")
            .WithIndividualTest("m2", "slgE")
            .WithIndividualTest("w6", "slgE")
            // Add tests with priority settings for the priority test
            .WithIndividualTest("pr", "El-G", includeInPriorityGroupA: false, orderInPriorityGroupA: 1)
            .WithIndividualTest("mp", "El-G", includeInPriorityGroupA: false, orderInPriorityGroupA: 2)
            .WithIndividualTest("prs", "El-G", includeInPriorityGroupA: true, orderInPriorityGroupA: 0)
            .WithIndividualTest("mps", "El-G", includeInPriorityGroupA: false, orderInPriorityGroupA: 0)
            .WithIndividualTest("e1", "slgE", includeInPriorityGroupA: true, orderInPriorityGroupA: 2)
            .WithIndividualTest("e5", "slgE", includeInPriorityGroupA: false, orderInPriorityGroupA: 3)
            .WithIndividualTest("ce", "El-G", includeInPriorityGroupA: false, orderInPriorityGroupA: 0)
            .WithIndividualTest("d1", "slgE", includeInPriorityGroupA: false, orderInPriorityGroupA: 0)
            .WithIndividualTest("f1", "slgE", includeInPriorityGroupA: true, orderInPriorityGroupA: 0)
            .WithIndividualTest("f13", "slgE", includeInPriorityGroupA: false, orderInPriorityGroupA: 0)
            .WithIndividualTest("f2", "slgE", includeInPriorityGroupA: false, orderInPriorityGroupA: 0)
            .WithIndividualTest("g6", "slgE", includeInPriorityGroupA: false, orderInPriorityGroupA: 0)
            .WithIndividualTest("t3", "slgE", includeInPriorityGroupA: false, orderInPriorityGroupA: 0)
            .WithIndividualTest("t9", "slgE", includeInPriorityGroupA: false, orderInPriorityGroupA: 0)
            .WithIndividualTest("w21", "slgE", includeInPriorityGroupA: false, orderInPriorityGroupA: 0)
            .WithTestAndLabNames("c1", "slgE", ("c1", 1), ("bhy", 2))
            .WithTestAndLabNames("c2", "slgE", ("c2", 1))
            .WithTestAndLabNames("Vd1", "View", ("Vd1", 1), ("bhy", 2))
            .WithTestAndLabNames("Mtw1R", "sIgE", ("Mtw-1R", 1))
            .Build();

    private async Task SeedParametersDatabase()
    {
        // Check if data is already seeded to avoid duplicate key errors
        var alreadySeeded = await _parametersDbContext.Methods.AnyAsync(m => m.MethodName == "El-G", TestContext.Current.CancellationToken);
        if (!alreadySeeded)
        {
            _parametersDbContext.MethodTests.AddRange(_testData.MethodTests);
            _parametersDbContext.Panels.AddRange(_testData.Panels);

            // Add method test lab names
            foreach (var methodTest in _testData.MethodTests)
            {
                if (methodTest.LabTestNames.Count > 0)
                {
                    _parametersDbContext.MethodTestLabNames.AddRange(methodTest.LabTestNames);
                }
            }

            await _parametersDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// Seeds a reflex definition in the Reflex module database (configuration).
    /// </summary>
    private async Task SeedReflexDefinition(string reflexDefName = "Reflexdef")
    {
        var sender = ServiceProvider.GetRequiredService<ISender>();

        // Create reflex definition using command
        var command = new Reflex.Application.Commands.CreateReflexDefinitionCommand.CreateReflexDefinitionCommand
        {
            Name = reflexDefName,
            Automatic = true,
            Active = true,
            HasRequestorEnabled = false,
            RequestorId = null,
            ReflexCondition = new Reflex.Application.Queries.ReflexConditionDto
            {
                TestName = "fx1",
                MethodName = "slgE",
                ResultType = ResultType.CutOff,
                CompareType = CompareType.EqualTo,
                CutOffCompareValue = "Positive",
                ReportingCutOff = true  // Required for CutOff validation
            },
            ReflexTests = new List<Reflex.Application.Queries.ReflexTestDto>
            {
                new()
                {
                    TestName = "f1",
                    MethodName = "slgE",
                    NumberOfReplicates = 1,
                    InstrumentDilutionFactor = 1,
                    TestSortOrder = 1
                }
            }
        };

        try
        {
            await sender.Send(command, TestContext.Current.CancellationToken);
        }
        catch (FluentValidation.ValidationException ex) when (ex.Message.Contains("already exists"))
        {
        }
    }
    #endregion

    #region Private Methods - Handler Creation

    private ProcessLisTestOrdersCommandHandler CreateHandler()
    {
        var mocks = new TestDependencies(ServiceProvider);

        return new ProcessLisTestOrdersCommandHandler(
            _requestDbContext,
            mocks.RequestSettings.Object,
            mocks.TubeSettings.Object,
            mocks.Logger.Object,
            mocks.Sender,
            mocks.MethodsService,
            mocks.DateTime);
    }

    private ProcessLisTestOrdersCommandHandler CreateHandler(RequestSettings settings)
    {
        var mocks = new TestDependencies(ServiceProvider);
        mocks.RequestSettings.Setup(x => x.Value).Returns(settings);

        return new ProcessLisTestOrdersCommandHandler(
            _requestDbContext,
            mocks.RequestSettings.Object,
            mocks.TubeSettings.Object,
            mocks.Logger.Object,
            mocks.Sender,
            mocks.MethodsService,
            mocks.DateTime);
    }

    private ProcessLisTestOrdersCommandHandler CreateHandlerWithPatientInfoEnabled()
    {
        var mocks = new TestDependencies(ServiceProvider);

        // Override to enable patient information
        mocks.RequestSettings.Setup(x => x.Value).Returns(new RequestSettings
        {
            IsPatientInformationEnabled = true,
        });

        return new ProcessLisTestOrdersCommandHandler(
            _requestDbContext,
            mocks.RequestSettings.Object,
            mocks.TubeSettings.Object,
            mocks.Logger.Object,
            mocks.Sender,
            mocks.MethodsService,
            mocks.DateTime);
    }

    private TestResultStatusChangedEventHandler CreateTestResultStatusChangedHandler()
    {
        var mediator = ServiceProvider.GetRequiredService<IMediator>();
        var displayNameProvider = ServiceProvider.GetRequiredService<ITestDisplayNameProvider>();
        var requestSettings = new Mock<IOptionsSnapshot<RequestSettings>>();
        requestSettings.Setup(x => x.Value).Returns(new RequestSettings());

        return new TestResultStatusChangedEventHandler(
            _requestDbContext,
            mediator,
            displayNameProvider,
            requestSettings.Object);
    }

    #endregion

    #region Private Methods - Notification Creation

    private static ProcessLisTestOrdersCommand CreateNotification(
        string sampleId,
        params LisTestOrder[] testOrders) => new()
        {
            LisChannelId = 1,
            Patients = [new LisPatient
        {
            Samples = [new LisSample
            {
                SampleId = sampleId,
                TestOrders = [..testOrders]
            }]
        }]
        };

    private static LisTestOrder CreateTestOrder(
        string testName,
        string methodName,
        bool panelExpansionEnabled) => new()
        {
            TestName = testName,
            MethodName = methodName,
            PanelExpansionEnabled = panelExpansionEnabled,
            Action = LisTestOrderAction.NewTest,
            NumberOfReplicates = 1,
            InstrumentDilutionFactor = 1
        };

    private static LisTestOrder CreateTestOrderWithPanelRecognition(
        string testName,
        string methodName,
        bool panelExpansionEnabled,
        bool autoPanelRecognitionFromOneMessage,
        bool autoPanelRecognitionFromMultipleMessage) => new()
        {
            TestName = testName,
            MethodName = methodName,
            PanelExpansionEnabled = panelExpansionEnabled,
            AutoPanelRecognitionFromOneMessage = autoPanelRecognitionFromOneMessage,
            AutoPanelRecognitionFromMultipleMessage = autoPanelRecognitionFromMultipleMessage,
            Action = LisTestOrderAction.NewTest,
            NumberOfReplicates = 1,
            InstrumentDilutionFactor = 1
        };

    private static LisTestOrder CreateTestOrderWithForceUniqueLabTestName(
        bool forceUniqueLabTestName,
        string labTestName,
        string methodName) => new()
        {
            TestName = labTestName,
            MethodName = methodName,
            ForceUniqueLaboratoryTestNames = forceUniqueLabTestName,
            PanelExpansionEnabled = false,
            Action = LisTestOrderAction.NewTest,
            NumberOfReplicates = 1,
            InstrumentDilutionFactor = 1
        };

    private static LisTestOrder CreateTestOrderWithPriorityA(
       string testName,
       string methodName,
       bool useRequestPriorityA,
       bool panelExpansionEnabled = false,
       bool forceUniqueLabTestName = false) => new()
       {
           TestName = testName,
           MethodName = methodName,
           UseRequestPriorityA = useRequestPriorityA,
           PanelExpansionEnabled = panelExpansionEnabled,
           ForceUniqueLaboratoryTestNames = forceUniqueLabTestName,
           Action = LisTestOrderAction.NewTest,
           NumberOfReplicates = 1,
           InstrumentDilutionFactor = 1
       };
    /// <summary>
    /// Creates a LIS test order with reflex information.
    /// </summary>
    private static LisTestOrder CreateTestOrderWithReflex(
        string testName,
        string reflexName,
        bool suppressReflex,
        bool autoPanelRecognitionFromOneMessage = false,
        bool autoPanelRecognitionFromMultipleMessage = false) => new()
        {
            TestName = testName,
            MethodName = "slgE",
            ReflexName = reflexName,
            Action = LisTestOrderAction.NewTest,
            NumberOfReplicates = 2,
            InstrumentDilutionFactor = 5,
            SuppressReflex = suppressReflex,
            PanelExpansionEnabled = false,
            AutoPanelRecognitionFromOneMessage = autoPanelRecognitionFromOneMessage,
            AutoPanelRecognitionFromMultipleMessage = autoPanelRecognitionFromMultipleMessage
        };

    #endregion

    #region Private Methods - Verification

    private async Task VerifyRequestCreatedWithTestCount(
        string expectedSampleId,
        int expectedTestCount)
    {
        var request = await _requestDbContext.Requests
             .AsNoTracking()
             .Include(r => r.TestOrders)
             .FirstOrDefaultAsync(r => r.SampleId == expectedSampleId, TestContext.Current.CancellationToken);

        request.Should().NotBeNull();
        request.TestOrders.Should().HaveCount(expectedTestCount);

    }

    private async Task VerifyTestOrdersCreated(string inputSampleId, params (string testName, string methodName)[] expectedOrders)
    {
        foreach (var (testName, methodName) in expectedOrders)
        {
            var exists = await _requestDbContext.TestOrders
             .AsNoTracking()
             .AnyAsync(t => t.Request.SampleId == inputSampleId && t.TestName == testName && t.MethodName == methodName, TestContext.Current.CancellationToken);

            exists.Should().BeTrue(
                $"Test order with TestName='{testName}' and MethodName='{methodName}' should exist");
        }
    }

    private async Task VerifyPanelAssignedForTests(string sampleId, int? panelId, params (string testName, string methodName)[] testNames)
    {
        var testMethods = testNames.Select(t => $"{t.testName}{t.methodName}").ToList();

        // All test orders fetched should have panelId
        var testOrders = await _requestDbContext.TestOrders
            .AsNoTracking()
            .Where(to => to.Request.SampleId == sampleId &&
                         testMethods.Contains(to.TestName + to.MethodName))
            .ToListAsync(TestContext.Current.CancellationToken);

        testOrders.Should().NotBeEmpty();

        if (panelId.HasValue)
        {
            testOrders.Should().OnlyContain(
                to => to.TestPanelIds.Contains(panelId.Value),
                $"All test orders should have panel ID {panelId.Value}");
        }
        else
        {
            testOrders.Should().OnlyContain(
                to => !to.TestPanelIds.Any(),
                "Test orders should have no panel IDs assigned");
        }

    }

    private async Task VerifyNoPanelAssignedForTests(string sampleId, params (string testName, string methodName)[] testNames)
    {
        var testMethods = testNames.Select(t => $"{t.testName}{t.methodName}").ToList();

        // All test orders fetched should have NO panel assignment
        var testOrders = await _requestDbContext.TestOrders
            .AsNoTracking()
            .Where(to => to.Request.SampleId == sampleId &&
                         testMethods.Contains(to.TestName + to.MethodName))
            .ToListAsync(TestContext.Current.CancellationToken);

        testOrders.Should().NotBeEmpty();
        testOrders.Should().OnlyContain(to => to.TestPanelIds.Count == 0,
            "All specified test orders should have no panel assignments");
    }

    private async Task VerifyReflexDefinitionPreserved(string sampleId, string reflexDefinitionName)
    {
        var triggerTestOrder = await _requestDbContext.TestOrders
            .AsNoTracking()
            .Include(to => to.ReflexDefinitions!)
            .ThenInclude(definition => definition.ReflexTests)
            .SingleAsync(
                to => to.Request.SampleId == sampleId && to.TestName == "fx1" && to.MethodName == "slgE",
                TestContext.Current.CancellationToken);

        triggerTestOrder.HasReflexTests.Should().BeTrue();
        triggerTestOrder.ReflexDefinitions.Should().ContainSingle();
        triggerTestOrder.ReflexDefinitions!.Single().Name.Should().Be(reflexDefinitionName);
        triggerTestOrder.ReflexDefinitions.Single().ReflexTests.Should().ContainSingle(
            reflexTest => reflexTest.TestName == "f1" && reflexTest.MethodName == "slgE");
    }

    private async Task VerifyTestOrderSettings(
        string sampleId,
        params (string testName, string methodName, int expectedReplicates, int expectedInstrumentDilutionFactor)[] expectedSettings)
    {
        var testOrders = await _requestDbContext.TestOrders
            .AsNoTracking()
            .Where(to => to.Request.SampleId == sampleId)
            .ToListAsync(TestContext.Current.CancellationToken);

        foreach (var (testName, methodName, expectedReplicates, expectedInstrumentDilutionFactor) in expectedSettings)
        {
            var testOrder = testOrders.Single(to => to.TestName == testName && to.MethodName == methodName);
            testOrder.NumberOfReplicates.Should().Be(expectedReplicates);
            testOrder.InstrumentDilutionFactor.Should().Be(expectedInstrumentDilutionFactor);
        }
    }

    private async Task<int> GetPanelIdByName(string panelName)
    {
        return await _parametersDbContext.Panels
            .AsNoTracking()
            .Where(p => p.PanelName == panelName)
            .Select(p => p.Id)
            .FirstAsync(TestContext.Current.CancellationToken);
    }

    private async Task DisablePanel(string panelName)
    {
        var panel = await _parametersDbContext.Panels
            .SingleAsync(p => p.PanelName == panelName, TestContext.Current.CancellationToken);

        panel.Enabled = false;
        await _parametersDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private async Task VerifyTestOrderSortOrder(string sampleId, (string testName, string methodName, int expectedSortOrder)[] expectedOrders)
    {
        var testOrders = await _requestDbContext.TestOrders
            .AsNoTracking()
            .Where(to => to.Request.SampleId == sampleId)
            .ToListAsync(TestContext.Current.CancellationToken);

        foreach (var (testName, methodName, expectedSortOrder) in expectedOrders)
        {
            var testOrder = testOrders.FirstOrDefault(to =>
                to.TestName == testName && to.MethodName == methodName);

            testOrder.Should().NotBeNull(
                $"Test order {testName}/{methodName} should exist");
            testOrder.TestSortOrder.Should().Be(expectedSortOrder,
                $"Test order {testName}/{methodName} should have sort order {expectedSortOrder}");
        }
    }

    #endregion
}

#region Test Support Classes

/// <summary>
/// Manages mock dependencies and actual services for the handler.
/// </summary>
internal sealed class TestDependencies
{
    public Mock<IOptionsSnapshot<RequestSettings>> RequestSettings { get; }
    public Mock<IOptionsSnapshot<TubeTypesSettings>> TubeSettings { get; }
    public Mock<ILogger<ProcessLisTestOrdersCommandHandler>> Logger { get; }
    public ISender Sender { get; }
    public IMethodsService MethodsService { get; }
    public Mock<ISchedulerFactory> SchedulerFactory { get; }
    public IDateTime DateTime { get; }  // Changed from Mock<IDateTime> to actual IDateTime

    public TestDependencies(IServiceProvider serviceProvider)
    {
        // Actual services from DI
        var parametersDbContext = serviceProvider.GetRequiredService<IParametersDbContext>();
        Sender = serviceProvider.GetRequiredService<ISender>();
        DateTime = serviceProvider.GetRequiredService<IDateTime>();  // Use actual service

        MethodsService = new MethodsService(
            parametersDbContext,
            new Mock<IAuthorizationService>().Object);

        // Mocked dependencies
        RequestSettings = CreateRequestSettingsMock();

        TubeSettings = new Mock<IOptionsSnapshot<TubeTypesSettings>>();
        TubeSettings.Setup(x => x.Value).Returns(new TubeTypesSettings
        {
            NormalTubeDefinitionId = "1",
            TubeTypeList = [new TubeType { TubeDefinitionId = 1 }]
        });

        Logger = new Mock<ILogger<ProcessLisTestOrdersCommandHandler>>();
        SchedulerFactory = CreateSchedulerFactoryMock();
    }

    private static Mock<IOptionsSnapshot<RequestSettings>> CreateRequestSettingsMock()
    {
        var mock = new Mock<IOptionsSnapshot<RequestSettings>>();
        mock.Setup(x => x.Value).Returns(new RequestSettings
        {
            IsPatientInformationEnabled = false,
        });
        return mock;
    }

    private static Mock<ISchedulerFactory> CreateSchedulerFactoryMock()
    {
        var mock = new Mock<ISchedulerFactory>();
        mock.Setup(x => x.GetScheduler(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Mock<IScheduler>().Object);
        return mock;
    }
}
/// <summary>
/// Fluent builder for creating test data with proper entity relationships.
/// </summary>
public sealed class TestDataBuilder
{
    private readonly List<Panel> _panels = [];
    private readonly List<MethodTest> _methodTests = [];
    private readonly Dictionary<string, Method> _methodsByName = new();

    public TestDataBuilder WithPanel(string panelName, params (string testName, string methodName)[] tests)
    {
        return WithPanel(panelName, tests
            .Select(test => (test.testName, test.methodName, replicates: 1, instrumentDilutionFactor: 1))
            .ToArray());
    }

    public TestDataBuilder WithPanel(
        string panelName,
        params (string testName, string methodName, int replicates, int instrumentDilutionFactor)[] tests)
    {
        var methodTestsForPanel = tests.Select((t, index) =>
        {
            var methodTest = GetOrCreateMethodTest(t.testName, t.methodName);
            // Set sort order for panel tests (1-based index)
            return (methodTest, t.replicates, t.instrumentDilutionFactor, SortOrder: (int?)(index + 1));
        }).ToList();

        _panels.Add(new Panel
        {
            PanelName = panelName,
            PanelLongName = $"{panelName} Panel",
            Tests = methodTestsForPanel.ConvertAll(mt => new PanelTest
            {
                MethodTest = mt.methodTest,
                Replicates = mt.replicates,
                InstrumentDilutionFactor = mt.instrumentDilutionFactor,
                SortOrder = mt.SortOrder
            })
        });

        return this;
    }

    public TestDataBuilder WithIndividualTest(string testName, string methodName)
    {
        var methodTest = GetOrCreateMethodTest(testName, methodName);
        AddLaboratoryTestName(methodTest, testName);
        return this;
    }

    public TestDataBuilder WithIndividualTest(string testName, string methodName, AssayTechnology technology)
    {
        var methodTest = GetOrCreateMethodTest(testName, methodName);
        AddLaboratoryTestName(methodTest, testName);
        methodTest.Method.Technology = technology;
        return this;
    }

    public TestDataBuilder WithIndividualTest(string testName, string methodName, bool includeInPriorityGroupA, int orderInPriorityGroupA)
    {
        var methodTest = GetOrCreateMethodTest(testName, methodName);
        AddLaboratoryTestName(methodTest, testName);

        methodTest.Priority = new MethodTestPriority
        {
            IncludeInPriorityGroupA = includeInPriorityGroupA,
            OrderInPriorityGroupA = orderInPriorityGroupA,
            IncludeInPriorityGroupB = false,
            OrderInPriorityGroupB = 0
        };

        return this;
    }

    private static void AddLaboratoryTestName(MethodTest methodTest, string laboratoryTestName)
    {
        if (methodTest.LabTestNames.Any(x => x.TestName == laboratoryTestName))
        {
            return;
        }

        methodTest.LabTestNames.Add(new MethodTestLabName
        {
            TestName = laboratoryTestName,
            SortOrder = methodTest.LabTestNames.Count + 1,
            MethodTest = methodTest
        });
    }

    public TestDataBuilder WithTestAndLabNames(string testName, string methodName, params (string labName, int sortOrder)[] labNames)
    {
        var methodTest = GetOrCreateMethodTest(testName, methodName);

        // Add laboratory test names
        foreach (var (labName, sortOrder) in labNames)
        {
            methodTest.LabTestNames.Add(new MethodTestLabName
            {
                TestName = labName,
                SortOrder = sortOrder,
                MethodTest = methodTest
            });
        }

        return this;
    }

    public TestData Build() => new()
    {
        Panels = _panels,
        MethodTests = _methodTests
    };

    private MethodTest GetOrCreateMethodTest(string testName, string methodName)
    {
        var existing = _methodTests.Find(mt => mt.TestName == testName && mt.Method.MethodName == methodName);

        if (existing is not null)
        {
            return existing;
        }

        var methodTest = new MethodTest
        {
            TestName = testName,
            FullTestName = $"{testName} Full",
            DefaultLabTestName = BuildDefaultLabTestName(testName),
            DiluentIdentity = $"DIL-{testName}",
            Method = GetOrCreateMethod(methodName),
            DefaultMethodConcentrationUnitId = 1,
            LabTestNames = [],
            DefaultDilutionMethod = DilutionMethod.InstrumentDilution
        };

        _methodTests.Add(methodTest);
        return methodTest;
    }

    private static string BuildDefaultLabTestName(string testName)
    {
        var withSuffix = $"{testName}-Lab";
        if (withSuffix.Length <= 10)
        {
            return withSuffix;
        }

        return testName.Length <= 10 ? testName : testName[..10];
    }

    private Method GetOrCreateMethod(string methodName)
    {
        if (_methodsByName.TryGetValue(methodName, out var existingMethod))
        {
            return existingMethod;
        }

        var method = new Method
        {
            MethodName = methodName,
            FullMethodName = $"{methodName} Full Method",
            AnalyticalRun = new MethodAnalyticalRun(),
            Calculation = new MethodCalculation(),
            Presentation = new MethodPresentation
            {
                XAxisText = "Concentration",
                XAxisUnit = "kUA/l",
                YAxisText = "Response",
                YAxisUnit = "RU"
            },
            Reporting = new MethodReporting
            {
                DefaultTextQuantitativeAbove = ">100",
                DefaultTextQuantitativeBelow = "<0.10"
            },
            ConcentrationUnits = [new MethodConcentrationUnit
            {
                UnitId = 1,
                ConcentrationUnit = "kUA/l"
            }]
        };

        _methodsByName[methodName] = method;
        return method;
    }
}

/// <summary>
/// Container for test data.
/// </summary>
public sealed class TestData
{
    public required List<Panel> Panels { get; init; }
    public required List<MethodTest> MethodTests { get; init; }
}

#endregion
