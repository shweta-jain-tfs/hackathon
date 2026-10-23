using Google.Protobuf;

namespace Pharos.Server.Instrument.Infrastructure.Grpc.Interceptors;

internal class BinaryGrpcMessage
{
    public BinaryGrpcMessage(IMessage protoMessage)
    {
        Payload = protoMessage.ToByteArray();
        Type = protoMessage.GetType().AssemblyQualifiedName ?? "";
    }

    public string Type { get; }
    public byte[] Payload { get; }
}