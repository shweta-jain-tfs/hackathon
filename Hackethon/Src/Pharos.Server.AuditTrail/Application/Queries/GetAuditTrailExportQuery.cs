using System.Text;
using Microsoft.Extensions.Logging;
using Pharos.Server.AuditTrail.Application.Interfaces;
using Pharos.Server.AuditTrail.Application.Services;

namespace Pharos.Server.AuditTrail.Application.Queries;

public record GetAuditTrailExportQuery : IRequest<string>
{
    public string? SearchText { get; set; }
    public IEnumerable<string>? UserNames { get; set; }
    public DateTimeOffset? FromDate { get; set; }
    public DateTimeOffset? ToDate { get; set; }
}
internal sealed class GetAuditTrailExportQueryHandler(IAuditTrailDbContext context, ILogger<GetAuditTrailExportQueryHandler> logger, DescriptionTemplateService descriptionService)
: IRequestHandler<GetAuditTrailExportQuery, string>
{
    public async Task<string> Handle(GetAuditTrailExportQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Audit Trail records export requested");

        var sb = new StringBuilder();
        var query = context.AuditTrailRecords.AsQueryable();

        if (request.FromDate.HasValue)
        {
            query = query.Where(r => r.Created >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(r => r.Created <= request.ToDate.Value.AddDays(1));
        }

        if (request.UserNames?.Any() == true)
        {
            query = query.Where(r => r.CreatedBy != null && request.UserNames.Contains(r.CreatedBy));
        }

        var records = await query
            .Select(r => new
            {
                r.Id,
                r.EventId,
                r.EventDataString,
                r.Created,
                r.CreatedBy,
                r.Comment
            })
            .ToListAsync(cancellationToken);

        // Apply SearchText filter in-memory
        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            var recordsWithDescriptions = records.Select(r => new
            {
                Record = r,
                Description = descriptionService.GetLocalizedDescription(r.EventId, r.EventDataString)
            }).ToList();

            recordsWithDescriptions = recordsWithDescriptions.Where(r =>
                    (!string.IsNullOrEmpty(r.Description) && r.Description.Contains(request.SearchText, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(r.Record.Comment) && r.Record.Comment.Contains(request.SearchText, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(r.Record.EventDataString) && r.Record.EventDataString.Contains(request.SearchText, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            records = recordsWithDescriptions.Select(r => r.Record).ToList();
        }

        sb.AppendLine("Id;Date;Operator;Activity;Event Data;Comment");

        foreach (var record in records)
        {
            var description = descriptionService.GetLocalizedDescription(record.EventId, record.EventDataString);

            sb.AppendLine($"{EscapeCsvField(record.Id)};" +
                          $"{EscapeCsvField(record.Created)};" +
                          $"{EscapeCsvField(record.CreatedBy)};" +
                          $"{EscapeCsvField(description)};" +
                          $"{EscapeCsvField(record.EventDataString)};" +
                          $"{EscapeCsvField(record.Comment)}");
        }

        return await Task.FromResult(sb.ToString());
    }

    private static string EscapeCsvField(object? value)
    {
        var stringValue = value?.ToString() ?? string.Empty;
        var singleLineValue = stringValue.ReplaceLineEndings(" | ");

        return stringValue.IndexOfAny([';', '"']) >= 0
            ? $"\"{singleLineValue.Replace("\"", "\"\"")}\""
            : singleLineValue;
    }
}
