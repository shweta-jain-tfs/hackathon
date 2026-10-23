using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Pharos.Server.Lis.Application.Interfaces;
using Pharos.Server.Lis.Application.Services;
namespace Pharos.Server.Lis.Tests;

public class LisTransportConnectionFactoryTests : IDisposable
{
    private readonly LisTransportConnectionFactory _factory;
    private readonly List<ILisTransportConnection> _connections;
    public LisTransportConnectionFactoryTests()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));
        var serviceProvider = services.BuildServiceProvider();
        _factory = new LisTransportConnectionFactory(serviceProvider);
        _connections = new List<ILisTransportConnection>();
    }
    public void Dispose()
    {
        foreach (var connection in _connections)
        {
            try
            {
                connection.Close();
            }
            catch
            {
                // ignored
            }
            finally
            {
                if (connection is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
        _connections.Clear();
        GC.SuppressFinalize(this);
    }
    [Fact]
    public void CreateTcpClientConnection_ShouldReturnValidConnection()
    {
        var connection = _factory.CreateTcpClientConnection(IPAddress.Loopback, 12347, 1000);
        _connections.Add(connection);
        connection.Should().NotBeNull();
        connection.State.Should().Be(LisConnectionState.Closed);
    }
    [Fact]
    public void CreateTcpServerConnection_ShouldReturnValidConnection()
    {
        var connection = _factory.CreateTcpServerConnection(12348);
        _connections.Add(connection);
        connection.Should().NotBeNull();
        connection.State.Should().Be(LisConnectionState.Closed);
    }
    [Fact]
    public void TcpClientConnection_Connect_WhenNoServer_ShouldRemainClosed()
    {
        var connection = _factory.CreateTcpClientConnection(IPAddress.Loopback, 99998, 1000);
        _connections.Add(connection);
        connection.Connect();
        connection.State.Should().Be(LisConnectionState.Closed, "because connection fails when there's no server listening on that port and state remains Closed");
    }
    [Fact]
    public void TcpClientConnection_ReadWriteTimeout_ShouldBeConfigurable()
    {
        var connection = _factory.CreateTcpClientConnection(IPAddress.Loopback, 12352, 1000);
        _connections.Add(connection);
        const int expectedTimeout = 3000;
        connection.ReadTimeout = expectedTimeout;
        connection.ReadTimeout.Should().Be(expectedTimeout);
    }
    [Fact]
    public void TcpServerConnection_ReadWriteTimeout_ShouldBeConfigurable()
    {
        var connection = _factory.CreateTcpServerConnection(12353);
        _connections.Add(connection);
        const int expectedTimeout = 4000;
        connection.ReadTimeout = expectedTimeout;
        connection.ReadTimeout.Should().Be(expectedTimeout);
    }
    [Fact]
    public void TcpConnections_DataAvailable_WhenNotConnected_ShouldReturnFalse()
    {
        var clientConnection = _factory.CreateTcpClientConnection(IPAddress.Loopback, 12354, 1000);
        var serverConnection = _factory.CreateTcpServerConnection(12355);
        _connections.AddRange([clientConnection, serverConnection]);

        clientConnection.DataAvailable.Should().BeFalse("because client is not connected");
        serverConnection.DataAvailable.Should().BeFalse("because server has no client connected");
    }
    [Fact]
    public void TcpConnections_WriteData_WhenNotConnected_ShouldThrowException()
    {
        var clientConnection = _factory.CreateTcpClientConnection(IPAddress.Loopback, 12356, 1000);
        var serverConnection = _factory.CreateTcpServerConnection(12357);
        _connections.AddRange([clientConnection, serverConnection]);
        var testData = new byte[] { 0x01, 0x02, 0x03 };

        clientConnection.Invoking(c => c.WriteData(testData))
            .Should().Throw<Exception>("because client is not connected");
        serverConnection.Invoking(c => c.WriteData(testData))
            .Should().Throw<Exception>("because server has no client connected");
    }
    [Fact]
    public void TcpConnections_ReadData_WhenNotConnected_ShouldThrowException()
    {
        var clientConnection = _factory.CreateTcpClientConnection(IPAddress.Loopback, 12358, 1000);
        var serverConnection = _factory.CreateTcpServerConnection(12359);
        _connections.AddRange([clientConnection, serverConnection]);
        var buffer = new byte[1024];

        clientConnection.Invoking(c => c.ReadData(buffer))
            .Should().Throw<Exception>("because client is not connected");
        serverConnection.Invoking(c => c.ReadData(buffer))
            .Should().Throw<Exception>("because server has no client connected");
    }
}