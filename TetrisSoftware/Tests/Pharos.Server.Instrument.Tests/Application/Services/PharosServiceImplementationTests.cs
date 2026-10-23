using System.Linq.Expressions;
using FluentValidation;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Pharos.Server.Application;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Result;
using Pharos.Server.Application.SystemErrors;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Instrument.Application.Interfaces;
using Tetris.Protocol.Pharos;
using Tetris.Protocol.Pharos.Storage;
using Tetris.Protocol.Pharos.TestResult;

namespace Pharos.Server.Instrument.Application.Services;

public class PharosServiceImplementationTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Trait("ProductRequirement", "PR_865")]
    [Trait("ModuleRequirement", "MR_3525")]
    public async Task SendAssayResult_ProcessesResultForKnownMethodAndTest(bool methodAndTestExist)
    {
        var mediator = new Mock<IMediator>();
        var methodsService = new Mock<IMethodsService>();
        methodsService.Setup(x => x.GetMethodTest("sIgE", "c1", It.IsAny<Expression<Func<MethodTest, bool>>>()))
            .ReturnsAsync(methodAndTestExist);
        var sut = new PharosServiceImplementation(
            Mock.Of<ICommunicationSessionManager>(), mediator.Object, Mock.Of<IPharosVersionService>(),
            Mock.Of<ITetrisProtocolInformation>(), methodsService.Object,
            Mock.Of<IValidator<InstrumentStorage>>(), Mock.Of<IValidator<TestOrderStatusChange>>(),
            Mock.Of<ILogger<PharosServiceImplementation>>());
        var message = new AssayResult { MethodName = "sIgE", TestName = "c1" };
        var context = new TestServerCallContext(new Metadata { { CommunicationSession.InstrumentSerialHeader, "SN001" } });

        var response = await sut.SendAssayResult(message, context);

        response.Should().BeOfType<Empty>();
        mediator.Verify(x => x.Send(It.Is<ProcessAssayResultCommand>(command =>
            command.InstrumentNumber == "SN001" && command.Message == message), It.IsAny<CancellationToken>()),
            methodAndTestExist ? Times.Once() : Times.Never());
        mediator.Verify(x => x.Send(It.Is<CreatePharosErrorCommand>(command =>
            command.ErrorCode == PharosErrorCodes.ResultForUnknownMethodAndTestCombination
            && command.Classification == ErrorClassification.Warning), It.IsAny<CancellationToken>()),
            methodAndTestExist ? Times.Never() : Times.Once());
    }

    private sealed class TestServerCallContext(Metadata requestHeaders) : ServerCallContext
    {
        protected override string MethodCore => "SendAssayResult";
        protected override string HostCore => "localhost";
        protected override string PeerCore => "ipv4:127.0.0.1:0";
        protected override DateTime DeadlineCore => DateTime.MaxValue;
        protected override Metadata RequestHeadersCore => requestHeaders;
        protected override CancellationToken CancellationTokenCore => TestContext.Current.CancellationToken;
        protected override Metadata ResponseTrailersCore { get; } = [];
        protected override Status StatusCore { get; set; }
        protected override WriteOptions? WriteOptionsCore { get; set; }
        protected override AuthContext AuthContextCore { get; } = new("test", []);
        protected override IDictionary<object, object> UserStateCore { get; } = new Dictionary<object, object>();

        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders)
        {
            return Task.CompletedTask;
        }

        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options)
        {
            return null!;
        }
    }
}