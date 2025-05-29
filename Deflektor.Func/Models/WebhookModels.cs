using System.Text.Json.Serialization;

namespace Deflektor.Func.Models;

/// <summary>
/// Webhook notification payload from Microsoft Graph
/// </summary>
public class WebhookPayload
{
    /// <summary>
    /// List of notification objects
    /// </summary>
    [JsonPropertyName("value")]
    public required List<WebhookValue> Value { get; set; }
}

/// <summary>
/// Individual notification in a webhook payload
/// </summary>
public class WebhookValue
{
    /// <summary>
    /// ID of the subscription that generated this notification
    /// </summary>
    [JsonPropertyName("subscriptionId")]
    public required string SubscriptionId { get; set; }
    
    /// <summary>
    /// Expiration date/time of the subscription
    /// </summary>
    [JsonPropertyName("subscriptionExpirationDateTime")]
    public DateTime SubscriptionExpirationDateTime { get; set; }
    
    /// <summary>
    /// Type of change that triggered the notification
    /// </summary>
    [JsonPropertyName("changeType")]
    public required string ChangeType { get; set; }
    
    /// <summary>
    /// Resource path that was changed
    /// </summary>
    [JsonPropertyName("resource")]
    public required string Resource { get; set; }
    
    /// <summary>
    /// Data about the resource that changed
    /// </summary>
    [JsonPropertyName("resourceData")]
    public required ResourceData ResourceData { get; set; }
    
    /// <summary>
    /// State provided when creating the subscription
    /// </summary>
    [JsonPropertyName("clientState")]
    public required string ClientState { get; set; }
    
    /// <summary>
    /// ID of the tenant where the change occurred
    /// </summary>
    [JsonPropertyName("tenantId")]
    public required string TenantId { get; set; }
}

/// <summary>
/// Data about the resource that changed
/// </summary>
public class ResourceData
{
    /// <summary>
    /// OData type of the resource
    /// </summary>
    [JsonPropertyName("@odata.type")]
    public required string ODataType { get; set; }

    /// <summary>
    /// OData ID of the resource
    /// </summary>
    [JsonPropertyName("@odata.id")]
    public required string ODataId { get; set; }

    /// <summary>
    /// OData etag of the resource
    /// </summary>
    [JsonPropertyName("@odata.etag")]
    public required string ODataEtag { get; set; }

    /// <summary>
    /// ID of the resource
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }
}