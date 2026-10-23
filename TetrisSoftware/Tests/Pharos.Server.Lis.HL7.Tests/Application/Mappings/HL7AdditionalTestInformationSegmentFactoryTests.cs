using FluentAssertions;
using Pharos.Server.Application.Lis;
using Pharos.Server.Lis.HL7.Application.Mappings;
using Pharos.Server.Lis.HL7.Application.Messages;
using Xunit;

namespace Pharos.Server.Lis.HL7.Tests.Application.Mappings;

public class HL7AdditionalTestInformationSegmentFactoryTests
{
    [Fact]
    public void CreateFromTestOrderAndResult_WithAllResultValues_ShouldCreateSeparateObxSegments()
    {
        // Arrange
        var testOrder = new LisTestOrder
        {
            MethodName = "sIgE",
            TestName = "t3",
            FullTestName = "Dog dander",
            ReportConcentration = true,
            ReportClass = true,
            ReportCutOff = true,
            ReportQuotient = true,
            TestResult = new LisSampleTestResult
            {
                Concentration = "3,41",
                Class = "---",
                CutOff = "Positive",
                Quotient = "8,45",
                Units = "kUA/l",
                MeasurementDateTime = new DateTimeOffset(2026, 7, 15, 14, 31, 4, TimeSpan.Zero)
            }
        };

        // Act
        var segments = HL7AdditionalTestInformationSegmentFactory.CreateFromTestOrderAndResult(
            testOrder,
            firstSequenceNumber: 1,
            forceUniqueLaboratoryTestNames: false,
            isSampleLocallyCreated: false);

        // Assert
        segments.Should().HaveCount(4);
        segments[0].SequenceNumber.Should().Be(1);
        segments[0].ObservationId.Should().Be("CONC-sIgE-t3");
        segments[0].ObservationText.Should().Be("Dog dander (t3)");
        segments[0].ObservationValue.Should().Be("3.41");
        segments[0].Units.Should().Be("kUA/l");
        segments[0].ResultStatus.Should().Be(HL7ObservationResultStatus.Final);
        segments[1].ObservationId.Should().Be("CLASS-sIgE-t3");
        segments[2].ObservationId.Should().Be("CUTOFF-sIgE-t3");
        segments[3].ObservationId.Should().Be("QUOT-sIgE-t3");
        segments[3].ObservationValue.Should().Be("8.45");
    }

    [Fact]
    public void CreateFromTestOrderAndResult_WithPendingResult_ShouldUsePendingStatus()
    {
        // Arrange
        var testOrder = new LisTestOrder
        {
            MethodName = "sIgE",
            TestName = "t3",
            ReportConcentration = true,
            TestResult = new LisSampleTestResult
            {
                Concentration = "1.2",
                Pending = true
            }
        };

        // Act
        var segments = HL7AdditionalTestInformationSegmentFactory.CreateFromTestOrderAndResult(
            testOrder,
            firstSequenceNumber: 3,
            forceUniqueLaboratoryTestNames: true,
            isSampleLocallyCreated: false);

        // Assert
        segments.Should().ContainSingle();
        segments[0].SequenceNumber.Should().Be(3);
        segments[0].ObservationId.Should().Be("CONC-t3");
        segments[0].ObservationText.Should().Be(" (t3)");
        segments[0].ResultStatus.Should().Be(HL7ObservationResultStatus.Pending);
    }

    [Fact]
    public void CreateFromTestOrderAndResult_WithoutConcentrationOrLaboratoryTestName_ShouldUsePrimeObservationTextFormat()
    {
        // Arrange
        var testOrder = new LisTestOrder
        {
            MethodName = "sIgE",
            FullTestName = "Dog dander",
            ReportClass = true,
            TestResult = new LisSampleTestResult
            {
                Class = "2"
            }
        };

        // Act
        var segments = HL7AdditionalTestInformationSegmentFactory.CreateFromTestOrderAndResult(
            testOrder,
            firstSequenceNumber: 2,
            forceUniqueLaboratoryTestNames: false,
            isSampleLocallyCreated: false);

        // Assert
        segments.Should().ContainSingle();
        segments[0].SequenceNumber.Should().Be(2);
        segments[0].ObservationId.Should().Be("CLASS-sIgE");
        segments[0].ObservationText.Should().Be("Dog dander ()");
    }

    [Fact]
    public void CreateFromTestOrderAndResult_WithEnabledReportingAndEmptyPendingResult_ShouldCreateEmptyObxSegments()
    {
        // Arrange
        var testOrder = new LisTestOrder
        {
            MethodName = "sIgE",
            TestName = "t3",
            FullTestName = "Dog dander",
            ReportConcentration = true,
            ReportClass = true,
            ReportCutOff = true,
            ReportQuotient = true,
            TestResult = new LisSampleTestResult
            {
                Pending = true
            }
        };

        // Act
        var segments = HL7AdditionalTestInformationSegmentFactory.CreateFromTestOrderAndResult(
            testOrder,
            firstSequenceNumber: 1,
            forceUniqueLaboratoryTestNames: false,
            isSampleLocallyCreated: false);

        // Assert
        segments.Should().HaveCount(4);
        segments.Select(x => x.ObservationId).Should().Equal("CONC-sIgE-t3", "CLASS-sIgE-t3", "CUTOFF-sIgE-t3", "QUOT-sIgE-t3");
        segments.Should().OnlyContain(x => x.ObservationValue == "");
        segments.Should().OnlyContain(x => x.ResultStatus == HL7ObservationResultStatus.Pending);
    }

    [Fact]
    public void CreateFromTestOrderAndResult_WithDisabledReportingAndResultValues_ShouldSuppressObxSegments()
    {
        // Arrange
        var testOrder = new LisTestOrder
        {
            MethodName = "sIgE",
            TestName = "t3",
            TestResult = new LisSampleTestResult
            {
                Concentration = "3,41",
                Class = "---",
                CutOff = "Positive",
                Quotient = "8,45",
                Units = "kUA/l"
            }
        };

        // Act
        var segments = HL7AdditionalTestInformationSegmentFactory.CreateFromTestOrderAndResult(
            testOrder,
            firstSequenceNumber: 1,
            forceUniqueLaboratoryTestNames: false,
            isSampleLocallyCreated: false);

        // Assert
        segments.Should().BeEmpty();
    }

    [Fact]
    public void CreateFromQualityControlResult_WithConcentration_ShouldCreateFinalConcentrationSegment()
    {
        // Arrange
        var qcTestResult = new LisQualityControlTestResult
        {
            MethodName = "sIgE",
            TestName = "d1",
            Concentration = "2,32",
            Units = "kU/L",
            MeasurementDateTime = new DateTimeOffset(2026, 7, 15, 14, 31, 4, TimeSpan.Zero)
        };

        // Act
        var segment = HL7AdditionalTestInformationSegmentFactory.CreateFromQualityControlResult(qcTestResult);

        // Assert
        segment.SequenceNumber.Should().Be(1);
        segment.ObservationId.Should().Be("CONC-sIgE-d1");
        segment.ObservationText.Should().Be("Concentration");
        segment.ObservationValue.Should().Be("2.32");
        segment.Units.Should().Be("kU/L");
        segment.ResultStatus.Should().Be(HL7ObservationResultStatus.Final);
        segment.ObservationDateTime.Should().Be(qcTestResult.MeasurementDateTime);
    }

    [Fact]
    public void CreateFromQualityControlResult_WithoutConcentration_ShouldCreateCanceledSegment()
    {
        // Arrange
        var qcTestResult = new LisQualityControlTestResult
        {
            MethodName = "sIgE",
            TestName = "d1"
        };

        // Act
        var segment = HL7AdditionalTestInformationSegmentFactory.CreateFromQualityControlResult(qcTestResult);

        // Assert
        segment.ObservationValue.Should().BeEmpty();
        segment.ResultStatus.Should().Be(HL7ObservationResultStatus.Canceled);
    }
}
