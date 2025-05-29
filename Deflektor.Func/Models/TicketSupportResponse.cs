using System.Text.Json.Serialization;

namespace Deflektor.Func;

/// <summary>
/// Response from the ticket processing system
/// </summary>
public class TicketSupportResponse
{
    /// <summary>
    /// The text content of the response
    /// </summary>
    [JsonPropertyName("text")]
    public required string Text { get; set; }
    
    /// <summary>
    /// The subject line for the response
    /// </summary>
    [JsonPropertyName("subject")]
    public required string Subject { get; set; }
}