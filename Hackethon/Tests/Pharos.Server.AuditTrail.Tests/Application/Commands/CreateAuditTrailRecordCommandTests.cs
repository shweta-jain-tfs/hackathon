using MediatR;
using Moq.EntityFrameworkCore;
using Pharos.Server.Application;
using Pharos.Server.Application.AuditTrail;
using Pharos.Server.Application.AuditTrail.Enums;
using Pharos.Server.AuditTrail.Application.Interfaces;
using Pharos.Server.AuditTrail.Domain;

namespace Pharos.Server.AuditTrail.Application.Commands;
public class CreateAuditLogCommandTests
{
    private readonly Mock<IAuditTrailDbContext> _dbContextMock;
    private readonly Mock<AuditDataProtectionService> _auditDataServiceMock;
    private readonly CreateAuditTrailRecordCommandHandler _sut;
    private readonly Mock<ICurrentUserService> _userService;

    public CreateAuditLogCommandTests()
    {
        _dbContextMock = new Mock<IAuditTrailDbContext>();
        _auditDataServiceMock = new Mock<AuditDataProtectionService>();
        _userService = new Mock<ICurrentUserService>();
        var senderMock = new Mock<ISender>();
        _sut = new CreateAuditTrailRecordCommandHandler(_dbContextMock.Object, _auditDataServiceMock.Object, _userService.Object, senderMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldCreateAuditTrailRecord()
    {
        MockAuditTrailDbContext();
        var eventData = new AuditEventData();
        eventData.AddData(EventDataFields.UserId, _userService.Object.UserId);
        eventData.AddData(EventDataFields.Permissions, new List<string> { "Read", "Write" });
        var request = new CreateAuditTrailRecordCommand
        {
            EventId = EventId.UserLoggedIntoPharos,
            EventData = [eventData],
            Level = EventLevel.Information,
            Comment = "Test comment"
        };
        const string fakeHash = "fakeHash";
        var data = $"{request.EventId}{request.EventData}{request.Level}{_userService.Object.UserId}{DateTimeOffset.Now}";
        _auditDataServiceMock.Setup(h => h.ComputeHash(string.Empty, data)).Returns(fakeHash);

        AuditTrailRecord? addedRecord = null;
        _dbContextMock.Setup(db => db.AuditTrailRecords.Add(It.IsAny<AuditTrailRecord>())).Callback<AuditTrailRecord>(r => addedRecord = r);

        await _sut.Handle(request, TestContext.Current.CancellationToken);

        _dbContextMock.Verify(db => db.AuditTrailRecords.Add(It.IsAny<AuditTrailRecord>()), Times.Once); 
        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        Assert.NotNull(addedRecord);
    }

    private void MockAuditTrailDbContext()
    {
        _dbContextMock.Setup(db => db.AuditTrailRecords).ReturnsDbSet(CreateAuditLog());
    }

    private List<AuditTrailRecord> CreateAuditLog()
    {
        var eventData = new AuditEventData();
        eventData.AddData(EventDataFields.UserId, _userService.Object.UserId);
        eventData.AddData(EventDataFields.Permissions, new List<string> { "Read", "Write" });
        var log = new List<AuditTrailRecord>
            {
                new()
                {
                    EventId = EventId.UserLoggedIntoPharos,
                    EventData = [eventData],
                    Comment = "Test comment"
                }
            };
        return log;
    }
}
