using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Pharos.Server.Lis.Application.Interfaces;
using Pharos.Server.Lis.Infrastructure.Transport;

namespace Pharos.Server.Lis.Tests;

public class TcpServerConnectionTests : IAsyncDisposable
{
    private readonly ILogger<TcpServerConnection> _logger = NullLogger<TcpServerConnection>.Instance;
    private readonly int _testPort = GetAvailablePort();
    private TcpServerConnection? _serverConnection;
    private TcpClient? _testClient;
    private readonly List<IDisposable> _disposables = new();
    private readonly CancellationTokenSource _testCts = new();

    public async ValueTask DisposeAsync()
    {
        // Cancel any ongoing operations
        await _testCts.CancelAsync();

        // Cleanup test client
        if (_testClient != null)
        {
            _testClient.Close();
            _testClient.Dispose();
            _testClient = null;
        }

        // Cleanup server connection
        if (_serverConnection != null)
        {
            _serverConnection.Close();
            _serverConnection.Dispose();
            _serverConnection = null;
        }

        // Cleanup any tracked disposables
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }
        _disposables.Clear();

        _testCts.Dispose();

        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Constructor_ShouldInitializeWithCorrectValues()
    {
        // Act
        _serverConnection = new TcpServerConnection(_testPort, _logger);

        // Assert
        _serverConnection.Should().NotBeNull();
        _serverConnection.State.Should().Be(LisConnectionState.Closed);
    }

    [Fact(Timeout = 10_000)]
    public async Task Connect_WhenClientConnects_ShouldTransitionToOpen()
    {
        // Arrange
        _serverConnection = new TcpServerConnection(_testPort, _logger);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(5));
        var stateChangeTask = WaitForStateChange(_serverConnection, LisConnectionState.Open, cts.Token);

        // Act
        _serverConnection.Connect();
        _testClient = new TcpClient();
        await _testClient.ConnectAsync(IPAddress.Loopback, _testPort, cts.Token);

        // Assert
        var stateChanged = await stateChangeTask;
        stateChanged.Should().BeTrue("server should accept connection within timeout");
        _serverConnection.State.Should().Be(LisConnectionState.Open);
    }

    [Fact(Timeout = 10_000)]
    public async Task Close_WhenStateOpen_ShouldCloseConnection()
    {
        // Arrange
        _serverConnection = new TcpServerConnection(_testPort, _logger);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        _serverConnection.Connect();
        // Verify connection is open
        _testClient = await ConnectClientAsync(_serverConnection, _testPort, cts.Token);

        // Act
        _serverConnection.Close();

        // Assert
        _serverConnection.State.Should().Be(LisConnectionState.Closed);
    }

    [Fact(Timeout = 10_000)]
    public async Task WriteData_WhenConnected_ShouldSendDataToClient()
    {
        // Arrange
        _serverConnection = new TcpServerConnection(_testPort, _logger);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        _serverConnection.Connect();
        _testClient = await ConnectClientAsync(_serverConnection, _testPort, cts.Token);

        var testData = new byte[] { 1, 2, 3, 4, 5 };
        var buffer = new byte[10];

        // Act
        _serverConnection.WriteData(testData);
        var stream = _testClient.GetStream();

        var totalRead = 0;
        while (totalRead < testData.Length)
        {
            var bytesRead = await stream.ReadAsync(buffer.AsMemory(totalRead, buffer.Length - totalRead), cts.Token);
            if (bytesRead == 0) { break; }
            totalRead += bytesRead;
        }

        // Assert
        totalRead.Should().Be(testData.Length);
        buffer.Take(testData.Length).Should().Equal(testData);
    }


    [Fact(Timeout = 10_000)]
    public async Task DataAvailable_WhenDataPresent_ShouldReturnTrue()
    {
        // Arrange
        _serverConnection = new TcpServerConnection(_testPort, _logger);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        _serverConnection.Connect();
        _testClient = await ConnectClientAsync(_serverConnection, _testPort, cts.Token);

        var testData = new byte[] { 1, 2, 3 };

        // Act
        var stream = _testClient.GetStream();
        await stream.WriteAsync(testData, cts.Token);
        await stream.FlushAsync(cts.Token);
        await Task.Delay(200, cts.Token);

        // Assert
        _serverConnection.DataAvailable.Should().BeTrue();
    }

    [Fact(Timeout = 10_000)]
    public async Task DataAvailable_WhenNoData_ShouldReturnFalse()
    {
        // Arrange
        _serverConnection = new TcpServerConnection(_testPort, _logger);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        _serverConnection.Connect();
        _testClient = await ConnectClientAsync(_serverConnection, _testPort, cts.Token);

        // Act & Assert
        _serverConnection.DataAvailable.Should().BeFalse();
    }

    [Fact(Timeout = 10_000)]
    public async Task OnStateChanged_WhenConnectionBroken_ShouldTransitionToConnecting()
    {
        // Arrange
        _serverConnection = new TcpServerConnection(_testPort, _logger);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        _serverConnection.Connect();
        _testClient = await ConnectClientAsync(_serverConnection, _testPort, cts.Token);

        _serverConnection.State.Should().Be(LisConnectionState.Open);

        var stateChangeTask = WaitForStateChange(_serverConnection, LisConnectionState.Connecting, cts.Token);

        // Act - Force connection to break by closing client
        _testClient.Close();

        var buffer = new byte[10];
        try
        {
            _serverConnection.ReadData(buffer);
        }
        catch
        {
            // Expected exception when connection is broken
        }

        // Assert
        var stateChanged = await stateChangeTask;
        stateChanged.Should().BeTrue("server should transition to Connecting after broken connection");
        _serverConnection.State.Should().Be(LisConnectionState.Connecting);
    }

    [Fact]
    public void Dispose_ShouldCleanupResourcesAndAllowMultipleCalls()
    {
        // Arrange
        _serverConnection = new TcpServerConnection(_testPort, _logger);

        _serverConnection.Connect();

        // Act & Assert
        _serverConnection.Invoking(s => s.Dispose()).Should().NotThrow();
        _serverConnection.Invoking(s => s.Dispose()).Should().NotThrow("multiple dispose calls should be safe");
    }

    [Fact(Timeout = 10_000)]
    public async Task Integration_MultipleClientsSequentially_ShouldHandleConnections()
    {
        // Arrange
        _serverConnection = new TcpServerConnection(_testPort, _logger);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        _serverConnection.Connect();

        // First client connects
        var client1 = await ConnectClientAsync(_serverConnection, _testPort, cts.Token);
        _disposables.Add(client1);

        // Send data from first client
        var testData1 = new byte[] { 1, 2, 3 };
        var stream1 = client1.GetStream();
        await stream1.WriteAsync(testData1, cts.Token);
        await stream1.FlushAsync(cts.Token);
        await Task.Delay(100, cts.Token);

        _serverConnection.DataAvailable.Should().BeTrue();

        // Close first client
        client1.Close();
        client1.Dispose();

        // Second client connects
        _testClient = new TcpClient();
        var stateChangeTask = WaitForStateChange(_serverConnection, LisConnectionState.Open, cts.Token);
        await _testClient.ConnectAsync(IPAddress.Loopback, _testPort, cts.Token);

        // Assert
        var stateChanged = await stateChangeTask;
        stateChanged.Should().BeTrue("server should accept new connection after previous client disconnected");
        _serverConnection.State.Should().Be(LisConnectionState.Open);
    }

    [Fact(Timeout = 10_000)]
    public async Task Integration_ServerReconnection_AfterClientDisconnects_ShouldAcceptNewConnection()
    {
        // Arrange
        _serverConnection = new TcpServerConnection(_testPort, _logger);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        _serverConnection.Connect();

        // First connection
        var client1 = await ConnectClientAsync(_serverConnection, _testPort, cts.Token);
        _disposables.Add(client1);

        // Simulate data exchange
        var testData = new byte[] { 1, 2, 3 };
        _serverConnection.WriteData(testData);
        await Task.Delay(100, cts.Token);

        // Force disconnect
        client1.Close();
        var buffer = new byte[10];
        try
        {
            _serverConnection.ReadData(buffer);
        }
        catch
        {
            // Expected exception
        }

        _serverConnection.State.Should().Be(LisConnectionState.Connecting, "server should detect the disconnect and accept again");

        // Act - New client connects
        _testClient = new TcpClient();
        var stateChangeTask = WaitForStateChange(_serverConnection, LisConnectionState.Open, cts.Token);
        await _testClient.ConnectAsync(IPAddress.Loopback, _testPort, cts.Token);

        // Assert
        var stateChanged = await stateChangeTask;
        stateChanged.Should().BeTrue("server should accept reconnection");
        _serverConnection.State.Should().Be(LisConnectionState.Open);

        // Verify new connection works
        _serverConnection.WriteData(testData);
        var stream = _testClient.GetStream();
        var newBuffer = new byte[10];

        var totalRead = 0;
        while (totalRead < testData.Length)
        {
            var bytesRead = await stream.ReadAsync(
                newBuffer.AsMemory(totalRead, newBuffer.Length - totalRead),
                cts.Token);
            if (bytesRead == 0)
            {
                break;
            }

            totalRead += bytesRead;
        }

        totalRead.Should().Be(testData.Length);
    }

    [Fact(Timeout = 10_000)]
    public async Task Integration_StateTransitions_ShouldFollowCorrectSequence()
    {
        // Arrange
        _serverConnection = new TcpServerConnection(_testPort, _logger);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(5));
        var stateTransitions = new List<LisConnectionState>();

        _serverConnection.StateChanged += (_, args) =>
        {
            stateTransitions.Add(args.NewState);
        };

        // Act
        // Initial state
        stateTransitions.Add(_serverConnection.State);

        // Connect
        _serverConnection.Connect();

        // Client connects
        _testClient = await ConnectClientAsync(_serverConnection, _testPort, cts.Token);

        // Close
        _serverConnection.Close();

        // Assert
        stateTransitions.Should().ContainInOrder(
            LisConnectionState.Closed,
            LisConnectionState.Connecting,
            LisConnectionState.Open,
            LisConnectionState.Closed
        );
    }

    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static async Task<bool> WaitForStateChange(
        TcpServerConnection connection,
        LisConnectionState expectedState,
        CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<bool>();

        // Register cancellation
        await using var registration = cancellationToken.Register(() => tcs.TrySetResult(false));

        EventHandler<LisConnectionStateChangedEventArgs> handler = (_, args) =>
        {
            if (args.NewState == expectedState)
            {
                tcs.TrySetResult(true);
            }
        };

        connection.StateChanged += handler;

        // Check if already in expected state
        if (connection.State == expectedState)
        {
            connection.StateChanged -= handler;
            return true;
        }

        return await tcs.Task;
    }

    /// <summary>
    /// Connects a client and waits until the server state is Open. Fails the test if that doesn't happen before cancellation.
    /// </summary>
    private static async Task<TcpClient> ConnectClientAsync(TcpServerConnection server, int port, CancellationToken cancellationToken)
    {
        var openTask = WaitForStateChange(server, LisConnectionState.Open, cancellationToken);

        var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port, cancellationToken);

        (await openTask).Should().BeTrue("server should accept connection within timeout");
        return client;
    }
}
