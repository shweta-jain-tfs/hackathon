using MediatR;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Localization;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Commands.MethodQualityControls;

public record UpdateLabQualityControlCommand(
    int Id,
    string IdOnBarcode,
    int BarcodeFormatId,
    int TubeTypeId,
    DateTimeOffset QcDetailsQueriedAt,
List<LabQualityControlUpdateTest> Tests) : IRequest;

public record LabQualityControlUpdateTest(
    int Id,
    int Replicates,
    double ReplicateCvLimit,
    int PreDilutionFactor,
    int InstrumentDilutionFactor,
    bool ApplyAcceptanceRange,
    bool UseAcceptanceRange,
    double MaxCv,
    int NumberOfSd,
    double? UpperLimit,
    double? LowerLimit,
    bool UseLotQualification,
    int NumberOfTests,
    int TestId,
    QualityControlType QualityControlType);

internal class UpdateLabQualityControlCommandHandler : IRequestHandler<UpdateLabQualityControlCommand>
{
    private readonly IParametersDbContext _context;
    private readonly IDilutionValidator _dilutionValidator;

    public UpdateLabQualityControlCommandHandler(IParametersDbContext context, IDilutionValidator dilutionValidator)
    {
        _context = context;
        _dilutionValidator = dilutionValidator;
    }

    public async Task Handle(UpdateLabQualityControlCommand request, CancellationToken cancellationToken)
    {
        var qc = await _context.LabQualityControls.Include(x => x.Tests).FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (qc == null)
        {
            throw new ObjectGoneException(Translations.ObjectGone);
        }

        qc.TubeTypeId = request.TubeTypeId;
        qc.IdOnBarcode = request.IdOnBarcode;
        qc.BarcodeFormatId = request.BarcodeFormatId == 0 ? null : request.BarcodeFormatId;

        foreach (var updatedTest in request.Tests)
        {
            await _dilutionValidator.Validate(updatedTest.TestId, updatedTest.PreDilutionFactor, updatedTest.InstrumentDilutionFactor);

            if (updatedTest.Id == 0)
            {
                var newTest = new LabQualityControlTest
                {
                    Enabled = false,
                    //Set valid default values (may be updated in the map values method)
                    MaxCv = 1,
                    NumberOfSd = 1,
                    Schedule = QualityControlSchedule.Default()
                };
                MapValues(newTest, updatedTest);
                newTest.TestId = updatedTest.TestId;

                qc.Tests.Add(newTest);
            }
            else
            {
                var existingTest = qc.Tests.OfType<LabQualityControlTest>().FirstOrDefault(x => x.Id == updatedTest.Id);
                if (existingTest == null)
                {
                    throw new ConflictException(Translations.ConflictingEdits);
                }
                MapValues(existingTest, updatedTest);
            }
        }

        var updatedTestIds = request.Tests.Select(x => x.Id);
        var existingTestIds = qc.Tests.Select(x => x.Id);
        var testsToRemove = existingTestIds.Except(updatedTestIds).ToList();

        foreach (var testId in testsToRemove)
        {
            var testToRemove = qc.Tests.FirstOrDefault(x => x.Id == testId);
            if (testToRemove != null && ((LabQualityControlTest)testToRemove).Created < request.QcDetailsQueriedAt) // keep tests that were added after the details were queried
            {
                var scheduleEntity = await _context.QualityControlSchedules.FirstOrDefaultAsync(x => x.QualityControlTestId == testToRemove.Id, cancellationToken);
                if (scheduleEntity != null)
                {
                    _context.QualityControlSchedules.Remove(scheduleEntity);
                }
                qc.Tests.Remove(testToRemove);
            }
        }

        qc.AddDomainEvent(new QualityControlScheduleUpdatedEvent());

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static void MapValues(LabQualityControlTest entity, LabQualityControlUpdateTest updatedTest)
    {
        entity.Replicates = updatedTest.Replicates;
        entity.ReplicateCvLimit = updatedTest.ReplicateCvLimit;
        entity.PreDilutionFactor = updatedTest.PreDilutionFactor;
        entity.InstrumentDilutionFactor = updatedTest.InstrumentDilutionFactor;
        entity.ApplyAcceptanceRange = updatedTest.ApplyAcceptanceRange;

        if (entity.ApplyAcceptanceRange)
        {
            entity.UseAcceptanceRange = updatedTest.UseAcceptanceRange;
            if (entity.UseAcceptanceRange)
            {
                entity.UpperLimit = updatedTest.UpperLimit;
                entity.LowerLimit = updatedTest.LowerLimit;
            }
            else
            {
                entity.MaxCv = updatedTest.MaxCv;
                entity.NumberOfSd = updatedTest.NumberOfSd;
            }
        }

        entity.UseLotQualification = updatedTest.UseLotQualification;
        if (entity.UseLotQualification)
        {
            entity.NumberOfTests = updatedTest.NumberOfTests;
        }

        entity.QualityControlType = updatedTest.QualityControlType;
    }
}