namespace Pharos.Client.Common.Models.DataServices;

/// <summary>
/// Marker interface indicating that the data source supports bypassing
/// the default max-row limit when requested by the client.
/// Requires corresponding server-side validation support.
/// </summary>
public interface ISupportsRowLimitBypass;