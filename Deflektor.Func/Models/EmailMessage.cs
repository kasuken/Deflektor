using System.Text.Json.Serialization;

namespace Deflektor.Func.Models;

public class EmailMessage
{
    [JsonPropertyName("emailId")]
    public required string EmailId { get; set; }
    
    [JsonPropertyName("subject")]
    public required string Subject { get; set; }
    
    [JsonPropertyName("body")]
    public required string Body { get; set; }
    
    [JsonPropertyName("sender")]
    public required string Sender { get; set; }
    
    [JsonPropertyName("recipient")]
    public required string Recipient { get; set; }
}