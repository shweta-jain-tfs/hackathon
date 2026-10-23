using System.Text;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Localization;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Parameters.Application.Mapping;
using Pharos.Server.Parameters.Application.Queries.GetMethodsAndTest;

namespace Pharos.Server.Parameters.Application.Commands.MethodTestDetails;

public class UpdateMethodTestDetailsCommand : IRequest
{
    public int Id { get; set; }
    public int Replicates { get; set; }
    public bool Active { get; set; }
    public int MethodConcentrationUnitId { get; set; }
    public bool ReportConcentration { get; set; }
    public bool ReportCutOff { get; set; }
    public QuotientReportMode ReportQuotient { get; set; }
    public bool ReportClass { get; set; }
    public int ReportClassId { get; set; }
    public int DilutionFactor { get; set; }
    public DilutionMethod DilutionMethod { get; set; }
    public double? CutOffConcentration { get; set; }
    public string? TextQuantitativeAbove { get; set; }
    public string? TextQuantitativeBelow { get; set; }
    public string? TextQuantitativeAboveForDilutedTest { get; set; }
    public string? TextQuantitativeBelowForDilutedTest { get; set; }
    public MethodTestPriorityDto? Priority { get; set; }

    /// <summary>
    /// The date and time when the method test details were queried. This is used to merge concurrent edits.
    /// </summary>
    public DateTimeOffset QueriedAt { get; set; }

    public List<MethodTestLabNameDto>? MethodTestLabNames { get; set; }
}

internal sealed class UpdateMethodTestDetailsCommandHandler(IParametersDbContext context, IOptionsSnapshot<LisSettings> lisSettings) : IRequestHandler<UpdateMethodTestDetailsCommand>
{
    private static readonly CompositeFormat _labNameAlreadyExistsFormat = CompositeFormat.Parse(Translations.LabNameAlreadyExists);

    public async Task Handle(UpdateMethodTestDetailsCommand request, CancellationToken cancellationToken)
    {
        var methodTest = await context.MethodTests
           .Where(x => x.Id == request.Id)
           .Include(x => x.Method)
           .Include(x => x.LabTestNames)
           .FirstOrDefaultAsync(cancellationToken)
           .ConfigureAwait(false);

        if (methodTest == null)
        {
            throw new ObjectGoneException(Translations.ObjectGone);
        }

        var wasActive = methodTest.Active;
        var requestedLabTestNames = GetRequestedLabTestNames(request.MethodTestLabNames);

        await ThrowIfLaboratoryNameExistsForAnotherTest(requestedLabTestNames.Select(x => x.TestName).ToList(), methodTest, cancellationToken)
            .ConfigureAwait(false);
        await ThrowIfInvalidConcentrationUnit(request, methodTest.MethodId, cancellationToken)
            .ConfigureAwait(false);
        await ThrowIfInvalidClassTable(request, methodTest.MethodId, cancellationToken)
            .ConfigureAwait(false);

        request.UpdateMethodTest(methodTest);

        methodTest.UserReportClassId = request.ReportClass ? request.ReportClassId : 0;
        UpdatePriority(methodTest, request.Priority);
        UpdateLaboratoryNames(methodTest, requestedLabTestNames, request.QueriedAt);

        if (wasActive != methodTest.Active)
        {
            methodTest.AddDomainEvent(new MethodTestActivatedEvent(methodTest.Method.MethodName, methodTest.TestName, methodTest.Active));
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static List<MethodTestLabNameDto> GetRequestedLabTestNames(IEnumerable<MethodTestLabNameDto>? labTestNames)
    {
        return (labTestNames ?? [])
        .Select(x => new MethodTestLabNameDto
        {
            Id = x.Id,
            TestName = x.TestName.Trim(),
            SortOrder = x.SortOrder
        })
        .ToList();
    }

    private static void UpdatePriority(MethodTest methodTest, MethodTestPriorityDto? priority)
    {
        if (priority == null)
        {
            return;
        }

        methodTest.Priority ??= new MethodTestPriority();
        priority.UpdateMethodTestPriority(methodTest.Priority);
    }

    private void UpdateLaboratoryNames(
        MethodTest methodTest,
        IReadOnlyCollection<MethodTestLabNameDto> requestedLabTestNames,
        DateTimeOffset queriedAt)
    {
        RemoveDeletedLaboratoryNames(methodTest, requestedLabTestNames, queriedAt);
        UpdateExistingLaboratoryNames(methodTest, requestedLabTestNames);
        AddNewLaboratoryNames(methodTest, requestedLabTestNames);
    }

    private void RemoveDeletedLaboratoryNames(
        MethodTest methodTest,
        IReadOnlyCollection<MethodTestLabNameDto> requestedLabTestNames,
        DateTimeOffset queriedAt)
    {
        var requestedLabTestNameIds = requestedLabTestNames.Select(x => x.Id).ToHashSet();
        var labNamesToRemove = methodTest.LabTestNames
            .Where(x => x.Created < queriedAt && !requestedLabTestNameIds.Contains(x.Id))
            .ToList();

        if (labNamesToRemove.Count > 0)
        {
            context.MethodTestLabNames.RemoveRange(labNamesToRemove);
        }
    }

    private static void UpdateExistingLaboratoryNames(MethodTest methodTest, IEnumerable<MethodTestLabNameDto> requestedLabTestNames)
    {
        foreach (var labName in requestedLabTestNames.Where(x => x.Id > 0))
        {
            var existingLabTestName = methodTest.LabTestNames.FirstOrDefault(x => x.Id == labName.Id);
            if (existingLabTestName == null)
            {
                throw new ObjectGoneException(Translations.ObjectGone);
            }

            existingLabTestName.TestName = labName.TestName;
            existingLabTestName.SortOrder = labName.SortOrder;
        }
    }

    private void AddNewLaboratoryNames(MethodTest methodTest, IEnumerable<MethodTestLabNameDto> requestedLabTestNames)
    {
        foreach (var labName in requestedLabTestNames.Where(x => x.Id == 0))
        {
            context.MethodTestLabNames.Add(new MethodTestLabName
            {
                MethodTestId = methodTest.Id,
                TestName = labName.TestName,
                SortOrder = labName.SortOrder
            });
        }
    }

    private async Task ThrowIfLaboratoryNameExistsForAnotherTest(
        IReadOnlyCollection<string> requestedLabNames,
        MethodTest methodTest,
        CancellationToken cancellationToken)
    {
        var query = context.MethodTestLabNames.Where(x => x.MethodTest.MethodId == methodTest.MethodId &&
                                                     x.MethodTestId != methodTest.Id &&
                                                     requestedLabNames.Contains(x.TestName));

        if (lisSettings.Value.LisChannels.Any(c => c.ImportSettings.ForceUniqueLaboratoryTestNames))
        {
            var duplicatesAcrossOtherMethodsQuery = context.MethodTestLabNames
                .Where(x =>
                    x.MethodTest.MethodId != methodTest.MethodId &&
                    x.MethodTest.Active &&
                    x.MethodTest.Method.Enabled &&
                    requestedLabNames.Contains(x.TestName));

            query = query.Union(duplicatesAcrossOtherMethodsQuery);
        }

        var duplicatedLabNames = await query
            .Select(x => x.TestName)
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (duplicatedLabNames.Count > 0)
        {
            var labNamesDisplay = string.Join(", ", duplicatedLabNames.Order(StringComparer.OrdinalIgnoreCase)
                                 .Select(labName => $"'{labName}'"));
            throw new ValidationException(string.Format(null, _labNameAlreadyExistsFormat, labNamesDisplay));
        }
    }

    private async Task ThrowIfInvalidConcentrationUnit(
        UpdateMethodTestDetailsCommand request,
        int methodId,
        CancellationToken cancellationToken)
    {
        if (!request.ReportConcentration)
        {
            return;
        }

        var concentrationUnitExists = await context.MethodConcentrationUnits
            .AnyAsync(x => x.MethodId == methodId && x.UnitId == request.MethodConcentrationUnitId, cancellationToken)
            .ConfigureAwait(false);

        if (concentrationUnitExists)
        {
            return;
        }

        throw new ValidationException("The selected concentration unit is not available for this method.");
    }

    private async Task ThrowIfInvalidClassTable(
        UpdateMethodTestDetailsCommand request,
        int methodId,
        CancellationToken cancellationToken)
    {
        if (!request.ReportClass || request.ReportClassId == 0)
        {
            return;
        }

        var classTableExists = await context.MethodClassTables
            .AnyAsync(x => x.MethodId == methodId && x.ClassTableId == request.ReportClassId, cancellationToken)
            .ConfigureAwait(false);
        var classTableBelowExists = await context.MethodClassTableBelows
            .AnyAsync(x => x.MethodId == methodId &&
                        x.ClassTableId == request.ReportClassId &&
                        !string.IsNullOrEmpty(x.UserTextBelowConcentration ?? x.DefaultTextBelowConcentration),
                cancellationToken)
            .ConfigureAwait(false);

        if (classTableExists && classTableBelowExists)
        {
            return;
        }

        throw new ValidationException("The selected class table is not available for this method.");
    }
}
