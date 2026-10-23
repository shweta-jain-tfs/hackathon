using MediatR;
using Pharos.Server.Application.Command;
using Pharos.Server.Application.Request;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Localization;
using Pharos.Server.Request.Application.Interfaces;
using RequestDeletedEvent = Pharos.Server.Request.Application.Events.RequestDeletedEvent;

namespace Pharos.Server.Request.Application.Commands.DeleteRequest;

internal record DeleteRequestCommand(IList<int> Ids) : IRequest<DeleteResponse>;

internal sealed class DeleteRequestCommandHandler(IRequestDbContext dbContext, IPublisher publisher) : IRequestHandler<DeleteRequestCommand, DeleteResponse>
{
    public async Task<DeleteResponse> Handle(DeleteRequestCommand request, CancellationToken cancellationToken)
    {
        var requests = await dbContext.Requests
            .Where(s => request.Ids.Contains(s.Id))
            .Include(s => s.Patient)
            .Include(s => s.TestOrders)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (requests.Count == 0)
        {
            if (request.Ids.Count == 1)
            {
                throw new ObjectGoneException(Translations.DeletedRequest);
            }
            throw new ObjectGoneException(Translations.DeletedRequests);
        }

        var testOrderIds = requests
            .SelectMany(r => r.TestOrders)
            .Select(to => to.Uid)
            .ToList();

        //possible patients to delete
        var patientIds = new HashSet<int>();

        foreach (var requestEntity in requests)
        {
            dbContext.Requests.Remove(requestEntity);
            requestEntity.AddDomainEvent(new RequestDeletedEvent(requestEntity));
            if (requestEntity.Patient != null)
            {
                patientIds.Add(requestEntity.Patient.Id);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await DeletePatientsWithNoRequests(patientIds, cancellationToken);

        await publisher.Publish(new TestOrdersDeletedEvent(testOrderIds), cancellationToken);

        return new DeleteResponse { Skipped = request.Ids.Count - requests.Count, Deleted = requests.Count };
    }

    private async Task DeletePatientsWithNoRequests(HashSet<int> patientIds, CancellationToken cancellationToken)
    {
        if (patientIds.Count > 0)
        {
            var patients = await dbContext.Patients
                .Where(p => patientIds.Contains(p.Id))
                .Include(x => x.Requests)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var patient in patients)
            {
                if (patient.Requests is { Count: 0 })
                {
                    dbContext.Patients.Remove(patient);
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
