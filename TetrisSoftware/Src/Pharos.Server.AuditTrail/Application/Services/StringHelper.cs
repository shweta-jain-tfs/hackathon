using System.Text;
using System.Text.Json;
using Pharos.Server.AuditTrail.Domain;

namespace Pharos.Server.AuditTrail.Application.Services;
internal class StringHelper
{
    /// <summary>
    /// Constructs a string to be used for hash for an audit trail record
    /// </summary>
    /// <param name="record"></param>
    /// <param name="sb"></param>
    /// <returns></returns>
    public static string BuildHashString(AuditTrailRecord record, StringBuilder sb)
    {
        sb.Append(record.EventId);
        foreach (var ed in record.EventData)
        {
            sb.Append(ed);
        }
        sb.Append(record.CreatedBy);
        sb.Append(record.Created);
        sb.Append(record.Comment);
        return sb.ToString();
    }

    /// <summary>
    /// Format the event data into a readable string representation.
    /// </summary>
    /// <param name="eventData"></param>
    /// <returns></returns>
    internal static string GetEventDataString(List<AuditEventData>? eventData)
    {
        if (eventData == null)
        {
            return string.Empty;
        }

        var output = new List<string>();

        // Iterate through each dictionary in the list of event data to format the key-value pairs.
        foreach (var ed in eventData)
        {
            foreach (var kvp in ed.Data)
            {
                // Get the index of the first '{' character to identify the start of a JSON object.
                var jsonStartIndex = kvp.Value.ToString().IndexOf('{');
                if (jsonStartIndex >= 0)
                {
                    try
                    {
                        var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(kvp.Value.ToString()[jsonStartIndex..]);
                        // Format the key/value pairs
                        output.AddRange(from innerKvp in dict let formattedKey = string.Join(" -> ", innerKvp.Key.Split(':')) select $"{formattedKey}: {innerKvp.Value}");
                    }
                    catch (JsonException)
                    {
                        var formattedKey = string.Join(" -> ", kvp.Key.Split(':'));
                        output.Add($"{formattedKey}: {kvp.Value}");
                    }
                }
                else
                {
                    var formattedKey = string.Join(" -> ", kvp.Key.Split(':'));
                    output.Add($"{formattedKey}: {kvp.Value}");
                }
            }
        }

        // Return plain text; UI preserves line breaks via whitespace-pre-line
        return string.Join(Environment.NewLine, output);
    }
}
