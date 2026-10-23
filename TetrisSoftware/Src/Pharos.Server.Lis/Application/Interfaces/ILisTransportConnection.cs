namespace Pharos.Server.Lis.Application.Interfaces;

/// <summary>
/// Defines an abstract event-driven LIS transport connection (such as TCP or RS-232).
/// </summary>
public interface ILisTransportConnection
{
    /// <summary>
    /// Gets or sets the amount of time in milliseconds that a read operation blocks waiting for data.
    /// </summary>
    int ReadTimeout { get; set; }

    /// <summary>
    /// Gets the current state of the connection.
    /// </summary>
    LisConnectionState State { get; }

    /// <summary>
    /// Gets a value that indicates whether data is available to be immediately read.
    /// </summary>
    bool DataAvailable { get; }

    /// <summary>
    /// Opens the connection.
    /// </summary>
    void Connect();

    /// <summary>
    /// Closes the connection an frees all unmanaged resources associated with it.
    /// </summary>
    void Close();

    /// <summary>
    /// Reads data from the connection and stores it to a byte array.
    /// </summary>
    /// <param name="buffer">An array of bytes to store data read from the connection.</param>
    /// <returns>The number of bytes read from the connection.</returns>
    int ReadData(byte[] buffer);

    /// <summary>
    /// Reads data from the connection and stores it to a byte array.
    /// </summary>
    /// <param name="buffer">An array of bytes to store data read from the connection.</param>
    /// <param name="offset">Location in the buffer where to store the data.</param>
    /// <param name="count">Number of bytes to read.</param>
    /// <returns>The number of bytes read from the connection.</returns>
    int ReadData(byte[] buffer, int offset, int count);

    /// <summary>
    /// Sends a data packet to LIS.
    /// </summary>
    /// <param name="data">The data packet</param>
    void WriteData(byte[] data);

    /// <summary>
    /// Occurs when the transport connection has been opened or closed.
    /// </summary>
    event EventHandler<LisConnectionStateChangedEventArgs>? StateChanged;
}