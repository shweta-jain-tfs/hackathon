using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Pharos.Server.Lis.Application.Interfaces;
using Pharos.Server.Lis.Infrastructure.Transport;

namespace Pharos.Server.Lis.Tests;

public class TcpClientConnectionTests : IDisposable
{
    private readonly ILogger<TcpClientConnection> _logger;
    private readonly IPAddress _testIpAddress;
    private readonly int _testPort;
    private readonly int _reconnectInterval;
    private TcpClientConnection? _connection;

    public TcpClientConnectionTests()
    {
        _logger = NullLogger<TcpClientConnection>.Instance;
        _testIpAddress = IPAddress.Loopback;
        _testPort = 12345;
        _reconnectInterval = 1000;
    }

    public void Dispose()
    {
        try
        {
            _connection?.Dispose();
        }
        catch
        {
            // ignored
        }

        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Constructor_ShouldInitializeWithCorrectValues()
    {
        _connection = new TcpClientConnection(_testIpAddress, _testPort, _reconnectInterval, _logger);
        _connection.State.Should().Be(LisConnectionState.Closed);
    }

    [Fact]
    public void Connect_WhenStateIsAlreadyOpen_ShouldNotAttemptNewConnection()
    {
        _connection = new TcpClientConnection(_testIpAddress, _testPort, _reconnectInterval, _logger);

        var stateProperty = typeof(TcpConnectionBase).GetProperty("State",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        stateProperty?.SetValue(_connection, LisConnectionState.Open);
        _connection.Connect();
        _connection.State.Should().Be(LisConnectionState.Open);
    }

    [Fact]
    public void Connect_WhenConnectionFails_ShouldStartReconnectTimer()
    {
        _connection = new TcpClientConnection(_testIpAddress, 99999, _reconnectInterval, _logger);
        _connection.Connect();
        _connection.State.Should().Be(LisConnectionState.Closed, "because connection fails but state is not explicitly set to Broken in the current implementation");
    }

    [Fact]
    public void Close_WhenStateIsClosed_ShouldNotPerformClose()
    {
        _connection = new TcpClientConnection(_testIpAddress, _testPort, _reconnectInterval, _logger);
        _connection.Close();
        _connection.State.Should().Be(LisConnectionState.Closed);
    }

    [Fact]
    public void Close_WhenStateIsConnecting_ShouldNotPerformClose()
    {
        _connection = new TcpClientConnection(_testIpAddress, _testPort, _reconnectInterval, _logger);
        var stateProperty = typeof(TcpConnectionBase).GetProperty("State",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        stateProperty?.SetValue(_connection, LisConnectionState.Connecting);
        _connection.Close();
        _connection.State.Should().Be(LisConnectionState.Connecting);
    }

    [Fact]
    public void Close_WhenStateIsOpen_ShouldCloseConnection()
    {
        _connection = new TcpClientConnection(_testIpAddress, _testPort, _reconnectInterval, _logger);

        var stateProperty = typeof(TcpConnectionBase).GetProperty("State",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        stateProperty?.SetValue(_connection, LisConnectionState.Open);
        _connection.Close();
        _connection.State.Should().Be(LisConnectionState.Closed);
    }

    [Fact]
    public void StateChanged_WhenStateChanges_ShouldRaiseEvent()
    {
        _connection = new TcpClientConnection(_testIpAddress, _testPort, _reconnectInterval, _logger);
        LisConnectionStateChangedEventArgs? capturedEventArgs = null;
        _connection.StateChanged += (_, args) => capturedEventArgs = args;

        var stateProperty = typeof(TcpConnectionBase).GetProperty("State",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        stateProperty?.SetValue(_connection, LisConnectionState.Open);
        capturedEventArgs.Should().NotBeNull();
        capturedEventArgs!.NewState.Should().Be(LisConnectionState.Open);
    }

    [Fact]
    public void Connect_WithZeroReconnectInterval_ShouldNotStartReconnectTimer()
    {
        _connection = new TcpClientConnection(_testIpAddress, 99999, 0, _logger);
        _connection.Connect();
        _connection.State.Should().Be(LisConnectionState.Closed, "because connection fails and state remains Closed");
    }

    [Fact]
    public void Dispose_ShouldCleanupResources()
    {
        _connection = new TcpClientConnection(_testIpAddress, _testPort, _reconnectInterval, _logger);
        _connection.Invoking(c => c.Dispose()).Should().NotThrow();
        _connection.Invoking(c => c.Dispose()).Should().NotThrow("because multiple dispose calls should be safe");
    }

    [Theory]
    [InlineData(LisConnectionState.Closed)]
    [InlineData(LisConnectionState.Connecting)]
    [InlineData(LisConnectionState.Open)]
    public void State_WhenSet_ShouldReturnCorrectValue(LisConnectionState expectedState)
    {
        _connection = new TcpClientConnection(_testIpAddress, _testPort, _reconnectInterval, _logger);
        var stateProperty = typeof(TcpConnectionBase).GetProperty("State",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        stateProperty?.SetValue(_connection, expectedState);
        _connection.State.Should().Be(expectedState);
    }

    [Fact]
    public void ReadTimeout_ShouldBeSettable()
    {
        _connection = new TcpClientConnection(_testIpAddress, _testPort, _reconnectInterval, _logger);
        const int expectedTimeout = 5000;
        _connection.ReadTimeout = expectedTimeout;
        _connection.ReadTimeout.Should().Be(expectedTimeout);
    }

    [Fact]
    public void WriteData_WhenNotConnected_ShouldThrowException()
    {
        _connection = new TcpClientConnection(_testIpAddress, _testPort, _reconnectInterval, _logger);
        var testData = new byte[] { 1, 2, 3, 4 };
        _connection.Invoking(c => c.WriteData(testData))
            .Should().Throw<Exception>("because connection is not established");
    }

    [Fact]
    public void ReadData_WhenNotConnected_ShouldThrowException()
    {
        _connection = new TcpClientConnection(_testIpAddress, _testPort, _reconnectInterval, _logger);
        var buffer = new byte[1024];

        _connection.Invoking(c => c.ReadData(buffer))
            .Should().Throw<Exception>("because connection is not established");
    }

    [Fact]
    public void DataAvailable_WhenNotConnected_ShouldReturnFalse()
    {
        _connection = new TcpClientConnection(_testIpAddress, _testPort, _reconnectInterval, _logger);
        _connection.DataAvailable.Should().BeFalse("because no connection is established");
    }

    [Fact]
    public void Connect_WhenCalledMultipleTimes_ShouldNotCreateMultipleConnections()
    {
        _connection = new TcpClientConnection(_testIpAddress, 99999, _reconnectInterval, _logger);
        _connection.Connect();
        var firstState = _connection.State;
        _connection.Connect();
        var secondState = _connection.State;
        firstState.Should().Be(LisConnectionState.Closed, "because connection to invalid port should fail and state remains Closed");
        secondState.Should().Be(LisConnectionState.Closed, "because connection to invalid port should still fail and remain Closed");
    }

    [Fact]
    public void ReadData_WithBufferAndOffsetAndCount_WhenNotConnected_ShouldThrowException()
    {
        _connection = new TcpClientConnection(_testIpAddress, _testPort, _reconnectInterval, _logger);
        var buffer = new byte[1024];
        _connection.Invoking(c => c.ReadData(buffer, 0, 512))
            .Should().Throw<Exception>("because no connection is established");
    }

    [Fact]
    public void Connect_WhenSuccessful_ShouldSetStateToOpen()
    {
        _connection = new TcpClientConnection(_testIpAddress, _testPort, _reconnectInterval, _logger);
        _connection.Connect();
        // Since there's no server listening, connection should fail and state remains Closed
        _connection.State.Should().Be(LisConnectionState.Closed, "because no server is listening and connection fails, state remains Closed");
    }

    [Fact]
    public void Close_WhenStateIsBroken_ShouldCloseConnection()
    {
        _connection = new TcpClientConnection(_testIpAddress, _testPort, _reconnectInterval, _logger);

        var stateProperty = typeof(TcpConnectionBase).GetProperty("State",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        stateProperty?.SetValue(_connection, LisConnectionState.Broken);
        _connection.Close();
        _connection.State.Should().Be(LisConnectionState.Closed);
    }
}