namespace Pharos.Server.Lis.Astm.Application.Messages;

internal sealed record AstmTestId(
    string TestName,
    string LisMethodId,
    int InstrumentDilution,
    int SuppressReflex,
    string ReflexName,
    int Replicates
);
