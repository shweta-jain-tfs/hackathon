using FluentAssertions;
using Pharos.Server.Application.AuditTrail;
using Pharos.Server.Application.AuditTrail.Enums;

namespace Pharos.Server.AuditTrail.Services;

public class StringHelperTests
{
    [Fact]
    public void GetEventDataString_ShouldPreserveValue_WhenJsonLikeFragmentIsMalformed()
    {
        var eventData = new AuditEventData();
        eventData.AddData(EventDataFields.Comments, "prefix { malformed JSON");

        var result = StringHelper.GetEventDataString([eventData]);

        result.Should().Be("Comments: prefix { malformed JSON");
    }
}
