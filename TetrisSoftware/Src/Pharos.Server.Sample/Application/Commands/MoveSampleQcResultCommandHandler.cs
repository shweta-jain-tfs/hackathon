using Pharos.Server.Application.Sample;

namespace Pharos.Server.Sample.Application.Commands
{
    internal class MoveSampleQcResultCommandHandler : IRequestHandler<MoveSampleQcResultCommand>
    {
        public MoveSampleQcResultCommandHandler()
        {
        }

        public async Task Handle(MoveSampleQcResultCommand request, CancellationToken cancellationToken)
        {
            // TODO: Handle the result of the move operation  

            await Task.CompletedTask;
        }
    }
}
