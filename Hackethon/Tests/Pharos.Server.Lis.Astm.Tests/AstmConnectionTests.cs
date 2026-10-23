using System.Reflection;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Application.SystemErrors;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Infrastructure;
using Pharos.Server.Lis.Application.Interfaces;
using Pharos.Server.Lis.Astm.Application.Interfaces;
using Pharos.Server.Lis.Astm.Application.Messages;
using Pharos.Server.Lis.Astm.Application.Services;
using Pharos.Server.Lis.Domain;

namespace Pharos.Server.Lis.Astm.Tests;

public class AstmConnectionTests
{
    private readonly Mock<ILisTransportConnectionFactory> _mockTransportConnectionFactory;
    private readonly Mock<IAstmDataLinkFactory> _mockDataLinkFactory;
    private readonly Mock<IAstmDataLink> _mockDataLink;
    private readonly Mock<IDateTime> _mockDateTime;
    private readonly ILogger<AstmConnection> _logger;
    private readonly Mock<ISender> _mockSender;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public AstmConnectionTests()
    {
        _mockTransportConnectionFactory = new Mock<ILisTransportConnectionFactory>();
        _mockDataLinkFactory = new Mock<IAstmDataLinkFactory>();
        _mockDataLink = new Mock<IAstmDataLink>();
        _mockDateTime = new Mock<IDateTime>();
        _logger = NullLogger<AstmConnection>.Instance;
        _mockSender = new Mock<ISender>();

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(provider => provider.GetService(typeof(ISender))).Returns(_mockSender.Object);
        var serviceScope = new Mock<IServiceScope>();
        serviceScope.SetupGet(scope => scope.ServiceProvider).Returns(serviceProvider.Object);
        var serviceScopeFactory = new Mock<IServiceScopeFactory>();
        serviceScopeFactory.Setup(factory => factory.CreateScope()).Returns(serviceScope.Object);
        _serviceScopeFactory = serviceScopeFactory.Object;

        // Setup the factory to return the mock data link
        _mockDataLinkFactory.Setup(f => f.CreateAstmDataLink(It.IsAny<AstmSettings>()))
            .Returns(_mockDataLink.Object);

        // Setup default DateTime behavior
        _mockDateTime.Setup(dt => dt.Now).Returns(DateTimeOffset.Now);
        _mockDateTime.Setup(dt => dt.UtcNow).Returns(DateTimeOffset.UtcNow);
    }


    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5389")]
    public void OnDataLinkMessageReceived_WhenSkipRequestorInfoIsTrue_ShouldNotMapRequestorInfoFromTestOrderRecord()
    {
        // Arrange
        var astmSettings = new AstmSettings
        {
            ImportSettings = new AstmImportSettings { SkipImportOfRequestorInformation = true },
        };

        var lisChannel = new LisChannel { AstmSettings = astmSettings };

        // Create connection with mocked dependencies
        var connection = new AstmConnection(1, lisChannel, LisConnectionRole.Both, _mockTransportConnectionFactory.Object,
            _mockDataLinkFactory.Object, _mockDateTime.Object, _logger, _serviceScopeFactory);

        // Create test records
        var patientRecord = new AstmPatientRecord
        {
            LaboratoryPatientId = "TestPatientId",
            PatientName1 = "Test Patient"
        };

        var testOrderRecord = new AstmTestOrderRecord
        {
            SpecimenId = "TestSampleId",
            RequestorId = "TestRequestorId",
            RequestorName = "Test Requestor",
            RequestorAddressLine1 = "123 Test Street",
            RequestorPhone = "555-1234"
        };
        testOrderRecord.UniversalTestIds.Add(new AstmTestId("TestName", "TestMethod", 1, 0, "", 1));

        // Create mock message
        var mockMessage = new Mock<IAstmMessage>();
        mockMessage.Setup(m => m.Records).Returns(new List<IAstmRecord> { patientRecord, testOrderRecord });

        TestOrderReceivedEventArgs? capturedEventArgs = null;
        connection.TestOrderReceived += (_, args) => capturedEventArgs = args;

        // Act - Invoke the private OnDataLinkMessageReceived method using reflection
        var method = typeof(AstmConnection).GetMethod("OnDataLinkMessageReceived",
            BindingFlags.NonPublic | BindingFlags.Instance);
        method?.Invoke(connection, [null, mockMessage.Object]);

        // Assert
        capturedEventArgs.Should().NotBeNull();
        capturedEventArgs!.Patients.Should().HaveCount(1);

        var patient = capturedEventArgs.Patients.First();
        patient.Samples.Should().HaveCount(1);

        var sample = patient.Samples.First();
        sample.Requestor.Should().BeNull("because SkipRequestorInfo is true, requestor information should not be mapped from test order record");
    }


    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5389")]
    public void OnDataLinkMessageReceived_WhenSkipRequestorInfoIsFalse_ShouldMapRequestorInfoFromTestOrderRecord()
    {
        // Arrange
        var astmSettings = new AstmSettings
        {
            ImportSettings = new AstmImportSettings { SkipImportOfRequestorInformation = false },
        };

        var lisChannel = new LisChannel { AstmSettings = astmSettings };

        // Create connection with mocked dependencies
        var connection = new AstmConnection(1, lisChannel, LisConnectionRole.Both, _mockTransportConnectionFactory.Object,
            _mockDataLinkFactory.Object, _mockDateTime.Object, _logger, _serviceScopeFactory);

        // Create test records
        var patientRecord = new AstmPatientRecord
        {
            LaboratoryPatientId = "TestPatientId",
            PatientName1 = "Test Patient"
        };

        var testOrderRecord = new AstmTestOrderRecord
        {
            SpecimenId = "TestSampleId",
            RequestorId = "TestRequestorId",
            RequestorName = "Test Requestor",
            RequestorAddressLine1 = "123 Test Street",
            RequestorAddressLine2 = "",
            RequestorAddressLine3 = "",
            RequestorAddressLine4 = "",
            RequestorAddressLine5 = "",
            RequestorPhone = "555-1234"
        };
        testOrderRecord.UniversalTestIds.Add(new AstmTestId("TestName", "TestMethod", 1, 0, "", 1));

        // Create mock message
        var mockMessage = new Mock<IAstmMessage>();
        mockMessage.Setup(m => m.Records).Returns(new List<IAstmRecord> { patientRecord, testOrderRecord });

        TestOrderReceivedEventArgs? capturedEventArgs = null;
        connection.TestOrderReceived += (_, args) => capturedEventArgs = args;

        // Act - Invoke the private OnDataLinkMessageReceived method using reflection
        var method = typeof(AstmConnection).GetMethod("OnDataLinkMessageReceived",
            BindingFlags.NonPublic | BindingFlags.Instance);
        method?.Invoke(connection, [null, mockMessage.Object]);

        // Assert
        capturedEventArgs.Should().NotBeNull();
        capturedEventArgs!.Patients.Should().HaveCount(1);

        var patient = capturedEventArgs.Patients.First();
        patient.Samples.Should().HaveCount(1);

        var sample = patient.Samples.First();
        sample.Requestor.Should().NotBeNull("because SkipRequestorInfo is false, requestor information should be mapped from test order record");
        sample.Requestor!.RequestorId.Should().Be("TestRequestorId");
        sample.Requestor.Name.Should().Be("Test Requestor");
        sample.Requestor.PhoneNumber.Should().Be("555-1234");
        sample.Requestor.Address.Should().Be("123 Test Street\n\n\n\n");
    }

    [Theory]
    [InlineData(false, "SID003")]
    [InlineData(true, "SID001", "SID002", "SID003")]
    public void OnDataLinkMessageReceived_WithRepeatedHeaders_HandlesMappedRecordsAccordingToSetting(
        bool ignoreErrors,
        params string[] expectedSampleIds)
    {
        var lisChannel = GetListTestChannel();
        lisChannel.AstmSettings!.ImportSettings.IgnoreNonCriticalImportErrors = ignoreErrors;
        var connection = CreateConnection(lisChannel);
        var message = new AstmMessageParser().ParseMessage(
            "H|\\^&|||Host|||||||P|1|20010226150028\r" +
            "P|1|PID001|RID001\r" +
            "O|1|SID001^N^01^1||^^^f1^sIgE^1|||20010226090000||||N||1||||||||||||O\r" +
            "H|\\^&|||Host|||||||P|2|20010226150029\r" +
            "P|1|PID002|RID002\r" +
            "O|1|SID002^N^01^2||^^^f2^sIgE^1|||20010226090000||||N||1||||||||||||O\r" +
            "H|\\^&|||Host|||||||P|3|20010226150030\r" +
            "P|1|PID003|RID003\r" +
            "O|1|SID003^N^01^3||^^^f3^sIgE^1|||20010226090000||||N||1||||||||||||O\r" +
            "L|1|F");

        var eventArgs = ReceiveMessage(connection, message);

        eventArgs.Patients.SelectMany(patient => patient.Samples)
            .Select(sample => sample.SampleId)
            .Should().Equal(expectedSampleIds);   
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OnDataLinkMessageReceived_WhenLastHeaderHasNoRecords_DiscardsAllExistingRecords(bool ignoreErrors)
    {
        var lisChannel = GetListTestChannel();
        lisChannel.AstmSettings!.ImportSettings.IgnoreNonCriticalImportErrors = ignoreErrors;
        var connection = CreateConnection(lisChannel);
        var message = new AstmMessageParser().ParseMessage(
            "H|\\^&|||Host|||||||P|1|20010226150028\r" +
            "P|1|PID001|RID001\r" +
            "O|1|SID001^N^01^1||^^^f1^sIgE^1|||20010226090000||||N||1||||||||||||O\r" +
            "H|\\^&|||Host|||||||P|2|20010226150029\r" +
            "P|1|PID002|RID002\r" +
            "O|1|SID002^N^01^2||^^^f2^sIgE^1|||20010226090000||||N||1||||||||||||O\r" +
            "H|\\^&|||Host|||||||P|3|20010226150030\r" +
            "L|1|F");

        var eventArgs = ReceiveMessage(connection, message);

        eventArgs.Patients.Should().BeEmpty();
    }

    [Theory]
    [InlineData(false, "^^^f1^sIgE^1", "INVALID", "20010226090000")]
    [InlineData(true, "^^^f1^sIgE^1", "INVALID", "20010226090000")]
    [InlineData(false, "^^^f1^sIgE^1\\^^^f2^sIgE^INVALID", "1", "20010226090000")]
    [InlineData(true, "^^^f1^sIgE^1\\^^^f2^sIgE^INVALID", "1", "20010226090000")]
    [InlineData(false, "^^^f1^sIgE^1", "1", "INVALID")]
    [InlineData(true, "^^^f1^sIgE^1", "1", "INVALID")]
    public void OnDataLinkMessageReceived_WithInvalidOrderFields_DiscardsOrderAndReportsError(
        bool ignoreErrors, string testIds, string dilution, string collectionDate)
    {
        var channel = GetListTestChannel();
        channel.AstmSettings!.ImportSettings.IgnoreNonCriticalImportErrors = ignoreErrors;
        channel.AstmSettings.ImportSettings.IgnoreImportedPreDilutionFactor = false;
        var connection = CreateConnection(channel);
        var message = new AstmMessageParser().ParseMessage(
            "H|\\^&|||Host|||||||P|1|20010226150028\r" +
            "P|1|PID001|RID001\r" +
            $"O|1|INVALID_SAMPLE^N^01^1||{testIds}|||{collectionDate}||||N||{dilution}||||||||||||O\r" +
            "O|2|VALID_SAMPLE^N^01^2||^^^f1^sIgE^1|||20010226090000||||N||1||||||||||||O\r" +
            "L|1|F");

        var eventArgs = ReceiveMessage(connection, message);

        var samples = eventArgs.Patients.SelectMany(patient => patient.Samples).ToList();
        var validSamples = samples.Where(sample => sample.SampleId == "VALID_SAMPLE").ToList();
        validSamples.Should().ContainSingle();
        validSamples[0].TestOrders.Should().ContainSingle().Which.PreDilutionFactor.Should().Be(1);
        _mockSender.Verify(sender => sender.Send(
            It.Is<CreatePharosErrorCommand>(command =>
                command.ErrorCode == PharosErrorCodes.LisInvalidMessage &&
                command.Classification == ErrorClassification.Error),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #region Patient age calculation & mapping Tests

    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5384")]
    public void OnDataLinkMessageReceived_WhenLisAgeAvailable_CalculatePatientAgeSettingDisabled_ShouldUseLisAge()
    {
        // Arrange
        var lisChannel = GetListTestChannel();

        var connection = new AstmConnection(1, lisChannel, LisConnectionRole.Both, _mockTransportConnectionFactory.Object,
            _mockDataLinkFactory.Object, _mockDateTime.Object, _logger, _serviceScopeFactory);

        var patientRecord = new AstmPatientRecord
        {
            LaboratoryPatientId = "TestPatientId",
            PatientName1 = "Test Patient",
            BirthDate = DateOnly.Parse("1990-01-15"),
            Age = 25
        };

        var testOrderRecord = new AstmTestOrderRecord { SpecimenId = "TestSampleId" };
        testOrderRecord.UniversalTestIds.Add(new AstmTestId("TestName", "TestMethod", 1, 0, "", 1));

        var mockMessage = new Mock<IAstmMessage>();
        mockMessage.Setup(m => m.Records).Returns(new List<IAstmRecord> { patientRecord, testOrderRecord });

        TestOrderReceivedEventArgs? capturedEventArgs = null;
        connection.TestOrderReceived += (_, args) => capturedEventArgs = args;

        // Act
        var method = typeof(AstmConnection).GetMethod("OnDataLinkMessageReceived",
            BindingFlags.NonPublic | BindingFlags.Instance);
        method?.Invoke(connection, [null, mockMessage.Object]);

        // Assert
        capturedEventArgs.Should().NotBeNull();
        var patient = capturedEventArgs!.Patients.First();
        patient.PatientAge.Should().Be(25, "because LIS-provided age should be used when calculation is disabled");
    }

    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5384")]
    public void OnDataLinkMessageReceived_WhenLisAgeAvailable_CalculatePatientAgeSettingEnabled_ShouldUseLisAge()
    {
        // Arrange
        var lisChannel = GetListTestChannel();
        SetCalculatePatientAgeSetting(lisChannel, true);

        var connection = new AstmConnection(1, lisChannel, LisConnectionRole.Both, _mockTransportConnectionFactory.Object,
            _mockDataLinkFactory.Object, _mockDateTime.Object, _logger, _serviceScopeFactory);

        var patientRecord = new AstmPatientRecord
        {
            LaboratoryPatientId = "TestPatientId",
            PatientName1 = "Test Patient",
            BirthDate = DateOnly.Parse("1990-01-15"),
            Age = 30 // LIS provides age
        };

        var testOrderRecord = new AstmTestOrderRecord { SpecimenId = "TestSampleId" };
        testOrderRecord.UniversalTestIds.Add(new AstmTestId("TestName", "TestMethod", 1, 0, "", 1));

        var mockMessage = new Mock<IAstmMessage>();
        mockMessage.Setup(m => m.Records).Returns(new List<IAstmRecord> { patientRecord, testOrderRecord });

        TestOrderReceivedEventArgs? capturedEventArgs = null;
        connection.TestOrderReceived += (_, args) => capturedEventArgs = args;

        // Act
        var method = typeof(AstmConnection).GetMethod("OnDataLinkMessageReceived",
            BindingFlags.NonPublic | BindingFlags.Instance);
        method?.Invoke(connection, [null, mockMessage.Object]);

        // Assert
        capturedEventArgs.Should().NotBeNull();
        var patient = capturedEventArgs!.Patients.First();
        patient.PatientAge.Should().Be(30, "because LIS-provided age should take precedence even when calculation is enabled");
    }

    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5384")]
    public void OnDataLinkMessageReceived_WhenLisAgeNotAvailable_CalculatePatientAgeSettingEnabled_BirthDateNotAvailable_ReturnAgeNull()
    {
        // Arrange
        var lisChannel = GetListTestChannel();
        SetCalculatePatientAgeSetting(lisChannel, true);

        var connection = new AstmConnection(1, lisChannel, LisConnectionRole.Both, _mockTransportConnectionFactory.Object,
            _mockDataLinkFactory.Object, _mockDateTime.Object, _logger, _serviceScopeFactory);

        var patientRecord = new AstmPatientRecord
        {
            LaboratoryPatientId = "TestPatientId",
            PatientName1 = "Test Patient",
            BirthDate = null,
            Age = null // No LIS age provided
        };

        var testOrderRecord = new AstmTestOrderRecord { SpecimenId = "TestSampleId" };
        testOrderRecord.UniversalTestIds.Add(new AstmTestId("TestName", "TestMethod", 1, 0, "", 1));

        var mockMessage = new Mock<IAstmMessage>();
        mockMessage.Setup(m => m.Records).Returns(new List<IAstmRecord> { patientRecord, testOrderRecord });

        TestOrderReceivedEventArgs? capturedEventArgs = null;
        connection.TestOrderReceived += (_, args) => capturedEventArgs = args;

        // Act
        var method = typeof(AstmConnection).GetMethod("OnDataLinkMessageReceived",
            BindingFlags.NonPublic | BindingFlags.Instance);
        method?.Invoke(connection, [null, mockMessage.Object]);

        // Assert
        capturedEventArgs.Should().NotBeNull();
        var patient = capturedEventArgs!.Patients.First();
        patient.PatientAge.Should().BeNull("because age cannot be calculated without birthdate");
    }

    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5384")]
    public void OnDataLinkMessageReceived_WhenFutureBirthDate_ShouldReturnZero()
    {
        // Arrange
        var lisChannel = GetListTestChannel();
        SetCalculatePatientAgeSetting(lisChannel, true);

        var fixedDate = new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero);
        _mockDateTime.Setup(dt => dt.Now).Returns(fixedDate);

        var connection = new AstmConnection(1, lisChannel, LisConnectionRole.Both, _mockTransportConnectionFactory.Object,
            _mockDataLinkFactory.Object, _mockDateTime.Object, _logger, _serviceScopeFactory);

        var futureBirthDate = DateOnly.FromDateTime(fixedDate.DateTime.AddYears(1));
        var patientRecord = new AstmPatientRecord
        {
            LaboratoryPatientId = "TestPatientId",
            PatientName1 = "Test Patient",
            BirthDate = futureBirthDate,
            Age = null
        };

        var testOrderRecord = new AstmTestOrderRecord { SpecimenId = "TestSampleId" };
        testOrderRecord.UniversalTestIds.Add(new AstmTestId("TestName", "TestMethod", 1, 0, "", 1));

        var mockMessage = new Mock<IAstmMessage>();
        mockMessage.Setup(m => m.Records).Returns(new List<IAstmRecord> { patientRecord, testOrderRecord });

        TestOrderReceivedEventArgs? capturedEventArgs = null;
        connection.TestOrderReceived += (_, args) => capturedEventArgs = args;

        // Act
        var method = typeof(AstmConnection).GetMethod("OnDataLinkMessageReceived",
            BindingFlags.NonPublic | BindingFlags.Instance);
        method?.Invoke(connection, [null, mockMessage.Object]);

        // Assert
        capturedEventArgs.Should().NotBeNull();
        var patient = capturedEventArgs!.Patients.First();
        patient.PatientAge.Should().Be(null, "because future birth dates should result in age 0, not negative values");
    }

    #endregion

    #region Delay Between Request Information Records Tests

    [Fact]
    public async Task SendMessageAsync_TestOrderQuery_WhenDelayIsZero_ShouldSendImmediately()
    {
        var lisChannel = GetListTestChannel();
        lisChannel.AstmSettings!.CommunicationSettings = new AstmCommunicationSettings
        {
            DelayBetweenRequestInformationRecords = 0
        };

        var mockTransportConnection = new Mock<ILisTransportConnection>();
        mockTransportConnection.Setup(x => x.State).Returns(LisConnectionState.Open);

        _mockTransportConnectionFactory.Setup(f => f.CreateTcpClientConnection(
            It.IsAny<System.Net.IPAddress>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(mockTransportConnection.Object);

        _mockDataLink.Setup(d => d.SendMessageAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        var connection = new AstmConnection(1, lisChannel, LisConnectionRole.Both, _mockTransportConnectionFactory.Object,
            _mockDataLinkFactory.Object, _mockDateTime.Object, _logger, _serviceScopeFactory);

        await connection.OpenAsync();

        var startTime = DateTimeOffset.Now;

        await connection.SendMessageAsync(LisMessageType.TestOrderQuery, "Message1");
        await connection.SendMessageAsync(LisMessageType.TestOrderQuery, "Message2");
        await connection.SendMessageAsync(LisMessageType.TestOrderQuery, "Message3");

        var endTime = DateTimeOffset.Now;
        var elapsed = (endTime - startTime).TotalMilliseconds;


        _mockDataLink.Verify(d => d.SendMessageAsync("Message1"), Times.Once);
        _mockDataLink.Verify(d => d.SendMessageAsync("Message2"), Times.Once);
        _mockDataLink.Verify(d => d.SendMessageAsync("Message3"), Times.Once);
        elapsed.Should().BeLessThan(2000, "because with zero delay, messages should be sent immediately");
    }

    [Fact]
    public async Task SendMessageAsync_MixedMessageTypes_ShouldOnlyDelayTestOrderQueries()
    {
        var delaySeconds = 2;
        var lisChannel = GetListTestChannel();
        lisChannel.AstmSettings!.CommunicationSettings = new AstmCommunicationSettings
        {
            DelayBetweenRequestInformationRecords = delaySeconds,
            CommunicationMode = AstmCommunicationMode.Tcp
        };

        var mockTransportConnection = new Mock<ILisTransportConnection>();
        mockTransportConnection.Setup(x => x.State).Returns(LisConnectionState.Open);

        _mockTransportConnectionFactory.Setup(f => f.CreateTcpClientConnection(
            It.IsAny<System.Net.IPAddress>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(mockTransportConnection.Object);

        _mockDataLink.Setup(d => d.SendMessageAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        _mockDateTime.Setup(dt => dt.Now).Returns(() => DateTimeOffset.Now);

        var connection = new AstmConnection(1, lisChannel, LisConnectionRole.Both, _mockTransportConnectionFactory.Object,
            _mockDataLinkFactory.Object, _mockDateTime.Object, _logger, _serviceScopeFactory);

        await connection.OpenAsync();

        // Send TestOrderQuery
        var start1 = DateTimeOffset.Now;
        await connection.SendMessageAsync(LisMessageType.TestOrderQuery, "Query1");
        var elapsed1 = (DateTimeOffset.Now - start1).TotalSeconds;

        // First query should be immediate
        elapsed1.Should().BeLessThan(1);

        // Send TestResult (should not be delayed)
        var start2 = DateTimeOffset.Now;
        await connection.SendMessageAsync(LisMessageType.SampleTestResult, "Result1");
        var elapsed2 = (DateTimeOffset.Now - start2).TotalSeconds;

        // Result should be immediate
        elapsed2.Should().BeLessThan(1);

        // Send another TestOrderQuery (should be delayed)
        var start3 = DateTimeOffset.Now;
        await connection.SendMessageAsync(LisMessageType.TestOrderQuery, "Query2");
        var elapsed3 = (DateTimeOffset.Now - start3).TotalSeconds;

        // Second query should respect the delay from first query
        elapsed3.Should().BeGreaterOrEqualTo(delaySeconds - 1);

        _mockDataLink.Verify(d => d.SendMessageAsync("Query1"), Times.Once);
        _mockDataLink.Verify(d => d.SendMessageAsync("Result1"), Times.Once);
        _mockDataLink.Verify(d => d.SendMessageAsync("Query2"), Times.Once);
    }

    #endregion

    #region Helper Methods

    private AstmConnection CreateConnection(LisChannel lisChannel)
    {
        return new AstmConnection(
            1,
            lisChannel,
            LisConnectionRole.Both,
            _mockTransportConnectionFactory.Object,
            _mockDataLinkFactory.Object,
            _mockDateTime.Object,
            _logger,
            _serviceScopeFactory);
    }

    private static TestOrderReceivedEventArgs ReceiveMessage(AstmConnection connection, IAstmMessage message)
    {
        TestOrderReceivedEventArgs? capturedEventArgs = null;
        connection.TestOrderReceived += (_, args) => capturedEventArgs = args;

        var method = typeof(AstmConnection).GetMethod("OnDataLinkMessageReceived", BindingFlags.NonPublic | BindingFlags.Instance);

        method!.Invoke(connection, [null, message]);

        return capturedEventArgs!;
    }

    private static LisChannel GetListTestChannel()
    {
        var astmSettings = new AstmSettings
        {
            ImportSettings = new AstmImportSettings { SkipImportOfRequestorInformation = false }
        };

        return new LisChannel
        {
            AstmSettings = astmSettings,
            ImportSettings = new LisImportSettings
            {
                CalculatePatientAge = false
            }
        };

    }

    private static void SetCalculatePatientAgeSetting(LisChannel lisChannel, bool enabled)
    {
        lisChannel.ImportSettings.CalculatePatientAge = enabled;
    }

    #endregion
}
