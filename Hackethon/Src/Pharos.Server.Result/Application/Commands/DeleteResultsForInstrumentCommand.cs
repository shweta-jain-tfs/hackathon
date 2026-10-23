using MediatR;
using Pharos.Server.Result.Application.Interfaces;

namespace Pharos.Server.Result.Application.Commands;

internal record DeleteResultsForInstrumentCommand(string InstrumentNumber) : IRequest;

internal sealed class DeleteResultsForInstrumentCommandHandler(IResultDbContext dbContext) : IRequestHandler<DeleteResultsForInstrumentCommand>
{
    public async Task Handle(DeleteResultsForInstrumentCommand command, CancellationToken cancellationToken)
    {
        // Delete test results.
        var testResults = await dbContext.TestResults
            .Where(r => r.InstrumentNumber == command.InstrumentNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (testResults.Count > 0)
        {
            dbContext.TestResults.RemoveRange(testResults);

            // Need to save first, due to "The DELETE statement conflicted with the REFERENCE constraint "FK_TestReplicates_AnalyticalRuns_AnalyticalRunId". The conflict occurred in database "PharosDB", table "Result.TestReplicates", column 'AnalyticalRunId'."
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // Delete analytical runs.
        var analyticalRuns = await dbContext.AnalyticalRuns
            .Where(r => r.InstrumentNumber == command.InstrumentNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (analyticalRuns.Count > 0)
        {
            dbContext.AnalyticalRuns.RemoveRange(analyticalRuns);
        }

        // Delete curves.
        var curves = await dbContext.Curves
            .Where(r => r.InstrumentNumber == command.InstrumentNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (curves.Count > 0)
        {
            dbContext.Curves.RemoveRange(curves);
        }

        if (dbContext.HasChanges())
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}