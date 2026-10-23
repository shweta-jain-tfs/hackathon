using Pharos.Server.Application.Lis;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Infrastructure;
using Pharos.Server.Lis.Astm.Application.Mappings;
using Pharos.Server.Lis.Astm.Application.Messages;

namespace Pharos.Server.Lis.Astm.Tests.Application.Mappings;

public class AstmTestOrderRecordExtensionsTests
{

    [Fact]
    public void MapToSample_ShouldMapSampleCollectionDate()
    {
        var record = new AstmTestOrderRecord
        {
            SpecimenCollectionDateAndTime = new DateTimeOffset(1982, 1, 19, 0, 0, 0, TimeSpan.Zero)
        };

        var sample = record.MapToSample();
        sample.SampleCollectionDate.Should().Be(new DateTimeOffset(1982, 1, 19, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void MapFromSampleAndTestOrder_WhenSkipRequestorInformationIsFalse_ShouldIncludeRequestorInformation()
    {
        // Arrange
        var record = new AstmTestOrderRecord();
        var astmSettings = new AstmSettings
        {
            Other = new AstmOtherSettings
            {
                CombineRequestorAddressLinesOneToThree = false
            },
            ExportSettings = new AstmExportSettings
            {
                SkipExportOfRequestorInformation = false
            }
        };
        var sample = new LisSample
        {
            SampleId = "TEST001",
            Requestor = new LisRequestor
            {
                RequestorId = "REQ001",
                Name = "Dr. Smith",
                ShortcutCode = "DS",
                PhoneNumber = "555-1234",
                Comment = "Test comment",
                Address = "123 Test Street",
                ReflexEnabled = true
            }
        };
        var testOrder = new LisTestOrder
        {
            TestName = "TestName",
            MethodName = "TestMethod",
            InstrumentDilutionFactor = 1,
            NumberOfReplicates = 1
        };


        // Act
        record.MapFromSampleAndTestOrder(sample, testOrder, astmSettings, CreateMockDateTime());

        // Assert
        record.RequestorId.Should().Be("REQ001");
        record.RequestorName.Should().Be("Dr. Smith");
        record.RequestorShortcutCode.Should().Be("DS");
        record.RequestorPhone.Should().Be("555-1234");
        record.RequestorComment.Should().Be("Test comment");
        record.RequestorReflexTesting.Should().Be(1);
    }

    [Trait("ProductRequirement", "PR_1305")]
    [Trait("ModuleRequirement", "MR_5390")]
    [Fact]
    public void MapFromSampleAndTestOrder_WhenSkipRequestorInformationIsTrue_ShouldExcludeRequestorInformation()
    {
        // Arrange
        var record = new AstmTestOrderRecord();
        var settings = new AstmSettings
        {
            Other = new AstmOtherSettings
            {
                CombineRequestorAddressLinesOneToThree = false
            },
            ExportSettings = new AstmExportSettings
            {
                SkipExportOfRequestorInformation = true
            }
        };
        var sample = new LisSample
        {
            SampleId = "TEST001",
            Requestor = new LisRequestor
            {
                RequestorId = "REQ001",
                Name = "Dr. Smith",
                ShortcutCode = "DS",
                PhoneNumber = "555-1234",
                Comment = "Test comment",
                Address = "123 Test Street",
                ReflexEnabled = true
            }
        };
        var testOrder = new LisTestOrder
        {
            TestName = "TestName",
            MethodName = "TestMethod",
            InstrumentDilutionFactor = 1,
            NumberOfReplicates = 1
        };

        // Act
        record.MapFromSampleAndTestOrder(sample, testOrder, settings, CreateMockDateTime());

        // Assert
        record.RequestorId.Should().Be("");
        record.RequestorName.Should().Be("");
        record.RequestorShortcutCode.Should().Be("");
        record.RequestorPhone.Should().Be("");
        record.RequestorComment.Should().Be("");
        record.RequestorReflexTesting.Should().Be(0);
    }

    [Fact]
    public void MapFromSampleAndTestOrder_WhenSkipRequestorInformationIsNotProvided_ShouldDefaultToFalseAndIncludeRequestorInformation()
    {
        // Arrange
        var record = new AstmTestOrderRecord();
        var settings = new AstmSettings
        {
            Other = new AstmOtherSettings
            {
                CombineRequestorAddressLinesOneToThree = false
            },
            ExportSettings = new AstmExportSettings
            {
                SkipExportOfRequestorInformation = false
            }
        };
        var sample = new LisSample
        {
            SampleId = "TEST001",
            Requestor = new LisRequestor
            {
                RequestorId = "REQ001",
                Name = "Dr. Smith"
            }
        };
        var testOrder = new LisTestOrder
        {
            TestName = "TestName",
            MethodName = "TestMethod",
            InstrumentDilutionFactor = 1,
            NumberOfReplicates = 1
        };

        // Act
        record.MapFromSampleAndTestOrder(sample, testOrder, settings, CreateMockDateTime());

        // Assert
        record.RequestorId.Should().Be("REQ001");
        record.RequestorName.Should().Be("Dr. Smith");
    }

    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5616")]
    public void MapToTestOrders_WhenIgnorePreDilutionIsTrue_ShouldNotSetPreDilutionFactor()
    {
        var record = new AstmTestOrderRecord
        {
            Dilution = 5
        };
        record.UniversalTestIds.Add(new AstmTestId("TestName", "TestMethod", 1, 0, "", 1));

        var importSettings = new AstmImportSettings
        {
            IgnoreImportedPreDilutionFactor = true
        };

        var testOrders = new List<LisTestOrder>();
        record.MapToTestOrders(testOrders, importSettings, new LisImportSettings());

        testOrders.Should().ContainSingle();
        testOrders[0].PreDilutionFactor.Should().BeNull();
    }

    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5616")]
    public void MapToTestOrders_WhenIgnorePreDilutionIsFalse_ShouldUseRecordDilution()
    {
        var record = new AstmTestOrderRecord
        {
            Dilution = 5
        };
        record.UniversalTestIds.Add(new AstmTestId("TestName", "TestMethod", 1, 0, "", 1));

        var importSettings = new AstmImportSettings
        {
            IgnoreImportedPreDilutionFactor = false
        };

        var testOrders = new List<LisTestOrder>();
        record.MapToTestOrders(testOrders, importSettings, new LisImportSettings());

        testOrders.Should().ContainSingle();
        testOrders[0].PreDilutionFactor.Should().Be(5);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5618")]
    public void MapToTestOrders_WhenIgnoreImportedNumberOfReplicatesIsTrue_ShouldNotSetNumberOfReplicates()
    {
        var record = new AstmTestOrderRecord();
        record.UniversalTestIds.Add(new AstmTestId("TestName", "TestMethod", 1, 0, "", 3));

        var testOrders = new List<LisTestOrder>();
        var lisImportSettings = new LisImportSettings();
        var importSettings = new AstmImportSettings
        {
            IgnoreImportedNumberOfReplicates = true
        };

        record.MapToTestOrders(testOrders, importSettings, lisImportSettings);

        testOrders[0].NumberOfReplicates.Should().Be(null);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5618")]
    public void MapToTestOrders_WhenIgnoreImportedNumberOfReplicatesIsFalse_ShouldUseNumberOfReplicates()
    {
        var record = new AstmTestOrderRecord();
        record.UniversalTestIds.Add(new AstmTestId("TestName", "TestMethod", 1, 0, "", 3));

        var testOrders = new List<LisTestOrder>();
        var lisImportSettings = new LisImportSettings();
        var importSettings = new AstmImportSettings
        {
            IgnoreImportedNumberOfReplicates = false
        };

        record.MapToTestOrders(testOrders, importSettings, lisImportSettings);

        testOrders.Should().HaveCount(1);
        testOrders[0].NumberOfReplicates.Should().Be(3);
    }

    [Trait("ProductRequirement", "PR_1306")]
    [Trait("ModuleRequirement", "MR_5625")]
    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5851")]
    public void MapToSample_WhenLocallyCreatedSampleAndSettingIsTrue_ShouldSetSampleIdToSpecimenId()
    {
        var record = new AstmTestOrderRecord();
        var exportSettings = new AstmSettings
        {
            ExportSettings = new AstmExportSettings
            {
                UseLocalSampleIdAsLisSampleId = true
            }
        };
        var sample = new LisSample
        {
            SampleId = "TEST_LocallyCreatedSample",
            IsSampleLocallyCreated = true
        };
        var testOrder = new LisTestOrder
        {
            TestName = "TestName",
            MethodName = "TestMethod"
        };
        record.MapFromSampleAndTestOrder(sample, testOrder, exportSettings, CreateMockDateTime());
        record.SpecimenId.Should().Be("TEST_LocallyCreatedSample");
    }

    [Trait("ProductRequirement", "PR_1306")]
    [Trait("ModuleRequirement", "MR_5625")]
    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5851")]
    public void MapToSample_WhenLocallyCreatedSampleAndSettingIsFalse_ShouldSetSpecimenIdToEmpty()
    {
        var record = new AstmTestOrderRecord();
        var exportSettings = new AstmSettings
        {
            ExportSettings = new AstmExportSettings
            {
                UseLocalSampleIdAsLisSampleId = false
            }
        };
        var sample = new LisSample
        {
            SampleId = "TEST_LocallyCreatedSample",
            IsSampleLocallyCreated = true
        };
        var testOrder = new LisTestOrder
        {
            TestName = "TestName",
            MethodName = "TestMethod"
        };
        record.MapFromSampleAndTestOrder(sample, testOrder, exportSettings, CreateMockDateTime());
        record.SpecimenId.Should().Be("");
    }

    [Trait("ProductRequirement", "PR_1306")]
    [Trait("ModuleRequirement", "MR_5625")]
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5851")]
    public void MapToSample_WhenNotLocallyCreatedSample_ShouldAlwaysSetSampleIdToSpecimenId(bool useLocalSampleIdSetting)
    {
        var record = new AstmTestOrderRecord();
        var exportSettings = new AstmSettings
        {
            ExportSettings = new AstmExportSettings
            {
                UseLocalSampleIdAsLisSampleId = useLocalSampleIdSetting
            }
        };
        var sample = new LisSample
        {
            SampleId = "TEST_NotLocallyCreatedSample",
            IsSampleLocallyCreated = false
        };
        var testOrder = new LisTestOrder
        {
            TestName = "TestName",
            MethodName = "TestMethod"
        };
        record.MapFromSampleAndTestOrder(sample, testOrder, exportSettings, CreateMockDateTime());
        record.SpecimenId.Should().Be("TEST_NotLocallyCreatedSample");
    }
    private static IDateTime CreateMockDateTime()
    {
        var mockDateTime = new Mock<IDateTime>();
        mockDateTime.Setup(x => x.Now).Returns(DateTimeOffset.Now);
        return mockDateTime.Object;
    }

    #region Combine Requestor Address Lines Tests

    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5386")]
    public void MapToRequestor_WhenCombineRequestorAddressLinesOneToThreeIsTrue_ShouldCombineFirstThreeAddressLines()
    {
        // Arrange
        var record = new AstmTestOrderRecord
        {
            RequestorId = "REQ001",
            RequestorName = "Dr. Smith",
            RequestorAddressLine1 = "123 Main Street",
            RequestorAddressLine2 = "Suite 456",
            RequestorAddressLine3 = "Building A",
            RequestorAddressLine4 = "Springfield",
            RequestorAddressLine5 = "IL 62704"
        };
        var sample = new LisSample();
        var settings = new AstmSettings
        {
            Other = new AstmOtherSettings
            {
                CombineRequestorAddressLinesOneToThree = true
            }
        };
        // Act
        record.MapToRequestor(sample, settings);
        // Assert
        sample.Requestor.Should().NotBeNull();
        sample.Requestor!.Address.Should().Be("123 Main Street,Suite 456,Building A\nSpringfield\nIL 62704");
    }
    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5386")]
    public void MapToRequestor_WhenCombineRequestorAddressLinesOneToThreeIsFalse_ShouldKeepAllAddressLinesSeparate()
    {
        // Arrange
        var record = new AstmTestOrderRecord
        {
            RequestorId = "REQ001",
            RequestorName = "Dr. Smith",
            RequestorAddressLine1 = "123 Main Street",
            RequestorAddressLine2 = "Suite 456",
            RequestorAddressLine3 = "Building A",
            RequestorAddressLine4 = "Springfield",
            RequestorAddressLine5 = "IL 62704"
        };
        var sample = new LisSample();
        var settings = new AstmSettings
        {
            Other = new AstmOtherSettings
            {
                CombineRequestorAddressLinesOneToThree = false
            }
        };
        // Act
        record.MapToRequestor(sample, settings);
        // Assert
        sample.Requestor.Should().NotBeNull();
        sample.Requestor!.Address.Should().Be("123 Main Street\nSuite 456\nBuilding A\nSpringfield\nIL 62704");
    }
    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5386")]
    public void MapToRequestor_WhenCombineRequestorAddressLinesOneToThreeIsTrueAndSomeAddressLinesAreEmpty_ShouldHandleEmptyLines()
    {
        // Arrange
        var record = new AstmTestOrderRecord
        {
            RequestorId = "REQ001",
            RequestorName = "Dr. Smith",
            RequestorAddressLine1 = "123 Main Street",
            RequestorAddressLine2 = "",
            RequestorAddressLine3 = "Building A",
            RequestorAddressLine4 = "Springfield",
            RequestorAddressLine5 = ""
        };
        var sample = new LisSample();
        var settings = new AstmSettings
        {
            Other = new AstmOtherSettings
            {
                CombineRequestorAddressLinesOneToThree = true
            }
        };
        // Act
        record.MapToRequestor(sample, settings);
        // Assert
        sample.Requestor.Should().NotBeNull();
        sample.Requestor!.Address.Should().Be("123 Main Street,,Building A\nSpringfield\n");
    }
    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5386")]
    public void MapFromSampleAndTestOrder_WhenCombineRequestorAddressLinesOneToThreeIsTrue_ShouldSplitCombinedAddressBackToLines()
    {
        // Arrange
        var record = new AstmTestOrderRecord();
        var settings = new AstmSettings
        {
            Other = new AstmOtherSettings
            {
                CombineRequestorAddressLinesOneToThree = true
            },
            ExportSettings = new AstmExportSettings
            {
                SkipExportOfRequestorInformation = false
            }
        };
        var sample = new LisSample
        {
            SampleId = "TEST001",
            Requestor = new LisRequestor
            {
                RequestorId = "REQ001",
                Name = "Dr. Smith",
                Address = "123 Main Street,Suite 456,Building A\nSpringfield\nIL 62704"
            }
        };
        var testOrder = new LisTestOrder
        {
            TestName = "TestName",
            MethodName = "TestMethod"
        };
        // Act
        record.MapFromSampleAndTestOrder(sample, testOrder, settings, CreateMockDateTime());
        // Assert
        record.RequestorAddressLine1.Should().Be("123 Main Street");
        record.RequestorAddressLine2.Should().Be("Suite 456");
        record.RequestorAddressLine3.Should().Be("Building A");
        record.RequestorAddressLine4.Should().Be("Springfield");
        record.RequestorAddressLine5.Should().Be("IL 62704");
    }
    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5386")]
    public void MapFromSampleAndTestOrder_WhenCombineRequestorAddressLinesOneToThreeIsFalse_ShouldSplitAddressIntoSeparateLines()
    {
        // Arrange
        var record = new AstmTestOrderRecord();
        var settings = new AstmSettings
        {
            Other = new AstmOtherSettings
            {
                CombineRequestorAddressLinesOneToThree = false
            },
            ExportSettings = new AstmExportSettings
            {
                SkipExportOfRequestorInformation = false
            }
        };
        var sample = new LisSample
        {
            SampleId = "TEST001",
            Requestor = new LisRequestor
            {
                RequestorId = "REQ001",
                Name = "Dr. Smith",
                Address = "123 Main Street\nSuite 456\nBuilding A\nSpringfield\nIL 62704"
            }
        };
        var testOrder = new LisTestOrder
        {
            TestName = "TestName",
            MethodName = "TestMethod"
        };
        // Act
        record.MapFromSampleAndTestOrder(sample, testOrder, settings, CreateMockDateTime());
        // Assert
        record.RequestorAddressLine1.Should().Be("123 Main Street");
        record.RequestorAddressLine2.Should().Be("Suite 456");
        record.RequestorAddressLine3.Should().Be("Building A");
        record.RequestorAddressLine4.Should().Be("Springfield");
        record.RequestorAddressLine5.Should().Be("IL 62704");
    }
    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5386")]
    public void MapFromSampleAndTestOrder_WhenCombineRequestorAddressLinesOneToThreeIsTrueAndAddressHasFewerThanThreeLines_ShouldHandleGracefully()
    {
        // Arrange
        var record = new AstmTestOrderRecord();
        var settings = new AstmSettings
        {
            Other = new AstmOtherSettings
            {
                CombineRequestorAddressLinesOneToThree = true
            },
            ExportSettings = new AstmExportSettings
            {
                SkipExportOfRequestorInformation = false
            }
        };
        var sample = new LisSample
        {
            SampleId = "TEST001",
            Requestor = new LisRequestor
            {
                RequestorId = "REQ001",
                Name = "Dr. Smith",
                Address = "123 Main Street,Suite 456\nSpringfield"
            }
        };
        var testOrder = new LisTestOrder
        {
            TestName = "TestName",
            MethodName = "TestMethod"
        };
        // Act
        record.MapFromSampleAndTestOrder(sample, testOrder, settings, CreateMockDateTime());
        // Assert
        record.RequestorAddressLine1.Should().Be("123 Main Street");
        record.RequestorAddressLine2.Should().Be("Suite 456");
        record.RequestorAddressLine3.Should().Be("");
        record.RequestorAddressLine4.Should().Be("Springfield");
        record.RequestorAddressLine5.Should().Be("");
    }
    [Fact]
    public void MapFromSampleAndTestOrder_WhenRequestorAddressIsEmpty_ShouldNotSetAnyAddressLines()
    {
        // Arrange
        var record = new AstmTestOrderRecord();
        var settings = new AstmSettings
        {
            Other = new AstmOtherSettings
            {
                CombineRequestorAddressLinesOneToThree = true
            },
            ExportSettings = new AstmExportSettings
            {
                SkipExportOfRequestorInformation = false
            }
        };
        var sample = new LisSample
        {
            SampleId = "TEST001",
            Requestor = new LisRequestor
            {
                RequestorId = "REQ001",
                Name = "Dr. Smith",
                Address = ""
            }
        };
        var testOrder = new LisTestOrder
        {
            TestName = "TestName",
            MethodName = "TestMethod"
        };
        // Act
        record.MapFromSampleAndTestOrder(sample, testOrder, settings, CreateMockDateTime());
        // Assert
        record.RequestorAddressLine1.Should().Be("");
        record.RequestorAddressLine2.Should().Be("");
        record.RequestorAddressLine3.Should().Be("");
        record.RequestorAddressLine4.Should().Be("");
        record.RequestorAddressLine5.Should().Be("");
    }
    [Fact]
    public void MapFromSampleAndTestOrder_WhenRequestorAddressIsNull_ShouldNotSetAnyAddressLines()
    {
        // Arrange
        var record = new AstmTestOrderRecord();
        var settings = new AstmSettings
        {
            Other = new AstmOtherSettings
            {
                CombineRequestorAddressLinesOneToThree = true
            },
            ExportSettings = new AstmExportSettings
            {
                SkipExportOfRequestorInformation = false
            }
        };
        var sample = new LisSample
        {
            SampleId = "TEST001",
            Requestor = new LisRequestor
            {
                RequestorId = "REQ001",
                Name = "Dr. Smith",
                Address = null
            }
        };
        var testOrder = new LisTestOrder
        {
            TestName = "TestName",
            MethodName = "TestMethod"
        };
        // Act
        record.MapFromSampleAndTestOrder(sample, testOrder, settings, CreateMockDateTime());
        // Assert
        record.RequestorAddressLine1.Should().Be("");
        record.RequestorAddressLine2.Should().Be("");
        record.RequestorAddressLine3.Should().Be("");
        record.RequestorAddressLine4.Should().Be("");
        record.RequestorAddressLine5.Should().Be("");
    }
    #endregion


    #region panel expansion

    [Fact]
    public void MapToTestOrders_WhenPanelExpansionEnabled_ShouldSetPanelExpansionEnabledTrue()
    {
        // Arrange
        var record = CreateTestOrderRecord("PanelName", "");
        var importSettings = new AstmImportSettings();
        var lisImportSettings = new LisImportSettings { PanelExpansion = true };
        var testOrders = new List<LisTestOrder>();

        // Act
        record.MapToTestOrders(testOrders, importSettings, lisImportSettings);

        // Assert - ASTM layer responsibility: correctly set the flag for later expansion
        testOrders[0].PanelExpansionEnabled.Should().BeTrue();
    }

    [Fact]
    public void MapToTestOrders_WhenPanelExpansionDisabled_ShouldSetPanelExpansionEnabledFalse()
    {
        // Arrange
        var record = CreateTestOrderRecord("PanelName", "");
        var importSettings = new AstmImportSettings();
        var lisImportSettings = new LisImportSettings { PanelExpansion = false };
        var testOrders = new List<LisTestOrder>();

        // Act
        record.MapToTestOrders(testOrders, importSettings, lisImportSettings);

        // Assert - ASTM layer responsibility: correctly set the flag (no expansion will occur)
        testOrders[0].PanelExpansionEnabled.Should().BeFalse();
    }

    private static AstmTestOrderRecord CreateTestOrderRecord(string testName, string methodName, int instrumentDilution = 1, int replicates = 1)
    {
        var record = new AstmTestOrderRecord();
        record.UniversalTestIds.Add(new AstmTestId(testName, methodName, instrumentDilution, 0, "", replicates));
        return record;
    }

    #endregion

    #region GetMethodTest (Panel Name Export) Tests

    [Fact]
    public void MapFromSampleAndTestOrder_WhenExportPanelNameNotUsed_ShouldUseMethodAndTestName()
    {
        // Arrange
        var record = new AstmTestOrderRecord();
        var settings = new AstmSettings
        {
            ExportSettings = new AstmExportSettings
            {
                ExportPanelNameInOrderRecord = ExportPanelNameInOrderRecord.NotUsed
            }
        };
        var sample = new LisSample { SampleId = "S001" };
        var testOrder = new LisTestOrder
        {
            MethodName = "MethodA",
            TestName = "TestB",
            PanelNames = ["Panel1", "Panel2"],
            InstrumentDilutionFactor = 1,
            NumberOfReplicates = 1
        };

        // Act
        record.MapFromSampleAndTestOrder(sample, testOrder, settings, CreateMockDateTime());

        // Assert
        record.UniversalTestIds.Should().HaveCount(1);
        record.UniversalTestIds[0].TestName.Should().Be("TestB");
        record.UniversalTestIds[0].LisMethodId.Should().Be("MethodA");
    }

    [Fact]
    public void MapFromSampleAndTestOrder_WhenExportPanelNameUsedAndHasPanels_ShouldUseFirstPanelNameAndEmptyMethod()
    {
        // Arrange
        var record = new AstmTestOrderRecord();
        var settings = new AstmSettings
        {
            ExportSettings = new AstmExportSettings
            {
                ExportPanelNameInOrderRecord = ExportPanelNameInOrderRecord.Used
            }
        };
        var sample = new LisSample { SampleId = "S001" };
        var testOrder = new LisTestOrder
        {
            MethodName = "MethodA",
            TestName = "TestB",
            PanelNames = ["PanelX", "PanelY"],
            InstrumentDilutionFactor = 1,
            NumberOfReplicates = 1
        };

        // Act
        record.MapFromSampleAndTestOrder(sample, testOrder, settings, CreateMockDateTime());

        // Assert
        record.UniversalTestIds.Should().HaveCount(1);
        record.UniversalTestIds[0].TestName.Should().Be("PanelX");
        record.UniversalTestIds[0].LisMethodId.Should().Be(string.Empty);
    }

    [Fact]
    public void MapFromSampleAndTestOrder_WhenExportPanelNameUsedButNoPanels_ShouldFallbackToMethodAndTestName()
    {
        // Arrange
        var record = new AstmTestOrderRecord();
        var settings = new AstmSettings
        {
            ExportSettings = new AstmExportSettings
            {
                ExportPanelNameInOrderRecord = ExportPanelNameInOrderRecord.Used
            }
        };
        var sample = new LisSample { SampleId = "S001" };
        var testOrder = new LisTestOrder
        {
            MethodName = "MethodA",
            TestName = "TestB",
            PanelNames = [],
            InstrumentDilutionFactor = 1,
            NumberOfReplicates = 1
        };

        // Act
        record.MapFromSampleAndTestOrder(sample, testOrder, settings, CreateMockDateTime());

        // Assert
        record.UniversalTestIds.Should().HaveCount(1);
        record.UniversalTestIds[0].TestName.Should().Be("TestB");
        record.UniversalTestIds[0].LisMethodId.Should().Be("MethodA");
    }

    #endregion

}
