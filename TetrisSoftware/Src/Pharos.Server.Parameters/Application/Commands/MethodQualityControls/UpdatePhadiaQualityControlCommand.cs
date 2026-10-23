using FluentValidation;
using MediatR;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Localization;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Commands.MethodQualityControls;

public record UpdatePhadiaQualityControlCommand(
    int Id,
    int TubeTypeId,
    List<PhadiaQualityControlUpdateTest> Tests) : IRequest;

public record PhadiaQualityControlUpdateTest(
    int Id,
    int Replicates,
    double ReplicateCvLimit,
    bool ApplyAcceptanceRange,
    bool UseAcceptanceRange,
    double MaxCv,
    int NumberOfSd,
    bool UseLotQualification,
    int NumberOfTests,
    QualityControlType QualityControlType) : IRequest;

internal class UpdatePhadiaQualityControlCommandHandler : IRequestHandler<UpdatePhadiaQualityControlCommand>
{
    private readonly IParametersDbContext _context;

    public UpdatePhadiaQualityControlCommandHandler(IParametersDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdatePhadiaQualityControlCommand request, CancellationToken cancellationToken)
    {
        var item = await _context.PhadiaQualityControl.Include(x => x.Tests).FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (item == null)
        {
            throw new ObjectGoneException(Translations.ObjectGone);
        }

        var updatedTestIds = request.Tests.Select(x => x.Id);
        var existingTestIds = item.Tests.Select(x => x.Id);
        var testsToRemove = existingTestIds.Except(updatedTestIds).ToList();

        if (testsToRemove.Count > 0)
        {
            throw new ValidationException("Tests cannot be added or removed from a phadia qc");
        }

        //only set tube id if it's a tube qc
        if (item.IsQcInTube)
        {
            item.TubeTypeId = request.TubeTypeId;
        }

        foreach (var test in request.Tests)
        {
            var qcTest = item.Tests.OfType<PhadiaQualityControlTest>().FirstOrDefault(x => x.Id == test.Id);
            if (qcTest == null)
            {
                throw new ValidationException("The test was not found");
            }

            MapValues(qcTest, test);
        }

        item.AddDomainEvent(new QualityControlScheduleUpdatedEvent());

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static void MapValues(PhadiaQualityControlTest qcTest, PhadiaQualityControlUpdateTest updatedTest)
    {
        qcTest.Replicates = updatedTest.Replicates;
        qcTest.ReplicateCvLimit = updatedTest.ReplicateCvLimit;
        qcTest.QualityControlType = updatedTest.QualityControlType;
        qcTest.ApplyAcceptanceRange = updatedTest.ApplyAcceptanceRange;
        qcTest.UseAcceptanceRange = updatedTest.UseAcceptanceRange;

        if (qcTest.ApplyAcceptanceRange && !qcTest.UseAcceptanceRange)
        {
            qcTest.MaxCv = updatedTest.MaxCv;
            qcTest.NumberOfSd = updatedTest.NumberOfSd;
        }

        qcTest.UseLotQualification = updatedTest.UseLotQualification;

        if (qcTest.UseLotQualification)
        {
            qcTest.NumberOfTests = updatedTest.NumberOfTests;
        }
    }
}