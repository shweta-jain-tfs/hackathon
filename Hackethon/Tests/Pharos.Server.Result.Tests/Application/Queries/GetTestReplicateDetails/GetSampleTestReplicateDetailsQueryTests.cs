using Moq.EntityFrameworkCore;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Queries.GetTestReplicateDetails;

public class GetSampleTestReplicateDetailsQueryTests
{
    [Fact]
    public async Task Handle_MapsReplicateDetailsAndAllFactorBranches()
    {
        var replicate1Measurement = new DateTimeOffset(2026, 6, 25, 9, 5, 0, TimeSpan.Zero);
        var replicate2Measurement = new DateTimeOffset(2026, 6, 25, 9, 10, 0, TimeSpan.Zero);

        var replicate1 = new SampleTestReplicate
        {
            Id = 10,
            ReplicateNumber = 2,
            MeasurementDateTime = replicate1Measurement,
            TestReplicateErrors =
            [
                new TestReplicateError { ErrorCode = 1001 },
                new TestReplicateError { ErrorCode = 2002 }
            ],
            TestReplicateReagents =
            [
                CreateReagent(ArticleType.EliaCalibratorWellCarrier, "CAL-BARCODE", "CAL-UDI", "LOT-CAL", true),
                CreateReagent(ArticleType.ConjugateCartridge, "CONJ-BARCODE", "CONJ-UDI", "LOT-CONJ", false)
            ]
        };

        var replicate2 = new SampleTestReplicate
        {
            Id = 20,
            ReplicateNumber = 1,
            MeasurementDateTime = replicate2Measurement,
            TestReplicateErrors =
            [
                new TestReplicateError { ErrorCode = 3003 }
            ],
            TestReplicateReagents =
            [
                CreateReagent(ArticleType.EliaWellCarrier, "ELIA-BARCODE", "ELIA-UDI", "LOT-ELIA", false)
            ]
        };

        var nonRequestedReplicate = new SampleTestReplicate
        {
            Id = 999,
            MeasurementDateTime = replicate2Measurement,
            TestReplicateErrors = [],
            TestReplicateReagents = []
        };

        var dbContextMock = new Mock<IResultDbContext>();
        dbContextMock
            .Setup(db => db.TestReplicates)
            .ReturnsDbSet(new List<TestReplicate> { replicate1, replicate2, nonRequestedReplicate });

        var barcodeServiceMock = new Mock<IReagentBarcodeService>();
        var calibratorExpiry = new DateTimeOffset(2026, 12, 31, 0, 0, 0, TimeSpan.Zero);
        var conjugateExpiry = new DateTimeOffset(2027, 1, 15, 0, 0, 0, TimeSpan.Zero);
        var eliaWellExpiry = new DateTimeOffset(2027, 2, 1, 0, 0, 0, TimeSpan.Zero);

        barcodeServiceMock
            .Setup(x => x.GetReagentLotInformation(ArticleType.EliaCalibratorWellCarrier, "CAL-BARCODE", "CAL-UDI"))
            .Returns(new EliaCalibratorWellLotInformation
            {
                LotNumber = "LOT-CAL",
                DisplayLotNumber = "LOT-CAL",
                ExpiryDate = calibratorExpiry,
                CorrectionFactorText = "0.97"
            });

        barcodeServiceMock
            .Setup(x => x.GetReagentLotInformation(ArticleType.ConjugateCartridge, "CONJ-BARCODE", "CONJ-UDI"))
            .Returns(new LotInformation
            {
                LotNumber = "LOT-CONJ",
                DisplayLotNumber = "LOT-CONJ",
                ExpiryDate = conjugateExpiry
            });

        barcodeServiceMock
            .Setup(x => x.GetReagentLotInformation(ArticleType.EliaWellCarrier, "ELIA-BARCODE", "ELIA-UDI"))
            .Returns(new EliaWellLotInformation
            {
                LotNumber = "LOT-ELIA",
                DisplayLotNumber = "LOT-ELIA",
                ExpiryDate = eliaWellExpiry,
                CorrectionFactor = 5.0,
                CorrectionFactorText = "5.00"
            });

        var sut = new GetTestReplicateDetailsQueryHandler(dbContextMock.Object, barcodeServiceMock.Object);

        var result = await sut.Handle(new GetTestReplicateDetailsQuery([10, 20]), TestContext.Current.CancellationToken);

        result.Should().HaveCount(2);
        result.Select(x => x.Id).Should().BeEquivalentTo([10, 20]);

        var resultReplicate1 = result.Single(x => x.Id == 10);
        resultReplicate1.ReplicateNumber.Should().Be(2);
        resultReplicate1.MeasurementDateTime.Should().Be(replicate1Measurement);
        resultReplicate1.Errors.Select(e => e.ErrorCode).Should().BeEquivalentTo([1001, 2002]);
        resultReplicate1.Reagents.Should().HaveCount(2);

        var calibratorReagent = resultReplicate1.Reagents.Single(x => x.ReagentType == ArticleType.EliaCalibratorWellCarrier);
        calibratorReagent.LotNumber.Should().Be("LOT-CAL");
        calibratorReagent.Expired.Should().BeTrue();
        calibratorReagent.ExpiryDate.Should().Be(calibratorExpiry);
        calibratorReagent.Factor.Should().Be("0.97");

        var conjugateReagent = resultReplicate1.Reagents.Single(x => x.ReagentType == ArticleType.ConjugateCartridge);
        conjugateReagent.ExpiryDate.Should().Be(conjugateExpiry);
        conjugateReagent.Factor.Should().BeNullOrEmpty();

        var resultReplicate2 = result.Single(x => x.Id == 20);
        resultReplicate2.ReplicateNumber.Should().Be(1);
        resultReplicate2.MeasurementDateTime.Should().Be(replicate2Measurement);
        resultReplicate2.Errors.Select(e => e.ErrorCode).Should().BeEquivalentTo([3003]);
        resultReplicate2.Reagents.Should().HaveCount(1);

        var eliaWellReagent = resultReplicate2.Reagents[0];
        eliaWellReagent.ReagentType.Should().Be(ArticleType.EliaWellCarrier);
        eliaWellReagent.ExpiryDate.Should().Be(eliaWellExpiry);
        eliaWellReagent.Factor.Should().Be("5.00");

        barcodeServiceMock.Verify(x => x.GetReagentLotInformation(ArticleType.EliaCalibratorWellCarrier, "CAL-BARCODE", "CAL-UDI"), Times.Once);
        barcodeServiceMock.Verify(x => x.GetReagentLotInformation(ArticleType.ConjugateCartridge, "CONJ-BARCODE", "CONJ-UDI"), Times.Once);
        barcodeServiceMock.Verify(x => x.GetReagentLotInformation(ArticleType.EliaWellCarrier, "ELIA-BARCODE", "ELIA-UDI"), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenLotInformationIsMissing_LeavesFactorUnsetAndExpiryDateDefault()
    {
        var replicate = new SampleTestReplicate
        {
            Id = 42,
            MeasurementDateTime = new DateTimeOffset(2026, 6, 25, 9, 5, 0, TimeSpan.Zero),
            TestReplicateErrors = [],
            TestReplicateReagents =
            [
                CreateReagent(ArticleType.EliaWellCarrier, "ELIA-MISSING", "UDI-MISSING", "LOT-MISSING", false)
            ]
        };

        var dbContextMock = new Mock<IResultDbContext>();
        dbContextMock
            .Setup(db => db.TestReplicates)
            .ReturnsDbSet(new List<TestReplicate> { replicate });

        var barcodeServiceMock = new Mock<IReagentBarcodeService>();
        barcodeServiceMock
            .Setup(x => x.GetReagentLotInformation(ArticleType.EliaWellCarrier, "ELIA-MISSING", "UDI-MISSING"))
            .Returns((LotInformation?)null);

        var sut = new GetTestReplicateDetailsQueryHandler(dbContextMock.Object, barcodeServiceMock.Object);

        var result = await sut.Handle(new GetTestReplicateDetailsQuery([42]), TestContext.Current.CancellationToken);

        result.Should().HaveCount(1);
        result[0].Reagents.Should().HaveCount(1);
        result[0].Reagents[0].Factor.Should().BeNullOrEmpty();
        result[0].Reagents[0].ExpiryDate.Should().Be(default);
    }

    private static TestReplicateReagent CreateReagent(ArticleType reagentType, string barcode, string udi, string lotNumber, bool expired)
    {
        return new TestReplicateReagent
        {
            ReagentType = reagentType,
            Barcode = barcode,
            Udi = udi,
            LotNumber = lotNumber,
            Expired = expired,
            SerialNumber = string.Empty
        };
    }
}
