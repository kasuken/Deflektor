namespace Deflektor.Func.Models;

/// <summary>
/// Configuration settings for the application
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Microsoft Graph API settings
    /// </summary>
    public GraphApiSettings GraphApi { get; set; } = new();

    /// <summary>
    /// Service Bus settings
    /// </summary>
    public ServiceBusSettings ServiceBus { get; set; } = new();

    /// <summary>
    /// Email processing settings
    /// </summary>
    public EmailSettings Email { get; set; } = new();
}

/// <summary>
/// Microsoft Graph API configuration
/// </summary>
public class GraphApiSettings
{
    /// <summary>
    /// Azure AD tenant ID
    /// </summary>
    public string TenantId { get; set; } = string.Empty;
    
    /// <summary>
    /// Azure AD application client ID
    /// </summary>
    public string ClientId { get; set; } = string.Empty;
    
    /// <summary>
    /// Azure AD application client secret
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;
}

/// <summary>
/// Service Bus configuration
/// </summary>
public class ServiceBusSettings
{
    /// <summary>
    /// Service Bus connection string
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;
    
    /// <summary>
    /// Queue name for email processing
    /// </summary>
    public string QueueName { get; set; } = string.Empty;
}

/// <summary>
/// Email processing configuration
/// </summary>
public class EmailSettings
{
    /// <summary>
    /// Email address for support mailbox
    /// </summary>
    public string SupportEmailAddress { get; set; } = "eba@ebarocks.onmicrosoft.com";
    
    /// <summary>
    /// Email reply prefix
    /// </summary>
    public string ReplySubjectPrefix { get; set; } = "Re from Deflektor: ";
}