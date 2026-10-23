using System.Text;
using Pharos.Server.Application;
using Pharos.Server.Application.Identity;
using Pharos.Server.AuditTrail.Application.Interfaces;
using Pharos.Server.AuditTrail.Application.Services;
using Pharos.Server.AuditTrail.Domain;

namespace Pharos.Server.AuditTrail.Application.Commands
{
    internal sealed class CreateAuditTrailRecordCommandHandler(IAuditTrailDbContext dbContext, AuditDataProtectionService auditDataProtectionService, ICurrentUserService currentUserService, ISender sender) : IRequestHandler<CreateAuditTrailRecordCommand, bool>
    {
        public async Task<bool> Handle(CreateAuditTrailRecordCommand request, CancellationToken cancellationToken)
        {
            var timestamp = DateTimeOffset.Now;
            var username = await GetCurrentUserName(cancellationToken);

            // This mechanism will be changed later when we store the hash differently.
            var previousHash = auditDataProtectionService.LoadHash();
            previousHash = previousHash == "{}" ? string.Empty : previousHash;

            var record = new AuditTrailRecord
            {
                EventId = request.EventId,
                EventData = request.EventData,
                Comment = request.Comment,
                Created = timestamp,
                CreatedBy = username?.UserName,
                EventDataString = StringHelper.GetEventDataString(request.EventData)
            };
            var newHash = auditDataProtectionService.ComputeHash(previousHash, StringHelper.BuildHashString(record, new StringBuilder()));

            record.Hash = newHash;

            dbContext.AuditTrailRecords.Add(record);
            await dbContext.SaveChangesAsync(cancellationToken);

            // Store last hash externally
            auditDataProtectionService.SaveHash(newHash);

            return true;
        }

        private async Task<ApplicationUserDto?> GetCurrentUserName(CancellationToken cancellationToken)
        {
            if(currentUserService.UserId == null)
            {
                return null;
            }
            var user = await sender.Send(new GetApplicationUserByIdQuery((Guid)currentUserService.UserId), cancellationToken);
            return user ?? throw new InvalidOperationException("Current user not found.");
        }
    }
}
