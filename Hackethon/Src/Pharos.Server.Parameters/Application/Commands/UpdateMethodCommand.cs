using FluentValidation;
using MediatR;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Localization;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Parameters.Application.Queries.GetMethods;

namespace Pharos.Server.Parameters.Application.Commands;

public class UpdateMethodCommand : IRequest
{
    public int Id { get; set; }
    public bool Enabled { get; set; }
    public string? MethodNameLis { get; set; }
    public int? DefaultLisChannelId { get; set; }
    public MethodReportingDto? Report { get; set; }
    public int MaxNumberOfDays { get; set; }
    public IList<MethodClassTableDto>? ClassTables { get; set; }
}

internal sealed class UpdateMethodCommandHandler(IParametersDbContext context) : IRequestHandler<UpdateMethodCommand>
{
    public async Task Handle(UpdateMethodCommand request, CancellationToken cancellationToken)
    {
        var item = await context.Methods
            .Include(x => x.ClassTableBelows)
            .Include(x => x.ClassTables)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
            .ConfigureAwait(false);

        if (item == null)
        {
            throw new ObjectGoneException(Translations.ObjectGone);
        }

        var hasChanges = UpdateMethodDetails(item, request);
        hasChanges |= UpdateMethodReport(item, request.Report!);
        hasChanges |= UpdateMethodClassTables(item, request.ClassTables ?? []);

        var calibrationCurve = await context.MethodCalibrationCurves
            .FirstOrDefaultAsync(x => x.MethodId == request.Id, cancellationToken)
            .ConfigureAwait(false);

        if (calibrationCurve != null)
        {
            hasChanges |= UpdatePropertyIfChanged(calibrationCurve.UserMaxNumberOfDays, v => calibrationCurve.UserMaxNumberOfDays = v, request.MaxNumberOfDays);
        }

        if (hasChanges)
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool UpdateMethodDetails(Method method, UpdateMethodCommand request)
    {
        var enabledChanged = UpdatePropertyIfChanged(method.Enabled, v => method.Enabled = v, request.Enabled);
        if (enabledChanged)
        {
            method.AddDomainEvent(new MethodEnabledEvent(method.MethodName, method.Enabled));
        }

        var hasChanges = enabledChanged;
        hasChanges |= UpdatePropertyIfChanged(method.MethodNameLis, v => method.MethodNameLis = v, request.MethodNameLis);
        hasChanges |= UpdatePropertyIfChanged(method.DefaultLisChannelId, v => method.DefaultLisChannelId = v, request.DefaultLisChannelId);

        return hasChanges;
    }

    private static bool UpdateMethodReport(Method method, MethodReportingDto report)
    {
        var hasChanges = UpdatePropertyIfChanged(method.Reporting.UserMinConcentrationToReport, v => method.Reporting.UserMinConcentrationToReport = v, report.MinConcentrationToReport);
        hasChanges |= UpdatePropertyIfChanged(method.Reporting.UserTextBelowCutOff, v => method.Reporting.UserTextBelowCutOff = v, report.TextBelowCutOff);
        hasChanges |= UpdatePropertyIfChanged(method.Reporting.UserTextAboveCutOff, v => method.Reporting.UserTextAboveCutOff = v, report.TextAboveCutOff);
        hasChanges |= UpdatePropertyIfChanged(method.Reporting.UserTextQuantitativeBelow, v => method.Reporting.UserTextQuantitativeBelow = v, report.TextQuantitativeBelow);
        hasChanges |= UpdatePropertyIfChanged(method.Reporting.UserTextQuantitativeDoubtful, v => method.Reporting.UserTextQuantitativeDoubtful = v, report.TextQuantitativeDoubtful);
        hasChanges |= UpdatePropertyIfChanged(method.Reporting.UserTextQuantitativeAbove, v => method.Reporting.UserTextQuantitativeAbove = v, report.TextQuantitativeAbove);
        hasChanges |= UpdatePropertyIfChanged(method.Reporting.UserUseCalculatedTextQuantitativeAbove, v => method.Reporting.UserUseCalculatedTextQuantitativeAbove = v, report.UseCalculatedTextQuantitativeAbove);

        return hasChanges;
    }

    private static bool UpdateMethodClassTables(Method method, IList<MethodClassTableDto> classTables)
    {
        var hasChanges = false;
        foreach (var requestClassTable in classTables)
        {
            var classTableBelow = method.ClassTableBelows.FirstOrDefault(x => x.ClassTableId == requestClassTable.ClassTableId);
            if (classTableBelow is null)
            {
                throw new ValidationException("Class table does not belong to the method.");
            }

            hasChanges |= UpdatePropertyIfChanged(
                classTableBelow.UserTextBelowConcentration,
                v => classTableBelow.UserTextBelowConcentration = v,
                requestClassTable.TextBelow);

            foreach (var requestRow in requestClassTable.ClassTableRow ?? [])
            {
                var classTableRow = method.ClassTables.FirstOrDefault(x =>
                    x.ClassTableId == requestClassTable.ClassTableId &&
                    x.ClassTableRowId == requestRow.ClassTableRowId);

                if (classTableRow is null)
                {
                    throw new ValidationException("Class table row does not belong to the method.");
                }

                hasChanges |= UpdatePropertyIfChanged(
                    classTableRow.UserTextAboveConcentration,
                    v => classTableRow.UserTextAboveConcentration = v,
                    requestRow.TextAbove);
            }
        }

        return hasChanges;
    }

    private static bool UpdatePropertyIfChanged<T>(T currentValue, Action<T> setValue, T newValue)
    {
        if (EqualityComparer<T>.Default.Equals(currentValue, newValue))
        {
            return false;
        }

        setValue(newValue);
        return true;
    }
}
