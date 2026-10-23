using FluentValidation;
using MediatR;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Commands.MethodQualityControls;

public record CreateLabQualityControlCommand(
    string Name,
    string IdOnBarcode,
    int BarcodeFormatId,
    int TubeTypeId,
    List<LabQualityControlCreateTest> Tests) : IRequest<int>;

public record LabQualityControlCreateTest(
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

internal class CreateLabQualityControlCommandHandler : IRequestHandler<CreateLabQualityControlCommand, int>
{
    private readonly IParametersDbContext _context;
    private readonly IDilutionValidator _dilutionValidator;

    public CreateLabQualityControlCommandHandler(IParametersDbContext context, IDilutionValidator dilutionValidator)
    {
        _context = context;
        _dilutionValidator = dilutionValidator;
    }

    public async Task<int> Handle(CreateLabQualityControlCommand request, CancellationToken cancellationToken)
    {
        //check if name already exists amongst phadia or lab quality controls
        if (await _context.PhadiaQualityControl.AnyAsync(x => x.Name == request.Name, cancellationToken) ||
            await _context.LabQualityControls.AnyAsync(x => x.Name == request.Name, cancellationToken))
        {
            throw new ValidationException("A quality control with the same name already exists.");
        }

        if (request.Tests.Count == 0)
        {
            throw new ValidationException("At least one test is required.");
        }

        var testIds = request.Tests.GroupBy(x => x.TestId);
        if (testIds.Any(x => x.Count() > 1))
        {
            throw new ValidationException("Duplicate tests are not allowed.");
        }

        var item = new LabQualityControl
        {
            Name = request.Name,
            IdOnBarcode = request.IdOnBarcode,
            BarcodeFormatId = request.BarcodeFormatId == 0 ? null : request.BarcodeFormatId,
            TubeTypeId = request.TubeTypeId,
            Tests = new List<BaseQualityControlTest>()
        };

        foreach (var test in request.Tests)
        {
            await _dilutionValidator.Validate(test.TestId, test.PreDilutionFactor, test.InstrumentDilutionFactor);

            var newTest = new LabQualityControlTest
            {
                Enabled = false,
                //Set valid default values (may be updated in the map values method)
                MaxCv = 1,
                NumberOfSd = 1,
                Schedule = QualityControlSchedule.Default()
            };
            MapValues(newTest, test);
            item.Tests.Add(newTest);
        }

        _context.LabQualityControls.Add(item);
        await _context.SaveChangesAsync(cancellationToken);

        return item.Id;
    }

    private static void MapValues(LabQualityControlTest entity, LabQualityControlCreateTest updatedTest)
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

        entity.TestId = updatedTest.TestId;
    }
}